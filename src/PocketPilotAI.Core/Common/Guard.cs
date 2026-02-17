namespace PocketPilotAI.Core.Common;

public static class Guard
{
  public static void AgainstNull(object? value, string name)
  {
    if (value is null)
    {
      throw new ArgumentNullException(name);
    }
  }

  public static void AgainstNullOrWhiteSpace(string? value, string name)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ArgumentException($"{name} cannot be empty.", name);
    }
  }

  public static void AgainstOutOfRange(decimal value, decimal min, decimal max, string name)
  {
    if (value < min || value > max)
    {
      throw new ArgumentOutOfRangeException(name, value, $"{name} must be between {min} and {max}.");
    }
  }
}
