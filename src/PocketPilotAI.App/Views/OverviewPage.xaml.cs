using PocketPilotAI.App.ViewModels;

namespace PocketPilotAI.App.Views;

public partial class OverviewPage : ContentPage
{
  private readonly OverviewViewModel viewModel;

  public OverviewPage(OverviewViewModel viewModel)
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
