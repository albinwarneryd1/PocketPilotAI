using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Transactions;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Application.Validation;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Core.Domain.Enums;
using PocketPilotAI.Core.Domain.ValueObjects;
using PocketPilotAI.Infrastructure.Persistence;

namespace PocketPilotAI.Infrastructure.Services;

public class TransactionService(AppDbContext dbContext) : ITransactionService
{
  public async Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(
    Guid userId,
    DateRange? range = null,
    string? search = null,
    CancellationToken cancellationToken = default)
  {
    IQueryable<Transaction> query = dbContext.Transactions
      .AsNoTracking()
      .Include(x => x.Category)
      .Include(x => x.Merchant)
      .Where(x => x.UserId == userId);

    if (range.HasValue)
    {
      query = query.Where(x => x.DateUtc >= range.Value.StartUtc && x.DateUtc <= range.Value.EndUtc);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
      string value = search.Trim().ToLowerInvariant();
      query = query.Where(x =>
        (x.Notes != null && x.Notes.ToLower().Contains(value)) ||
        (x.Merchant != null && x.Merchant.Name.ToLower().Contains(value)) ||
        (x.Category != null && x.Category.Name.ToLower().Contains(value)));
    }

    List<Transaction> transactions = await query
      .OrderByDescending(x => x.DateUtc)
      .ToListAsync(cancellationToken);

    return transactions.Select(Map).ToList();
  }

  public async Task<Result<TransactionDto>> GetByIdAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
  {
    Transaction? entity = await dbContext.Transactions
      .AsNoTracking()
      .Include(x => x.Merchant)
      .Include(x => x.Category)
      .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserId == userId, cancellationToken);

