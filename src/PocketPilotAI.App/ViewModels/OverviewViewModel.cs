using System.Collections.ObjectModel;
using System.Windows.Input;
using PocketPilotAI.App.Services;
using PocketPilotAI.Core.Application.Dtos.Ai;
using PocketPilotAI.Core.Application.Dtos.Budgets;
using PocketPilotAI.Core.Application.Dtos.Transactions;
using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.App.ViewModels;

public class OverviewViewModel(ApiClient apiClient, OfflineCacheService cacheService, UserSessionService sessionService) : BaseViewModel
{
  private string greeting = "Hej";
  private string periodLabel = string.Empty;
  private int healthScore;
  private string healthSummary = "Score pending";
  private string monthlyNarrative = "Preparing AI narrative...";
  private string recommendationText = "Open simulator and test one spending cap scenario.";

  public ObservableCollection<InsightTile> Insights { get; } = [];

  public ObservableCollection<TrendBadgeItem> PatternBadges { get; } = [];

  public ObservableCollection<MonthlyTrendItem> SpendingTrend { get; } = [];

  public string Greeting
  {
    get => greeting;
    set => SetProperty(ref greeting, value);
  }

  public string PeriodLabel
  {
    get => periodLabel;
    set => SetProperty(ref periodLabel, value);
  }

  public int HealthScore
  {
    get => healthScore;
    set => SetProperty(ref healthScore, value);
  }

  public string HealthSummary
  {
    get => healthSummary;
    set => SetProperty(ref healthSummary, value);
  }

  public string MonthlyNarrative
  {
    get => monthlyNarrative;
    set => SetProperty(ref monthlyNarrative, value);
  }

  public string RecommendationText
  {
    get => recommendationText;
    set => SetProperty(ref recommendationText, value);
  }

  public ICommand RefreshCommand => new Command(async () => await LoadAsync());

  public ICommand OpenSimulationCommand => new Command(async () => await Shell.Current.GoToAsync("//insights"));

  public async Task LoadAsync()
  {
    if (!sessionService.IsAuthenticated)
    {
      Greeting = "Sign in to load insights";
      return;
    }

    IsBusy = true;
    try
    {
      DateTime now = DateTime.UtcNow;
      PeriodLabel = now.ToString("MMMM yyyy");
      Greeting = string.IsNullOrWhiteSpace(sessionService.DisplayName)
        ? "Hej"
        : $"Hej {sessionService.DisplayName}";

      IReadOnlyList<TransactionDto> transactions = await apiClient.GetAsync<List<TransactionDto>>("/api/transactions") ?? [];
      IReadOnlyList<BudgetDto> budgets =
        await apiClient.GetAsync<List<BudgetDto>>($"/api/budgets?year={now.Year}&month={now.Month}") ?? [];

      IReadOnlyList<InsightCardDto> leakCards = await apiClient.PostAsync<List<InsightCardDto>>("/api/insights/leaks", new LeakFinderRequest
      {
        FromUtc = now.AddMonths(-1),
        ToUtc = now,
        MaxSuggestions = 6
      }) ?? [];

      IReadOnlyList<InsightCardDto> monthlySummary =
        await apiClient.PostAsync<List<InsightCardDto>>("/api/insights/monthly-summary", new MonthlySummaryRequest
        {
          Year = now.Year,
          Month = now.Month
        }) ?? [];

      cacheService.Set("latest-transactions", transactions);
      cacheService.Set("latest-budgets", budgets);

      decimal income = transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
      decimal expenses = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
      decimal remainingBudget = budgets.Sum(x => x.RemainingAmount);

      decimal recurringExpenses = transactions
        .Where(x => x.Type == TransactionType.Expense && x.IsRecurring)
        .Sum(x => x.Amount);

      decimal recurringShare = expenses <= 0 ? 0 : recurringExpenses / expenses;

      HealthScore = BuildHealthScore(income, expenses, remainingBudget, recurringShare);
      HealthSummary = BuildHealthSummary(HealthScore);

      MonthlyNarrative = monthlySummary.FirstOrDefault()?.Description
        ?? "No monthly summary available yet. Add more transactions to improve signal quality.";

      RecommendationText = monthlySummary.FirstOrDefault()?.SuggestedAction
        ?? "Start with one scenario: cap your fastest-growing category for 30 days.";

      Insights.Clear();
      foreach (InsightCardDto card in leakCards.Take(6))
      {
        Insights.Add(new InsightTile
        {
          Title = card.Title,
          Description = card.Description,
          ActionText = card.SuggestedAction,
          SavingsText = $"{card.EstimatedMonthlySavings:0} SEK/month potential"
        });
      }

      PatternBadges.Clear();
      foreach (TrendBadgeItem badge in BuildPatternBadges(transactions, recurringShare))
      {
        PatternBadges.Add(badge);
      }

      SpendingTrend.Clear();
      foreach (MonthlyTrendItem trend in BuildSpendingTrend(transactions, now))
      {
        SpendingTrend.Add(trend);
      }
    }
    finally
    {
      IsBusy = false;
    }
  }

