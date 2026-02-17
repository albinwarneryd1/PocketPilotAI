using PocketPilotAI.Core.Application.Dtos.Budgets;
using PocketPilotAI.Core.Application.Dtos.Transactions;
using PocketPilotAI.Core.Application.Validation;
using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.UnitTests;

public class ValidationTests
{
  [Fact]
  public void TransactionCreate_ShouldFail_WhenAmountIsZero()
  {
    CreateTransactionRequest request = new()
    {
      AccountId = Guid.NewGuid(),
      Amount = 0,
      Type = TransactionType.Expense
    };

    var result = TransactionValidator.ValidateCreate(request);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void TransactionCreate_ShouldPass_WithValidExpense()
  {
    CreateTransactionRequest request = new()
    {
      AccountId = Guid.NewGuid(),
      Amount = 250,
      MerchantName = "Market",
      Type = TransactionType.Expense
    };

    var result = TransactionValidator.ValidateCreate(request);

    Assert.True(result.IsSuccess);
  }

  [Fact]
  public void BudgetSet_ShouldFail_WhenThresholdOutOfRange()
  {
    SetBudgetRequest request = new()
    {
      CategoryId = Guid.NewGuid(),
      PlannedAmount = 5000,
      AlertThresholdPercent = 120
    };

    var result = BudgetValidator.ValidateSet(request);

    Assert.True(result.IsFailure);
  }

  [Fact]
  public void BudgetSet_ShouldPass_WithValidInput()
  {
    SetBudgetRequest request = new()
    {
      CategoryId = Guid.NewGuid(),
      PlannedAmount = 5000,
      AlertThresholdPercent = 80
    };

    var result = BudgetValidator.ValidateSet(request);

    Assert.True(result.IsSuccess);
  }
}
