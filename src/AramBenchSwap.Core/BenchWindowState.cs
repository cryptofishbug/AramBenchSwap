namespace AramBenchSwap.Core
{
    public sealed class BenchWindowState
    {
        private BenchWindowState(bool shouldShow, bool shouldRenderBench, string status)
        {
            ShouldShow = shouldShow;
            ShouldRenderBench = shouldRenderBench;
            Status = status;
        }

        public bool ShouldShow { get; private set; }
        public bool ShouldRenderBench { get; private set; }
        public string Status { get; private set; }

        public static BenchWindowState Decide(ChampSelectSession session, bool manuallyOpened)
        {
            if (session != null && session.IsAvailable && session.BenchEnabled && session.BenchChampions.Count > 0)
            {
                return new BenchWindowState(true, true, "Click a bench champion icon to swap.");
            }

            if (manuallyOpened)
            {
                return new BenchWindowState(true, false, "Waiting for ARAM bench...");
            }

            return new BenchWindowState(false, false, "Waiting for ARAM bench...");
        }
    }
}
