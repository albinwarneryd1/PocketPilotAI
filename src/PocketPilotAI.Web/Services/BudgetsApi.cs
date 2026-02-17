using PocketPilotAI.Core.Application.Dtos.Budgets;

namespace PocketPilotAI.Web.Services;

public class BudgetsApi(ApiClient apiClient)
{
  public async Task<IReadOnlyList<BudgetDto>> GetMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    => await apiClient.GetAsync<List<BudgetDto>>($"/api/budgets?year={year}&month={month}", cancellationToken) ?? [];

  public async Task<BudgetDto?> SetAsync(SetBudgetRequest request, CancellationToken cancellationToken = default)
    => await apiClient.PostAsync<BudgetDto>("/api/budgets", request, cancellationToken);
}
