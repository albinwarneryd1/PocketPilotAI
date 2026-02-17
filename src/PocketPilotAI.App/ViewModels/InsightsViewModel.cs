using System.Collections.ObjectModel;
using System.Windows.Input;
using PocketPilotAI.App.Services;
using PocketPilotAI.Core.Application.Dtos.Ai;
using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.App.ViewModels;

public class InsightsViewModel(ApiClient apiClient, UserSessionService sessionService) : BaseViewModel
{
  private string scenarioName = "MAUI Scenario";
  private int year = DateTime.UtcNow.Year;
  private int month = DateTime.UtcNow.Month;
  private string categoryName = "Eating Out";
  private decimal value = 15m;
  private WhatIfActionType selectedActionType = WhatIfActionType.ReduceCategoryPercent;
  private string simulationSummary = string.Empty;

  public ObservableCollection<InsightCardDto> Cards { get; } = [];

  public ObservableCollection<WhatIfScenarioTemplateDto> Templates { get; } = [];

  public ObservableCollection<string> Recommendations { get; } = [];

  public IReadOnlyList<WhatIfActionType> ActionTypes { get; } = Enum.GetValues<WhatIfActionType>();

  public string ScenarioName
  {
    get => scenarioName;
    set => SetProperty(ref scenarioName, value);
  }

  public int Year
  {
    get => year;
    set => SetProperty(ref year, value);
  }

  public int Month
  {
    get => month;
    set => SetProperty(ref month, value);
  }

  public string CategoryName
  {
    get => categoryName;
    set => SetProperty(ref categoryName, value);
  }

  public decimal Value
  {
    get => value;
    set => SetProperty(ref this.value, value);
  }

  public WhatIfActionType SelectedActionType
  {
    get => selectedActionType;
    set => SetProperty(ref selectedActionType, value);
  }

  public string SimulationSummary
  {
    get => simulationSummary;
    set => SetProperty(ref simulationSummary, value);
  }

  public ICommand RefreshCommand => new Command(async () => await LoadAsync());

  public ICommand LoadTemplatesCommand => new Command(async () => await LoadTemplatesAsync());

  public ICommand RunWhatIfCommand => new Command(async () => await RunWhatIfAsync());

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
      IReadOnlyList<InsightCardDto> cards = await apiClient.PostAsync<List<InsightCardDto>>("/api/insights/leaks", new LeakFinderRequest
      {
        FromUtc = DateTime.UtcNow.AddMonths(-1),
        ToUtc = DateTime.UtcNow,
        MaxSuggestions = 3
      }) ?? [];

      Cards.Clear();
      foreach (InsightCardDto card in cards)
      {
        Cards.Add(card);
      }
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
      SelectedActionType = first.ActionType;
      CategoryName = first.SuggestedCategory;
      Value = first.SuggestedValue;
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
        return;
      }

      SimulationSummary =
        $"Baseline net: {result.Baseline.Net:0} SEK, simulated net: {result.Simulated.Net:0} SEK, delta: {result.Delta.NetDelta:0} SEK.";

      Recommendations.Clear();
      foreach (string rec in result.Recommendations)
      {
        Recommendations.Add(rec);
      }
    }
    finally
    {
      IsBusy = false;
    }
  }
}
