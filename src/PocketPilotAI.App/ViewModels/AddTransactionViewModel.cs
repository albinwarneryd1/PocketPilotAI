using System.Windows.Input;
using PocketPilotAI.App.Services;
using PocketPilotAI.Core.Application.Dtos.Transactions;

namespace PocketPilotAI.App.ViewModels;

public class AddTransactionViewModel(ApiClient apiClient) : BaseViewModel
{
  private string accountId = string.Empty;
  private string merchantName = string.Empty;
  private decimal amount;
  private string currency = "SEK";
  private DateTime dateUtc = DateTime.UtcNow;
  private string notes = string.Empty;
  private string statusMessage = string.Empty;

  public string AccountId
  {
    get => accountId;
    set => SetProperty(ref accountId, value);
  }

  public string MerchantName
  {
    get => merchantName;
    set => SetProperty(ref merchantName, value);
  }

  public decimal Amount
  {
    get => amount;
    set => SetProperty(ref amount, value);
  }

  public string Currency
  {
    get => currency;
    set => SetProperty(ref currency, value);
  }

  public DateTime DateUtc
  {
    get => dateUtc;
    set => SetProperty(ref dateUtc, value);
  }

  public string Notes
  {
    get => notes;
    set => SetProperty(ref notes, value);
  }

  public string StatusMessage
  {
    get => statusMessage;
    set => SetProperty(ref statusMessage, value);
  }

  public ICommand SaveCommand => new Command(async () => await SaveAsync());

  private async Task SaveAsync()
  {
    if (!Guid.TryParse(AccountId, out Guid accountGuid))
    {
      StatusMessage = "Account ID must be a valid GUID.";
      return;
    }

    IsBusy = true;
    try
    {
      TransactionDto? created = await apiClient.PostAsync<TransactionDto>("/api/transactions", new CreateTransactionRequest
      {
        AccountId = accountGuid,
        MerchantName = MerchantName,
        Amount = Amount,
        Currency = Currency,
        DateUtc = DateTime.SpecifyKind(DateUtc, DateTimeKind.Utc),
        Notes = Notes
      });

      StatusMessage = created is null
        ? "Could not save transaction. Check auth/API availability."
        : "Transaction saved.";

      if (created is not null)
      {
        MerchantName = string.Empty;
        Amount = 0;
        Notes = string.Empty;
      }
    }
    finally
    {
      IsBusy = false;
    }
  }
}
