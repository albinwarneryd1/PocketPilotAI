namespace PocketPilotAI.Core.Domain.Entities;

public class Budget
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid UserId { get; set; }

  public Guid CategoryId { get; set; }

  public DateOnly Month { get; set; } = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

  public decimal PlannedAmount { get; set; }

  public decimal AlertThresholdPercent { get; set; } = 80m;

  public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

  public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

  public User? User { get; set; }

  public Category? Category { get; set; }
}
