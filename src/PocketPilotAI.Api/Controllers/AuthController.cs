using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketPilotAI.Api.Auth;
using PocketPilotAI.Api.Mapping;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Core.Application.Interfaces;

namespace PocketPilotAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, IUserService userService, IJwtTokenService jwtTokenService) : ControllerBase
{
  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
  {
    var result = await authService.RegisterAsync(
      request,
      Request.Headers.UserAgent.ToString(),
      HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
      cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return BadRequest(new { error = result.Error });
    }

    return Ok(WithAccessToken(result.Value));
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
  {
    var result = await authService.LoginAsync(
      request,
      Request.Headers.UserAgent.ToString(),
      HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
      cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return Unauthorized(new { error = result.Error });
    }

    return Ok(WithAccessToken(result.Value));
  }

  [HttpPost("refresh")]
  public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
  {
    var result = await authService.RefreshAsync(
      request,
      Request.Headers.UserAgent.ToString(),
      HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
      cancellationToken);

    if (result.IsFailure || result.Value is null)
    {
      return Unauthorized(new { error = result.Error });
    }

    return Ok(WithAccessToken(result.Value));
  }

  [Authorize]
  [HttpPost("logout")]
  public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await authService.LogoutAsync(userId, request, cancellationToken);
    return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok();
  }

  [Authorize]
  [HttpPost("logout-all")]
  public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await authService.LogoutAllAsync(userId, cancellationToken);
    return result.IsFailure ? BadRequest(new { error = result.Error }) : Ok();
  }

  [Authorize]
  [HttpGet("me")]
  public async Task<IActionResult> Me(CancellationToken cancellationToken)
  {
    Guid userId = User.GetRequiredUserId();
    var result = await userService.GetByIdAsync(userId, cancellationToken);
    return result.IsFailure || result.Value is null
      ? NotFound(new { error = result.Error })
      : Ok(result.Value);
  }

  private AuthTokenDto WithAccessToken(AuthTokenDto value)
  {
    var (token, expiresUtc) = jwtTokenService.CreateToken(value.User);

    value.AccessToken = token;
    value.ExpiresUtc = expiresUtc;

    return value;
  }
}
