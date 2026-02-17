using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PocketPilotAI.Core.Application.Dtos.Users;

namespace PocketPilotAI.App.Services;

public class AuthApi(HttpClient httpClient, UserSessionService session)
{
  public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    => await PostAsync("/api/auth/login", request, cancellationToken);

  public async Task<AuthResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    => await PostAsync("/api/auth/register", request, cancellationToken);

  public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
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

  private async Task<AuthResult> PostAsync(string path, object payload, CancellationToken cancellationToken)
  {
    try
    {
      using HttpResponseMessage response = await httpClient.PostAsJsonAsync(path, payload, cancellationToken);
      if (!response.IsSuccessStatusCode)
      {
        return new AuthResult(null, await ReadErrorAsync(response, cancellationToken), response.StatusCode);
      }

      AuthTokenDto? token = await response.Content.ReadFromJsonAsync<AuthTokenDto>(cancellationToken: cancellationToken);
      return token is null
        ? new AuthResult(null, "API returned an empty auth response.", response.StatusCode)
        : new AuthResult(token, null, response.StatusCode);
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
      string baseAddress = httpClient.BaseAddress?.ToString() ?? "(not configured)";
      return new AuthResult(null, $"Could not reach API at {baseAddress}.", null);
    }
  }

  private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    string fallback = $"API rejected request ({(int)response.StatusCode} {response.ReasonPhrase}).";
    string raw = await response.Content.ReadAsStringAsync(cancellationToken);

    if (string.IsNullOrWhiteSpace(raw))
    {
      return fallback;
    }

    try
    {
      using JsonDocument doc = JsonDocument.Parse(raw);
      if (doc.RootElement.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String)
      {
        return error.GetString() ?? fallback;
      }
    }
    catch (JsonException)
    {
    }

    return fallback;
  }
}

public sealed record AuthResult(AuthTokenDto? Token, string? Error, HttpStatusCode? StatusCode);
