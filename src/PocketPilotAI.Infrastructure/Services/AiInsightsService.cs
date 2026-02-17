using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Ai;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Common;
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

  public Task<Result<WhatIfSimulationResultDto>> RunWhatIfSimulationAsync(
    Guid userId,
    WhatIfSimulationRequest request,
    CancellationToken cancellationToken = default)
  {
    decimal total = Math.Round(request.MonthlyAdjustment * request.NumberOfMonths, 2);

    WhatIfSimulationResultDto result = new()
    {
      ScenarioName = request.ScenarioName,
      EstimatedMonthlyImpact = request.MonthlyAdjustment,
      EstimatedTotalSavings = total,
      Notes =
      [
        $"Applied monthly adjustment: {request.MonthlyAdjustment:0.##} SEK.",
        $"Simulation horizon: {request.NumberOfMonths} months.",
        $"Estimated total impact: {total:0.##} SEK."
      ]
    };

    return Task.FromResult(Result<WhatIfSimulationResultDto>.Success(result));
  }

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
