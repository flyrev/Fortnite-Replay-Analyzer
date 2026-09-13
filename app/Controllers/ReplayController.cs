using FortniteReplayAnalyzer.Controllers.ExternalApis;
using FortniteReplayAnalyzer.Data;
using FortniteReplayAnalyzer.ExternalApis;
using FortniteReplayAnalyzer.ReplayProcessing;
using FortniteReplayReader;
using FortniteReplayReader.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
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
        private readonly IReplayAnalysisStorage replayStorage;

        public ReplayController(ILogger<ReplayController> logger, FortniteIoApiClient apiClient, IReplayAnalysisStorage replayStorage)
        {
            this.logger = logger;
            this.apiClient = apiClient;
            this.replayStorage = replayStorage;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromForm] IFormFile replay)
        {
            if (replay == null || replay.Length == 0)
            {
                return BadRequest(new { error = "No replay file was uploaded. Please choose a Fortnite .replay file." });
            }

            FortniteReplay parsedReplay;
            try
            {
                var reader = new OodleReplayReader(logger);
                parsedReplay = reader.ReadReplay(replay.OpenReadStream());
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read uploaded replay '{FileName}' ({Length} bytes).", replay.FileName, replay.Length);
                return BadRequest(new { error = "This file could not be read as a Fortnite replay. It may be corrupted, incomplete (still recording), or from an unsupported game version." });
            }

            FortniteGame replayInfo = await RetrievePlayerNamesAndParse(parsedReplay);

            var guid = parsedReplay.Header?.Guid;

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var gameAnalysis = new FortniteGameAnalysis
            {
                Successful = true,
                AnalysisUrl = string.IsNullOrEmpty(guid) ? null : $"/view/{guid}",
                Game = replayInfo
            };

            // Persisting the analysis only powers the shareable "/view/{guid}" link. If storage
            // is unconfigured or unavailable, still return the analysis instead of failing (500).
            if (!string.IsNullOrEmpty(guid))
            {
                try
                {
                    var storedJson = JsonConvert.SerializeObject(gameAnalysis, serializerSettings);
                    await replayStorage.UploadJson(guid, storedJson);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to store replay analysis for {Guid}; returning analysis without a shareable link.", guid);
                    gameAnalysis.AnalysisUrl = null;
                }
            }

            var json = JsonConvert.SerializeObject(gameAnalysis, serializerSettings);
            return Content(json, "application/json");
        }

        private async Task<FortniteGame> RetrievePlayerNamesAndParse(FortniteReplay parsedReplay)
        {

            var players = (parsedReplay.PlayerData ?? Enumerable.Empty<PlayerData>())
                .Where(IsRealPlayer)
                .Select(playerData => playerData.EpicId)
                .Where(epicId => !string.IsNullOrWhiteSpace(epicId))
                .Distinct()
                .ToList();

            Dictionary<string, string> playerNamesFromApi;
            try
            {
                playerNamesFromApi = await apiClient.GetDisplayNamesFromEpicIds(players);
                logger.LogInformation($"Retrieved player {playerNamesFromApi.Count()} names.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to retrieve player display names; continuing without them.");
                playerNamesFromApi = new Dictionary<string, string>();
            }

            var replayInfo = new FortniteReplayAnalyzer().Analyze(parsedReplay, playerNamesFromApi);
            return replayInfo;
        }

        private static bool IsBot(PlayerData playerData)
        {
            return playerData.IsBot;
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
            return Content(json, "application/json");
        }

    }
}
