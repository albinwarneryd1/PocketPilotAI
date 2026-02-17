using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.Core.Application.Dtos.Ai;

public class WhatIfSimulationRequest
{
  public string ScenarioName { get; set; } = string.Empty;

  public int Year { get; set; }

  public int Month { get; set; }

  public IReadOnlyList<WhatIfActionDto> Actions { get; set; } = [];
}

public class WhatIfActionDto
{
  public WhatIfActionType Type { get; set; }

  public string Label { get; set; } = string.Empty;

  public string CategoryName { get; set; } = string.Empty;

  public decimal Value { get; set; }

  public string Currency { get; set; } = "SEK";

  public TransactionType TransactionType { get; set; } = TransactionType.Expense;

  public bool IsRecurring { get; set; } = true;

  public DateTime? EffectiveDateUtc { get; set; }
}

public enum WhatIfActionType
{
  ReduceCategoryPercent = 1,
  ReduceCategoryFixed = 2,
  AddRecurringIncome = 3,
  AddRecurringExpense = 4,
  AddOneOffTransaction = 5,
  RemoveDetectedSubscriptions = 6
}

public class WhatIfScenarioTemplateDto
{
  public string Name { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public WhatIfActionType ActionType { get; set; }

  public string SuggestedCategory { get; set; } = string.Empty;

  public decimal SuggestedValue { get; set; }
}

public class WhatIfSimulationResultDto
{
  public string ScenarioName { get; set; } = string.Empty;

  public WhatIfKpiDto Baseline { get; set; } = new();

  public WhatIfKpiDto Simulated { get; set; } = new();

  public WhatIfKpiDeltaDto Delta { get; set; } = new();

  public string Explanation { get; set; } = string.Empty;

  public IReadOnlyList<string> Recommendations { get; set; } = [];

  public IReadOnlyList<WhatIfAppliedActionDto> AppliedActions { get; set; } = [];
}

public class WhatIfKpiDto
{
  public decimal Income { get; set; }

  public decimal Expenses { get; set; }

  public decimal Net { get; set; }

  public decimal SavingsRatePercent { get; set; }

  public decimal AverageDailySpend { get; set; }

  public string BiggestExpenseCategory { get; set; } = string.Empty;
}

public class WhatIfKpiDeltaDto
{
  public decimal IncomeDelta { get; set; }

  public decimal ExpenseDelta { get; set; }

  public decimal NetDelta { get; set; }

  public decimal SavingsRateDeltaPercent { get; set; }
}

public class WhatIfAppliedActionDto
{
  public WhatIfActionType Type { get; set; }

  public string Label { get; set; } = string.Empty;

  public decimal EstimatedMonthlyImpact { get; set; }
}
