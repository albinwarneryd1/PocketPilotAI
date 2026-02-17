namespace PocketPilotAI.App.Services;

public class UserSessionService
{
  public Guid UserId { get; private set; }

  public string AccessToken { get; private set; } = string.Empty;

  public bool IsAuthenticated => UserId != Guid.Empty && !string.IsNullOrWhiteSpace(AccessToken);

  public void Set(Guid userId, string accessToken)
  {
    UserId = userId;
    AccessToken = accessToken;
  }

  public void Clear()
  {
    UserId = Guid.Empty;
    AccessToken = string.Empty;
  }
}
