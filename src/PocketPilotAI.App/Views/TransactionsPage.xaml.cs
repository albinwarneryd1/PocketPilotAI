using PocketPilotAI.App.ViewModels;

namespace PocketPilotAI.App.Views;

public partial class TransactionsPage : ContentPage
{
  private readonly TransactionsViewModel viewModel;

  public TransactionsPage(TransactionsViewModel viewModel)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    BindingContext = viewModel;
  }

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    await viewModel.LoadAsync();
  }
}
