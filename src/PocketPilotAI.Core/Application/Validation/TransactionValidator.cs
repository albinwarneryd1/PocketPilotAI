using PocketPilotAI.Core.Application.Dtos.Transactions;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.Enums;

namespace PocketPilotAI.Core.Application.Validation;

public static class TransactionValidator
{
  public static Result ValidateCreate(CreateTransactionRequest request)
  {
    if (request.AccountId == Guid.Empty)
    {
      return Result.Failure("AccountId is required.");
    }

    if (request.Amount <= 0)
    {
      return Result.Failure("Amount must be greater than zero.");
    }

    if (request.Type == TransactionType.Transfer && string.IsNullOrWhiteSpace(request.Notes))
    {
      return Result.Failure("Transfer transactions must include notes about destination.");
    }

    return Result.Success();
  }

  public static Result ValidateUpdate(UpdateTransactionRequest request)
  {
    if (request.Amount is <= 0)
    {
      return Result.Failure("Amount, when provided, must be greater than zero.");
    }

    return Result.Success();
  }
}
