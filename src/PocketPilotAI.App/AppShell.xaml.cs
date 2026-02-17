using PocketPilotAI.App.Navigation;

namespace PocketPilotAI.App;

public partial class AppShell : Shell
{
  public AppShell()
  {
    InitializeComponent();

    Routing.RegisterRoute(Routes.Overview, typeof(Views.OverviewPage));
    Routing.RegisterRoute(Routes.Transactions, typeof(Views.TransactionsPage));
    Routing.RegisterRoute(Routes.AddTransaction, typeof(Views.AddTransactionPage));
    Routing.RegisterRoute(Routes.Insights, typeof(Views.InsightsPage));
  }
}
