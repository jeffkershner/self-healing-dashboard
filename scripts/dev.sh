#!/usr/bin/env bash
# Runs the app locally in Development mode (fake signed-in user, no Entra needed).
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
exec dotnet run --project src/Dashboard.Server --launch-profile http
