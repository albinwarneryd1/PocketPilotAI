using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Infrastructure.Persistence;
using PocketPilotAI.Infrastructure.Security;

namespace PocketPilotAI.Infrastructure.Services;

public class AuthService(AppDbContext dbContext) : IAuthService
{
  private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

  public async Task<Result<AuthTokenDto>> RegisterAsync(
    RegisterUserRequest request,
    string userAgent,
    string ipAddress,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
      return Result<AuthTokenDto>.Failure("Email and password are required.");
    }

    string email = request.Email.Trim().ToLowerInvariant();

    bool exists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
    if (exists)
    {
      return Result<AuthTokenDto>.Failure("Email already exists.");
    }

    var (hash, salt, iterations) = PasswordHasher.Hash(request.Password);

    User user = new()
    {
      Email = email,
      DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
      PasswordHash = hash,
      PasswordSalt = salt,
      PasswordIterations = iterations,
      CreatedUtc = DateTime.UtcNow,
      UpdatedUtc = DateTime.UtcNow
    };

    dbContext.Users.Add(user);

    (string refreshToken, RefreshToken refreshEntity) = CreateRefreshToken(user.Id, userAgent, ipAddress);
    dbContext.RefreshTokens.Add(refreshEntity);

    await dbContext.SaveChangesAsync(cancellationToken);

    return Result<AuthTokenDto>.Success(new AuthTokenDto
    {
      AccessToken = string.Empty,
      ExpiresUtc = DateTime.MinValue,
      RefreshToken = refreshToken,
      RefreshTokenExpiresUtc = refreshEntity.ExpiresUtc,
      User = UserService.Map(user)
    });
  }

  public async Task<Result<AuthTokenDto>> LoginAsync(
    LoginRequest request,
    string userAgent,
    string ipAddress,
    CancellationToken cancellationToken = default)
  {
    string email = request.Email.Trim().ToLowerInvariant();

    User? user = await dbContext.Users
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt, user.PasswordIterations))
    {
      return Result<AuthTokenDto>.Failure("Invalid credentials.");
    }

    (string refreshToken, RefreshToken refreshEntity) = CreateRefreshToken(user.Id, userAgent, ipAddress);

    dbContext.RefreshTokens.Add(refreshEntity);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Result<AuthTokenDto>.Success(new AuthTokenDto
    {
      AccessToken = string.Empty,
      ExpiresUtc = DateTime.MinValue,
      RefreshToken = refreshToken,
      RefreshTokenExpiresUtc = refreshEntity.ExpiresUtc,
      User = UserService.Map(user)
    });
  }

  public async Task<Result<AuthTokenDto>> RefreshAsync(
    RefreshTokenRequest request,
    string userAgent,
    string ipAddress,
    CancellationToken cancellationToken = default)
  {
    string hash = TokenHasher.HashToken(request.RefreshToken);

    RefreshToken? current = await dbContext.RefreshTokens
      .Include(x => x.User)
      .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

    if (current is null || current.User is null)
    {
      return Result<AuthTokenDto>.Failure("Refresh token is invalid or expired.");
    }

    if (current.RevokedUtc is not null && !string.IsNullOrWhiteSpace(current.ReplacedByTokenHash))
    {
      await RevokeAllUserTokensAsync(current.UserId, cancellationToken);
      return Result<AuthTokenDto>.Failure("Refresh token reuse detected. All sessions were revoked.");
    }

    if (!current.IsActive)
    {
      return Result<AuthTokenDto>.Failure("Refresh token is invalid or expired.");
    }

    (string replacementToken, RefreshToken replacementEntity) = CreateRefreshToken(current.UserId, userAgent, ipAddress);

    current.RevokedUtc = DateTime.UtcNow;
    current.ReplacedByTokenHash = replacementEntity.TokenHash;

    dbContext.RefreshTokens.Add(replacementEntity);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Result<AuthTokenDto>.Success(new AuthTokenDto
    {
      AccessToken = string.Empty,
      ExpiresUtc = DateTime.MinValue,
      RefreshToken = replacementToken,
      RefreshTokenExpiresUtc = replacementEntity.ExpiresUtc,
      User = UserService.Map(current.User)
    });
  }

  public async Task<Result> LogoutAsync(
    Guid userId,
    LogoutRequest request,
    CancellationToken cancellationToken = default)
  {
    string hash = TokenHasher.HashToken(request.RefreshToken);

    RefreshToken? token = await dbContext.RefreshTokens
      .FirstOrDefaultAsync(x => x.UserId == userId && x.TokenHash == hash, cancellationToken);

    if (token is null)
    {
      return Result.Success();
    }

    if (token.RevokedUtc is null)
    {
      token.RevokedUtc = DateTime.UtcNow;
      await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Result.Success();
  }

  public async Task<Result> LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    await RevokeAllUserTokensAsync(userId, cancellationToken);
    return Result.Success();
  }

  private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken)
  {
    List<RefreshToken> tokens = await dbContext.RefreshTokens
      .Where(x => x.UserId == userId && x.RevokedUtc == null)
      .ToListAsync(cancellationToken);

    foreach (RefreshToken token in tokens)
    {
      token.RevokedUtc = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static (string rawToken, RefreshToken entity) CreateRefreshToken(Guid userId, string userAgent, string ipAddress)
  {
    string rawToken = TokenHasher.CreateOpaqueToken();

    return (rawToken, new RefreshToken
    {
      UserId = userId,
      TokenHash = TokenHasher.HashToken(rawToken),
      CreatedUtc = DateTime.UtcNow,
      ExpiresUtc = DateTime.UtcNow.Add(RefreshTokenLifetime),
      UserAgent = Truncate(userAgent, 500),
      IpAddress = Truncate(ipAddress, 120)
    });
  }

  private static string Truncate(string value, int max)
    => string.IsNullOrWhiteSpace(value)
      ? string.Empty
      : (value.Length <= max ? value : value[..max]);
}
