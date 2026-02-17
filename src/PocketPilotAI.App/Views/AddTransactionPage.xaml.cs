using PocketPilotAI.App.ViewModels;

namespace PocketPilotAI.App.Views;

public partial class AddTransactionPage : ContentPage
{
  public AddTransactionPage(AddTransactionViewModel viewModel)
  {
    InitializeComponent();
    BindingContext = viewModel;
  }

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    AddRoot.Opacity = 0;
    AddRoot.TranslationY = 16;
    await Task.WhenAll(
      AddRoot.FadeToAsync(1, 260, Easing.CubicOut),
      AddRoot.TranslateToAsync(0, 0, 260, Easing.CubicOut));
  }
}
