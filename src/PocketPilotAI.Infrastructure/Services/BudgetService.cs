using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Budgets;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Application.Validation;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Core.Domain.Enums;
using PocketPilotAI.Infrastructure.Persistence;

namespace PocketPilotAI.Infrastructure.Services;

public class BudgetService(AppDbContext dbContext) : IBudgetService
{
  public async Task<IReadOnlyList<BudgetDto>> GetMonthlyBudgetsAsync(
    Guid userId,
    int year,
    int month,
    CancellationToken cancellationToken = default)
  {
    DateOnly monthStart = new(year, month, 1);
    DateTime startUtc = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
    DateTime endUtc = startUtc.AddMonths(1).AddTicks(-1);

    List<Budget> budgets = await dbContext.Budgets
      .AsNoTracking()
      .Include(x => x.Category)
      .Where(x => x.UserId == userId && x.Month == monthStart)
      .ToListAsync(cancellationToken);

    Dictionary<Guid, decimal> spentByCategory = await dbContext.Transactions
      .AsNoTracking()
      .Where(x =>
        x.UserId == userId &&
        x.CategoryId != null &&
        x.Type == TransactionType.Expense &&
        x.DateUtc >= startUtc &&
        x.DateUtc <= endUtc)
      .GroupBy(x => x.CategoryId!.Value)
      .Select(g => new { CategoryId = g.Key, Spent = g.Sum(x => x.Amount) })
      .ToDictionaryAsync(x => x.CategoryId, x => x.Spent, cancellationToken);

    return budgets.Select(b => new BudgetDto
    {
      Id = b.Id,
      CategoryId = b.CategoryId,
      CategoryName = b.Category?.Name ?? string.Empty,
      Month = b.Month,
      PlannedAmount = b.PlannedAmount,
      SpentAmount = spentByCategory.GetValueOrDefault(b.CategoryId)
    }).ToList();
  }

  public async Task<Result<BudgetDto>> SetBudgetAsync(
    Guid userId,
    SetBudgetRequest request,
    CancellationToken cancellationToken = default)
  {
    Result validation = BudgetValidator.ValidateSet(request);
    if (validation.IsFailure)
    {
      return Result<BudgetDto>.Failure(validation.Error ?? "Validation failed.");
    }

    Category? category = await dbContext.Categories.AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == request.CategoryId && x.UserId == userId, cancellationToken);

    if (category is null)
    {
      return Result<BudgetDto>.Failure("Category not found.");
    }

    Budget? budget = await dbContext.Budgets
      .FirstOrDefaultAsync(
        x => x.UserId == userId && x.CategoryId == request.CategoryId && x.Month == request.Month,
        cancellationToken);

    if (budget is null)
    {
      budget = new Budget
      {
        UserId = userId,
        CategoryId = request.CategoryId,
        Month = request.Month,
        PlannedAmount = request.PlannedAmount,
        AlertThresholdPercent = request.AlertThresholdPercent,
        CreatedUtc = DateTime.UtcNow,
        UpdatedUtc = DateTime.UtcNow
      };

      dbContext.Budgets.Add(budget);
    }
    else
    {
      budget.PlannedAmount = request.PlannedAmount;
      budget.AlertThresholdPercent = request.AlertThresholdPercent;
      budget.UpdatedUtc = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    BudgetProgressDto progress = await GetProgressAsync(userId, request.CategoryId, request.Month, cancellationToken);

    return Result<BudgetDto>.Success(new BudgetDto
    {
      Id = budget.Id,
      CategoryId = budget.CategoryId,
      CategoryName = category.Name,
      Month = budget.Month,
      PlannedAmount = budget.PlannedAmount,
      SpentAmount = progress.SpentAmount
    });
  }

  public async Task<Result<BudgetProgressDto>> GetBudgetProgressAsync(
    Guid userId,
    Guid categoryId,
    int year,
    int month,
    CancellationToken cancellationToken = default)
  {
    DateOnly budgetMonth = new(year, month, 1);

    Budget? budget = await dbContext.Budgets
      .AsNoTracking()
      .FirstOrDefaultAsync(
        x => x.UserId == userId && x.CategoryId == categoryId && x.Month == budgetMonth,
        cancellationToken);

    if (budget is null)
    {
      return Result<BudgetProgressDto>.Failure("Budget not found.");
    }

    BudgetProgressDto progress = await GetProgressAsync(userId, categoryId, budgetMonth, cancellationToken, budget.PlannedAmount);
    return Result<BudgetProgressDto>.Success(progress);
  }

  private async Task<BudgetProgressDto> GetProgressAsync(
    Guid userId,
    Guid categoryId,
    DateOnly month,
    CancellationToken cancellationToken,
    decimal? plannedAmountOverride = null)
  {
    DateTime startUtc = new(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    DateTime endUtc = startUtc.AddMonths(1).AddTicks(-1);

    decimal spent = await dbContext.Transactions
      .AsNoTracking()
      .Where(x =>
        x.UserId == userId &&
        x.CategoryId == categoryId &&
        x.Type == TransactionType.Expense &&
        x.DateUtc >= startUtc &&
        x.DateUtc <= endUtc)
      .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

    decimal planned = plannedAmountOverride
      ?? await dbContext.Budgets
        .AsNoTracking()
        .Where(x => x.UserId == userId && x.CategoryId == categoryId && x.Month == month)
        .Select(x => x.PlannedAmount)
        .FirstOrDefaultAsync(cancellationToken);

    DateTime utcNow = DateTime.UtcNow;
    int elapsedDays = Math.Clamp(utcNow.Day, 1, DateTime.DaysInMonth(month.Year, month.Month));
    int totalDays = DateTime.DaysInMonth(month.Year, month.Month);
    decimal pace = elapsedDays > 0 ? spent / elapsedDays : 0m;
    decimal forecast = Math.Round(pace * totalDays, 2);

    return new BudgetProgressDto
    {
      Month = month,
      PlannedAmount = planned,
      SpentAmount = spent,
      RemainingAmount = planned - spent,
      ForecastEndOfMonthSpend = forecast,
      IsLikelyToExceed = planned > 0 && forecast > planned
    };
  }
}
