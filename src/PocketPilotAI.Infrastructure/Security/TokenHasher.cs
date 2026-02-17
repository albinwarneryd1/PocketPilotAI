using System.Security.Cryptography;
using System.Text;

namespace PocketPilotAI.Infrastructure.Security;

internal static class TokenHasher
{
  public static string CreateOpaqueToken()
    => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

  public static string HashToken(string token)
  {
    byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(bytes);
  }
}
