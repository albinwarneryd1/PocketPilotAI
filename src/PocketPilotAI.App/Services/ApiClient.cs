using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PocketPilotAI.Core.Application.Dtos.Users;

namespace PocketPilotAI.App.Services;

public class ApiClient(HttpClient httpClient, UserSessionService session)
{
  private readonly SemaphoreSlim refreshLock = new(1, 1);

  public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    => await SendAsync<T>(() =>
    {
      HttpRequestMessage request = new(HttpMethod.Get, path);
      AttachAuth(request);
      return request;
    }, cancellationToken);

  public async Task<T?> PostAsync<T>(string path, object payload, CancellationToken cancellationToken = default)
    => await SendAsync<T>(() =>
    {
      HttpRequestMessage request = new(HttpMethod.Post, path)
      {
        Content = JsonContent.Create(payload)
      };

      AttachAuth(request);
      return request;
    }, cancellationToken);

  private async Task<T?> SendAsync<T>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
  {
    using HttpRequestMessage request = requestFactory();
    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

    if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync(cancellationToken))
    {
      using HttpRequestMessage retry = requestFactory();
      using HttpResponseMessage retryResponse = await httpClient.SendAsync(retry, cancellationToken);
      return retryResponse.IsSuccessStatusCode
        ? await retryResponse.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
        : default;
    }

    return response.IsSuccessStatusCode
      ? await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
      : default;
  }

  private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(session.RefreshToken))
    {
      return false;
    }

    await refreshLock.WaitAsync(cancellationToken);
    try
    {
      if (session.AccessExpiresUtc > DateTime.UtcNow.AddSeconds(10))
      {
        return true;
      }

      using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
        "/api/auth/refresh",
        new RefreshTokenRequest { RefreshToken = session.RefreshToken },
        cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        await session.ClearAsync();
        return false;
      }

      AuthTokenDto? token = await response.Content.ReadFromJsonAsync<AuthTokenDto>(cancellationToken: cancellationToken);
      if (token is null)
      {
        await session.ClearAsync();
        return false;
      }

      await session.UpdateTokensAsync(token);
      return true;
    }
    finally
    {
      refreshLock.Release();
    }
  }

  private void AttachAuth(HttpRequestMessage request)
  {
    if (!string.IsNullOrWhiteSpace(session.AccessToken))
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
    }
  }
}
