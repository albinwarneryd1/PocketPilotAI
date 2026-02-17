using PocketPilotAI.App.ViewModels;

namespace PocketPilotAI.App.Views;

public partial class LoginPage : ContentPage
{
  public LoginPage(LoginViewModel viewModel, AppShell shell)
  {
    InitializeComponent();
    BindingContext = viewModel;

    viewModel.AuthenticationSucceeded += () =>
    {
      if (Application.Current?.Windows.FirstOrDefault() is Window window)
      {
        window.Page = shell;
      }
    };
  }
}
