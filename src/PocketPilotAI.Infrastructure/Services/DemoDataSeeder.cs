using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Core.Domain.Enums;
using PocketPilotAI.Infrastructure.Persistence;
using PocketPilotAI.Infrastructure.Security;

namespace PocketPilotAI.Infrastructure.Services;

public interface IDemoDataSeeder
{
  Task SeedAsync(CancellationToken cancellationToken = default);

  Task ResetAndSeedAsync(CancellationToken cancellationToken = default);
}

public class DemoDataSeeder(AppDbContext dbContext) : IDemoDataSeeder
{
  public async Task SeedAsync(CancellationToken cancellationToken = default)
  {
    bool hasDemo = await dbContext.Users.AnyAsync(x => x.IsDemo, cancellationToken);
    if (hasDemo)
    {
      return;
    }

    await SeedInternalAsync(cancellationToken);
  }

  public async Task ResetAndSeedAsync(CancellationToken cancellationToken = default)
  {
    List<User> demoUsers = await dbContext.Users.Where(x => x.IsDemo).ToListAsync(cancellationToken);
    if (demoUsers.Count > 0)
    {
      dbContext.Users.RemoveRange(demoUsers);
      await dbContext.SaveChangesAsync(cancellationToken);
    }

    await SeedInternalAsync(cancellationToken);
  }

