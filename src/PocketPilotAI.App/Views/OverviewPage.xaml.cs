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

    OverviewRoot.Opacity = 0;
    OverviewRoot.TranslationY = 16;
    await viewModel.LoadAsync();
    await Task.WhenAll(
      OverviewRoot.FadeToAsync(1, 260, Easing.CubicOut),
      OverviewRoot.TranslateToAsync(0, 0, 260, Easing.CubicOut));
  }
}
