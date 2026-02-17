namespace PocketPilotAI.Core.Application.Dtos.Transactions;

public class SplitTransactionRequest
{
  public ICollection<SplitTransactionItemDto> Parts { get; set; } = new List<SplitTransactionItemDto>();
}

public class SplitTransactionItemDto
{
  public Guid CategoryId { get; set; }

  public decimal Amount { get; set; }

  public string Notes { get; set; } = string.Empty;
}
