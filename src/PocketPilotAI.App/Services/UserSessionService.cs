using PocketPilotAI.Core.Application.Dtos.Users;

namespace PocketPilotAI.App.Services;

public class UserSessionService
{
  private const string AccessTokenKey = "pp_access_token";
  private const string RefreshTokenKey = "pp_refresh_token";
  private const string AccessExpiresKey = "pp_access_expires_utc";
  private const string RefreshExpiresKey = "pp_refresh_expires_utc";
  private const string UserIdKey = "pp_user_id";
  private const string DisplayNameKey = "pp_display_name";

  public Guid UserId { get; private set; }

  public string DisplayName { get; private set; } = string.Empty;

  public string AccessToken { get; private set; } = string.Empty;

  public string RefreshToken { get; private set; } = string.Empty;

  public DateTime AccessExpiresUtc { get; private set; }

  public DateTime RefreshExpiresUtc { get; private set; }

  public bool IsAuthenticated =>
    UserId != Guid.Empty &&
    !string.IsNullOrWhiteSpace(AccessToken) &&
    !string.IsNullOrWhiteSpace(RefreshToken) &&
    RefreshExpiresUtc > DateTime.UtcNow;

  public async Task RestoreAsync()
  {
    string? accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
    string? refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
    string? userId = await SecureStorage.Default.GetAsync(UserIdKey);
    string? displayName = await SecureStorage.Default.GetAsync(DisplayNameKey);
    string? accessExpires = await SecureStorage.Default.GetAsync(AccessExpiresKey);
    string? refreshExpires = await SecureStorage.Default.GetAsync(RefreshExpiresKey);

    AccessToken = accessToken ?? string.Empty;
    RefreshToken = refreshToken ?? string.Empty;
    DisplayName = displayName ?? string.Empty;

    UserId = Guid.TryParse(userId, out Guid parsed) ? parsed : Guid.Empty;
    AccessExpiresUtc = DateTime.TryParse(accessExpires, out DateTime ae) ? ae : DateTime.MinValue;
    RefreshExpiresUtc = DateTime.TryParse(refreshExpires, out DateTime re) ? re : DateTime.MinValue;
  }

  public async Task SetSessionAsync(AuthTokenDto token)
  {
    UserId = token.User.Id;
    DisplayName = token.User.DisplayName;
    AccessToken = token.AccessToken;
    RefreshToken = token.RefreshToken;
    AccessExpiresUtc = token.ExpiresUtc;
    RefreshExpiresUtc = token.RefreshTokenExpiresUtc;

    await PersistAsync();
  }

  public async Task UpdateTokensAsync(AuthTokenDto token)
  {
    AccessToken = token.AccessToken;
    RefreshToken = token.RefreshToken;
    AccessExpiresUtc = token.ExpiresUtc;
    RefreshExpiresUtc = token.RefreshTokenExpiresUtc;

    await PersistAsync();
  }

  public async Task ClearAsync()
  {
    UserId = Guid.Empty;
    DisplayName = string.Empty;
    AccessToken = string.Empty;
    RefreshToken = string.Empty;
    AccessExpiresUtc = DateTime.MinValue;
    RefreshExpiresUtc = DateTime.MinValue;

    SecureStorage.Default.Remove(AccessTokenKey);
    SecureStorage.Default.Remove(RefreshTokenKey);
    SecureStorage.Default.Remove(UserIdKey);
    SecureStorage.Default.Remove(DisplayNameKey);
    SecureStorage.Default.Remove(AccessExpiresKey);
    SecureStorage.Default.Remove(RefreshExpiresKey);

    await Task.CompletedTask;
  }

  private async Task PersistAsync()
  {
    await SecureStorage.Default.SetAsync(AccessTokenKey, AccessToken);
    await SecureStorage.Default.SetAsync(RefreshTokenKey, RefreshToken);
    await SecureStorage.Default.SetAsync(UserIdKey, UserId.ToString());
    await SecureStorage.Default.SetAsync(DisplayNameKey, DisplayName);
    await SecureStorage.Default.SetAsync(AccessExpiresKey, AccessExpiresUtc.ToString("O"));
    await SecureStorage.Default.SetAsync(RefreshExpiresKey, RefreshExpiresUtc.ToString("O"));
  }
}
