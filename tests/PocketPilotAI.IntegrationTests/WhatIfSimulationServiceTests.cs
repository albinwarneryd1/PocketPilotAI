using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PocketPilotAI.Core.Application.Dtos.Ai;
using PocketPilotAI.Core.Domain.Entities;
using PocketPilotAI.Core.Domain.Enums;
using PocketPilotAI.Infrastructure.Ai;
using PocketPilotAI.Infrastructure.Persistence;
using PocketPilotAI.Infrastructure.Services;

namespace PocketPilotAI.IntegrationTests;

public class WhatIfSimulationServiceTests
{
  [Fact]
  public async Task RunWhatIfSimulation_ShouldReturnDeltaAndRecommendations()
  {
    await using AppDbContext db = CreateDbContext();

    User user = new()
    {
      Email = "sim@test.com",
      DisplayName = "Sim",
      PasswordHash = "hash",
      PasswordSalt = "salt",
      PasswordIterations = 120000
    };

    Account account = new()
    {
      UserId = user.Id,
      Name = "Main",
      Currency = "SEK",
      Type = AccountType.Checking,
      OpeningBalance = 10000,
      CurrentBalance = 10000
    };

    Category eatingOut = new() { UserId = user.Id, Name = "Eating Out", ColorHex = "#DD6B20" };
    Category incomeCategory = new() { UserId = user.Id, Name = "Income", ColorHex = "#22A06B" };

    DateTime monthDate = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 10, 0, 0, 0, DateTimeKind.Utc);

    Transaction salary = new()
    {
      UserId = user.Id,
      AccountId = account.Id,
      CategoryId = incomeCategory.Id,
      Amount = 30000,
      Type = TransactionType.Income,
      Source = TransactionSource.Manual,
      DateUtc = monthDate,
      Currency = "SEK"
    };

    Transaction food = new()
    {
      UserId = user.Id,
      AccountId = account.Id,
      CategoryId = eatingOut.Id,
      Amount = 4000,
      Type = TransactionType.Expense,
      Source = TransactionSource.Manual,
      DateUtc = monthDate,
      Currency = "SEK",
      IsRecurring = true
    };

    db.Users.Add(user);
    db.Accounts.Add(account);
    db.Categories.AddRange(eatingOut, incomeCategory);
    db.Transactions.AddRange(salary, food);
    await db.SaveChangesAsync();

    IConfiguration config = new ConfigurationBuilder().Build();
    OpenAiClient openAiClient = new(new HttpClient(), config, NullLogger<OpenAiClient>.Instance);
    AiInsightsService service = new(db, openAiClient);

    var result = await service.RunWhatIfSimulationAsync(user.Id, new WhatIfSimulationRequest
    {
      ScenarioName = "Cut dining",
      Year = DateTime.UtcNow.Year,
      Month = DateTime.UtcNow.Month,
      Actions =
      [
        new WhatIfActionDto
        {
          Type = WhatIfActionType.ReduceCategoryPercent,
          Label = "Reduce dining 20%",
          CategoryName = "Eating Out",
          Value = 20
        }
      ]
    });

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.True(result.Value!.Delta.NetDelta > 0);
    Assert.Equal(3, result.Value.Recommendations.Count);
    Assert.NotEmpty(result.Value.Explanation);
  }

  private static AppDbContext CreateDbContext()
  {
    DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"whatif-flow-{Guid.NewGuid()}")
      .Options;

    return new AppDbContext(options);
  }
}
