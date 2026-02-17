namespace PocketPilotAI.Core.Application.Dtos.Ai;

public class WhatIfSimulationRequest
{
  public string ScenarioName { get; set; } = string.Empty;

  public decimal MonthlyAdjustment { get; set; }

  public int NumberOfMonths { get; set; } = 3;
}

public class WhatIfSimulationResultDto
{
  public string ScenarioName { get; set; } = string.Empty;

  public decimal EstimatedTotalSavings { get; set; }

  public decimal EstimatedMonthlyImpact { get; set; }

  public IReadOnlyList<string> Notes { get; set; } = Array.Empty<string>();
}
