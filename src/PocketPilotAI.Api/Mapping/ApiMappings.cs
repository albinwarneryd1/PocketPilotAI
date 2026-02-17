using System.Security.Claims;

namespace PocketPilotAI.Api.Mapping;

public static class ApiMappings
{
  public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
  {
    string? id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(id, out Guid parsed)
      ? parsed
      : throw new UnauthorizedAccessException("Invalid or missing user id claim.");
  }
}
