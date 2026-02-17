using PocketPilotAI.Core.Application.Dtos.Ai;

namespace PocketPilotAI.Web.Services;

public class InsightsApi(ApiClient apiClient)
{
  public async Task<IReadOnlyList<InsightCardDto>> GetLeakInsightsAsync(LeakFinderRequest request, CancellationToken cancellationToken = default)
    => await apiClient.PostAsync<List<InsightCardDto>>("/api/insights/leaks", request, cancellationToken) ?? [];

  public async Task<IReadOnlyList<InsightCardDto>> GetMonthlySummaryAsync(MonthlySummaryRequest request, CancellationToken cancellationToken = default)
    => await apiClient.PostAsync<List<InsightCardDto>>("/api/insights/monthly-summary", request, cancellationToken) ?? [];

  public async Task<IReadOnlyList<WhatIfScenarioTemplateDto>> GetWhatIfTemplatesAsync(CancellationToken cancellationToken = default)
    => await apiClient.GetAsync<List<WhatIfScenarioTemplateDto>>("/api/insights/what-if/templates", cancellationToken) ?? [];

  public async Task<WhatIfSimulationResultDto?> SimulateAsync(WhatIfSimulationRequest request, CancellationToken cancellationToken = default)
    => await apiClient.PostAsync<WhatIfSimulationResultDto>("/api/insights/what-if/simulate", request, cancellationToken);
}
