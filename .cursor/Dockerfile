# Development environment for the Fortnite Replay Analyzer.
# Provides the .NET 10 SDK (ASP.NET Core web app + CLI) and Node.js 22
# (React ClientApp built with react-scripts), matching the versions pinned
# by the project (net10.0) and CI (.github/workflows/ci.yml uses node 22).
FROM mcr.microsoft.com/dotnet/sdk:10.0

ENV DEBIAN_FRONTEND=noninteractive \
    NPM_CONFIG_AUDIT=false \
    NPM_CONFIG_FUND=false \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

# System packages, Node.js 22 (NodeSource), and a non-root "ubuntu" user.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl git ca-certificates gnupg sudo \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --shell /bin/bash --uid 1000 ubuntu \
    && echo 'ubuntu ALL=(ALL) NOPASSWD:ALL' > /etc/sudoers.d/ubuntu \
    && chmod 0440 /etc/sudoers.d/ubuntu

USER ubuntu
