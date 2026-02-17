namespace PocketPilotAI.Core.Common;

public readonly record struct Money(decimal Amount, string Currency)
{
  public static Money Zero(string currency) => new(0m, currency.ToUpperInvariant());

  public Money Add(Money other)
  {
    EnsureSameCurrency(other);
    return this with { Amount = Amount + other.Amount };
  }

  public Money Subtract(Money other)
  {
    EnsureSameCurrency(other);
    return this with { Amount = Amount - other.Amount };
  }

  private void EnsureSameCurrency(Money other)
  {
    if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("Cannot operate on money with different currencies.");
    }
  }
}
