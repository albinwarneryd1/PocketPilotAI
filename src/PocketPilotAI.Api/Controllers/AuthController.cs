using Microsoft.AspNetCore.Mvc;
using PocketPilotAI.Api.Auth;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Core.Application.Interfaces;

namespace PocketPilotAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService userService, IJwtTokenService jwtTokenService) : ControllerBase
{
  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
  {
    var result = await userService.RegisterAsync(request, cancellationToken);
    if (result.IsFailure || result.Value is null)
    {
      return BadRequest(new { error = result.Error });
    }

    var (token, expiresUtc) = jwtTokenService.CreateToken(result.Value);

    return Ok(new AuthTokenDto
    {
      AccessToken = token,
      ExpiresUtc = expiresUtc,
      User = result.Value
    });
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
  {
    var result = await userService.LoginAsync(request, cancellationToken);
    if (result.IsFailure || result.Value is null)
    {
      return Unauthorized(new { error = result.Error });
    }

    var (token, expiresUtc) = jwtTokenService.CreateToken(result.Value.User);

    return Ok(new AuthTokenDto
    {
      AccessToken = token,
      ExpiresUtc = expiresUtc,
      User = result.Value.User
    });
  }
}
