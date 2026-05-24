namespace AramBenchSwap.Core
{
    public sealed class BenchChampion
    {
        public BenchChampion(int championId, bool isPriority)
            : this(championId, isPriority, true)
        {
        }

        public BenchChampion(int championId, bool isPriority, bool isSelectable)
        {
            ChampionId = championId;
            IsPriority = isPriority;
            IsSelectable = isSelectable;
        }

        public int ChampionId { get; private set; }
        public bool IsPriority { get; private set; }
        public bool IsSelectable { get; private set; }

        public BenchChampion WithSelectable(bool isSelectable)
        {
            return new BenchChampion(ChampionId, IsPriority, isSelectable);
        }
    }
}
