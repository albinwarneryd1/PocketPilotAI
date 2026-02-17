using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Api.Auth;
using PocketPilotAI.Api.Middleware;
using PocketPilotAI.Api.OpenApi;
using PocketPilotAI.Infrastructure.DependencyInjection;
using PocketPilotAI.Infrastructure.Persistence;
using PocketPilotAI.Infrastructure.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task ApplyDatabaseAsync(WebApplication app)
{
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
