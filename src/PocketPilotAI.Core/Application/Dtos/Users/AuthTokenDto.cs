namespace PocketPilotAI.Core.Application.Dtos.Users;

public class AuthTokenDto
{
  public string AccessToken { get; set; } = string.Empty;

  public DateTime ExpiresUtc { get; set; }

  public string RefreshToken { get; set; } = string.Empty;

  public DateTime RefreshTokenExpiresUtc { get; set; }

  public UserDto User { get; set; } = new();
}
