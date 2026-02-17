using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.Core.Application.Dtos.Transactions;

public class TransactionDto
{
  public Guid Id { get; set; }

  public Guid AccountId { get; set; }

  public Guid? MerchantId { get; set; }

  public Guid? CategoryId { get; set; }

  public decimal Amount { get; set; }

  public string Currency { get; set; } = "SEK";

  public DateTime DateUtc { get; set; }

  public TransactionType Type { get; set; }

  public string MerchantName { get; set; } = string.Empty;

  public string CategoryName { get; set; } = string.Empty;

  public string Notes { get; set; } = string.Empty;

  public bool IsRecurring { get; set; }
}
