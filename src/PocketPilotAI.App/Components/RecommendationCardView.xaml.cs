using System.Windows.Input;

namespace PocketPilotAI.App.Components;

public partial class RecommendationCardView : ContentView
{
  public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(RecommendationCardView), string.Empty);
  public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(RecommendationCardView), string.Empty);
  public static readonly BindableProperty CtaTextProperty = BindableProperty.Create(nameof(CtaText), typeof(string), typeof(RecommendationCardView), "Take action");
  public static readonly BindableProperty CtaCommandProperty = BindableProperty.Create(nameof(CtaCommand), typeof(ICommand), typeof(RecommendationCardView));

  public string Title
  {
    get => (string)GetValue(TitleProperty);
    set => SetValue(TitleProperty, value);
  }

  public string Description
  {
    get => (string)GetValue(DescriptionProperty);
    set => SetValue(DescriptionProperty, value);
  }

  public string CtaText
  {
    get => (string)GetValue(CtaTextProperty);
    set => SetValue(CtaTextProperty, value);
  }

  public ICommand? CtaCommand
  {
    get => (ICommand?)GetValue(CtaCommandProperty);
    set => SetValue(CtaCommandProperty, value);
  }

  public RecommendationCardView()
  {
    InitializeComponent();
  }
}
