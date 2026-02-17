using System.Collections.ObjectModel;
using System.Windows.Input;
using PocketPilotAI.App.Services;
using PocketPilotAI.Core.Application.Dtos.Transactions;

namespace PocketPilotAI.App.ViewModels;

public class TransactionsViewModel(ApiClient apiClient, UserSessionService sessionService) : BaseViewModel
{
  private int pulseScore;
  private string pulseSummary = "No transaction data yet.";
  private string patternNarrative = "Waiting for data...";

  public ObservableCollection<TransactionListItem> Items { get; } = [];

  public ObservableCollection<TrendBadgeItem> PatternBadges { get; } = [];

  public int PulseScore
  {
    get => pulseScore;
    set => SetProperty(ref pulseScore, value);
  }

  public string PulseSummary
  {
    get => pulseSummary;
    set => SetProperty(ref pulseSummary, value);
  }

  public string PatternNarrative
  {
    get => patternNarrative;
    set => SetProperty(ref patternNarrative, value);
  }

  public ICommand RefreshCommand => new Command(async () => await LoadAsync());

  public ICommand OpenAddCommand => new Command(async () => await Shell.Current.GoToAsync("//add-transaction"));

  public async Task LoadAsync()
  {
    if (!sessionService.IsAuthenticated)
    {
      PatternNarrative = "Sign in to view transaction patterns.";
      return;
    }

    IsBusy = true;
    try
    {
      IReadOnlyList<TransactionDto> transactions = await apiClient.GetAsync<List<TransactionDto>>("/api/transactions") ?? [];
      List<TransactionDto> ordered = transactions.OrderByDescending(x => x.DateUtc).ToList();

      int recurringCount = ordered.Count(x => x.IsRecurring);
      int uncategorizedCount = ordered.Count(x => string.IsNullOrWhiteSpace(x.CategoryName));
      decimal avgAmount = ordered.Any() ? ordered.Average(x => x.Amount) : 0;

      PulseScore = BuildPulseScore(ordered.Count, uncategorizedCount, recurringCount);
      PulseSummary = $"Average size {avgAmount:0} SEK. Recurring entries: {recurringCount}.";

      string topCategory = ordered
        .Where(x => !string.IsNullOrWhiteSpace(x.CategoryName))
        .GroupBy(x => x.CategoryName)
        .OrderByDescending(g => g.Sum(x => x.Amount))
        .Select(g => g.Key)
        .FirstOrDefault() ?? "Uncategorized";

      PatternNarrative =
        $"Top spending concentration is {topCategory}. Categorize uncategorized entries first, then cap one repeated merchant pattern this week.";

      PatternBadges.Clear();
      PatternBadges.Add(new TrendBadgeItem($"{ordered.Count} entries", "neutral"));
      PatternBadges.Add(new TrendBadgeItem($"{recurringCount} recurring", recurringCount > 0 ? "up" : "neutral"));
      PatternBadges.Add(new TrendBadgeItem($"{uncategorizedCount} uncategorized", uncategorizedCount > 0 ? "down" : "up"));

      Items.Clear();
      foreach (TransactionDto tx in ordered)
      {
        Items.Add(new TransactionListItem
        {
          Merchant = string.IsNullOrWhiteSpace(tx.MerchantName) ? "Unknown merchant" : tx.MerchantName,
          Category = string.IsNullOrWhiteSpace(tx.CategoryName) ? "Uncategorized" : tx.CategoryName,
          AmountText = $"{tx.Amount:0.##} {tx.Currency}",
          DateText = tx.DateUtc.ToString("yyyy-MM-dd"),
          TypeText = tx.Type.ToString(),
          Notes = string.IsNullOrWhiteSpace(tx.Notes) ? "-" : tx.Notes,
          IsRecurring = tx.IsRecurring,
          AccountId = tx.AccountId.ToString()
        });
      }
    }
    finally
    {
      IsBusy = false;
    }
  }

  private static int BuildPulseScore(int total, int uncategorized, int recurring)
  {
    if (total == 0)
    {
      return 0;
    }

    decimal score = 75m;
    score -= (uncategorized / (decimal)total) * 40m;
    score += recurring > 0 ? 8m : -5m;

    return (int)Math.Clamp(Math.Round(score), 0, 100);
  }
}

public class TransactionListItem
{
  public string Merchant { get; set; } = string.Empty;

  public string Category { get; set; } = string.Empty;

  public string AmountText { get; set; } = string.Empty;

  public string DateText { get; set; } = string.Empty;

  public string TypeText { get; set; } = string.Empty;

  public string Notes { get; set; } = string.Empty;

  public string AccountId { get; set; } = string.Empty;

  public bool IsRecurring { get; set; }
}
