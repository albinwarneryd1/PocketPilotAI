namespace PocketPilotAI.Contracts.Errors;

public class ApiError
{
  public string Code { get; set; } = "unexpected_error";

  public string Message { get; set; } = "An unexpected error occurred.";

  public string? TraceId { get; set; }

  public Dictionary<string, string[]> ValidationErrors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
