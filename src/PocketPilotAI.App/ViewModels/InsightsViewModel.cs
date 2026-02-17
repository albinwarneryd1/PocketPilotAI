using System.Collections.ObjectModel;
using System.Windows.Input;
using PocketPilotAI.App.Services;
using PocketPilotAI.Core.Application.Dtos.Ai;
using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.App.ViewModels;

public class InsightsViewModel(ApiClient apiClient, UserSessionService sessionService) : BaseViewModel
{
  private string scenarioName = "30-day plan";
  private int year = DateTime.UtcNow.Year;
  private int month = DateTime.UtcNow.Month;
  private string categoryName = "Eating Out";
  private decimal value = 15m;
  private WhatIfActionType selectedActionType = WhatIfActionType.ReduceCategoryPercent;

  private string monthlyNarrative = "Loading AI narrative...";
  private int impactScore;
  private string impactSummary = "No simulation yet.";
  private string simulationSummary = string.Empty;
  private string lastUpdatedLabel = "Not simulated";
  private string topRecommendation = "Run a simulation to get your strongest next action.";
  private bool hasSimulationResult;

  private bool suppressLiveSimulation;
  private CancellationTokenSource? liveSimulationCts;

  public ObservableCollection<InsightCardDto> Cards { get; } = [];

  public ObservableCollection<WhatIfScenarioTemplateDto> Templates { get; } = [];

  public ObservableCollection<string> Recommendations { get; } = [];

  public ObservableCollection<KpiComparisonItem> KpiComparisons { get; } = [];

  public ObservableCollection<TrendBadgeItem> LeakBadges { get; } = [];

  public IReadOnlyList<WhatIfActionType> ActionTypes { get; } = Enum.GetValues<WhatIfActionType>();

  public string ScenarioName
  {
    get => scenarioName;
    set => SetScenarioProperty(ref scenarioName, value);
  }

  public int Year
  {
    get => year;
    set => SetScenarioProperty(ref year, value);
  }

  public int Month
  {
    get => month;
    set => SetScenarioProperty(ref month, value);
  }

  public string CategoryName
  {
    get => categoryName;
    set => SetScenarioProperty(ref categoryName, value);
  }

  public decimal Value
  {
    get => value;
    set => SetScenarioProperty(ref this.value, value);
  }

  public WhatIfActionType SelectedActionType
  {
    get => selectedActionType;
    set => SetScenarioProperty(ref selectedActionType, value);
  }

  public string MonthlyNarrative
  {
    get => monthlyNarrative;
    set => SetProperty(ref monthlyNarrative, value);
  }

  public int ImpactScore
  {
    get => impactScore;
    set => SetProperty(ref impactScore, value);
  }

  public string ImpactSummary
  {
    get => impactSummary;
    set => SetProperty(ref impactSummary, value);
  }

  public string SimulationSummary
  {
    get => simulationSummary;
    set => SetProperty(ref simulationSummary, value);
  }

  public string LastUpdatedLabel
  {
    get => lastUpdatedLabel;
    set => SetProperty(ref lastUpdatedLabel, value);
  }

  public string TopRecommendation
  {
    get => topRecommendation;
    set => SetProperty(ref topRecommendation, value);
  }

  public bool HasSimulationResult
  {
    get => hasSimulationResult;
    set => SetProperty(ref hasSimulationResult, value);
  }

  public ICommand RefreshCommand => new Command(async () => await LoadAsync());

  public ICommand LoadTemplatesCommand => new Command(async () => await LoadTemplatesAsync());

  public ICommand ApplyTemplateCommand => new Command<WhatIfScenarioTemplateDto>(ApplyTemplate);

  public ICommand RunWhatIfCommand => new Command(async () => await RunWhatIfAsync());

  private void SetScenarioProperty<T>(ref T field, T value)
  {
    bool changed = SetProperty(ref field, value);
    if (!changed || suppressLiveSimulation)
    {
      return;
    }

    _ = QueueLiveSimulationAsync();
  }

