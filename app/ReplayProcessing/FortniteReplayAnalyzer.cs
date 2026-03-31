using FortniteReplayAnalyzer.Data;
using FortniteReplayReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FortniteReplayAnalyzer
{
    public class FortniteReplayAnalyzer
    {
        public FortniteGame Analyze(FortniteReplay replay, Dictionary<string, string> displayNameFromEpicId)
        {
            displayNameFromEpicId ??= new Dictionary<string, string>();

            var realPlayers = replay.PlayerData?
                .Where(player => !string.IsNullOrWhiteSpace(player.EpicId))
                .GroupBy(player => player.EpicId!)
                .Select(group => group.First())
                .ToList()
                ?? new List<PlayerData>();
            var platformFromEpicId = realPlayers
                .Where(player => !string.IsNullOrWhiteSpace(player.EpicId))
                .GroupBy(player => player.EpicId!)
                .ToDictionary(group => group.Key, group => group.First().Platform);
            var platformStatistics = realPlayers
                .Where(player => !string.IsNullOrWhiteSpace(player.Platform))
                .GroupBy(player => player.Platform!)
                .ToDictionary(group => group.Key, group => group.Count());

            var eliminations = replay.Eliminations?
                .Where(elimination => !elimination.Knocked)
                .Where(elimination => elimination.Eliminator != elimination.Eliminated)
                .OrderBy(elimination => elimination.Info.StartTime)
                .Select(elimination => new FortniteElimination
                {
                    EliminatedBy = new FortnitePlayer
                    {
                        Name = displayNameFromEpicId.GetValueOrDefault(elimination.Eliminator, "Unknown player (" + elimination.Eliminator + ")"),
                        EpicId = elimination.Eliminator,
                        Platform = platformFromEpicId.GetValueOrDefault(elimination.Eliminator)
                    },
                    Eliminated = new FortnitePlayer
                    {
                        Name = displayNameFromEpicId.GetValueOrDefault(elimination.Eliminated, "Unknown player (" + elimination.Eliminated + ")"),
                        EpicId = elimination.Eliminated,
                        Platform = platformFromEpicId.GetValueOrDefault(elimination.Eliminated)
                    }
                })
                .ToList()
                ?? new List<FortniteElimination>();

            var playerCount = replay.TeamStats == null ? realPlayers.Count : Convert.ToInt32(replay.TeamStats.TotalPlayers);
            var realPlayerCount = realPlayers.Count;
            var winningPlayerIds = replay.GameData?.WinningPlayerIds?.ToList() ?? new List<int>();

            var displayNamesOfWinners = winningPlayerIds
                .Select(playerId => realPlayers.FirstOrDefault(playerData => playerData.Id == playerId)?.EpicId)
                .Where(epicId => !string.IsNullOrWhiteSpace(epicId))
                .Select(epicId => displayNameFromEpicId.GetValueOrDefault(epicId, "Unknown player"))
                .ToList();

            var replayInfo = new FortniteGame
            {
                Guid = replay.Header.Guid,
                PlayerCount = playerCount,
                RealPlayerCount = realPlayerCount,
                PlatformStatistics = platformStatistics,
                Eliminations = eliminations,
                WinningPlayerIds = winningPlayerIds,
                WinningDisplayNames = displayNamesOfWinners,
                BotCount = playerCount - realPlayerCount,
                BusRouteRaw = replay.MapData?.BattleBusFlightPaths?.ToList() ?? new List<BattleBus>()
            };

            return replayInfo;
        }

    }
}
