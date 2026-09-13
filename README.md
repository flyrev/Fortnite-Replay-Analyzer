# Fortnite-Replay-Analyzer
The Ultimate Fortnite Replay Analyzer. Hosted at https://frenzy-farm-app.azurewebsites.net

## Running locally

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Node.js 20+.

```sh
cd app
dotnet run
```

Then open `https://localhost:5001` (the exact URL is printed in the console when the app starts). The first build installs the React dependencies (`npm ci`) and compiles the client; subsequent runs are faster.

### Optional: storage

Replay analyses are persisted in the cloud so they can be shared by link. Unless configured, local runs simply don't persist anything.

- **Azure Blob** (used by the hosted app): set `AZURE_STORAGE_ACCOUNT_URI` and `AZURE_STORAGE_CONTAINER`. Authentication uses Azure managed identity / `DefaultAzureCredential`.
- **AWS S3**: set `AWS_S3_ACCESS_KEY`, `AWS_S3_ACCESS_SECRET` and `AWS_S3_BUCKET`.

Either provider works when its environment variables are present. If both are set, Azure takes precedence.