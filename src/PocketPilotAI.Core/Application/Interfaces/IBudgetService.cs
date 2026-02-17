using PocketPilotAI.Core.Application.Dtos.Budgets;
using PocketPilotAI.Core.Common;

namespace PocketPilotAI.Core.Application.Interfaces;

public interface IBudgetService
{
  Task<IReadOnlyList<BudgetDto>> GetMonthlyBudgetsAsync(Guid userId, int year, int month, CancellationToken cancellationToken = default);

  Task<Result<BudgetDto>> SetBudgetAsync(Guid userId, SetBudgetRequest request, CancellationToken cancellationToken = default);

  Task<Result<BudgetProgressDto>> GetBudgetProgressAsync(
    Guid userId,
    Guid categoryId,
    int year,
    int month,
    CancellationToken cancellationToken = default);
}
