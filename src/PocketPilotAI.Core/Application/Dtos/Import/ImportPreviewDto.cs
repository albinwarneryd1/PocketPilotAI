namespace PocketPilotAI.Core.Application.Dtos.Import;

public class ImportPreviewDto
{
  public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();

  public Dictionary<string, string> DetectedMapping { get; set; } = new(StringComparer.OrdinalIgnoreCase);

  public IReadOnlyList<ImportRowPreviewDto> Rows { get; set; } = Array.Empty<ImportRowPreviewDto>();
}

public class ImportRowPreviewDto
{
  public DateTime DateUtc { get; set; }

  public string Merchant { get; set; } = string.Empty;

  public decimal Amount { get; set; }

  public string Currency { get; set; } = "SEK";

  public string? Category { get; set; }
}