  public async Task LoadAsync()
  {
    if (!sessionService.IsAuthenticated)
    {
      SimulationSummary = "You must sign in first.";
      return;
    }

    IsBusy = true;
    try
    {
      DateTime now = DateTime.UtcNow;

      IReadOnlyList<InsightCardDto> cards = await apiClient.PostAsync<List<InsightCardDto>>("/api/insights/leaks", new LeakFinderRequest
      {
        FromUtc = now.AddMonths(-1),
        ToUtc = now,
        MaxSuggestions = 6
      }) ?? [];

      Cards.Clear();
      foreach (InsightCardDto card in cards)
      {
        Cards.Add(card);
      }

      LeakBadges.Clear();
      foreach (TrendBadgeItem badge in BuildLeakBadges(cards))
      {
        LeakBadges.Add(badge);
      }

      IReadOnlyList<InsightCardDto> monthlySummary =
        await apiClient.PostAsync<List<InsightCardDto>>("/api/insights/monthly-summary", new MonthlySummaryRequest
        {
          Year = now.Year,
          Month = now.Month
        }) ?? [];

      MonthlyNarrative = monthlySummary.FirstOrDefault()?.Description
        ?? "No monthly narrative yet. Add more categorized transactions for stronger AI context.";
    }
    finally
    {
      IsBusy = false;
    }
  }

  public async Task LoadTemplatesAsync()
  {
    if (!sessionService.IsAuthenticated)
    {
      return;
    }

    suppressLiveSimulation = true;
    try
    {
      IReadOnlyList<WhatIfScenarioTemplateDto> templates =
        await apiClient.GetAsync<List<WhatIfScenarioTemplateDto>>("/api/insights/what-if/templates") ?? [];

      Templates.Clear();
      foreach (WhatIfScenarioTemplateDto template in templates)
      {
        Templates.Add(template);
      }

      WhatIfScenarioTemplateDto? first = Templates.FirstOrDefault();
      if (first is not null)
      {
        ApplyTemplate(first);
      }
    }
    finally
    {
      suppressLiveSimulation = false;
    }
  }