  private static int BuildHealthScore(decimal income, decimal expenses, decimal remainingBudget, decimal recurringShare)
  {
    decimal savingsRate = income <= 0 ? 0 : (income - expenses) / income;

    decimal score = 55m;
    score += savingsRate * 30m;
    score += remainingBudget > 0 ? 12m : -12m;
    score += recurringShare < 0.35m ? 8m : -8m;

    return (int)Math.Clamp(Math.Round(score), 0, 100);
  }

  private static string BuildHealthSummary(int score)
    => score switch
    {
      >= 80 => "Strong trajectory. Keep automating your best habits.",
      >= 60 => "Stable trajectory. One focused improvement can boost savings.",
      >= 40 => "Risk zone. Tighten one discretionary category this week.",
      _ => "Critical trend. Start with recurring cost cleanup now."
    };

  private static IReadOnlyList<TrendBadgeItem> BuildPatternBadges(IReadOnlyList<TransactionDto> transactions, decimal recurringShare)
  {
    decimal totalSpend = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
    decimal weekendSpend = transactions
      .Where(x => x.Type == TransactionType.Expense)
      .Where(x => x.DateUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
      .Sum(x => x.Amount);

    decimal weekendShare = totalSpend <= 0 ? 0 : weekendSpend / totalSpend;
    int uncategorizedCount = transactions.Count(x => string.IsNullOrWhiteSpace(x.CategoryName));

    return
    [
      new TrendBadgeItem($"Weekend {(weekendShare * 100m):0}%", weekendShare > 0.38m ? "down" : "up"),
      new TrendBadgeItem($"Recurring {(recurringShare * 100m):0}%", recurringShare > 0.42m ? "down" : "up"),
      new TrendBadgeItem(uncategorizedCount > 0 ? $"{uncategorizedCount} uncategorized" : "Categories clean", uncategorizedCount > 0 ? "down" : "up")
    ];
  }

  private static IReadOnlyList<MonthlyTrendItem> BuildSpendingTrend(IReadOnlyList<TransactionDto> transactions, DateTime now)
  {
    List<MonthlyTrendItem> months = [];

    for (int i = 5; i >= 0; i--)
    {
      DateTime month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
      decimal spend = transactions
        .Where(x => x.Type == TransactionType.Expense)
        .Where(x => x.DateUtc.Year == month.Year && x.DateUtc.Month == month.Month)
        .Sum(x => x.Amount);

      months.Add(new MonthlyTrendItem
      {
        Label = month.ToString("MMM"),
        Amount = spend
      });
    }

    decimal max = months.Any() ? months.Max(x => x.Amount) : 0;
    foreach (MonthlyTrendItem month in months)
    {
      month.Ratio = max <= 0 ? 0 : (double)(month.Amount / max);
    }

    return months;
  }
}

public class InsightTile
{
  public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public string ActionText { get; set; } = string.Empty;

  public string SavingsText { get; set; } = string.Empty;
}

public class TrendBadgeItem
{
  public TrendBadgeItem(string text, string variant)
  {
    Text = text;
    Variant = variant;
  }

  public string Text { get; }

  public string Variant { get; }
}

public class MonthlyTrendItem
{
  public string Label { get; set; } = string.Empty;

  public decimal Amount { get; set; }

  public double Ratio { get; set; }
}
