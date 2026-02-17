namespace PocketPilotAI.Core.Domain.Entities;

public class User
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public string Email { get; set; } = string.Empty;

  public string DisplayName { get; set; } = string.Empty;

  public string PasswordHash { get; set; } = string.Empty;

  public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

  public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

  public ICollection<Account> Accounts { get; set; } = new List<Account>();

  public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

  public ICollection<Budget> Budgets { get; set; } = new List<Budget>();

  public ICollection<Category> Categories { get; set; } = new List<Category>();

  public ICollection<RecurringPayment> RecurringPayments { get; set; } = new List<RecurringPayment>();
}
