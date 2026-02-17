namespace PocketPilotAI.Core.Domain.Entities;

public class RefreshToken
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid UserId { get; set; }

  public string TokenHash { get; set; } = string.Empty;

  public DateTime ExpiresUtc { get; set; }

  public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

  public DateTime? RevokedUtc { get; set; }

  public string? ReplacedByTokenHash { get; set; }

  public string UserAgent { get; set; } = string.Empty;

  public string IpAddress { get; set; } = string.Empty;

  public User? User { get; set; }

  public bool IsActive => RevokedUtc is null && ExpiresUtc > DateTime.UtcNow;
}
