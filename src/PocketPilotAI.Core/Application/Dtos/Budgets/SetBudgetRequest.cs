namespace PocketPilotAI.Core.Application.Dtos.Budgets;

public class SetBudgetRequest
{
  public Guid CategoryId { get; set; }

  public DateOnly Month { get; set; } = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

  public decimal PlannedAmount { get; set; }

  public decimal AlertThresholdPercent { get; set; } = 80m;
}
