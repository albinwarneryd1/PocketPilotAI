namespace PocketPilotAI.Web.State;

public class UiState
{
  public bool IsLoading { get; private set; }

  public string? LastError { get; private set; }

  public event Action? Changed;

  public void StartLoading()
  {
    IsLoading = true;
    LastError = null;
    Changed?.Invoke();
  }

  public void StopLoading(string? error = null)
  {
    IsLoading = false;
    LastError = error;
    Changed?.Invoke();
  }
}
