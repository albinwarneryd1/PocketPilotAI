using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketPilotAI.Api.Mapping;
using PocketPilotAI.Core.Application.Dtos.Transactions;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Domain.ValueObjects;

namespace PocketPilotAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> Get(
    [FromQuery] DateTime? fromUtc,
    [FromQuery] DateTime? toUtc,
    [FromQuery] string? search,
    CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();

    DateRange? range = null;
    if (fromUtc.HasValue && toUtc.HasValue)
    {
      range = new DateRange(fromUtc.Value, toUtc.Value);
    }

    var items = await transactionService.GetTransactionsAsync(userId, range, search, cancellationToken);
    return Ok(items);
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await transactionService.CreateAsync(userId, request, cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return BadRequest(new { error = result.Error });
    }

    return CreatedAtAction(nameof(GetById), new { transactionId = result.Value.Id }, result.Value);
  }

  [HttpGet("{transactionId:guid}")]
  public async Task<IActionResult> GetById(Guid transactionId, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await transactionService.GetByIdAsync(userId, transactionId, cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return NotFound(new { error = result.Error });
    }

    return Ok(result.Value);
  }

  [HttpPut("{transactionId:guid}")]
  public async Task<IActionResult> Update(Guid transactionId, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await transactionService.UpdateAsync(userId, transactionId, request, cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return BadRequest(new { error = result.Error });
    }

    return Ok(result.Value);
  }

  [HttpDelete("{transactionId:guid}")]
  public async Task<IActionResult> Delete(Guid transactionId, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await transactionService.DeleteAsync(userId, transactionId, cancellationToken);

    return result.IsFailure
      ? NotFound(new { error = result.Error })
      : NoContent();
  }

  [HttpPost("{transactionId:guid}/split")]
  public async Task<IActionResult> Split(Guid transactionId, [FromBody] SplitTransactionRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await transactionService.SplitAsync(userId, transactionId, request, cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return BadRequest(new { error = result.Error });
    }

    return Ok(result.Value);
  }

  [HttpGet("suggest-category")]
  public async Task<IActionResult> SuggestCategory([FromQuery] string merchantName, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await transactionService.SuggestCategoryAsync(userId, merchantName, cancellationToken);

    return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok(new { categoryId = result.Value });
  }
}