  private async Task SeedInternalAsync(CancellationToken cancellationToken)
  {
    DateTime now = DateTime.UtcNow;
    var (passwordHash, salt, iterations) = PasswordHasher.Hash("Demo1234!");

    User user = new()
    {
      Email = "demo@pocketpilot.ai",
      DisplayName = "Demo User",
      PasswordHash = passwordHash,
      PasswordSalt = salt,
      PasswordIterations = iterations,
      IsDemo = true,
      CreatedUtc = now,
      UpdatedUtc = now
    };

    Account account = new()
    {
      UserId = user.Id,
      Name = "Main Account",
      Currency = "SEK",
      Type = AccountType.Checking,
      OpeningBalance = 25000m,
      CurrentBalance = 25000m,
      CreatedUtc = now
    };

    user.Accounts.Add(account);

    Dictionary<string, Category> categories =
      CreateCategories(user.Id);

    List<Merchant> merchants =
    [
      NewMerchant(user.Id, "Fresh Market", categories["Groceries"].Id),
      NewMerchant(user.Id, "City Commute", categories["Transport"].Id),
      NewMerchant(user.Id, "StreamFlix", categories["Subscriptions"].Id),
      NewMerchant(user.Id, "Daily Bites", categories["Eating Out"].Id),
      NewMerchant(user.Id, "PowerGrid", categories["Utilities"].Id),
      NewMerchant(user.Id, "FitLab", categories["Health"].Id),
      NewMerchant(user.Id, "ShopSquare", categories["Shopping"].Id)
    ];

    List<Transaction> transactions = CreateTransactions(user.Id, account.Id, merchants, categories, now);

    decimal income = transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
    decimal expenses = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
    account.CurrentBalance = account.OpeningBalance + income - expenses;

    List<Budget> budgets =
    [
      NewBudget(user.Id, categories["Groceries"].Id, now, 4200m),
      NewBudget(user.Id, categories["Eating Out"].Id, now, 2800m),
      NewBudget(user.Id, categories["Transport"].Id, now, 1800m),
      NewBudget(user.Id, categories["Subscriptions"].Id, now, 900m),
      NewBudget(user.Id, categories["Shopping"].Id, now, 2600m),
      NewBudget(user.Id, categories["Utilities"].Id, now, 2200m)
    ];

    dbContext.Users.Add(user);
    dbContext.Categories.AddRange(categories.Values);
    dbContext.Merchants.AddRange(merchants);
    dbContext.Transactions.AddRange(transactions);
    dbContext.Budgets.AddRange(budgets);

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static Dictionary<string, Category> CreateCategories(Guid userId)
    => new(StringComparer.OrdinalIgnoreCase)
    {
      ["Income"] = NewCategory(userId, "Income", "#22A06B"),
      ["Groceries"] = NewCategory(userId, "Groceries", "#2F855A"),
      ["Eating Out"] = NewCategory(userId, "Eating Out", "#DD6B20"),
      ["Transport"] = NewCategory(userId, "Transport", "#2B6CB0"),
      ["Subscriptions"] = NewCategory(userId, "Subscriptions", "#805AD5"),
      ["Utilities"] = NewCategory(userId, "Utilities", "#4A5568"),
      ["Shopping"] = NewCategory(userId, "Shopping", "#D53F8C"),
      ["Health"] = NewCategory(userId, "Health", "#319795")
    };

  private static List<Transaction> CreateTransactions(
    Guid userId,
    Guid accountId,
    IReadOnlyList<Merchant> merchants,
    IReadOnlyDictionary<string, Category> categories,
    DateTime now)
  {
    List<Transaction> items = [];

    for (int i = 0; i < 3; i++)
    {
      DateTime monthStart = new DateTime(now.Year, now.Month, 1, 8, 0, 0, DateTimeKind.Utc).AddMonths(-i);

      items.Add(new Transaction
      {
        UserId = userId,
        AccountId = accountId,
        Amount = 32000m,
        Currency = "SEK",
        DateUtc = monthStart.AddDays(1),
        Type = TransactionType.Income,
        Source = TransactionSource.Seed,
        CategoryId = categories["Income"].Id,
        Notes = "Salary",
        CreatedUtc = now,
        UpdatedUtc = now
      });

      items.AddRange(CreateRecurringExpense(userId, accountId, merchants, categories["Subscriptions"].Id, "StreamFlix", 179m, monthStart.AddDays(4), now));
      items.AddRange(CreateRecurringExpense(userId, accountId, merchants, categories["Utilities"].Id, "PowerGrid", 1360m, monthStart.AddDays(8), now));
      items.AddRange(CreateRecurringExpense(userId, accountId, merchants, categories["Transport"].Id, "City Commute", 995m, monthStart.AddDays(3), now));

      items.AddRange(CreateVariableExpenses(userId, accountId, merchants, categories, monthStart, i, now));
    }

    return items.OrderBy(x => x.DateUtc).ToList();
  }

  private static IEnumerable<Transaction> CreateVariableExpenses(
    Guid userId,
    Guid accountId,
    IReadOnlyList<Merchant> merchants,
    IReadOnlyDictionary<string, Category> categories,
    DateTime monthStart,
    int monthOffset,
    DateTime now)
  {
    decimal eatingOutBase = monthOffset switch
    {
      0 => 4700m,
      1 => 3400m,
      _ => 2800m
    };

    decimal shoppingBase = monthOffset switch
    {
      0 => 3600m,
      1 => 2400m,
      _ => 1800m
    };

    List<Transaction> rows = [];
    rows.AddRange(SpreadExpenses(userId, accountId, merchants, categories["Groceries"].Id, "Fresh Market", 4100m, monthStart, 4, now));
    rows.AddRange(SpreadExpenses(userId, accountId, merchants, categories["Eating Out"].Id, "Daily Bites", eatingOutBase, monthStart, 6, now));
    rows.AddRange(SpreadExpenses(userId, accountId, merchants, categories["Shopping"].Id, "ShopSquare", shoppingBase, monthStart, 3, now));
    rows.AddRange(SpreadExpenses(userId, accountId, merchants, categories["Health"].Id, "FitLab", 850m, monthStart, 2, now));

    return rows;
  }

  private static IEnumerable<Transaction> SpreadExpenses(
    Guid userId,
    Guid accountId,
    IReadOnlyList<Merchant> merchants,
    Guid categoryId,
    string merchantName,
    decimal total,
    DateTime monthStart,
    int parts,
    DateTime now)
  {
    Merchant? merchant = merchants.FirstOrDefault(x => string.Equals(x.Name, merchantName, StringComparison.OrdinalIgnoreCase));
    decimal each = Math.Round(total / parts, 2);

    for (int i = 0; i < parts; i++)
    {
      yield return new Transaction
      {
        UserId = userId,
        AccountId = accountId,
        MerchantId = merchant?.Id,
        CategoryId = categoryId,
        Amount = each,
        Currency = "SEK",
        DateUtc = monthStart.AddDays(2 + (i * 4)),
        Type = TransactionType.Expense,
        Source = TransactionSource.Seed,
        Notes = "Seeded variable expense",
        CreatedUtc = now,
        UpdatedUtc = now
      };
    }
  }

  private static IEnumerable<Transaction> CreateRecurringExpense(
    Guid userId,
    Guid accountId,
    IReadOnlyList<Merchant> merchants,
    Guid categoryId,
    string merchantName,
    decimal amount,
    DateTime dateUtc,
    DateTime now)
  {
    Merchant? merchant = merchants.FirstOrDefault(x => string.Equals(x.Name, merchantName, StringComparison.OrdinalIgnoreCase));

    yield return new Transaction
    {
      UserId = userId,
      AccountId = accountId,
      MerchantId = merchant?.Id,
      CategoryId = categoryId,
      Amount = amount,
      Currency = "SEK",
      DateUtc = dateUtc,
      Type = TransactionType.Expense,
      Source = TransactionSource.Seed,
      Notes = "Recurring seeded expense",
      IsRecurring = true,
      CreatedUtc = now,
      UpdatedUtc = now
    };
  }

  private static Category NewCategory(Guid userId, string name, string color)
    => new()
    {
      UserId = userId,
      Name = name,
      ColorHex = color,
      IsSystem = false
    };

  private static Merchant NewMerchant(Guid userId, string name, Guid categoryId)
    => new()
    {
      UserId = userId,
      Name = name,
      NormalizedName = name.Trim().ToLowerInvariant(),
      DefaultCategoryId = categoryId,
      CreatedUtc = DateTime.UtcNow
    };

  private static Budget NewBudget(Guid userId, Guid categoryId, DateTime now, decimal planned)
    => new()
    {
      UserId = userId,
      CategoryId = categoryId,
      Month = new DateOnly(now.Year, now.Month, 1),
      PlannedAmount = planned,
      AlertThresholdPercent = 80,
      CreatedUtc = DateTime.UtcNow,
      UpdatedUtc = DateTime.UtcNow
    };
}
