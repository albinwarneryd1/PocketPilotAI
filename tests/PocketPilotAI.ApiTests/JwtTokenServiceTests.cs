using Microsoft.Extensions.Options;
using PocketPilotAI.Api.Auth;
using PocketPilotAI.Core.Application.Dtos.Users;

namespace PocketPilotAI.ApiTests;

public class JwtTokenServiceTests
{
  [Fact]
  public void CreateToken_ShouldReturnTokenAndFutureExpiry()
  {
    JwtOptions jwtOptions = new()
    {
      Issuer = "PocketPilotAI",
      Audience = "PocketPilotAI.Clients",
      Key = "this-is-a-long-enough-test-secret-key-12345",
      ExpirationMinutes = 30
    };

    JwtTokenService service = new(Options.Create(jwtOptions));

    var (token, expiresUtc) = service.CreateToken(new UserDto
    {
      Id = Guid.NewGuid(),
      Email = "test@example.com",
      DisplayName = "Test User"
    });

    Assert.False(string.IsNullOrWhiteSpace(token));
    Assert.True(expiresUtc > DateTime.UtcNow);
  }
}
