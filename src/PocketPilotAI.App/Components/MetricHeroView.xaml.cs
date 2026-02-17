namespace PocketPilotAI.App.Components;

public partial class MetricHeroView : ContentView
{
  private CancellationTokenSource? animationCts;

  public static readonly BindableProperty EyebrowProperty = BindableProperty.Create(
    nameof(Eyebrow),
    typeof(string),
    typeof(MetricHeroView),
    string.Empty);

  public static readonly BindableProperty TitleProperty = BindableProperty.Create(
    nameof(Title),
    typeof(string),
    typeof(MetricHeroView),
    string.Empty);

  public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(
    nameof(Subtitle),
    typeof(string),
    typeof(MetricHeroView),
    string.Empty);

  public static readonly BindableProperty ScoreCaptionProperty = BindableProperty.Create(
    nameof(ScoreCaption),
    typeof(string),
    typeof(MetricHeroView),
    "Score");

  public static readonly BindableProperty ScoreProperty = BindableProperty.Create(
    nameof(Score),
    typeof(int),
    typeof(MetricHeroView),
    0,
    propertyChanged: OnScoreChanged);

  public string Eyebrow
  {
    get => (string)GetValue(EyebrowProperty);
    set => SetValue(EyebrowProperty, value);
  }

  public string Title
  {
    get => (string)GetValue(TitleProperty);
    set => SetValue(TitleProperty, value);
  }

  public string Subtitle
  {
    get => (string)GetValue(SubtitleProperty);
    set => SetValue(SubtitleProperty, value);
  }

  public string ScoreCaption
  {
    get => (string)GetValue(ScoreCaptionProperty);
    set => SetValue(ScoreCaptionProperty, value);
  }

  public int Score
  {
    get => (int)GetValue(ScoreProperty);
    set => SetValue(ScoreProperty, value);
  }

  public MetricHeroView()
  {
    InitializeComponent();
    ScoreLabel.Text = "0";
  }

  private static void OnScoreChanged(BindableObject bindable, object oldValue, object newValue)
  {
    MetricHeroView view = (MetricHeroView)bindable;
    int from = oldValue is int oldInt ? oldInt : 0;
    int to = newValue is int newInt ? newInt : 0;
    _ = view.AnimateScoreAsync(from, to);
  }

  private async Task AnimateScoreAsync(int from, int to)
  {
    animationCts?.Cancel();
    animationCts?.Dispose();
    animationCts = new CancellationTokenSource();
    CancellationToken token = animationCts.Token;

    int clampedTarget = Math.Clamp(to, 0, 100);
    int clampedStart = Math.Clamp(from, 0, 100);

    const int steps = 16;
    for (int i = 1; i <= steps; i++)
    {
      token.ThrowIfCancellationRequested();

      decimal t = i / (decimal)steps;
      int current = (int)Math.Round(clampedStart + ((clampedTarget - clampedStart) * t));

      ScoreLabel.Text = current.ToString();
      ScoreProgress.Progress = current / 100d;

      await Task.Delay(22, token);
    }

    ScoreLabel.Text = clampedTarget.ToString();
    ScoreProgress.Progress = clampedTarget / 100d;
  }
}
