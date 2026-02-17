using PocketPilotAI.App.ViewModels;

namespace PocketPilotAI.App.Views;

public partial class InsightsPage : ContentPage
{
  private readonly InsightsViewModel viewModel;

  public InsightsPage(InsightsViewModel viewModel)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    BindingContext = viewModel;
  }

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    await viewModel.LoadAsync();
    await viewModel.LoadTemplatesAsync();
  }
}
