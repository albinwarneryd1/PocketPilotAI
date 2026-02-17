namespace PocketPilotAI.Web.State;

public class UserSessionState
{
  public string AccessToken { get; private set; } = string.Empty;

  public Guid UserId { get; private set; }

  public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && UserId != Guid.Empty;

  public event Action? Changed;

  public void SetSession(Guid userId, string accessToken)
  {
    UserId = userId;
    AccessToken = accessToken;
    Changed?.Invoke();
  }

  public void Clear()
  {
    UserId = Guid.Empty;
    AccessToken = string.Empty;
    Changed?.Invoke();
  }
}
