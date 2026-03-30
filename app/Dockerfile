FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ENV NPM_CONFIG_AUDIT=false \
    NPM_CONFIG_FUND=false
RUN apt-get update -y
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
RUN apt-get install nodejs -yq

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore FortniteReplayAnalyzer.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish FortniteReplayAnalyzer.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FortniteReplayAnalyzer.dll"]
