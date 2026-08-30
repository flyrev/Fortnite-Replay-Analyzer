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

            // Fields on the parsed replay are populated opportunistically by the reader and
            // are frequently null/absent depending on the replay (e.g. incomplete matches or
            // maps without recorded bus paths). Treat every collection as optional.
            var players = replay.PlayerData ?? Enumerable.Empty<PlayerData>();

            var realPlayers = players
                .Where(player => !string.IsNullOrWhiteSpace(player.EpicId))
                .GroupBy(player => player.EpicId)
                .Select(group => group.First())
                .ToList();
            var platformStatistics = realPlayers.GroupBy(player => player.Platform)
                .ToDictionary(group => group.Key, group => group.Count());

            var eliminations = replay.Eliminations?
                .Where(elimination => !elimination.Knocked)
                .Where(elimination => elimination.Eliminator != elimination.Eliminated)
                .OrderBy(elimination => elimination.Info?.StartTime ?? 0)
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
                .ToList() ?? new List<FortniteElimination>();

            var playerCount = Convert.ToInt32(replay.TeamStats?.TotalPlayers ?? 0);
            var realPlayerCount = realPlayers.Count;

            var winningPlayerIds = (replay.GameData?.WinningPlayerIds ?? Enumerable.Empty<int>()).ToList();
            var displayNamesOfWinners = winningPlayerIds
                .Select(playerId => players.FirstOrDefault(playerData => playerData.Id == playerId)?.EpicId)
                .Where(epicId => !string.IsNullOrWhiteSpace(epicId))
                .Select(epicId => displayNameFromEpicId.GetValueOrDefault(epicId, "Unknown player"))
                .ToList();

            var replayInfo = new FortniteGame
            {
                Guid = replay.Header?.Guid,
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
