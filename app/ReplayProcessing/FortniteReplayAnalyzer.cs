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
            var realPlayers = replay.PlayerData.Where(player => player.EpicId != null && player.EpicId.Length > 0).Distinct();
            var platformStatistics = realPlayers.GroupBy(player => player.Platform)
                      .ToDictionary(x => x.Key, x => x.ToList().Count());

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

            var busRoute = replay.MapData.BattleBusFlightPaths.ToList();

            var playerCount = Convert.ToInt32(replay.TeamStats.TotalPlayers);
            var realPlayerCount = realPlayers.ToList().Count();

            var displayNamesOfWinners = new List<string>();

            try
            {
                var winners = replay.GameData.WinningPlayerIds.Select(playerId => replay.PlayerData
            .Where(playerData => playerData.Id == playerId).FirstOrDefault()).ToList();

                var epicIdsOfWinners = winners.Where(winner => winner != null && winner.EpicId != null)
                    .Select(winner => winner.EpicId);

                displayNamesOfWinners = epicIdsOfWinners.Where(epicIdOfWinner => epicIdOfWinner != null)
                    .Select(epicIdOfWinner => displayNameFromEpicId.GetValueOrDefault(epicIdOfWinner, "Unknown player")).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            var replayInfo = new FortniteGame
            {
                Guid = replay.Header.Guid,
                PlayerCount = playerCount,
                RealPlayerCount = realPlayerCount,
                PlatformStatistics = platformStatistics,
                Eliminations = eliminations,//,
                WinningPlayerIds = replay.GameData.WinningPlayerIds,
                WinningDisplayNames = displayNamesOfWinners,
                BotCount = playerCount - realPlayerCount,
                BusRouteRaw = busRoute,
                //ReplayRaw = replay
                //MapDataRaw = replay.MapData,
                //GameDataRaw = replay.GameData,
            };

            return replayInfo;
        }

    }
}
