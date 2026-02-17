using System.Text.Json;
using PocketPilotAI.Core.Application.Dtos.Ai;

namespace PocketPilotAI.Infrastructure.Ai;

public static class AiResponseParser
{
  public static IReadOnlyList<InsightCardDto> ParseInsightCards(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return Array.Empty<InsightCardDto>();
    }

    try
    {
      using JsonDocument doc = JsonDocument.Parse(json);
      JsonElement root = doc.RootElement;

      JsonElement insightsElement = root.ValueKind switch
      {
        JsonValueKind.Array => root,
        JsonValueKind.Object when root.TryGetProperty("insights", out JsonElement list) => list,
        _ => default
      };

      if (insightsElement.ValueKind != JsonValueKind.Array)
      {
        return Array.Empty<InsightCardDto>();
      }

      List<InsightCardDto> cards = new();
      foreach (JsonElement item in insightsElement.EnumerateArray())
      {
        cards.Add(new InsightCardDto
        {
          Title = item.TryGetProperty("title", out JsonElement title) ? title.GetString() ?? string.Empty : string.Empty,
          Description = item.TryGetProperty("description", out JsonElement description) ? description.GetString() ?? string.Empty : string.Empty,
          SuggestedAction = item.TryGetProperty("suggestedAction", out JsonElement action) ? action.GetString() ?? string.Empty : string.Empty,
          EstimatedMonthlySavings = item.TryGetProperty("estimatedMonthlySavings", out JsonElement savings) ? savings.GetDecimal() : 0m,
          Confidence = item.TryGetProperty("confidence", out JsonElement confidence) ? confidence.GetDecimal() : 0.6m,
          Metrics = item.TryGetProperty("metrics", out JsonElement metrics) && metrics.ValueKind == JsonValueKind.Object
            ? metrics.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        });
      }

      return cards;
    }
    catch
    {
      return Array.Empty<InsightCardDto>();
    }
  }
}
