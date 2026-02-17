using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Infrastructure.Persistence;

namespace PocketPilotAI.ApiTests;

public class AuthRefreshPersistenceApiTests : IClassFixture<TestApiFactory>
{
  private readonly TestApiFactory factory;

  public AuthRefreshPersistenceApiTests(TestApiFactory factory)
  {
    this.factory = factory;
  }

  [Fact]
  public async Task RefreshRotation_ShouldPersistRevokedAndReplacementTokens()
  {
    HttpClient client = factory.CreateClient();
    await factory.EnsureDatabaseCreatedAsync();

    string email = $"persist-{Guid.NewGuid():N}@test.com";

    HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserRequest
    {
      Email = email,
      Password = "StrongPass123!",
      DisplayName = "Persist"
    });

    registerResponse.EnsureSuccessStatusCode();

    AuthTokenDto? registerToken = await registerResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(registerToken);
    Assert.False(string.IsNullOrWhiteSpace(registerToken!.RefreshToken));

    string firstRefreshToken = registerToken.RefreshToken;
    string firstHash = HashToken(firstRefreshToken);

    await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
    {
      AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      var tokenRow = await db.RefreshTokens.SingleAsync(x => x.TokenHash == firstHash);
      Assert.Null(tokenRow.RevokedUtc);
      Assert.Null(tokenRow.ReplacedByTokenHash);
    }

    HttpResponseMessage refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
    {
      RefreshToken = firstRefreshToken
    });

    refreshResponse.EnsureSuccessStatusCode();

    AuthTokenDto? refreshedToken = await refreshResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(refreshedToken);
    Assert.False(string.IsNullOrWhiteSpace(refreshedToken!.RefreshToken));
    Assert.NotEqual(firstRefreshToken, refreshedToken.RefreshToken);

    string secondHash = HashToken(refreshedToken.RefreshToken);

    await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
    {
      AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

      var oldToken = await db.RefreshTokens.SingleAsync(x => x.TokenHash == firstHash);
      var newToken = await db.RefreshTokens.SingleAsync(x => x.TokenHash == secondHash);

      Assert.NotNull(oldToken.RevokedUtc);
      Assert.Equal(secondHash, oldToken.ReplacedByTokenHash);
      Assert.Null(newToken.RevokedUtc);
    }

    HttpResponseMessage reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
    {
      RefreshToken = firstRefreshToken
    });

    Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

    await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
    {
      AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

      int activeTokenCount = await db.RefreshTokens.CountAsync(
        x => x.UserId == registerToken.User.Id && x.RevokedUtc == null);

      Assert.Equal(0, activeTokenCount);
    }
  }

  private static string HashToken(string token)
  {
    byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(bytes);
  }
}

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
  private readonly string databaseFilePath = Path.Combine(Path.GetTempPath(), $"pocketpilotai-api-tests-{Guid.NewGuid():N}.db");

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureAppConfiguration((_, configBuilder) =>
    {
      configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:DefaultConnection"] = $"Data Source={databaseFilePath}",
        ["Database:Provider"] = "sqlite",
        ["Database:ApplyMigrationsOnStartup"] = "false",
        ["DemoSeed:Enabled"] = "false",
        ["Jwt:Issuer"] = "PocketPilotAI.Tests",
        ["Jwt:Audience"] = "PocketPilotAI.Tests.Clients",
        ["Jwt:Key"] = "this-is-a-long-enough-test-secret-key-12345",
        ["Jwt:ExpirationMinutes"] = "30"
      });
    });

    builder.ConfigureServices(services =>
    {
      services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
      services.RemoveAll<DbContextOptions<AppDbContext>>();
      services.RemoveAll<AppDbContext>();

      services.AddSingleton(_ =>
      {
        SqliteConnection connection = new($"Data Source={databaseFilePath}");
        connection.Open();
        return connection;
      });

      services.AddDbContext<AppDbContext>((serviceProvider, options) =>
      {
        SqliteConnection connection = serviceProvider.GetRequiredService<SqliteConnection>();
        options.UseSqlite(connection);
      });
    });
  }

  public async Task EnsureDatabaseCreatedAsync()
  {
    await using AsyncServiceScope scope = Services.CreateAsyncScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);

    if (disposing)
    {
      if (File.Exists(databaseFilePath))
      {
        File.Delete(databaseFilePath);
      }
    }
  }
}
