using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PocketPilotAI.Core.Application.Dtos.Users;
using PocketPilotAI.Web.State;

namespace PocketPilotAI.Web.Services;

public class ApiClient(HttpClient httpClient, UserSessionState userSessionState)
{
  private readonly SemaphoreSlim refreshLock = new(1, 1);

  public async Task<T?> GetAsync<T>(string route, CancellationToken cancellationToken = default)
    => await SendAsync<T>(() =>
    {
      HttpRequestMessage request = new(HttpMethod.Get, route);
      AddAuth(request);
      return request;
    }, cancellationToken);

  public async Task<T?> PostAsync<T>(string route, object payload, CancellationToken cancellationToken = default)
    => await SendAsync<T>(() =>
    {
      HttpRequestMessage request = new(HttpMethod.Post, route)
      {
        Content = JsonContent.Create(payload)
      };

      AddAuth(request);
      return request;
    }, cancellationToken);

  private async Task<T?> SendAsync<T>(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
  {
    using HttpRequestMessage request = requestFactory();
    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

    if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshTokenAsync(cancellationToken))
    {
      using HttpRequestMessage retry = requestFactory();
      using HttpResponseMessage retryResponse = await httpClient.SendAsync(retry, cancellationToken);
      return await ReadAsync<T>(retryResponse, cancellationToken);
    }

    return await ReadAsync<T>(response, cancellationToken);
  }

  private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(userSessionState.RefreshToken))
    {
      return false;
    }

    await refreshLock.WaitAsync(cancellationToken);
    try
    {
      if (userSessionState.ExpiresUtc > DateTime.UtcNow.AddSeconds(10))
      {
        return true;
      }

      using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
        "/api/auth/refresh",
        new RefreshTokenRequest { RefreshToken = userSessionState.RefreshToken },
        cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        userSessionState.Clear();
        return false;
      }

      AuthTokenDto? token = await response.Content.ReadFromJsonAsync<AuthTokenDto>(cancellationToken: cancellationToken);
      if (token is null)
      {
        userSessionState.Clear();
        return false;
      }

      userSessionState.UpdateAccessToken(token);
      return true;
    }
    finally
    {
      refreshLock.Release();
    }
  }

  private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    if (!response.IsSuccessStatusCode)
    {
      return default;
    }

    if (typeof(T) == typeof(string))
    {
      object text = await response.Content.ReadAsStringAsync(cancellationToken);
      return (T)text;
    }

    return await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
  }

  private void AddAuth(HttpRequestMessage request)
  {
    if (!string.IsNullOrWhiteSpace(userSessionState.AccessToken))
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userSessionState.AccessToken);
    }
  }
}
