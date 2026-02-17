using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Infrastructure.Persistence;
using PocketPilotAI.Infrastructure.Services;

namespace PocketPilotAI.IntegrationTests;

public class AuthServiceFlowTests
{
  [Fact]
  public async Task Register_Login_Refresh_LogoutAll_ShouldRotateAndRevokeTokens()
  {
    await using AppDbContext db = CreateDbContext();
    AuthService authService = new(db);

    var register = await authService.RegisterAsync(
      new RegisterUserRequest
      {
        Email = "flow@test.com",
        Password = "StrongPass123!",
        DisplayName = "Flow"
      },
      "integration-test",
      "127.0.0.1");

    Assert.True(register.IsSuccess);
    Assert.NotNull(register.Value);
    Assert.False(string.IsNullOrWhiteSpace(register.Value!.RefreshToken));

    var login = await authService.LoginAsync(
      new LoginRequest
      {
        Email = "flow@test.com",
        Password = "StrongPass123!"
      },
      "integration-test",
      "127.0.0.1");

    Assert.True(login.IsSuccess);
    Assert.NotNull(login.Value);

    string originalLoginRefreshToken = login.Value!.RefreshToken;

    var refreshed = await authService.RefreshAsync(
      new RefreshTokenRequest { RefreshToken = originalLoginRefreshToken },
      "integration-test",
      "127.0.0.1");

    Assert.True(refreshed.IsSuccess);
    Assert.NotEqual(originalLoginRefreshToken, refreshed.Value!.RefreshToken);

    var reuseAttempt = await authService.RefreshAsync(
      new RefreshTokenRequest { RefreshToken = originalLoginRefreshToken },
      "integration-test",
      "127.0.0.1");

    Assert.True(reuseAttempt.IsFailure);

    var logoutAll = await authService.LogoutAllAsync(register.Value.User.Id);
    Assert.True(logoutAll.IsSuccess);

    bool hasAnyActive = await db.RefreshTokens
      .Where(x => x.UserId == register.Value.User.Id)
      .AnyAsync(x => x.RevokedUtc == null);

    Assert.False(hasAnyActive);
  }

  private static AppDbContext CreateDbContext()
  {
    DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"auth-flow-{Guid.NewGuid()}")
      .Options;

    return new AppDbContext(options);
  }
}
