namespace PocketPilotAI.Core.Application.Dtos.Budgets;

public class BudgetDto
{
  public Guid Id { get; set; }

  public Guid CategoryId { get; set; }

  public string CategoryName { get; set; } = string.Empty;

  public DateOnly Month { get; set; }

  public decimal PlannedAmount { get; set; }

  public decimal SpentAmount { get; set; }

  public decimal RemainingAmount => PlannedAmount - SpentAmount;

  public decimal UtilizationPercent => PlannedAmount <= 0 ? 0 : Math.Round((SpentAmount / PlannedAmount) * 100m, 2);
}
