using PocketPilotAI.App.Services;
using PocketPilotAI.App.Views;

namespace PocketPilotAI.App;

public partial class App : Application
{
  private readonly AppShell shell;
  private readonly LoginPage loginPage;
  private readonly UserSessionService sessionService;

  public App(AppShell shell, LoginPage loginPage, UserSessionService sessionService)
  {
    InitializeComponent();

    this.shell = shell;
    this.loginPage = loginPage;
    this.sessionService = sessionService;

    this.sessionService.RestoreAsync().GetAwaiter().GetResult();
  }

  protected override Window CreateWindow(IActivationState? activationState)
  {
    Page startPage = sessionService.IsAuthenticated ? shell : loginPage;
    return new Window(startPage);
  }
}
