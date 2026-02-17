namespace PocketPilotAI.Core.Application.Dtos.Budgets;

public class BudgetProgressDto
{
  public DateOnly Month { get; set; }

  public decimal PlannedAmount { get; set; }

  public decimal SpentAmount { get; set; }

  public decimal RemainingAmount { get; set; }

  public decimal ForecastEndOfMonthSpend { get; set; }

  public bool IsLikelyToExceed { get; set; }
}
