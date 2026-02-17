using System.Net.Http.Headers;
using System.Net.Http.Json;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Web.State;

namespace PocketPilotAI.Web.Services;

public class AuthApi(HttpClient httpClient, UserSessionState session)
{
  public async Task<AuthTokenDto?> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    => await PostAsync("/api/auth/register", request, cancellationToken);

  public async Task<AuthTokenDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    => await PostAsync("/api/auth/login", request, cancellationToken);

  public async Task<AuthTokenDto?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    => await PostAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = refreshToken }, cancellationToken);

  public async Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
  {
    if (!session.IsAuthenticated)
    {
      return true;
    }

    using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/logout")
    {
      Content = JsonContent.Create(new LogoutRequest { RefreshToken = session.RefreshToken })
    };

    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    return response.IsSuccessStatusCode;
  }

  public async Task<UserDto?> GetMeAsync(CancellationToken cancellationToken = default)
  {
    using HttpRequestMessage request = new(HttpMethod.Get, "/api/auth/me");

    if (!string.IsNullOrWhiteSpace(session.AccessToken))
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
    }

    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      return null;
    }

    return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
  }

  private async Task<AuthTokenDto?> PostAsync(string path, object payload, CancellationToken cancellationToken)
  {
    using HttpResponseMessage response = await httpClient.PostAsJsonAsync(path, payload, cancellationToken);
    return response.IsSuccessStatusCode
      ? await response.Content.ReadFromJsonAsync<AuthTokenDto>(cancellationToken: cancellationToken)
      : null;
  }
}
