using System.Windows.Input;
using PocketPilotAI.App.Services;
using PocketPilotAI.Core.Application.Dtos.Users;

namespace PocketPilotAI.App.ViewModels;

public class LoginViewModel(AuthApi authApi, UserSessionService sessionService) : BaseViewModel
{
  private bool isRegisterMode;
  private string displayName = string.Empty;
  private string email = string.Empty;
  private string password = string.Empty;
  private string statusMessage = string.Empty;

  public event Action? AuthenticationSucceeded;

  public bool IsRegisterMode
  {
    get => isRegisterMode;
    set => SetProperty(ref isRegisterMode, value);
  }

  public string DisplayName
  {
    get => displayName;
    set => SetProperty(ref displayName, value);
  }

  public string Email
  {
    get => email;
    set => SetProperty(ref email, value);
  }

  public string Password
  {
    get => password;
    set => SetProperty(ref password, value);
  }

  public string StatusMessage
  {
    get => statusMessage;
    set => SetProperty(ref statusMessage, value);
  }

  public ICommand SubmitCommand => new Command(async () => await SubmitAsync());

  public ICommand ToggleModeCommand => new Command(() => IsRegisterMode = !IsRegisterMode);

  private async Task SubmitAsync()
  {
    if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
    {
      StatusMessage = "Email and password are required.";
      return;
    }

    IsBusy = true;
    StatusMessage = string.Empty;

    try
    {
      AuthTokenDto? response = IsRegisterMode
        ? await authApi.RegisterAsync(new RegisterUserRequest
        {
          DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? Email : DisplayName,
          Email = Email,
          Password = Password
        })
        : await authApi.LoginAsync(new LoginRequest
        {
          Email = Email,
          Password = Password
        });

      if (response is null)
      {
        StatusMessage = "Authentication failed. Check credentials/API.";
        return;
      }

      await sessionService.SetSessionAsync(response);
      AuthenticationSucceeded?.Invoke();
    }
    finally
    {
      IsBusy = false;
    }
  }
}
