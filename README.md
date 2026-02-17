# PocketPilotAI

PocketPilotAI is a production-oriented personal finance platform with a shared domain core, API backend, MAUI app, and Blazor web client.

## Projects

- `src/PocketPilotAI.Core`: domain, DTOs, interfaces, validation
- `src/PocketPilotAI.Infrastructure`: EF Core, service implementations, AI/import providers
- `src/PocketPilotAI.Api`: ASP.NET Core API with auth and Swagger
- `src/PocketPilotAI.Web`: Blazor web frontend
- `src/PocketPilotAI.App`: .NET MAUI frontend
- `tests/*`: unit, integration, and API tests

## Quick Start

1. Install .NET SDK listed in `global.json`.
2. Copy `.env.example` into your local environment variables.
3. Run `dotnet restore PocketPilotAI.sln`.
4. Start API: `dotnet run --project src/PocketPilotAI.Api`.
