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

    TransactionsRoot.Opacity = 0;
    TransactionsRoot.TranslationY = 16;
    await viewModel.LoadAsync();
    await Task.WhenAll(
      TransactionsRoot.FadeToAsync(1, 260, Easing.CubicOut),
      TransactionsRoot.TranslateToAsync(0, 0, 260, Easing.CubicOut));
  }
}
