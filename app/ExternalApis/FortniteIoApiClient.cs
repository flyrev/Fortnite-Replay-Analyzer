using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FortniteReplayAnalyzer.Controllers.ExternalApis
{
    public class EpicIdNamePair
    {
        public string EpicId;
        public string Name;
    }
    public class FortniteIoApiClient
    {
        private readonly ILogger<FortniteIoApiClient> logger;
        private readonly string apiKey;
        private readonly string baseUrl;

        public FortniteIoApiClient(ILogger<FortniteIoApiClient> logger, string baseUrl, string apiKey)
        {
            this.logger = logger;
            this.baseUrl = baseUrl;
            this.apiKey = apiKey;
        }

        public async Task<Dictionary<string, string>> GetDisplayNamesFromEpicIds(IEnumerable<string> epicIds)
        {
            if (apiKey == null)
            {
                logger.LogWarning("Fortnite.io API key is not set. Cannot retrieve player names.");
                return new Dictionary<string, string>();
            }

            if (baseUrl == null)
            {
                logger.LogWarning("Fortnite.io API base URL is not set. Cannot retrieve player names.");
                return new Dictionary<string, string>();
            }

            return await GetLookupTableFromApi(epicIds);
        }

        private async Task<Dictionary<string, string>> GetLookupTableFromApi(IEnumerable<string> epicIds)
        {
            var url = CreateFortniteIoApiUrl(epicIds);
            logger.LogInformation($"Retrieving player names from {url}");
            return await CreateLookupTableFromApi(url);
        }

        private async Task<Dictionary<string, string>> CreateLookupTableFromApi(string url)
        {
            using var client = CreateFortniteIoApiClient();
            var response = await client.GetAsync(url);
            var jsonResponseRaw = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(jsonResponseRaw);
            var accounts = jsonObject.SelectToken("accounts").Value<JArray>();
            if (accounts == null)
            {
                logger.LogWarning("Result set did not include an \"accounts\" field");
                return new Dictionary<string, string>();
            }
            logger.LogInformation($"Retrieved {accounts.Count} accounts from FortniteApi.io");
            return CreateLookupTableFromAccounts(accounts);
        }

        private HttpClient CreateFortniteIoApiClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", apiKey);
            return client;
        }

        private static Dictionary<string, string> CreateLookupTableFromAccounts(JArray accounts)
        {
            var epicIdsAndNames = accounts.Select(element => new EpicIdNamePair
            {
                EpicId = element.SelectToken("id").Value<string>().ToUpper(),
                Name = element.SelectToken("username").Value<string>(),
            });

            return epicIdsAndNames.ToDictionary(item => item.EpicId, item => item.Name);
        }

        private string CreateFortniteIoApiUrl(IEnumerable<string> epicIds)
        {
            var epicIdsAsUrlParameter = string.Join(",", epicIds).ToLower();
            return $"{baseUrl}/v1/lookupUsername?id={epicIdsAsUrlParameter}";
        }
    }
}
