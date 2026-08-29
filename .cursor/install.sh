#!/usr/bin/env bash
# Idempotent Cloud Agent bootstrap for the Fortnite Replay Analyzer.
# Runs from the repository root after checkout.
set -euo pipefail

export NPM_CONFIG_AUDIT=false
export NPM_CONFIG_FUND=false
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Install the .NET 10 SDK (ASP.NET Core web app + CLI target net10.0).
# Ubuntu 24.04 ships dotnet-sdk-10.0 in its feed; apt is a no-op if present.
if ! command -v dotnet >/dev/null 2>&1; then
  sudo apt-get update
  sudo apt-get install -y --no-install-recommends dotnet-sdk-10.0
fi

# React ClientApp dependencies (react-scripts, node 22 per CI).
npm --prefix app/ClientApp ci

# Restore .NET dependencies for the web app and the CLI.
dotnet restore app/FortniteReplayAnalyzer.csproj
dotnet restore cli/cli.csproj
