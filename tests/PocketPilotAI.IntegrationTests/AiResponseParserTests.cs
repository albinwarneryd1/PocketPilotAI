using PocketPilotAI.Infrastructure.Ai;

namespace PocketPilotAI.IntegrationTests;

public class AiResponseParserTests
{
  [Fact]
  public void ParseInsightCards_ShouldParseValidJsonPayload()
  {
    const string json = """
      {
        "insights": [
          {
            "title": "Leak detected in dining",
            "description": "Dining spend increased.",
            "suggestedAction": "Cut one meal per week.",
            "estimatedMonthlySavings": 700,
            "confidence": 0.81,
            "metrics": { "changePercent": "38" }
          }
        ]
      }
      """;

    var cards = AiResponseParser.ParseInsightCards(json);

    Assert.Single(cards);
    Assert.Equal("Leak detected in dining", cards[0].Title);
    Assert.Equal(700m, cards[0].EstimatedMonthlySavings);
  }
}
