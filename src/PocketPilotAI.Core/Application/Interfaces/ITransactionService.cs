using PocketPilotAI.Core.Application.Dtos.Transactions;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.ValueObjects;

namespace PocketPilotAI.Core.Application.Interfaces;

public interface ITransactionService
{
  Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(
    Guid userId,
    DateRange? range = null,
    string? search = null,
    CancellationToken cancellationToken = default);

  Task<Result<TransactionDto>> GetByIdAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default);

  Task<Result<TransactionDto>> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken = default);

  Task<Result<TransactionDto>> UpdateAsync(Guid userId, Guid transactionId, UpdateTransactionRequest request, CancellationToken cancellationToken = default);

  Task<Result> DeleteAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default);

  Task<Result<IReadOnlyList<TransactionDto>>> SplitAsync(
    Guid userId,
    Guid transactionId,
    SplitTransactionRequest request,
    CancellationToken cancellationToken = default);

  Task<Result<Guid?>> SuggestCategoryAsync(
    Guid userId,
    string merchantName,
    CancellationToken cancellationToken = default);
}
