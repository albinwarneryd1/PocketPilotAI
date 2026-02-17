using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Ai;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Core.Domain.Enums;
using PocketPilotAI.Infrastructure.Ai;
using PocketPilotAI.Infrastructure.Persistence;

namespace PocketPilotAI.Infrastructure.Services;

public class AiInsightsService(AppDbContext dbContext, OpenAiClient openAiClient) : IAiInsightsService
{
  public async Task<Result<IReadOnlyList<InsightCardDto>>> GetLeakInsightsAsync(
    Guid userId,
    LeakFinderRequest request,
    CancellationToken cancellationToken = default)
  {
    List<SpendingBucket> current = await GetCategorySpendAsync(userId, request.FromUtc, request.ToUtc, cancellationToken);

    TimeSpan span = request.ToUtc - request.FromUtc;
    DateTime previousTo = request.FromUtc.AddTicks(-1);
    DateTime previousFrom = previousTo - span;
    List<SpendingBucket> previous = await GetCategorySpendAsync(userId, previousFrom, previousTo, cancellationToken);

    string input = JsonSerializer.Serialize(new
    {
      period = new { from = request.FromUtc, to = request.ToUtc },
      current,
      previous,
      maxSuggestions = request.MaxSuggestions
    });

    string template = LoadPromptTemplate("LeakFinder.prompt.txt",
      "Return JSON: { \"insights\": [ { title, description, suggestedAction, estimatedMonthlySavings, confidence, metrics } ] }. Use only provided transaction data and be concrete.");

    string? aiJson = await openAiClient.GenerateJsonAsync(template, input, cancellationToken);
    IReadOnlyList<InsightCardDto> parsed = AiResponseParser.ParseInsightCards(aiJson);

    if (parsed.Count > 0)
    {
      return Result<IReadOnlyList<InsightCardDto>>.Success(parsed.Take(request.MaxSuggestions).ToList());
    }

    IReadOnlyList<InsightCardDto> fallback = BuildFallbackLeakInsights(current, previous, request.MaxSuggestions);
    return Result<IReadOnlyList<InsightCardDto>>.Success(fallback);
  }

