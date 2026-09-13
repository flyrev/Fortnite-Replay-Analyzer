using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FortniteReplayAnalyzer.ExternalApis
{
    public class AzureBlobReplayAnalysisStorage : IReplayAnalysisStorage
    {
        private readonly ILogger<AzureBlobReplayAnalysisStorage> logger;
        private readonly Uri accountUri;
        private readonly string containerName;
        private readonly Task<BlobContainerClient> container;

        public AzureBlobReplayAnalysisStorage(ILogger<AzureBlobReplayAnalysisStorage> logger, Uri accountUri, string containerName)
        {
            this.logger = logger;
            this.accountUri = accountUri;
            this.containerName = containerName;

            if (accountUri == null || string.IsNullOrWhiteSpace(containerName))
            {
                logger.LogWarning("Azure Blob storage is not configured; replay analyses will not be persisted.");
                container = null;
            }
            else
            {
                logger.LogInformation($"Using Azure Blob storage container {containerName} at {accountUri}");
                container = GetOrCreateContainerAsync();
            }
        }

        public async Task UploadJson(string guid, string json)
        {
            var blob = await BlobForAsync(guid);
            if (blob == null)
            {
                return;
            }

            await blob.UploadAsync(BinaryData.FromString(json), overwrite: true);
        }

        public async Task<string> ReadJsonDataAsync(string guid)
        {
            var blob = await BlobForAsync(guid);
            if (blob == null || !await blob.ExistsAsync())
            {
                return "{}";
            }

            var download = await blob.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        private async Task<BlobClient> BlobForAsync(string guid)
        {
            var client = container == null ? null : await container;
            return client?.GetBlobClient(guid);
        }

        private async Task<BlobContainerClient> GetOrCreateContainerAsync()
        {
            var service = new BlobServiceClient(accountUri, new DefaultAzureCredential());
            var client = service.GetBlobContainerClient(containerName);
            await client.CreateIfNotExistsAsync();
            return client;
        }
    }
}