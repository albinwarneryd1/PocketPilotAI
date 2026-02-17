using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Core.Common;

namespace PocketPilotAI.Core.Application.Interfaces;

public interface IAuthService
{
  Task<Result<AuthTokenDto>> RegisterAsync(
    RegisterUserRequest request,
    string userAgent,
    string ipAddress,
    CancellationToken cancellationToken = default);

  Task<Result<AuthTokenDto>> LoginAsync(
    LoginRequest request,
    string userAgent,
    string ipAddress,
    CancellationToken cancellationToken = default);

  Task<Result<AuthTokenDto>> RefreshAsync(
    RefreshTokenRequest request,
    string userAgent,
    string ipAddress,
    CancellationToken cancellationToken = default);

  Task<Result> LogoutAsync(
    Guid userId,
    LogoutRequest request,
    CancellationToken cancellationToken = default);

  Task<Result> LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default);
}
