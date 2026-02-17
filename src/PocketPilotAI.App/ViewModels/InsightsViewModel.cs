using System.Collections.ObjectModel;
using System.Windows.Input;
using PocketPilotAI.App.Services;
using PocketPilotAI.Core.Application.Dtos.Ai;

namespace PocketPilotAI.App.ViewModels;

public class InsightsViewModel(ApiClient apiClient) : BaseViewModel
{
  public ObservableCollection<InsightCardDto> Cards { get; } = [];

  public ICommand RefreshCommand => new Command(async () => await LoadAsync());

  public async Task LoadAsync()
  {
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
}
