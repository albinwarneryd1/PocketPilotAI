using System.Windows.Input;
using PocketPilotAI.Core.Application.Dtos.Budgets;
using PocketPilotAI.Core.Application.Dtos.Transactions;
using PocketPilotAI.Core.Domain.Enums;
using PocketPilotAI.App.Services;

namespace PocketPilotAI.App.ViewModels;

public class OverviewViewModel(ApiClient apiClient, OfflineCacheService cacheService) : BaseViewModel
{
  private decimal income;
  private decimal expenses;
  private decimal remainingBudget;
  private string topMerchant = "-";

  public decimal Income
  {
    get => income;
    set => SetProperty(ref income, value);
  }

  public decimal Expenses
  {
    get => expenses;
    set => SetProperty(ref expenses, value);
  }

  public decimal RemainingBudget
  {
    get => remainingBudget;
    set => SetProperty(ref remainingBudget, value);
  }

  public string TopMerchant
  {
    get => topMerchant;
    set => SetProperty(ref topMerchant, value);
  }

  public ICommand RefreshCommand => new Command(async () => await LoadAsync());

  public async Task LoadAsync()
  {
    IsBusy = true;
    try
    {
      IReadOnlyList<TransactionDto> transactions = await apiClient.GetAsync<List<TransactionDto>>("/api/transactions") ?? [];
      IReadOnlyList<BudgetDto> budgets =
        await apiClient.GetAsync<List<BudgetDto>>($"/api/budgets?year={DateTime.UtcNow.Year}&month={DateTime.UtcNow.Month}") ?? [];

      cacheService.Set("latest-transactions", transactions);
      cacheService.Set("latest-budgets", budgets);

      Income = transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
      Expenses = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
      RemainingBudget = budgets.Sum(x => x.RemainingAmount);
      TopMerchant = transactions
        .Where(x => !string.IsNullOrWhiteSpace(x.MerchantName))
        .GroupBy(x => x.MerchantName)
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault() ?? "-";
    }
    finally
    {
      IsBusy = false;
    }
  }
}