    return entity is null
      ? Result<TransactionDto>.Failure("Transaction not found.")
      : Result<TransactionDto>.Success(Map(entity));
  }

  public async Task<Result<TransactionDto>> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken = default)
  {
    Result validation = TransactionValidator.ValidateCreate(request);
    if (validation.IsFailure)
    {
      return Result<TransactionDto>.Failure(validation.Error ?? "Validation failed.");
    }

    Account? account = await dbContext.Accounts
      .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.UserId == userId, cancellationToken);
    if (account is null)
    {
      return Result<TransactionDto>.Failure("Account not found.");
    }

    Merchant merchant = await GetOrCreateMerchantAsync(userId, request.MerchantName, request.CategoryId, cancellationToken);

    Transaction transaction = new()
    {
      UserId = userId,
      AccountId = request.AccountId,
      MerchantId = merchant.Id,
      CategoryId = request.CategoryId,
      Amount = request.Amount,
      Currency = request.Currency.ToUpperInvariant(),
      DateUtc = request.DateUtc,
      Type = request.Type,
      Source = TransactionSource.Manual,
      Notes = request.Notes,
      IsRecurring = request.IsRecurring,
      CreatedUtc = DateTime.UtcNow,
      UpdatedUtc = DateTime.UtcNow
    };

    account.CurrentBalance += SignedAmount(transaction.Amount, transaction.Type);

    dbContext.Transactions.Add(transaction);
    await dbContext.SaveChangesAsync(cancellationToken);

    transaction.Merchant = merchant;

    if (transaction.CategoryId.HasValue)
    {
      transaction.Category = await dbContext.Categories
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == transaction.CategoryId.Value, cancellationToken);
    }

    return Result<TransactionDto>.Success(Map(transaction));
  }

  public async Task<Result<TransactionDto>> UpdateAsync(
    Guid userId,
    Guid transactionId,
    UpdateTransactionRequest request,
    CancellationToken cancellationToken = default)
  {
    Result validation = TransactionValidator.ValidateUpdate(request);
    if (validation.IsFailure)
    {
      return Result<TransactionDto>.Failure(validation.Error ?? "Validation failed.");
    }

    Transaction? transaction = await dbContext.Transactions
      .Include(x => x.Account)
      .Include(x => x.Category)
      .Include(x => x.Merchant)
      .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserId == userId, cancellationToken);

    if (transaction is null)
    {
      return Result<TransactionDto>.Failure("Transaction not found.");
    }

    if (request.Amount.HasValue && request.Amount.Value != transaction.Amount)
    {
      if (transaction.Account is not null)
      {
        transaction.Account.CurrentBalance -= SignedAmount(transaction.Amount, transaction.Type);
        transaction.Account.CurrentBalance += SignedAmount(request.Amount.Value, request.Type ?? transaction.Type);
      }

      transaction.Amount = request.Amount.Value;
    }

    if (request.Type.HasValue && request.Type.Value != transaction.Type)
    {
      if (transaction.Account is not null && !request.Amount.HasValue)
      {
        transaction.Account.CurrentBalance -= SignedAmount(transaction.Amount, transaction.Type);
        transaction.Account.CurrentBalance += SignedAmount(transaction.Amount, request.Type.Value);
      }

      transaction.Type = request.Type.Value;
    }

    if (request.CategoryId.HasValue)
    {
      transaction.CategoryId = request.CategoryId;
    }

    if (request.DateUtc.HasValue)
    {
      transaction.DateUtc = request.DateUtc.Value;
    }

    if (request.Notes is not null)
    {
      transaction.Notes = request.Notes;
    }

    if (request.IsRecurring.HasValue)
    {
      transaction.IsRecurring = request.IsRecurring.Value;
    }

    transaction.UpdatedUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync(cancellationToken);

    return Result<TransactionDto>.Success(Map(transaction));
  }

  public async Task<Result> DeleteAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
  {
    Transaction? transaction = await dbContext.Transactions
      .Include(x => x.Account)
      .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserId == userId, cancellationToken);
    if (transaction is null)
    {
      return Result.Failure("Transaction not found.");
    }

    if (transaction.Account is not null)
    {
      transaction.Account.CurrentBalance -= SignedAmount(transaction.Amount, transaction.Type);
    }

    dbContext.Transactions.Remove(transaction);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }

  public async Task<Result<IReadOnlyList<TransactionDto>>> SplitAsync(
    Guid userId,
    Guid transactionId,
    SplitTransactionRequest request,
    CancellationToken cancellationToken = default)
  {
    Transaction? parent = await dbContext.Transactions
      .Include(x => x.Merchant)
      .Include(x => x.Category)
      .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserId == userId, cancellationToken);

    if (parent is null)
    {
      return Result<IReadOnlyList<TransactionDto>>.Failure("Transaction not found.");
    }

    if (request.Parts.Count < 2)
    {
      return Result<IReadOnlyList<TransactionDto>>.Failure("At least two split parts are required.");
    }

    decimal sum = request.Parts.Sum(x => x.Amount);
    if (Math.Abs(sum - parent.Amount) > 0.01m)
    {
      return Result<IReadOnlyList<TransactionDto>>.Failure("Split parts must match parent transaction amount.");
    }

    List<Transaction> children = new();
    foreach (SplitTransactionItemDto part in request.Parts)
    {
      Transaction child = new()
      {
        UserId = userId,
        AccountId = parent.AccountId,
        ParentTransactionId = parent.Id,
        MerchantId = parent.MerchantId,
        CategoryId = part.CategoryId,
        Amount = part.Amount,
        Currency = parent.Currency,
        DateUtc = parent.DateUtc,
        Type = parent.Type,
        Source = parent.Source,
        Notes = part.Notes,
        IsRecurring = parent.IsRecurring,
        CreatedUtc = DateTime.UtcNow,
        UpdatedUtc = DateTime.UtcNow
      };

      children.Add(child);
    }

    dbContext.Transactions.AddRange(children);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Result<IReadOnlyList<TransactionDto>>.Success(children.Select(Map).ToList());
  }

  public async Task<Result<Guid?>> SuggestCategoryAsync(
    Guid userId,
    string merchantName,
    CancellationToken cancellationToken = default)
  {
    string normalized = NormalizeMerchant(merchantName);

    Merchant? merchant = await dbContext.Merchants.AsNoTracking()
      .FirstOrDefaultAsync(x => x.NormalizedName == normalized && (x.UserId == null || x.UserId == userId), cancellationToken);

    return Result<Guid?>.Success(merchant?.DefaultCategoryId);
  }

  private async Task<Merchant> GetOrCreateMerchantAsync(
    Guid userId,
    string merchantName,
    Guid? defaultCategoryId,
    CancellationToken cancellationToken)
  {
    string normalized = NormalizeMerchant(merchantName);

    Merchant? merchant = await dbContext.Merchants
      .FirstOrDefaultAsync(x => x.NormalizedName == normalized && x.UserId == userId, cancellationToken);

    if (merchant is not null)
    {
      if (defaultCategoryId.HasValue && merchant.DefaultCategoryId is null)
      {
        merchant.DefaultCategoryId = defaultCategoryId;
      }

      return merchant;
    }

    merchant = new Merchant
    {
      UserId = userId,
      Name = merchantName.Trim(),
      NormalizedName = normalized,
      DefaultCategoryId = defaultCategoryId,
      CreatedUtc = DateTime.UtcNow
    };

    dbContext.Merchants.Add(merchant);
    await dbContext.SaveChangesAsync(cancellationToken);
    return merchant;
  }

  private static string NormalizeMerchant(string merchantName)
    => string.IsNullOrWhiteSpace(merchantName)
      ? "unknown"
      : merchantName.Trim().ToLowerInvariant();

  private static decimal SignedAmount(decimal amount, TransactionType type)
    => type switch
    {
      TransactionType.Income => amount,
      TransactionType.Transfer => 0,
      _ => -amount
    };

  private static TransactionDto Map(Transaction entity)
    => new()
    {
      Id = entity.Id,
      AccountId = entity.AccountId,
      MerchantId = entity.MerchantId,
      CategoryId = entity.CategoryId,
      Amount = entity.Amount,
      Currency = entity.Currency,
      DateUtc = entity.DateUtc,
      Type = entity.Type,
      MerchantName = entity.Merchant?.Name ?? string.Empty,
      CategoryName = entity.Category?.Name ?? string.Empty,
      Notes = entity.Notes,
      IsRecurring = entity.IsRecurring
    };
}
