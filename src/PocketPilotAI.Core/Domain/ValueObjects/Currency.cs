namespace PocketPilotAI.Core.Domain.ValueObjects;

public readonly record struct Currency
{
  public Currency(string code)
  {
    if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
    {
      throw new ArgumentException("Currency code must be a 3-letter ISO code.", nameof(code));
    }

    Code = code.Trim().ToUpperInvariant();
  }

  public string Code { get; }

  public override string ToString() => Code;
}
