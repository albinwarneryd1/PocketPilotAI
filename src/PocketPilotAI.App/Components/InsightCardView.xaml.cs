namespace PocketPilotAI.App.Components;

public partial class InsightCardView : ContentView
{
  public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(InsightCardView), string.Empty);
  public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(InsightCardView), string.Empty);
  public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(nameof(ActionText), typeof(string), typeof(InsightCardView), string.Empty);
  public static readonly BindableProperty SavingsTextProperty = BindableProperty.Create(nameof(SavingsText), typeof(string), typeof(InsightCardView), string.Empty);

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

  public string ActionText
  {
    get => (string)GetValue(ActionTextProperty);
    set => SetValue(ActionTextProperty, value);
  }

  public string SavingsText
  {
    get => (string)GetValue(SavingsTextProperty);
    set => SetValue(SavingsTextProperty, value);
  }

  public InsightCardView()
  {
    InitializeComponent();
  }
}
