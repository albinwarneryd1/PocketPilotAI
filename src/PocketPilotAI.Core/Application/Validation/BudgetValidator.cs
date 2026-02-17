using PocketPilotAI.Core.Application.Dtos.Budgets;
using PocketPilotAI.Core.Common;

namespace PocketPilotAI.Core.Application.Validation;

public static class BudgetValidator
{
  public static Result ValidateSet(SetBudgetRequest request)
  {
    if (request.CategoryId == Guid.Empty)
    {
      return Result.Failure("CategoryId is required.");
    }

    if (request.PlannedAmount <= 0)
    {
      return Result.Failure("PlannedAmount must be greater than zero.");
    }

    if (request.AlertThresholdPercent is < 1 or > 100)
    {
      return Result.Failure("AlertThresholdPercent must be between 1 and 100.");
    }

    return Result.Success();
  }
}
