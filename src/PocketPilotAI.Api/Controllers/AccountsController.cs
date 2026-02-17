using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Api.Mapping;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Core.Domain.Enums;
using PocketPilotAI.Infrastructure.Persistence;

namespace PocketPilotAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountsController(AppDbContext dbContext) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> Get(CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();

    var accounts = await dbContext.Accounts
      .AsNoTracking()
      .Where(x => x.UserId == userId)
      .OrderBy(x => x.Name)
      .Select(x => new
      {
        x.Id,
        x.Name,
        x.Type,
        x.Currency,
        x.OpeningBalance,
        x.CurrentBalance
      })
      .ToListAsync(cancellationToken);

    return Ok(accounts);
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();

    Account entity = new()
    {
      UserId = userId,
      Name = request.Name,
      Currency = request.Currency.ToUpperInvariant(),
      Type = request.Type,
      OpeningBalance = request.OpeningBalance,
      CurrentBalance = request.OpeningBalance,
      CreatedUtc = DateTime.UtcNow
    };

    dbContext.Accounts.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);

    return CreatedAtAction(nameof(Get), new { id = entity.Id }, new
    {
      entity.Id,
      entity.Name,
      entity.Type,
      entity.Currency,
      entity.OpeningBalance,
      entity.CurrentBalance
    });
  }

  public class CreateAccountRequest
  {
    public string Name { get; set; } = string.Empty;

    public string Currency { get; set; } = "SEK";

    public AccountType Type { get; set; } = AccountType.Checking;

    public decimal OpeningBalance { get; set; }
  }
}
