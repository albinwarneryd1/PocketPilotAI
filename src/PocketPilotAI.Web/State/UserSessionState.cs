using PocketPilotAI.Core.Application.Dtos.Users;

namespace PocketPilotAI.Web.State;

public class UserSessionState
{
  public string AccessToken { get; private set; } = string.Empty;

  public string RefreshToken { get; private set; } = string.Empty;

  public DateTime ExpiresUtc { get; private set; }

  public DateTime RefreshTokenExpiresUtc { get; private set; }

  public Guid UserId { get; private set; }

  public string DisplayName { get; private set; } = string.Empty;

  public bool IsAuthenticated =>
    !string.IsNullOrWhiteSpace(AccessToken) &&
    !string.IsNullOrWhiteSpace(RefreshToken) &&
    UserId != Guid.Empty &&
    ExpiresUtc > DateTime.UtcNow;

  public event Action? Changed;

  public void SetSession(AuthTokenDto token)
  {
    UserId = token.User.Id;
    DisplayName = token.User.DisplayName;
    AccessToken = token.AccessToken;
    RefreshToken = token.RefreshToken;
    ExpiresUtc = token.ExpiresUtc;
    RefreshTokenExpiresUtc = token.RefreshTokenExpiresUtc;
    Changed?.Invoke();
  }

  public void UpdateAccessToken(AuthTokenDto token)
  {
    AccessToken = token.AccessToken;
    ExpiresUtc = token.ExpiresUtc;
    RefreshToken = token.RefreshToken;
    RefreshTokenExpiresUtc = token.RefreshTokenExpiresUtc;
    Changed?.Invoke();
  }

  public void Clear()
  {
    UserId = Guid.Empty;
    DisplayName = string.Empty;
    AccessToken = string.Empty;
    RefreshToken = string.Empty;
    ExpiresUtc = DateTime.MinValue;
    RefreshTokenExpiresUtc = DateTime.MinValue;
    Changed?.Invoke();
  }
}
