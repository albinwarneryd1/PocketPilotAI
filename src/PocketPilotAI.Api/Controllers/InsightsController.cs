using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketPilotAI.Api.Mapping;
using PocketPilotAI.Core.Application.Dtos.Ai;
using PocketPilotAI.Core.Application.Interfaces;

namespace PocketPilotAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InsightsController(IAiInsightsService insightsService) : ControllerBase
{
  [HttpPost("leaks")]
  public async Task<IActionResult> LeakFinder([FromBody] LeakFinderRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();

    var result = await insightsService.GetLeakInsightsAsync(userId, request, cancellationToken);
    return result.IsFailure || result.Value is null
      ? BadRequest(new { error = result.Error })
      : Ok(result.Value);
  }

  [HttpPost("monthly-summary")]
  public async Task<IActionResult> MonthlySummary([FromBody] MonthlySummaryRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();

    var result = await insightsService.GetMonthlySummaryAsync(userId, request, cancellationToken);
    return result.IsFailure || result.Value is null
      ? BadRequest(new { error = result.Error })
      : Ok(result.Value);
  }

  [HttpGet("what-if/templates")]
  public async Task<IActionResult> WhatIfTemplates(CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await insightsService.GetWhatIfTemplatesAsync(userId, cancellationToken);

    return result.IsFailure || result.Value is null
      ? BadRequest(new { error = result.Error })
      : Ok(result.Value);
  }

  [HttpPost("what-if/simulate")]
  public async Task<IActionResult> WhatIf([FromBody] WhatIfSimulationRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();

    var result = await insightsService.RunWhatIfSimulationAsync(userId, request, cancellationToken);
    return result.IsFailure || result.Value is null
      ? BadRequest(new { error = result.Error })
      : Ok(result.Value);
  }
}
