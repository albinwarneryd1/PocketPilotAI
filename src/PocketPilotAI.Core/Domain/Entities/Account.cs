using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.Core.Domain.Entities;

public class Account
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid UserId { get; set; }

  public string Name { get; set; } = string.Empty;

  public string Currency { get; set; } = "SEK";

  public AccountType Type { get; set; } = AccountType.Checking;

  public decimal OpeningBalance { get; set; }

  public decimal CurrentBalance { get; set; }

  public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

  public User? User { get; set; }

  public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

  public ICollection<RecurringPayment> RecurringPayments { get; set; } = new List<RecurringPayment>();
}