  public async Task<Result<IReadOnlyList<InsightCardDto>>> GetMonthlySummaryAsync(
    Guid userId,
    MonthlySummaryRequest request,
    CancellationToken cancellationToken = default)
  {
    DateTime from = new(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    DateTime to = from.AddMonths(1).AddTicks(-1);

    decimal income = await dbContext.Transactions
      .AsNoTracking()
      .Where(x => x.UserId == userId && x.Type == TransactionType.Income && x.DateUtc >= from && x.DateUtc <= to)
      .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

    decimal expenses = await dbContext.Transactions
      .AsNoTracking()
      .Where(x => x.UserId == userId && x.Type == TransactionType.Expense && x.DateUtc >= from && x.DateUtc <= to)
      .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

    List<SpendingBucket> topCategories = await GetCategorySpendAsync(userId, from, to, cancellationToken);

    string input = JsonSerializer.Serialize(new
    {
      year = request.Year,
      month = request.Month,
      income,
      expenses,
      balance = income - expenses,
      topCategories
    });

    string template = LoadPromptTemplate("MonthlySummary.prompt.txt",
      "Return JSON: { \"insights\": [ { title, description, suggestedAction, estimatedMonthlySavings, confidence, metrics } ] }.");

    string? aiJson = await openAiClient.GenerateJsonAsync(template, input, cancellationToken);
    IReadOnlyList<InsightCardDto> parsed = AiResponseParser.ParseInsightCards(aiJson);
    if (parsed.Count > 0)
    {
      return Result<IReadOnlyList<InsightCardDto>>.Success(parsed);
    }

    List<InsightCardDto> fallback =
    [
      new()
      {
        Title = "Monthly cashflow summary",
        Description = $"Income: {income:0.##} SEK, expenses: {expenses:0.##} SEK, net: {(income - expenses):0.##} SEK.",
        SuggestedAction = income >= expenses
          ? "Auto-transfer at least 20% of net positive cashflow to savings right after salary day."
          : "Pause one discretionary category and rebalance fixed costs until monthly net turns positive.",
        EstimatedMonthlySavings = Math.Max(0m, (income - expenses) * 0.2m),
        Confidence = 0.75m,
        Metrics = new Dictionary<string, string>
        {
          ["income"] = income.ToString("0.##"),
          ["expenses"] = expenses.ToString("0.##"),
          ["net"] = (income - expenses).ToString("0.##")
        }
      }
    ];

    return Result<IReadOnlyList<InsightCardDto>>.Success(fallback);
  }

  public Task<Result<IReadOnlyList<WhatIfScenarioTemplateDto>>> GetWhatIfTemplatesAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
  {
    IReadOnlyList<WhatIfScenarioTemplateDto> templates =
    [
      new()
      {
        Name = "Reduce category by percent",
        Description = "Lower spending in one category by a chosen percent.",
        ActionType = WhatIfActionType.ReduceCategoryPercent,
        SuggestedCategory = "Eating Out",
        SuggestedValue = 15
      },
      new()
      {
        Name = "Reduce category by fixed amount",
        Description = "Apply a monthly fixed reduction cap.",
        ActionType = WhatIfActionType.ReduceCategoryFixed,
        SuggestedCategory = "Shopping",
        SuggestedValue = 600
      },
      new()
      {
        Name = "Add recurring income",
        Description = "Add a stable monthly side income.",
        ActionType = WhatIfActionType.AddRecurringIncome,
        SuggestedCategory = "Income",
        SuggestedValue = 2000
      },
      new()
      {
        Name = "Add recurring expense",
        Description = "Model a new monthly cost before committing.",
        ActionType = WhatIfActionType.AddRecurringExpense,
        SuggestedCategory = "Utilities",
        SuggestedValue = 500
      },
      new()
      {
        Name = "Add one-off transaction",
        Description = "Simulate a one-time income or expense impact.",
        ActionType = WhatIfActionType.AddOneOffTransaction,
        SuggestedCategory = "Shopping",
        SuggestedValue = 1500
      },
      new()
      {
        Name = "Remove detected subscriptions",
        Description = "Simulate cancelling recurring subscription charges.",
        ActionType = WhatIfActionType.RemoveDetectedSubscriptions,
        SuggestedCategory = "Subscriptions",
        SuggestedValue = 0
      }
    ];

    return Task.FromResult(Result<IReadOnlyList<WhatIfScenarioTemplateDto>>.Success(templates));
  }

  public async Task<Result<WhatIfSimulationResultDto>> RunWhatIfSimulationAsync(
    Guid userId,
    WhatIfSimulationRequest request,
    CancellationToken cancellationToken = default)
  {
    if (request.Actions.Count == 0)
    {
      return Result<WhatIfSimulationResultDto>.Failure("At least one simulation action is required.");
    }

    DateTime from = new(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    DateTime to = from.AddMonths(1).AddTicks(-1);

    List<Transaction> monthTransactions = await dbContext.Transactions
      .AsNoTracking()
      .Include(x => x.Category)
      .Where(x => x.UserId == userId && x.DateUtc >= from && x.DateUtc <= to)
      .ToListAsync(cancellationToken);

    WhatIfKpiDto baseline = BuildKpi(monthTransactions, request.Year, request.Month);
    WhatIfKpiDto simulated = CloneKpi(baseline);
    List<WhatIfAppliedActionDto> appliedActions = [];

    Dictionary<string, decimal> categorySpend = monthTransactions
      .Where(x => x.Type == TransactionType.Expense)
      .GroupBy(x => x.Category?.Name ?? "Uncategorized", StringComparer.OrdinalIgnoreCase)
      .ToDictionary(x => x.Key, x => x.Sum(y => y.Amount), StringComparer.OrdinalIgnoreCase);

    foreach (WhatIfActionDto action in request.Actions)
    {
      decimal impact = ApplyAction(action, simulated, categorySpend, monthTransactions);
      appliedActions.Add(new WhatIfAppliedActionDto
      {
        Type = action.Type,
        Label = string.IsNullOrWhiteSpace(action.Label) ? action.Type.ToString() : action.Label,
        EstimatedMonthlyImpact = Math.Round(impact, 2)
      });
    }

    SimulatedPostAdjustments(simulated);

    WhatIfKpiDeltaDto delta = new()
    {
      IncomeDelta = Math.Round(simulated.Income - baseline.Income, 2),
      ExpenseDelta = Math.Round(simulated.Expenses - baseline.Expenses, 2),
      NetDelta = Math.Round(simulated.Net - baseline.Net, 2),
      SavingsRateDeltaPercent = Math.Round(simulated.SavingsRatePercent - baseline.SavingsRatePercent, 2)
    };

    List<string> recommendations = BuildRecommendations(appliedActions, simulated, baseline);

    WhatIfSimulationResultDto result = new()
    {
      ScenarioName = string.IsNullOrWhiteSpace(request.ScenarioName) ? "What-if simulation" : request.ScenarioName,
      Baseline = baseline,
      Simulated = simulated,
      Delta = delta,
      Explanation = $"Scenario shifts net cash flow by {delta.NetDelta:0.##} SEK for {request.Month:D2}/{request.Year} based on {request.Actions.Count} applied actions.",
      Recommendations = recommendations.Take(3).ToList(),
      AppliedActions = appliedActions
    };

    while (result.Recommendations.Count < 3)
    {
      var list = result.Recommendations.ToList();
      list.Add("Review the scenario weekly and keep only adjustments that stay realistic for your routine.");
      result.Recommendations = list;
    }

    return Result<WhatIfSimulationResultDto>.Success(result);
  }

  private static decimal ApplyAction(
    WhatIfActionDto action,
    WhatIfKpiDto simulated,
    IReadOnlyDictionary<string, decimal> categorySpend,
    IReadOnlyList<Transaction> transactions)
  {
    decimal impact = 0m;

    switch (action.Type)
    {
      case WhatIfActionType.ReduceCategoryPercent:
      {
        string category = string.IsNullOrWhiteSpace(action.CategoryName) ? "Uncategorized" : action.CategoryName;
        decimal spend = categorySpend.GetValueOrDefault(category, 0m);
        decimal pct = Math.Clamp(action.Value, 0m, 100m) / 100m;
        decimal reduction = Math.Round(spend * pct, 2);
        simulated.Expenses -= reduction;
        impact += reduction;
        break;
      }
      case WhatIfActionType.ReduceCategoryFixed:
      {
        string category = string.IsNullOrWhiteSpace(action.CategoryName) ? "Uncategorized" : action.CategoryName;
        decimal spend = categorySpend.GetValueOrDefault(category, 0m);
        decimal reduction = Math.Min(spend, Math.Max(0m, action.Value));
        simulated.Expenses -= reduction;
        impact += reduction;
        break;
      }
      case WhatIfActionType.AddRecurringIncome:
      {
        decimal added = Math.Max(0m, action.Value);
        simulated.Income += added;
        impact += added;
        break;
      }
      case WhatIfActionType.AddRecurringExpense:
      {
        decimal added = Math.Max(0m, action.Value);
        simulated.Expenses += added;
        impact -= added;
        break;
      }
      case WhatIfActionType.AddOneOffTransaction:
      {
        decimal amount = Math.Abs(action.Value);
        if (action.TransactionType == TransactionType.Income)
        {
          simulated.Income += amount;
          impact += amount;
        }
        else
        {
          simulated.Expenses += amount;
          impact -= amount;
        }

        break;
      }
      case WhatIfActionType.RemoveDetectedSubscriptions:
      {
        decimal recurringExpenseTotal = transactions
          .Where(x => x.Type == TransactionType.Expense && x.IsRecurring)
          .Sum(x => x.Amount);

        decimal removable = action.Value > 0
          ? Math.Min(recurringExpenseTotal, action.Value)
          : recurringExpenseTotal;

        simulated.Expenses -= removable;
        impact += removable;
        break;
      }
    }

    return impact;
  }

  private static void SimulatedPostAdjustments(WhatIfKpiDto simulated)
  {
    if (simulated.Expenses < 0)
    {
      simulated.Expenses = 0;
    }

    simulated.Net = simulated.Income - simulated.Expenses;
    simulated.SavingsRatePercent = simulated.Income <= 0 ? 0 : Math.Round((simulated.Net / simulated.Income) * 100m, 2);

    int days = Math.Max(1, DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month));
    simulated.AverageDailySpend = Math.Round(simulated.Expenses / days, 2);
  }

  private static List<string> BuildRecommendations(
    IReadOnlyList<WhatIfAppliedActionDto> applied,
    WhatIfKpiDto simulated,
    WhatIfKpiDto baseline)
  {
    List<string> recommendations = [];

    WhatIfAppliedActionDto? topPositive = applied
      .Where(x => x.EstimatedMonthlyImpact > 0)
      .OrderByDescending(x => x.EstimatedMonthlyImpact)
      .FirstOrDefault();

    if (topPositive is not null)
    {
      recommendations.Add($"Prioritize '{topPositive.Label}' first; it contributes about {topPositive.EstimatedMonthlyImpact:0.##} SEK/month improvement.");
    }

    if (simulated.Net > baseline.Net)
    {
      recommendations.Add($"If you keep this scenario, route at least {(simulated.Net - baseline.Net) * 0.5m:0.##} SEK/month to savings automatically.");
    }
    else
    {
      recommendations.Add("Current scenario weakens monthly net cash flow; reduce one added expense before adopting it.");
    }

    recommendations.Add($"Track {simulated.BiggestExpenseCategory} weekly to keep average daily spend near {simulated.AverageDailySpend:0.##} SEK.");

    return recommendations;
  }

  private static WhatIfKpiDto BuildKpi(IReadOnlyList<Transaction> transactions, int year, int month)
  {
    decimal income = transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
    decimal expenses = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
    decimal net = income - expenses;

    int days = DateTime.DaysInMonth(year, month);

    string biggestCategory = transactions
      .Where(x => x.Type == TransactionType.Expense)
      .GroupBy(x => x.Category?.Name ?? "Uncategorized")
      .Select(x => new { Name = x.Key, Total = x.Sum(y => y.Amount) })
      .OrderByDescending(x => x.Total)
      .Select(x => x.Name)
      .FirstOrDefault() ?? "Uncategorized";

    return new WhatIfKpiDto
    {
      Income = Math.Round(income, 2),
      Expenses = Math.Round(expenses, 2),
      Net = Math.Round(net, 2),
      SavingsRatePercent = income <= 0 ? 0 : Math.Round((net / income) * 100m, 2),
      AverageDailySpend = Math.Round(expenses / Math.Max(1, days), 2),
      BiggestExpenseCategory = biggestCategory
    };
  }

  private static WhatIfKpiDto CloneKpi(WhatIfKpiDto source)
    => new()
    {
      Income = source.Income,
      Expenses = source.Expenses,
      Net = source.Net,
      SavingsRatePercent = source.SavingsRatePercent,
      AverageDailySpend = source.AverageDailySpend,
      BiggestExpenseCategory = source.BiggestExpenseCategory
    };

  private async Task<List<SpendingBucket>> GetCategorySpendAsync(
    Guid userId,
    DateTime fromUtc,
    DateTime toUtc,
    CancellationToken cancellationToken)
  {
    return await dbContext.Transactions
      .AsNoTracking()
      .Where(x => x.UserId == userId && x.Type == TransactionType.Expense && x.DateUtc >= fromUtc && x.DateUtc <= toUtc)
      .GroupBy(x => x.Category != null ? x.Category.Name : "Uncategorized")
      .Select(g => new SpendingBucket(g.Key, g.Sum(x => x.Amount)))
      .OrderByDescending(x => x.Amount)
      .ToListAsync(cancellationToken);
  }

  private static IReadOnlyList<InsightCardDto> BuildFallbackLeakInsights(
    IReadOnlyList<SpendingBucket> current,
    IReadOnlyList<SpendingBucket> previous,
    int maxSuggestions)
  {
    Dictionary<string, decimal> previousMap = previous.ToDictionary(x => x.Name, x => x.Amount, StringComparer.OrdinalIgnoreCase);

    IEnumerable<InsightCardDto> insights = current
      .Select(x =>
      {
        decimal previousAmount = previousMap.GetValueOrDefault(x.Name);
        decimal changePercent = previousAmount <= 0 ? 100m : Math.Round(((x.Amount - previousAmount) / previousAmount) * 100m, 2);
        decimal suggestedCut = Math.Round(x.Amount * 0.2m, 2);

        return new InsightCardDto
        {
          Title = $"Leak detected in {x.Name}",
          Description = previousAmount <= 0
            ? $"New expense pattern in {x.Name}: {x.Amount:0.##} SEK this period."
            : $"{x.Name} is up {changePercent:0.##}% ({previousAmount:0.##} -> {x.Amount:0.##} SEK).",
          SuggestedAction = $"Cap {x.Name} spending by 20% next month using a weekly limit.",
          EstimatedMonthlySavings = suggestedCut,
          Confidence = 0.72m,
          Metrics = new Dictionary<string, string>
          {
            ["category"] = x.Name,
            ["current"] = x.Amount.ToString("0.##"),
            ["previous"] = previousAmount.ToString("0.##"),
            ["changePercent"] = changePercent.ToString("0.##")
          }
        };
      })
      .OrderByDescending(x => x.EstimatedMonthlySavings)
      .Take(maxSuggestions);

    return insights.ToList();
  }

  private static string LoadPromptTemplate(string fileName, string fallback)
  {
    string path = Path.Combine(AppContext.BaseDirectory, "Ai", "PromptTemplates", fileName);
    return File.Exists(path) ? File.ReadAllText(path) : fallback;
  }

  private sealed record SpendingBucket(string Name, decimal Amount);
}
