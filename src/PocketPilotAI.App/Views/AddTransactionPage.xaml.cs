using PocketPilotAI.App.ViewModels;

namespace PocketPilotAI.App.Views;

public partial class AddTransactionPage : ContentPage
{
  public AddTransactionPage(AddTransactionViewModel viewModel)
  {
    InitializeComponent();
    BindingContext = viewModel;
  }
}
