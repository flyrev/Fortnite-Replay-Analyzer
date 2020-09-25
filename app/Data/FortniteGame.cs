using FortniteReplayReader.Models;
using System.Collections;
using System.Collections.Generic;

namespace FortniteReplayAnalyzer.Data
{
    public class FortniteGame
    {
        public string Guid { get; internal set; }
        public IList<FortnitePlayer> Players { get; internal set; }
        public IList<FortniteElimination> Eliminations { get; internal set; }
        public IList<FortnitePlayer> Winners { get; internal set; }
        public Dictionary<string, int> PlatformStatistics { get; internal set; }
        public int PlayerCount { get; internal set; }
        public int RealPlayerCount { get; internal set; }
        public int BotCount { get; internal set; }
        public List<BattleBus> BusRouteRaw { get; internal set; }
        public FortniteReplay ReplayRaw { get; internal set; }
        public GameData GameDataRaw { get; internal set; }
        public MapData MapDataRaw { get; internal set; }
        public IEnumerable<string> WinningDisplayNames { get; internal set; }
        public IEnumerable<int> WinningPlayerIds { get; internal set; }
    }
}
