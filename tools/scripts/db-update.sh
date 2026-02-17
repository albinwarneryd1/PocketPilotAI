#!/usr/bin/env bash
set -euo pipefail

dotnet ef database update --project src/PocketPilotAI.Infrastructure --startup-project src/PocketPilotAI.Api
