using PocketPilotAI.Core.Application.Dtos.Transactions;

namespace PocketPilotAI.Web.Services;

public class TransactionsApi(ApiClient apiClient)
{
  public async Task<IReadOnlyList<TransactionDto>> GetAsync(CancellationToken cancellationToken = default)
    => await apiClient.GetAsync<List<TransactionDto>>("/api/transactions", cancellationToken) ?? [];

  public async Task<TransactionDto?> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    => await apiClient.PostAsync<TransactionDto>("/api/transactions", request, cancellationToken);
}