  public async Task RunWhatIfAsync()
  {
    if (!sessionService.IsAuthenticated)
    {
      SimulationSummary = "You must sign in first.";
      return;
    }

    IsBusy = true;
    try
    {
      WhatIfSimulationResultDto? result = await apiClient.PostAsync<WhatIfSimulationResultDto>(
        "/api/insights/what-if/simulate",
        new WhatIfSimulationRequest
        {
          ScenarioName = ScenarioName,
          Year = Year,
          Month = Month,
          Actions =
          [
            new WhatIfActionDto
            {
              Type = SelectedActionType,
              Label = SelectedActionType.ToString(),
              CategoryName = CategoryName,
              Value = Value,
              TransactionType = SelectedActionType == WhatIfActionType.AddRecurringIncome
                ? TransactionType.Income
                : TransactionType.Expense,
              IsRecurring = SelectedActionType != WhatIfActionType.AddOneOffTransaction,
              EffectiveDateUtc = DateTime.UtcNow
            }
          ]
        });

      if (result is null)
      {
        SimulationSummary = "Simulation failed.";
        ImpactScore = 0;
        ImpactSummary = "No simulation data.";
        HasSimulationResult = false;
        KpiComparisons.Clear();
        Recommendations.Clear();
        TopRecommendation = "Simulation failed. Adjust input and try again.";
        LastUpdatedLabel = "Simulation failed";
        return;
      }

      SimulationSummary =
        $"Baseline net {result.Baseline.Net:0} SEK -> simulated net {result.Simulated.Net:0} SEK (delta {result.Delta.NetDelta:0} SEK).";

      ImpactScore = BuildImpactScore(result.Delta.NetDelta, result.Delta.SavingsRateDeltaPercent);
      ImpactSummary = $"Net change {result.Delta.NetDelta:0} SEK, savings change {result.Delta.SavingsRateDeltaPercent:0.0}%";

      Recommendations.Clear();
      foreach (string rec in result.Recommendations)
      {
        Recommendations.Add(rec);
      }

      TopRecommendation = result.Recommendations.FirstOrDefault()
        ?? "Use this scenario as baseline and review again in one week.";

      KpiComparisons.Clear();
      foreach (KpiComparisonItem item in BuildComparisonItems(result))
      {
        KpiComparisons.Add(item);
      }

      LastUpdatedLabel = $"Updated {DateTime.Now:HH:mm:ss}";
      HasSimulationResult = true;
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task QueueLiveSimulationAsync()
  {
    liveSimulationCts?.Cancel();
    liveSimulationCts?.Dispose();
    liveSimulationCts = new CancellationTokenSource();

    try
    {
      await Task.Delay(350, liveSimulationCts.Token);
      await RunWhatIfAsync();
    }
    catch (TaskCanceledException)
    {
      // Ignore high-frequency UI updates.
    }
  }

  private void ApplyTemplate(WhatIfScenarioTemplateDto? template)
  {
    if (template is null)
    {
      return;
    }

    suppressLiveSimulation = true;
    try
    {
      SelectedActionType = template.ActionType;
      CategoryName = template.SuggestedCategory;
      Value = template.SuggestedValue;
      ScenarioName = $"{template.Name} scenario";
    }
    finally
    {
      suppressLiveSimulation = false;
    }

    _ = QueueLiveSimulationAsync();
  }

  private static int BuildImpactScore(decimal netDelta, decimal savingsRateDelta)
  {
    decimal score = 50m;
    score += Math.Clamp(netDelta / 100m, -25m, 35m);
    score += Math.Clamp(savingsRateDelta * 2m, -20m, 25m);

    return (int)Math.Clamp(Math.Round(score), 0, 100);
  }

  private static IReadOnlyList<KpiComparisonItem> BuildComparisonItems(WhatIfSimulationResultDto result)
    =>
    [
      CreateItem("Income", result.Baseline.Income, result.Simulated.Income, false),
      CreateItem("Expenses", result.Baseline.Expenses, result.Simulated.Expenses, false),
      CreateItem("Net", result.Baseline.Net, result.Simulated.Net, false),
      CreateItem("Savings rate", result.Baseline.SavingsRatePercent, result.Simulated.SavingsRatePercent, true)
    ];

  private static KpiComparisonItem CreateItem(string label, decimal baseline, decimal simulated, bool isPercent)
  {
    decimal max = Math.Max(Math.Abs(baseline), Math.Abs(simulated));
    double baselineRatio = max <= 0 ? 0 : decimal.ToDouble(Math.Abs(baseline) / max);
    double simulatedRatio = max <= 0 ? 0 : decimal.ToDouble(Math.Abs(simulated) / max);

    return new KpiComparisonItem
    {
      Label = label,
      Baseline = baseline,
      Simulated = simulated,
      BaselineRatio = baselineRatio,
      SimulatedRatio = simulatedRatio,
      IsPercent = isPercent
    };
  }

  private static IReadOnlyList<TrendBadgeItem> BuildLeakBadges(IReadOnlyList<InsightCardDto> cards)
  {
    if (cards.Count == 0)
    {
      return [new TrendBadgeItem("No leaks detected", "up")];
    }

    decimal potential = cards.Sum(x => x.EstimatedMonthlySavings);
    decimal confidence = cards.Average(x => x.Confidence);

    return
    [
      new TrendBadgeItem($"{cards.Count} insights", "neutral"),
      new TrendBadgeItem($"{potential:0} SEK potential", "up"),
      new TrendBadgeItem($"{confidence * 100m:0}% confidence", confidence >= 0.7m ? "up" : "neutral")
    ];
  }
}

public class KpiComparisonItem
{
  public string Label { get; set; } = string.Empty;

  public decimal Baseline { get; set; }

  public decimal Simulated { get; set; }

  public double BaselineRatio { get; set; }

  public double SimulatedRatio { get; set; }

  public bool IsPercent { get; set; }

  public string BaselineText => IsPercent ? $"{Baseline:0.0}%" : $"{Baseline:0} SEK";

  public string SimulatedText => IsPercent ? $"{Simulated:0.0}%" : $"{Simulated:0} SEK";
}
