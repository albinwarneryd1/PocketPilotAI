using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using PocketPilotAI.Api.Auth;
using PocketPilotAI.Api.Middleware;
using PocketPilotAI.Api.OpenApi;
using PocketPilotAI.Infrastructure.DependencyInjection;
using PocketPilotAI.Infrastructure.Persistence;
using PocketPilotAI.Infrastructure.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

int authPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit")
  ?? (builder.Environment.IsDevelopment() ? 120 : 8);
int authQueueLimit = builder.Configuration.GetValue<int?>("RateLimiting:Auth:QueueLimit")
  ?? (builder.Environment.IsDevelopment() ? 20 : 2);
int authWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:Auth:WindowSeconds") ?? 60;

builder.Services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
  options.AddFixedWindowLimiter("auth-login", policy =>
  {
    policy.PermitLimit = authPermitLimit;
    policy.Window = TimeSpan.FromSeconds(authWindowSeconds);
    policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    policy.QueueLimit = authQueueLimit;
  });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddPocketPilotSwagger();

WebApplication app = builder.Build();

await ApplyDatabaseAsync(app);

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task ApplyDatabaseAsync(WebApplication app)
{
  bool applyMigrationsOnStartup = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);
  if (!applyMigrationsOnStartup)
  {
    return;
  }

  using IServiceScope scope = app.Services.CreateScope();
  AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

  await db.Database.MigrateAsync();

  bool shouldSeed = app.Configuration.GetValue("DemoSeed:Enabled", app.Environment.IsDevelopment());
  if (shouldSeed)
  {
    IDemoDataSeeder seeder = scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
    await seeder.SeedAsync();
  }
}

public partial class Program
{
}
