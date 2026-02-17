namespace PocketPilotAI.Core.Application.Dtos.Ai;

public class InsightCardDto
{
  public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public string SuggestedAction { get; set; } = string.Empty;

  public decimal EstimatedMonthlySavings { get; set; }

  public decimal Confidence { get; set; }

  public Dictionary<string, string> Metrics { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
