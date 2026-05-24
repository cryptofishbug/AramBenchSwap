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
            return Decide("ChampSelect", session, manuallyOpened);
        }

        public static BenchWindowState Decide(string gameflowPhase, ChampSelectSession session, bool manuallyOpened)
        {
            return Decide(gameflowPhase, session, manuallyOpened ? DisplayMode.Overlay : DisplayMode.BenchOnly);
        }

        public static BenchWindowState Decide(string gameflowPhase, ChampSelectSession session, DisplayMode displayMode)
        {
            if (gameflowPhase != "ChampSelect")
            {
                if (displayMode == DisplayMode.Overlay && IsOverlayStatusPhase(gameflowPhase))
                {
                    return new BenchWindowState(true, false, FriendlyStatus(gameflowPhase));
                }

                return new BenchWindowState(false, false, FriendlyStatus(gameflowPhase));
            }

            if (session != null && session.IsAvailable && session.BenchEnabled && session.BenchChampions.Count > 0)
            {
                return new BenchWindowState(true, true, "Click a bench champion icon to swap.");
            }

            if (displayMode == DisplayMode.Overlay)
            {
                return new BenchWindowState(true, false, "Waiting for ARAM bench...");
            }

            return new BenchWindowState(false, false, "Waiting for ARAM bench...");
        }

        private static bool IsOverlayStatusPhase(string gameflowPhase)
        {
            switch (gameflowPhase)
            {
                case "Lobby":
                case "Matchmaking":
                case "ReadyCheck":
                    return true;
                default:
                    return false;
            }
        }

        private static string FriendlyStatus(string gameflowPhase)
        {
            switch (gameflowPhase)
            {
                case "Lobby":
                    return "Lobby";
                case "Matchmaking":
                    return "Matchmaking";
                case "ReadyCheck":
                    return "Ready check";
                case "GameStart":
                    return "Game starting";
                case "InProgress":
                    return "Game in progress";
                case "Reconnect":
                    return "Reconnect available";
                case "WaitingForStats":
                case "PreEndOfGame":
                case "EndOfGame":
                    return "Game ended";
                default:
                    return "Waiting for League Client...";
            }
        }
    }
}
