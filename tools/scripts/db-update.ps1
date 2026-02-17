$ErrorActionPreference = "Stop"
dotnet ef database update --project src/PocketPilotAI.Infrastructure --startup-project src/PocketPilotAI.Api
