namespace PocketPilotAI.Core.Domain.ValueObjects;

public readonly record struct DateRange
{
  public DateRange(DateTime startUtc, DateTime endUtc)
  {
    if (startUtc > endUtc)
    {
      throw new ArgumentException("Start date must be less than or equal to end date.");
    }

    StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
    EndUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
  }

  public DateTime StartUtc { get; }

  public DateTime EndUtc { get; }

  public bool Contains(DateTime valueUtc) => valueUtc >= StartUtc && valueUtc <= EndUtc;

  public int TotalDays => (int)(EndUtc - StartUtc).TotalDays + 1;
}
