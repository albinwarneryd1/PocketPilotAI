using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketPilotAI.Api.Mapping;
using PocketPilotAI.Core.Application.Dtos.Budgets;
using PocketPilotAI.Core.Application.Interfaces;

namespace PocketPilotAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BudgetsController(IBudgetService budgetService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> Get([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var budgets = await budgetService.GetMonthlyBudgetsAsync(userId, year, month, cancellationToken);
    return Ok(budgets);
  }

  [HttpPost]
  public async Task<IActionResult> Set([FromBody] SetBudgetRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await budgetService.SetBudgetAsync(userId, request, cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return BadRequest(new { error = result.Error });
    }

    return Ok(result.Value);
  }

  [HttpGet("{categoryId:guid}/progress")]
  public async Task<IActionResult> Progress(Guid categoryId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await budgetService.GetBudgetProgressAsync(userId, categoryId, year, month, cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return NotFound(new { error = result.Error });
    }

    return Ok(result.Value);
  }
}
