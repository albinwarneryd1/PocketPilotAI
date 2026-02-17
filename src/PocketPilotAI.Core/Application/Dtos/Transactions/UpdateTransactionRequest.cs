using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.Core.Application.Dtos.Transactions;

public class UpdateTransactionRequest
{
  public Guid? CategoryId { get; set; }

  public decimal? Amount { get; set; }

  public DateTime? DateUtc { get; set; }

  public TransactionType? Type { get; set; }

  public string? Notes { get; set; }

  public bool? IsRecurring { get; set; }
}
