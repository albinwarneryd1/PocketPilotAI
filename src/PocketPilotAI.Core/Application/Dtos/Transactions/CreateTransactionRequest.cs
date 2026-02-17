using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.Core.Application.Dtos.Transactions;

public class CreateTransactionRequest
{
  public Guid AccountId { get; set; }

  public string MerchantName { get; set; } = string.Empty;

  public Guid? CategoryId { get; set; }

  public decimal Amount { get; set; }

  public string Currency { get; set; } = "SEK";

  public DateTime DateUtc { get; set; } = DateTime.UtcNow;

  public TransactionType Type { get; set; } = TransactionType.Expense;

  public string Notes { get; set; } = string.Empty;

  public bool IsRecurring { get; set; }
}
