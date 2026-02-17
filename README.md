# PocketPilotAI

[![CI](https://github.com/albinwarneryd1/PocketPilotAI/actions/workflows/ci.yml/badge.svg)](https://github.com/albinwarneryd1/PocketPilotAI/actions/workflows/ci.yml)
[![License: TBD](https://img.shields.io/badge/license-TBD-lightgrey.svg)](#license)

PocketPilotAI is a cross-platform personal finance application that helps users understand spending behavior and improve monthly cash flow using actionable AI insights.

## Quick Start

```bash
# 1) Clone
git clone https://github.com/albinwarneryd1/PocketPilotAI.git
cd PocketPilotAI

# 2) Restore
dotnet restore PocketPilotAI.sln

# 3) Set env vars (or user-secrets/appsettings)
cp .env.example .env

# 4) Run API (applies migrations on startup by default)
dotnet run --project src/PocketPilotAI.Api --urls https://localhost:7174

# 5) Run MAUI (MacCatalyst)
export POCKETPILOTAI_API_BASE_URL="https://localhost:7174"
dotnet build src/PocketPilotAI.App/PocketPilotAI.App.csproj -f net10.0-maccatalyst
open src/PocketPilotAI.App/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/PocketPilotAI.app

# 6) Run Web client
dotnet run --project src/PocketPilotAI.Web
```

If `dotnet run` fails with `PocketPilotAI.App.app ... no such file`, use the `open .../PocketPilotAI.app` command above.

## 1. Project Overview

PocketPilotAI solves a common problem: people track expenses, but still struggle to know what to change.

It combines traditional finance tracking with AI-assisted analysis to answer:

- Where are my spending leaks?
- What small changes can I make this month?
- How would a scenario impact my net cash flow?

### Key Features

- Transaction tracking (income/expense) with categories and merchants
- Budget tracking and monthly spend visibility
- AI leak detection based on user transaction data
- Monthly summary insights
- What-if simulation engine for scenario-based KPI recalculation
- JWT + refresh-token authentication across Web and MAUI
- Demo seed data for fast development/testing

### Why AI in PocketPilotAI

AI is used to convert transaction patterns into practical recommendations. Core financial calculations remain deterministic in the backend; AI adds explanation quality and suggestion framing.

## 2. Architecture Overview

PocketPilotAI follows a layered architecture with API-first boundaries and shared business logic.

- **Core**: domain entities, DTOs, contracts, and application interfaces
- **Infrastructure**: EF Core persistence, auth implementation, AI integration, seeding
- **API**: HTTP surface, authentication, middleware, endpoint orchestration
- **Clients (Web + MAUI)**: presentation, session state, and API consumption

### Responsibility Split

- Business rules and calculations belong in Core/Infrastructure services.
- API controllers stay thin and coordinate request/response.
- Clients should not contain domain logic.

## 3. Project Structure

```text
PocketPilotAI/
├─ src/
│  ├─ PocketPilotAI.Core/            # Domain + application contracts
│  ├─ PocketPilotAI.Infrastructure/  # EF Core + service implementations + AI client
│  ├─ PocketPilotAI.Api/             # ASP.NET Core API, auth, middleware, endpoints
│  ├─ PocketPilotAI.Web/             # Blazor web client
│  ├─ PocketPilotAI.App/             # .NET MAUI client
│  └─ PocketPilotAI.Contracts/       # Shared API contract models
├─ tests/
│  ├─ PocketPilotAI.UnitTests/
│  ├─ PocketPilotAI.IntegrationTests/
│  └─ PocketPilotAI.ApiTests/
├─ tools/scripts/                    # DB helper scripts
├─ docs/                             # Architecture/API/docs
└─ .github/workflows/ci.yml          # CI pipeline
```

## 4. Tech Stack

### Backend

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core
- JWT Bearer authentication
- Refresh token rotation + reuse detection

### Frontend

- Blazor Web (interactive server components)
- .NET MAUI (Android, iOS, MacCatalyst, Windows)

### Database

- SQL Server (primary runtime target)
- EF Core migrations for schema evolution
- SQLite used in API test environment

### AI Components

- OpenAI Responses API integration (`gpt-4.1-mini` default)
- Prompt-template based leak and summary generation
- Deterministic what-if simulation and KPI delta calculation

## 5. Getting Started (Step-by-Step)

### Prerequisites

- .NET SDK `10.0.103` (see `global.json`)
- SQL Server instance (local/container/remote)
- Optional: .NET MAUI workloads for app development
- Optional: `dotnet-ef` tool for manual migration commands

Install EF tool if needed:

```bash
dotnet tool install --global dotnet-ef
```

### 1) Clone

```bash
git clone https://github.com/albinwarneryd1/PocketPilotAI.git
cd PocketPilotAI
```

### 2) Restore packages

```bash
dotnet restore PocketPilotAI.sln
```

### 3) Configure connection string + secrets

Set configuration via environment variables or `appsettings.Development.json`.

Minimum required values:

- `POCKETPILOTAI_CONNECTION`
- `POCKETPILOTAI_JWT_KEY`

Optional (AI):

- `OPENAI_API_KEY`

See `.env.example` for available keys.

### 4) Apply EF migrations

The API applies migrations on startup by default.

Manual option:

```bash
# macOS/Linux
./tools/scripts/db-update.sh

# PowerShell
./tools/scripts/db-update.ps1
```

### 5) Seed demo data

Default behavior:

- `DemoSeed:Enabled=true` in development triggers seed on API startup.

Optional dev endpoints (requires authenticated user, Development environment):

- `POST /api/dev/seed/apply`
- `POST /api/dev/seed/reset`

### 6) Run API

```bash
dotnet run --project src/PocketPilotAI.Api --urls https://localhost:7174
```

### 7) Run MAUI client (MacCatalyst)

```bash
export POCKETPILOTAI_API_BASE_URL="https://localhost:7174"
dotnet build src/PocketPilotAI.App/PocketPilotAI.App.csproj -f net10.0-maccatalyst
open src/PocketPilotAI.App/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/PocketPilotAI.app
```

MAUI API base URL is configurable through:

- `POCKETPILOTAI_API_BASE_URL`

Known issue (current tooling):

- In some .NET 10 + MacCatalyst setups, `dotnet run` tries to open `PocketPilotAI.App.app` while the built bundle is `PocketPilotAI.app`.
- Use the `open .../PocketPilotAI.app` command as a reliable workaround.

### 8) Run Web client

```bash
dotnet run --project src/PocketPilotAI.Web
```

Default API base URL in Web: `https://localhost:7174` (configurable via `ApiBaseUrl`).

## 6. Configuration

### App settings

Main API config lives in:

- `src/PocketPilotAI.Api/appsettings.json`
- `src/PocketPilotAI.Api/appsettings.Development.json`

Key sections:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpirationMinutes`
- `Ai:Model`
- `DemoSeed:Enabled`
- `Database:ApplyMigrationsOnStartup`

### Environment variables

Supported runtime variables include:

- `POCKETPILOTAI_CONNECTION`
- `POCKETPILOTAI_JWT_ISSUER`
- `POCKETPILOTAI_JWT_AUDIENCE`
- `POCKETPILOTAI_JWT_KEY`
- `OPENAI_API_KEY`
- `DEMOSEED__ENABLED`
- `POCKETPILOTAI_API_BASE_URL` (MAUI)

### Secrets handling

- Use environment variables or `dotnet user-secrets` for local secrets.
- Do not commit real secrets to git.
- Keep production secrets in secure secret stores (for example cloud key vaults).

## 7. Authentication Flow

- User registers or logs in via `POST /api/auth/register` or `POST /api/auth/login`.
- API returns:
  - short-lived **access token** (JWT)
  - long-lived **refresh token**
- Access token is sent in `Authorization: Bearer <token>`.
- On `401`, clients call `POST /api/auth/refresh` and rotate tokens.
- Refresh token reuse is detected server-side; on detected reuse, active sessions are revoked.

### Token storage by client

- **Web**: session state in-memory (`UserSessionState`) with automatic refresh handling.
- **MAUI**: tokens persisted using `SecureStorage` (`UserSessionService`).

## 8. AI Features

### Leak Detection

`POST /api/insights/leaks`

- Compares current vs previous period category spending
- Returns prioritized leak cards with estimated savings and concrete actions

### Monthly Summary

`POST /api/insights/monthly-summary`

- Summarizes income/expense balance and spending profile

### What-if Simulation

`GET /api/insights/what-if/templates`
`POST /api/insights/what-if/simulate`

- Applies scenario actions (reduce category %, fixed reduction, recurring income/expense, one-off, remove subscriptions)
- Recalculates KPIs and returns baseline/simulated deltas + recommendations

### Deterministic vs AI-generated

- KPI math, scenario effects, and deltas are deterministic backend logic.
- AI is used for narrative insight generation when `OPENAI_API_KEY` is configured.
- If AI is unavailable, fallback logic still returns usable insights.

## 9. Development Notes

### Migrations workflow

Create migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/PocketPilotAI.Infrastructure \
  --startup-project src/PocketPilotAI.Api
```

Apply migration:

```bash
dotnet ef database update \
  --project src/PocketPilotAI.Infrastructure \
  --startup-project src/PocketPilotAI.Api
```

### Safe feature development

- Add/modify domain models in `Core` first.
- Implement behavior in `Infrastructure` services.
- Expose through API controllers.
- Consume via typed API clients in Web/MAUI.
- Add tests in `UnitTests`, `IntegrationTests`, or `ApiTests` based on scope.

### Common pitfalls

- Putting business logic in controllers or UI layers
- Forgetting migration updates after model changes
- Hardcoding secrets in config files
- Using AI output as source-of-truth for calculations

## 10. Roadmap

- Improve recommendation ranking and confidence scoring
- Expand analytics dashboards and trend visualizations
- CSV/export pipeline improvements (reports, statement exports)
- Advanced multi-action scenario builder with saved simulations

## 11. Screenshots

Screenshots can be added in `docs/screenshots/`.

Suggested assets:

- Overview dashboard
- Transactions flow
- AI insights cards
- What-if simulation (Web + MAUI)

## 12. License

License is currently **TBD**.

Add your preferred license file (for example MIT) and update this section.
