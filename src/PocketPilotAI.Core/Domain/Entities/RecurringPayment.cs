namespace PocketPilotAI.Core.Domain.Entities;

public class RecurringPayment
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid UserId { get; set; }

  public Guid AccountId { get; set; }

  public Guid? MerchantId { get; set; }

  public Guid? CategoryId { get; set; }

  public decimal Amount { get; set; }

  public string Currency { get; set; } = "SEK";

  public string Frequency { get; set; } = "Monthly";

  public DateOnly NextRunDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date);

  public bool IsActive { get; set; } = true;

  public User? User { get; set; }

  public Account? Account { get; set; }

  public Merchant? Merchant { get; set; }

  public Category? Category { get; set; }
}
