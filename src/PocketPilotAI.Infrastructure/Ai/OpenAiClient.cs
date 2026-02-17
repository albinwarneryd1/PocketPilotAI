using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PocketPilotAI.Infrastructure.Ai;

public class OpenAiClient(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiClient> logger)
{
  public async Task<string?> GenerateJsonAsync(string prompt, string modelInput, CancellationToken cancellationToken = default)
  {
    string? apiKey = configuration["OPENAI_API_KEY"] ?? configuration["Ai:ApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
    {
      logger.LogInformation("OPENAI_API_KEY not configured; AI generation will use fallback mode.");
      return null;
    }

    string model = configuration["Ai:Model"] ?? "gpt-4.1-mini";

    using HttpRequestMessage request = new(HttpMethod.Post, "https://api.openai.com/v1/responses");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

    object payload = new
    {
      model,
      input = new object[]
      {
        new
        {
          role = "system",
          content = new object[]
          {
            new { type = "input_text", text = prompt }
          }
        },
        new
        {
          role = "user",
          content = new object[]
          {
            new { type = "input_text", text = modelInput }
          }
        }
      },
      text = new
      {
        format = new { type = "json_object" }
      }
    };

    request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    string content = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      logger.LogWarning("OpenAI call failed with status {StatusCode}: {Body}", response.StatusCode, content);
      return null;
    }

    using JsonDocument doc = JsonDocument.Parse(content);

    if (doc.RootElement.TryGetProperty("output_text", out JsonElement outputTextElement))
    {
      return outputTextElement.GetString();
    }

    if (doc.RootElement.TryGetProperty("output", out JsonElement output) && output.ValueKind == JsonValueKind.Array)
    {
      foreach (JsonElement item in output.EnumerateArray())
      {
        if (!item.TryGetProperty("content", out JsonElement contentNode) || contentNode.ValueKind != JsonValueKind.Array)
        {
          continue;
        }

        foreach (JsonElement part in contentNode.EnumerateArray())
        {
          if (part.TryGetProperty("text", out JsonElement textNode))
          {
            return textNode.GetString();
          }
        }
      }
    }

    return null;
  }
}
