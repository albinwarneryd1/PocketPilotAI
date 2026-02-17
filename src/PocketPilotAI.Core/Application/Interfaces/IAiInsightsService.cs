using PocketPilotAI.Core.Application.Dtos.Ai;
using PocketPilotAI.Core.Common;

namespace PocketPilotAI.Core.Application.Interfaces;

public interface IAiInsightsService
{
  Task<Result<IReadOnlyList<InsightCardDto>>> GetLeakInsightsAsync(
    Guid userId,
    LeakFinderRequest request,
    CancellationToken cancellationToken = default);

  Task<Result<IReadOnlyList<InsightCardDto>>> GetMonthlySummaryAsync(
    Guid userId,
    MonthlySummaryRequest request,
    CancellationToken cancellationToken = default);

  Task<Result<IReadOnlyList<WhatIfScenarioTemplateDto>>> GetWhatIfTemplatesAsync(
    Guid userId,
    CancellationToken cancellationToken = default);

  Task<Result<WhatIfSimulationResultDto>> RunWhatIfSimulationAsync(
    Guid userId,
    WhatIfSimulationRequest request,
    CancellationToken cancellationToken = default);
}
