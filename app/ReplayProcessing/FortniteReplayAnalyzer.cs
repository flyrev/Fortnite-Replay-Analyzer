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
            var realPlayers = replay.PlayerData
                .Where(player => !string.IsNullOrWhiteSpace(player.EpicId))
                .GroupBy(player => player.EpicId)
                .Select(group => group.First())
                .ToList();
            var platformStatistics = realPlayers.GroupBy(player => player.Platform)
                .ToDictionary(group => group.Key, group => group.Count());

            var eliminations = replay.Eliminations
                .Where(elimination => !elimination.Knocked)
                .Where(elimination => elimination.Eliminator != elimination.Eliminated)
                .OrderBy(elimination => elimination.Info.StartTime)
                .Select(elimination => new FortniteElimination
                {
                    EliminatedBy = new FortnitePlayer
                    {
                        Name = displayNameFromEpicId.GetValueOrDefault(elimination.Eliminator, "Unknown player (" + elimination.Eliminator + ")"),
                        EpicId = elimination.Eliminator,
                        Platform = realPlayers.Where(player => player.EpicId == elimination.Eliminator).Select(playerData => playerData.Platform).FirstOrDefault()
                    },
                    Eliminated = new FortnitePlayer
                    {
                        Name = displayNameFromEpicId.GetValueOrDefault(elimination.Eliminated, "Unknown player (" + elimination.Eliminated + ")"),
                        EpicId = elimination.Eliminated,
                        Platform = realPlayers.Where(player => player.EpicId == elimination.Eliminated).Select(playerData => playerData.Platform).FirstOrDefault()
                    }
                })
                .ToList();

            var playerCount = Convert.ToInt32(replay.TeamStats.TotalPlayers);
            var realPlayerCount = realPlayers.Count;

            var displayNamesOfWinners = replay.GameData.WinningPlayerIds
                .Select(playerId => replay.PlayerData.FirstOrDefault(playerData => playerData.Id == playerId)?.EpicId)
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
                WinningPlayerIds = replay.GameData.WinningPlayerIds,
                WinningDisplayNames = displayNamesOfWinners,
                BotCount = playerCount - realPlayerCount,
                BusRouteRaw = replay.MapData.BattleBusFlightPaths.ToList()
            };

            return replayInfo;
        }

    }
}
