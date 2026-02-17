using Microsoft.Extensions.Logging;
using PocketPilotAI.App.Services;
using PocketPilotAI.App.ViewModels;
using PocketPilotAI.App.Views;

namespace PocketPilotAI.App;

public static class MauiProgram
{
  public static MauiApp CreateMauiApp()
  {
    MauiAppBuilder builder = MauiApp.CreateBuilder();

    builder
      .UseMauiApp<App>()
      .ConfigureFonts(fonts =>
      {
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
      });

    string baseUrl = Environment.GetEnvironmentVariable("POCKETPILOTAI_API_BASE_URL") ?? "https://localhost:7174";
    builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri(baseUrl));

    builder.Services.AddSingleton<UserSessionService>();
    builder.Services.AddSingleton<OfflineCacheService>();

    builder.Services.AddTransient<OverviewViewModel>();
    builder.Services.AddTransient<TransactionsViewModel>();
    builder.Services.AddTransient<AddTransactionViewModel>();
    builder.Services.AddTransient<InsightsViewModel>();

    builder.Services.AddTransient<OverviewPage>();
    builder.Services.AddTransient<TransactionsPage>();
    builder.Services.AddTransient<AddTransactionPage>();
    builder.Services.AddTransient<InsightsPage>();
    builder.Services.AddTransient<AppShell>();

#if DEBUG
    builder.Logging.AddDebug();
#endif

    return builder.Build();
  }
}
