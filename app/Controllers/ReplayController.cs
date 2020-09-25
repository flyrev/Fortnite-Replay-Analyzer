using FortniteReplayAnalyzer.Controllers.ExternalApis;
using FortniteReplayAnalyzer.Data;
using FortniteReplayAnalyzer.ExternalApis;
using FortniteReplayReader;
using FortniteReplayReader.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Threading.Tasks;

namespace FortniteReplayAnalyzer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReplayController : ControllerBase
    {
        private readonly ILogger<ReplayController> logger;
        private readonly FortniteIoApiClient apiClient;
        private readonly ReplayAnalysisStorage replayStorage;

        public ReplayController(ILogger<ReplayController> logger, FortniteIoApiClient apiClient, ReplayAnalysisStorage replayStorage)
        {
            this.logger = logger;
            this.apiClient = apiClient;
            this.replayStorage = replayStorage;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromForm] IFormFile replay)
        {
            var reader = new ReplayReader();
            var parsedReplay = reader.ReadReplay(replay.OpenReadStream());
            FortniteGame replayInfo = await RetrievePlayerNamesAndParse(parsedReplay);

            var guid = parsedReplay.Header.Guid;

            var gameAnalysis = new FortniteGameAnalysis
            {
                Successful = true,
                AnalysisUrl = $"/view/{guid}",
                RawData = $"/replay/{guid}",
                Game = replayInfo
            };

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var json = JsonConvert.SerializeObject(gameAnalysis, serializerSettings);
            _ = replayStorage.UploadJson(guid, json);

            return Ok(json);
        }

        private async Task<FortniteGame> RetrievePlayerNamesAndParse(FortniteReplay parsedReplay)
        {

            var players = parsedReplay.PlayerData.Where(playerData => IsRealPlayer(playerData)).Select(playerData => playerData.EpicId).ToList();
            var playerNamesFromApi = await apiClient.GetDisplayNamesFromEpicIds(players);
            logger.LogInformation($"Retrieved player {playerNamesFromApi.Count()} names.");
            var replayInfo = new FortniteReplayAnalyzer().Analyze(parsedReplay, playerNamesFromApi);
            return replayInfo;
        }

        private static bool IsBot(PlayerData playerData)
        {
            return playerData.IsBot.GetValueOrDefault(false);
        }

        private static bool IsRealPlayer(PlayerData playerData)
        {
            return !IsBot(playerData);
        }

        [HttpGet]
        [Route("{gameId}")]
        public async Task<IActionResult> GetAsync(string gameId)
        {
            var json = await replayStorage.ReadJsonDataAsync(gameId);
            return Ok(json);
        }

    }
}
