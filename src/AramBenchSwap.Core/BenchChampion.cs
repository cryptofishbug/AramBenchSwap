namespace AramBenchSwap.Core
{
    public sealed class BenchChampion
    {
        public BenchChampion(int championId, bool isPriority)
        {
            ChampionId = championId;
            IsPriority = isPriority;
        }

        public int ChampionId { get; private set; }
        public bool IsPriority { get; private set; }
    }
}
