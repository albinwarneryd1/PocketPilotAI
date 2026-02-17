using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Infrastructure.Ai;
using PocketPilotAI.Infrastructure.Persistence;
using PocketPilotAI.Infrastructure.Services;

namespace PocketPilotAI.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    string? connectionString = configuration.GetConnectionString("DefaultConnection")
      ?? configuration["POCKETPILOTAI_CONNECTION"];
    string provider = (configuration["Database:Provider"] ?? "sqlserver").Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(connectionString))
    {
      throw new InvalidOperationException("Missing connection string. Configure ConnectionStrings:DefaultConnection or POCKETPILOTAI_CONNECTION.");
    }

    services.AddDbContext<AppDbContext>(options =>
    {
      if (provider == "sqlite")
      {
        options.UseSqlite(connectionString);
        return;
      }

      if (provider == "sqlserver")
      {
        options.UseSqlServer(connectionString);
        return;
      }

      throw new InvalidOperationException("Unsupported database provider. Use Database:Provider = sqlserver or sqlite.");
    });

    services.AddHttpClient<OpenAiClient>();

    services.AddScoped<ITransactionService, TransactionService>();
    services.AddScoped<IBudgetService, BudgetService>();
    services.AddScoped<IImportService, ImportService>();
    services.AddScoped<IAiInsightsService, AiInsightsService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

    return services;
  }
}
