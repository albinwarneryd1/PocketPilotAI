namespace PocketPilotAI.App.Components;

public partial class TrendBadgeView : ContentView
{
  public static readonly BindableProperty TextProperty = BindableProperty.Create(
    nameof(Text), typeof(string), typeof(TrendBadgeView), string.Empty);

  public static readonly BindableProperty VariantProperty = BindableProperty.Create(
    nameof(Variant), typeof(string), typeof(TrendBadgeView), "neutral", propertyChanged: OnVariantChanged);

  public string Text
  {
    get => (string)GetValue(TextProperty);
    set => SetValue(TextProperty, value);
  }

  public string Variant
  {
    get => (string)GetValue(VariantProperty);
    set => SetValue(VariantProperty, value);
  }

  public TrendBadgeView()
  {
    InitializeComponent();
    ApplyVariant();
  }

  private static void OnVariantChanged(BindableObject bindable, object oldValue, object newValue)
  {
    ((TrendBadgeView)bindable).ApplyVariant();
  }

  private void ApplyVariant()
  {
    string variant = Variant.ToLowerInvariant();

    if (variant == "up")
    {
      BadgeBorder.BackgroundColor = Color.FromArgb("#1A0F8A78");
      BadgeBorder.Stroke = Color.FromArgb("#330F8A78");
      return;
    }

    if (variant == "down")
    {
      BadgeBorder.BackgroundColor = Color.FromArgb("#1AD94848");
      BadgeBorder.Stroke = Color.FromArgb("#33D94848");
      return;
    }

    BadgeBorder.BackgroundColor = Color.FromArgb("#FFF8FBFC");
    BadgeBorder.Stroke = Color.FromArgb("#FFDCE6EC");
  }
}
