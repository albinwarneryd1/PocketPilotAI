using System.Collections.ObjectModel;
using System.Windows.Input;
using PocketPilotAI.App.Services;
using PocketPilotAI.Core.Application.Dtos.Transactions;

namespace PocketPilotAI.App.ViewModels;

public class TransactionsViewModel(ApiClient apiClient) : BaseViewModel
{
  public ObservableCollection<TransactionDto> Items { get; } = [];

  public ICommand RefreshCommand => new Command(async () => await LoadAsync());

  public async Task LoadAsync()
  {
    IsBusy = true;
    try
    {
      IReadOnlyList<TransactionDto> items = await apiClient.GetAsync<List<TransactionDto>>("/api/transactions") ?? [];
      Items.Clear();
      foreach (TransactionDto item in items)
      {
        Items.Add(item);
      }
    }
    finally
    {
      IsBusy = false;
    }
  }
}
