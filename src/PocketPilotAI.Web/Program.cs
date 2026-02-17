using PocketPilotAI.Web.Components;
using PocketPilotAI.Web.Services;
using PocketPilotAI.Web.State;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents();

string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7174";

builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddScoped<TransactionsApi>();
builder.Services.AddScoped<BudgetsApi>();
builder.Services.AddScoped<InsightsApi>();
builder.Services.AddScoped<UserSessionState>();
builder.Services.AddScoped<UiState>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/not-found", createScopeForErrors: true);
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

app.Run();
