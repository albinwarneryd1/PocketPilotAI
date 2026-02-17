using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.Core.Domain.Entities;

public class Transaction
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid UserId { get; set; }

  public Guid AccountId { get; set; }

  public Guid? MerchantId { get; set; }

  public Guid? CategoryId { get; set; }

  public Guid? ParentTransactionId { get; set; }

  public decimal Amount { get; set; }

  public string Currency { get; set; } = "SEK";

  public DateTime DateUtc { get; set; } = DateTime.UtcNow;

  public TransactionType Type { get; set; } = TransactionType.Expense;

  public TransactionSource Source { get; set; } = TransactionSource.Manual;

  public string Notes { get; set; } = string.Empty;

  public bool IsRecurring { get; set; }

  public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

  public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

  public User? User { get; set; }

  public Account? Account { get; set; }

  public Merchant? Merchant { get; set; }

  public Category? Category { get; set; }

  public Transaction? ParentTransaction { get; set; }

  public ICollection<Transaction> SplitChildren { get; set; } = new List<Transaction>();
}
