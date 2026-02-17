using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketPilotAI.Api.Mapping;
using PocketPilotAI.Core.Application.Interfaces;

namespace PocketPilotAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ImportController(IImportService importService) : ControllerBase
{
  [HttpPost("preview")]
  public async Task<IActionResult> Preview([FromForm] IFormFile file, CancellationToken cancellationToken)
  {
    if (file.Length == 0)
    {
      return BadRequest(new { error = "File is empty." });
    }

    await using Stream stream = file.OpenReadStream();
    var result = await importService.PreviewAsync(stream, cancellationToken);

    return result.IsFailure || result.Value is null
      ? BadRequest(new { error = result.Error })
      : Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Import([FromForm] Guid accountId, [FromForm] IFormFile file, CancellationToken cancellationToken)
  {
    if (file.Length == 0)
    {
      return BadRequest(new { error = "File is empty." });
    }

    Guid userId = User.GetRequiredUserId();

    await using Stream stream = file.OpenReadStream();
    var result = await importService.ImportAsync(userId, accountId, stream, cancellationToken);

    return result.IsFailure || result.Value is null
      ? BadRequest(new { error = result.Error })
      : Ok(result.Value);
  }
}
