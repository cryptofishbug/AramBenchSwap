using System.Collections.Generic;
using System.Linq;

namespace AramBenchSwap.Core
{
    public sealed class ChampSelectSession
    {
        public ChampSelectSession(bool isAvailable, bool benchEnabled, long localPlayerCellId, IEnumerable<BenchChampion> benchChampions)
        {
            IsAvailable = isAvailable;
            BenchEnabled = benchEnabled;
            LocalPlayerCellId = localPlayerCellId;
            BenchChampions = (benchChampions ?? Enumerable.Empty<BenchChampion>()).ToList().AsReadOnly();
        }

        public bool IsAvailable { get; private set; }
        public bool BenchEnabled { get; private set; }
        public long LocalPlayerCellId { get; private set; }
        public IList<BenchChampion> BenchChampions { get; private set; }

        public ChampSelectSession WithBenchChampions(IEnumerable<BenchChampion> benchChampions)
        {
            return new ChampSelectSession(IsAvailable, BenchEnabled, LocalPlayerCellId, benchChampions);
        }
    }
}
