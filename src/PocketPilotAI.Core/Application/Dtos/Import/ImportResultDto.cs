namespace PocketPilotAI.Core.Application.Dtos.Import;

public class ImportResultDto
{
  public int TotalRows { get; set; }

  public int ImportedRows { get; set; }

  public int SkippedRows { get; set; }

  public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}
