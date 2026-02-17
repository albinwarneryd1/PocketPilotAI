using System.Net.Http.Headers;
using System.Net.Http.Json;
using PocketPilotAI.Web.State;

namespace PocketPilotAI.Web.Services;

public class ApiClient(HttpClient httpClient, UserSessionState userSessionState)
{
  public async Task<T?> GetAsync<T>(string route, CancellationToken cancellationToken = default)
  {
    using HttpRequestMessage request = new(HttpMethod.Get, route);
    AddAuth(request);

    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    return await ReadAsync<T>(response, cancellationToken);
  }

  public async Task<T?> PostAsync<T>(string route, object payload, CancellationToken cancellationToken = default)
  {
    using HttpRequestMessage request = new(HttpMethod.Post, route)
    {
      Content = JsonContent.Create(payload)
    };

    AddAuth(request);

    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    return await ReadAsync<T>(response, cancellationToken);
  }

  private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    if (!response.IsSuccessStatusCode)
    {
      return default;
    }

    return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
  }

  private void AddAuth(HttpRequestMessage request)
  {
    if (!string.IsNullOrWhiteSpace(userSessionState.AccessToken))
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userSessionState.AccessToken);
    }
  }
}
