namespace PocketPilotAI.App.Components;

public partial class AIExplanationBlockView : ContentView
{
  public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(AIExplanationBlockView), string.Empty);
  public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(string), typeof(AIExplanationBlockView), string.Empty);

  public string Title
  {
    get => (string)GetValue(TitleProperty);
    set => SetValue(TitleProperty, value);
  }

  public string Body
  {
    get => (string)GetValue(BodyProperty);
    set => SetValue(BodyProperty, value);
  }

  public AIExplanationBlockView()
  {
    InitializeComponent();
  }
}
