using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Infrastructure.Persistence;
using PocketPilotAI.Infrastructure.Security;

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
    await dbContext.SaveChangesAsync(cancellationToken);

    return Result<UserDto>.Success(Map(user));
  }

  public async Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    User? user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    return user is null
      ? Result<UserDto>.Failure("User not found.")
      : Result<UserDto>.Success(Map(user));
  }

  public static UserDto Map(User user)
    => new()
    {
      Id = user.Id,
      Email = user.Email,
      DisplayName = user.DisplayName
    };
}
