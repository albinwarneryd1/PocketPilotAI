namespace PocketPilotAI.Core.Domain.Entities;

public class Merchant
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid? UserId { get; set; }

  public string Name { get; set; } = string.Empty;

  public string NormalizedName { get; set; } = string.Empty;

  public Guid? DefaultCategoryId { get; set; }

  public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

  public User? User { get; set; }

  public Category? DefaultCategory { get; set; }

  public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

  public ICollection<RecurringPayment> RecurringPayments { get; set; } = new List<RecurringPayment>();
}
