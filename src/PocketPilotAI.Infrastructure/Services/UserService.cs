using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Infrastructure.Persistence;

namespace PocketPilotAI.Infrastructure.Services;

public class UserService(AppDbContext dbContext) : IUserService
{
  public async Task<Result<UserDto>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
      return Result<UserDto>.Failure("Email and password are required.");
    }

    string email = request.Email.Trim().ToLowerInvariant();
    bool exists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);

    if (exists)
    {
      return Result<UserDto>.Failure("Email already exists.");
    }

    User user = new()
    {
      Email = email,
      DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
      PasswordHash = HashPassword(request.Password),
      CreatedUtc = DateTime.UtcNow,
      UpdatedUtc = DateTime.UtcNow
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Result<UserDto>.Success(Map(user));
  }

  public async Task<Result<AuthTokenDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
  {
    string email = request.Email.Trim().ToLowerInvariant();

    User? user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    if (user is null || user.PasswordHash != HashPassword(request.Password))
    {
      return Result<AuthTokenDto>.Failure("Invalid credentials.");
    }

    return Result<AuthTokenDto>.Success(new AuthTokenDto
    {
      AccessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
      ExpiresUtc = DateTime.UtcNow.AddHours(1),
      User = Map(user)
    });
  }

  public async Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    User? user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    return user is null
      ? Result<UserDto>.Failure("User not found.")
      : Result<UserDto>.Success(Map(user));
  }

  private static UserDto Map(User user)
    => new()
    {
      Id = user.Id,
      Email = user.Email,
      DisplayName = user.DisplayName
    };

  private static string HashPassword(string password)
  {
    byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(bytes);
  }
}
