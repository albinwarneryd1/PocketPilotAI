using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Core.Common;

namespace PocketPilotAI.Core.Application.Interfaces;

public interface IUserService
{
  Task<Result<UserDto>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

  Task<Result<AuthTokenDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

  Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
