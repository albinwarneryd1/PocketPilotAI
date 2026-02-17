using System.Security.Cryptography;

namespace PocketPilotAI.Infrastructure.Security;

internal static class PasswordHasher
{
  public static (string hash, string salt, int iterations) Hash(string password, int iterations = 120_000)
  {
    byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
    byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, iterations, HashAlgorithmName.SHA256, 32);

    return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes), iterations);
  }

  public static bool Verify(string password, string hash, string salt, int iterations)
  {
    byte[] saltBytes = Convert.FromBase64String(salt);
    byte[] hashBytes = Convert.FromBase64String(hash);

    byte[] candidate = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, iterations, HashAlgorithmName.SHA256, hashBytes.Length);
    return CryptographicOperations.FixedTimeEquals(candidate, hashBytes);
  }
}
