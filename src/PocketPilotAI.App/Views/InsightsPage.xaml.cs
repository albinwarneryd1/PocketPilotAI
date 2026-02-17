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

    InsightsRoot.Opacity = 0;
    InsightsRoot.TranslationY = 16;
    await viewModel.LoadAsync();
    await viewModel.LoadTemplatesAsync();
    await viewModel.RunWhatIfAsync();
    await Task.WhenAll(
      InsightsRoot.FadeToAsync(1, 260, Easing.CubicOut),
      InsightsRoot.TranslateToAsync(0, 0, 260, Easing.CubicOut));
  }
}
