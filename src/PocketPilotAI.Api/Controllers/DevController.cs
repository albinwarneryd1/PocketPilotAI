using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketPilotAI.Infrastructure.Services;

namespace PocketPilotAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dev/seed")]
public class DevController(IHostEnvironment environment, IDemoDataSeeder demoDataSeeder) : ControllerBase
{
  [HttpPost("apply")]
  public async Task<IActionResult> Apply(CancellationToken cancellationToken)
  {
    if (!environment.IsDevelopment())
    {
      return NotFound();
    }

    await demoDataSeeder.SeedAsync(cancellationToken);
    return Ok(new { status = "seeded" });
  }

  [HttpPost("reset")]
  public async Task<IActionResult> Reset(CancellationToken cancellationToken)
  {
    if (!environment.IsDevelopment())
    {
      return NotFound();
    }

    await demoDataSeeder.ResetAndSeedAsync(cancellationToken);
    return Ok(new { status = "reset-and-seeded" });
  }
}
