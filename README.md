# PocketPilotAI

PocketPilotAI is a production-oriented personal finance platform with shared domain logic, an ASP.NET Core API, a Blazor web client, and a .NET MAUI app.

## Projects

- `src/PocketPilotAI.Core`: domain, DTOs, interfaces, validation
- `src/PocketPilotAI.Infrastructure`: EF Core, auth/session services, seed data, AI/import services
- `src/PocketPilotAI.Api`: API endpoints, JWT auth, middleware, Swagger
- `src/PocketPilotAI.Web`: Blazor dashboard + auth + what-if simulation UI
- `src/PocketPilotAI.App`: MAUI app with auth flow, secure token storage, and what-if simulation UI
- `tests/*`: unit, integration, and API tests

## Quick Start

1. Ensure SQL Server is reachable via `ConnectionStrings:DefaultConnection`.
2. Set env vars from `.env.example` (especially JWT key).
3. Run API:
   - `dotnet run --project src/PocketPilotAI.Api`
4. Run web:
   - `dotnet run --project src/PocketPilotAI.Web`
5. Run MAUI (Mac Catalyst):
   - `dotnet build src/PocketPilotAI.App/PocketPilotAI.App.csproj -f net10.0-maccatalyst`

## Demo Seed

- API applies migrations automatically on startup.
- Demo seeding runs by default in development (`DemoSeed:Enabled=true`).
- Demo user:
  - Email: `demo@pocketpilot.ai`
  - Password: `Demo1234!`

## Key API Endpoints

- Auth:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `POST /api/auth/refresh`
  - `POST /api/auth/logout`
  - `POST /api/auth/logout-all`
  - `GET /api/auth/me`
- Insights:
  - `POST /api/insights/leaks`
  - `POST /api/insights/monthly-summary`
  - `GET /api/insights/what-if/templates`
  - `POST /api/insights/what-if/simulate`
