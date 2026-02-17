namespace PocketPilotAI.Api.Auth;

public class JwtOptions
{
  public const string SectionName = "Jwt";

  public string Issuer { get; set; } = "PocketPilotAI";

  public string Audience { get; set; } = "PocketPilotAI.Clients";

  public string Key { get; set; } = "replace-with-a-long-random-secret";

  public int ExpirationMinutes { get; set; } = 60;
}
