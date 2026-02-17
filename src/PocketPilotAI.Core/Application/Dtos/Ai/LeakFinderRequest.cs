namespace PocketPilotAI.Core.Application.Dtos.Ai;

public class LeakFinderRequest
{
  public DateTime FromUtc { get; set; }

  public DateTime ToUtc { get; set; }

  public int MaxSuggestions { get; set; } = 3;
}
