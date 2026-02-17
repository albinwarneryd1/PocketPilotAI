using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PocketPilotAI.App.Services;

public class ApiClient(HttpClient httpClient, UserSessionService session)
{
  public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
  {
    using HttpRequestMessage request = new(HttpMethod.Get, path);
    AttachAuth(request);

    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    return response.IsSuccessStatusCode
      ? await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
      : default;
  }

  public async Task<T?> PostAsync<T>(string path, object payload, CancellationToken cancellationToken = default)
  {
    using HttpRequestMessage request = new(HttpMethod.Post, path)
    {
      Content = JsonContent.Create(payload)
    };

    AttachAuth(request);

    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    return response.IsSuccessStatusCode
      ? await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
      : default;
  }

  private void AttachAuth(HttpRequestMessage request)
  {
    if (!string.IsNullOrWhiteSpace(session.AccessToken))
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
    }
  }
}
