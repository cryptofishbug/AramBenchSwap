using System;
using System.Linq;
using AramBenchSwap.Core;

namespace AramBenchSwap.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                ParsesLockfile();
                ParsesBenchChampionsFromSession();
                ParsesPickableChampionIds();
                StartsBenchSwapCooldownWhenBenchFirstAppears();
                StartsBenchSwapCooldownWhenChampSelectBeginsBeforeBenchAppears();
                KeepsBenchSwapCooldownUntilThreeSecondsPass();
                UsesRemainingCooldownAsRefreshDelay();
                BuildsBenchSwapRequestWithoutBody();
                RejectsBenchSwapWhenChampionIsNotOnBench();
                RefreshesBenchBeforeSendingSwapRequest();
                KeepsUnselectableBenchChampionVisible();
                RejectsBenchSwapWhenChampionIsNotSelectable();
                ReadsConnectionFromFirstExistingLockfile();
                ReadsLockfileWhileWriterKeepsItOpen();
                BuildsLcuBasicAuthHeader();
                HidesWaitingWindowUnlessManuallyOpened();
                KeepsManualWindowVisibleWhileWaiting();
                PositionsPanelAboveLeagueClientTopCenter();
                DoublesDefaultPanelWidthForBenchIcons();
                ClampsPanelAboveClientToWorkAreaTop();
                AllowsCertificateBypassOnlyForLocalLcuUrls();
                OverlayModeShowsStatusBeforeChampSelect();
                BenchOnlyModeHidesStatusBeforeChampSelect();
                OverlayModeShowsChampSelectWaitingStatus();
                BenchOnlyModeHidesChampSelectWaitingStatus();
                HidesOverlayDuringInProgressButKeepsStatusText();
                ShowsOverlayOnlyForChampSelectBench();
                ParsesDisplayModePreference();
                Console.WriteLine("All tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void ParsesLockfile()
        {
            var connection = LockfileParser.Parse("LeagueClient:3210:45678:secret-token:https");

            AssertEqual("LeagueClient", connection.ProcessName, "process name");
            AssertEqual(3210, connection.ProcessId, "process id");
            AssertEqual(45678, connection.Port, "port");
            AssertEqual("secret-token", connection.Password, "password");
            AssertEqual("https", connection.Protocol, "protocol");
            AssertEqual("https://127.0.0.1:45678", connection.BaseUrl, "base url");
        }

        private static void ParsesBenchChampionsFromSession()
        {
            const string json = @"{
              ""benchEnabled"": true,
              ""localPlayerCellId"": 2,
              ""benchChampions"": [
                { ""championId"": 22, ""isPriority"": false },
                { ""championId"": 103, ""isPriority"": true }
              ],
              ""myTeam"": [
                { ""cellId"": 2, ""championId"": 81 }
              ]
            }";

            var session = ChampSelectSessionParser.Parse(json);

            AssertTrue(session.IsAvailable, "session should be available");
            AssertTrue(session.BenchEnabled, "bench should be enabled");
            AssertEqual(2L, session.LocalPlayerCellId, "local cell id");
            AssertEqual(2, session.BenchChampions.Count, "bench champion count");
            AssertEqual(22, session.BenchChampions[0].ChampionId, "first champion id");
            AssertFalse(session.BenchChampions[0].IsPriority, "first priority");
            AssertEqual(103, session.BenchChampions[1].ChampionId, "second champion id");
            AssertTrue(session.BenchChampions[1].IsPriority, "second priority");
        }

        private static void ParsesPickableChampionIds()
        {
            const string json = "[22, 103]";

            var championIds = ChampionAvailabilityParser.ParseSelectableChampionIds(json).ToList();

            AssertEqual(2, championIds.Count, "selectable champion count");
            AssertTrue(championIds.Contains(22), "pickable champion 22");
            AssertTrue(championIds.Contains(103), "pickable champion 103");
        }

        private static void StartsBenchSwapCooldownWhenBenchFirstAppears()
        {
            var cooldown = new BenchSwapCooldown(TimeSpan.FromSeconds(3));
            var session = new ChampSelectSession(true, true, 1, new[] { new BenchChampion(22, false) });
            var now = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc);

            cooldown.Update("ChampSelect", session, now);

            AssertTrue(cooldown.IsActive(now), "bench swap cooldown should start when bench appears");
            AssertEqual(TimeSpan.FromSeconds(3), cooldown.Remaining(now), "bench swap cooldown remaining time");
            AssertFalse(cooldown.IsActive(now.AddSeconds(3)), "bench swap cooldown should end after three seconds");
            AssertEqual(TimeSpan.Zero, cooldown.Remaining(now.AddSeconds(3)), "expired bench swap cooldown remaining time");
        }

        private static void StartsBenchSwapCooldownWhenChampSelectBeginsBeforeBenchAppears()
        {
            var cooldown = new BenchSwapCooldown(TimeSpan.FromSeconds(3));
            var emptySession = new ChampSelectSession(true, true, 1, new BenchChampion[0]);
            var benchSession = new ChampSelectSession(true, true, 1, new[] { new BenchChampion(22, false) });
            var now = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc);

            cooldown.Update("ChampSelect", emptySession, now);
            cooldown.Update("ChampSelect", benchSession, now.AddSeconds(2));

            AssertEqual(TimeSpan.FromSeconds(1), cooldown.Remaining(now.AddSeconds(2)), "cooldown should be anchored to champ select start");
        }

        private static void KeepsBenchSwapCooldownUntilThreeSecondsPass()
        {
            var cooldown = new BenchSwapCooldown(TimeSpan.FromSeconds(3));
            var session = new ChampSelectSession(true, true, 1, new[] { new BenchChampion(22, false) });
            var now = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc);

            cooldown.Update("ChampSelect", session, now);
            cooldown.Update("ChampSelect", session, now.AddSeconds(1));

            AssertTrue(cooldown.IsActive(now.AddSeconds(2)), "bench swap cooldown should remain active before three seconds");
            AssertFalse(cooldown.IsActive(now.AddSeconds(3.1)), "bench swap cooldown should expire after three seconds");

            cooldown.Update("InProgress", null, now.AddSeconds(4));
            cooldown.Update("ChampSelect", session, now.AddSeconds(5));

            AssertTrue(cooldown.IsActive(now.AddSeconds(5)), "bench swap cooldown should restart for a later champ select");
        }

        private static void UsesRemainingCooldownAsRefreshDelay()
        {
            AssertEqual(TimeSpan.FromMilliseconds(250), CooldownRefreshDelay.Calculate(TimeSpan.FromMilliseconds(250)), "cooldown refresh delay");
            AssertEqual(TimeSpan.FromMilliseconds(1), CooldownRefreshDelay.Calculate(TimeSpan.FromTicks(1)), "minimum cooldown refresh delay");
            AssertEqual(TimeSpan.Zero, CooldownRefreshDelay.Calculate(TimeSpan.Zero), "expired cooldown refresh delay");
        }


        private static void BuildsBenchSwapRequestWithoutBody()
        {
            var transport = new RecordingTransport();
            var client = new LcuClient(new LcuConnection("LeagueClient", 1, 1234, "pw", "https"), transport);
            var session = new ChampSelectSession(true, true, 1, new[] { new BenchChampion(22, false) });

            var result = client.SwapBenchChampion(session, 22);

            AssertTrue(result.Success, "swap should be allowed");
            AssertEqual("POST", transport.LastMethod, "method");
            AssertEqual("https://127.0.0.1:1234/lol-champ-select/v1/session/bench/swap/22", transport.LastUrl, "url");
            AssertEqual(null, transport.LastBody, "body");
        }

        private static void RejectsBenchSwapWhenChampionIsNotOnBench()
        {
            var transport = new RecordingTransport();
            var client = new LcuClient(new LcuConnection("LeagueClient", 1, 1234, "pw", "https"), transport);
            var session = new ChampSelectSession(true, true, 1, new[] { new BenchChampion(22, false) });

            var result = client.SwapBenchChampion(session, 103);

            AssertFalse(result.Success, "swap should be rejected");
            AssertTrue(result.Message.Contains("bench"), "failure should mention bench");
            AssertEqual(null, transport.LastMethod, "transport should not be called");
        }

        private static void KeepsUnselectableBenchChampionVisible()
        {
            var transport = new RecordingTransport();
            transport.QueueResponse(200, @"{
              ""benchEnabled"": true,
              ""localPlayerCellId"": 1,
              ""benchChampions"": [
                { ""championId"": 22, ""isPriority"": false },
                { ""championId"": 103, ""isPriority"": false }
              ]
            }");
            transport.QueueResponse(200, "[22]");
            var client = new LcuClient(new LcuConnection("LeagueClient", 1, 1234, "pw", "https"), transport);

            var session = client.GetBenchAwareChampSelectSession();

            AssertEqual(2, session.BenchChampions.Count, "bench champion count should not be filtered");
            AssertTrue(session.BenchChampions[0].IsSelectable, "owned bench champion should be selectable");
            AssertFalse(session.BenchChampions[1].IsSelectable, "unowned bench champion should be visible but disabled");
            AssertEqual("https://127.0.0.1:1234/lol-champ-select/v1/pickable-champion-ids", transport.LastUrl, "pickable champion endpoint");
        }

        private static void RefreshesBenchBeforeSendingSwapRequest()
        {
            var transport = new RecordingTransport();
            transport.QueueResponse(200, @"{
              ""benchEnabled"": true,
              ""localPlayerCellId"": 1,
              ""benchChampions"": [
                { ""championId"": 22, ""isPriority"": false }
              ]
            }");
            var client = new LcuClient(new LcuConnection("LeagueClient", 1, 1234, "pw", "https"), transport);

            var result = client.RefreshAndSwapBenchChampion(103);

            AssertFalse(result.Success, "stale bench swap should be rejected");
            AssertTrue(result.Message.Contains("bench"), "failure should mention bench");
            AssertEqual(0, transport.PostCount, "POST should not be sent");
            AssertEqual("GET", transport.LastMethod, "last method");
        }

        private static void RejectsBenchSwapWhenChampionIsNotSelectable()
        {
            var transport = new RecordingTransport();
            transport.QueueResponse(200, @"{
              ""benchEnabled"": true,
              ""localPlayerCellId"": 1,
              ""benchChampions"": [
                { ""championId"": 22, ""isPriority"": false },
                { ""championId"": 103, ""isPriority"": false }
              ]
            }");
            transport.QueueResponse(200, "[22]");
            var client = new LcuClient(new LcuConnection("LeagueClient", 1, 1234, "pw", "https"), transport);

            var result = client.RefreshAndSwapBenchChampion(103);

            AssertFalse(result.Success, "unselectable bench champion should be rejected");
            AssertTrue(result.Message.Contains("bench"), "failure should mention filtered bench");
            AssertEqual(0, transport.PostCount, "POST should not be sent for unselectable champion");
        }

        private sealed class RecordingTransport : ILcuTransport
        {
            public string LastMethod;
            public string LastUrl;
            public string LastBody;
            public int CallCount;
            public int PostCount;
            private readonly System.Collections.Generic.Queue<LcuResponse> _responses = new System.Collections.Generic.Queue<LcuResponse>();

            public void QueueResponse(int statusCode, string body)
            {
                _responses.Enqueue(new LcuResponse(statusCode, body));
            }

            public LcuResponse Send(string method, string url, string password, string body)
            {
                CallCount++;
                if (method == "POST")
                {
                    PostCount++;
                }

                LastMethod = method;
                LastUrl = url;
                LastBody = body;
                if (_responses.Count > 0)
                {
                    return _responses.Dequeue();
                }

                return new LcuResponse(200, "{}");
            }
        }

        private static void ReadsConnectionFromFirstExistingLockfile()
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aram-bench-swap-tests-" + Guid.NewGuid());
            System.IO.Directory.CreateDirectory(root);
            try
            {
                var missing = System.IO.Path.Combine(root, "missing.lockfile");
                var existing = System.IO.Path.Combine(root, "lockfile");
                System.IO.File.WriteAllText(existing, "LeagueClient:7:2468:pw:https");

                LcuConnection connection;
                var found = LeagueClientLocator.TryReadConnection(new[] { missing, existing }, out connection);

                AssertTrue(found, "lockfile should be found");
                AssertEqual(2468, connection.Port, "located port");
                AssertEqual("pw", connection.Password, "located password");
            }
            finally
            {
                System.IO.Directory.Delete(root, true);
            }
        }

        private static void ReadsLockfileWhileWriterKeepsItOpen()
        {
            var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aram-bench-swap-tests-" + Guid.NewGuid());
            System.IO.Directory.CreateDirectory(root);
            try
            {
                var existing = System.IO.Path.Combine(root, "lockfile");
                using (var writer = new System.IO.FileStream(existing, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                using (var streamWriter = new System.IO.StreamWriter(writer))
                {
                    streamWriter.Write("LeagueClient:7:2468:pw:https");
                    streamWriter.Flush();

                    LcuConnection connection;
                    var found = LeagueClientLocator.TryReadConnection(new[] { existing }, out connection);

                    AssertTrue(found, "shared lockfile should be readable");
                    AssertEqual(2468, connection.Port, "shared lockfile port");
                }
            }
            finally
            {
                System.IO.Directory.Delete(root, true);
            }
        }

        private static void BuildsLcuBasicAuthHeader()
        {
            var header = LcuAuth.CreateBasicAuthHeader("secret-token");

            AssertEqual("Basic cmlvdDpzZWNyZXQtdG9rZW4=", header, "basic auth header");
        }

        private static void HidesWaitingWindowUnlessManuallyOpened()
        {
            var session = new ChampSelectSession(false, false, -1, new BenchChampion[0]);

            var state = BenchWindowState.Decide(session, false);

            AssertFalse(state.ShouldShow, "waiting window should hide by default");
            AssertFalse(state.ShouldRenderBench, "waiting window should not render bench");
        }

        private static void KeepsManualWindowVisibleWhileWaiting()
        {
            var session = new ChampSelectSession(false, false, -1, new BenchChampion[0]);

            var state = BenchWindowState.Decide(session, true);

            AssertTrue(state.ShouldShow, "manual waiting window should stay visible");
            AssertFalse(state.ShouldRenderBench, "manual waiting window should not render bench");
            AssertTrue(state.Status.Contains("Waiting"), "manual status should explain waiting state");
        }

        private static void PositionsPanelAboveLeagueClientTopCenter()
        {
            var position = WindowPlacement.CalculateAboveTopCenter(
                new WindowBounds(100, 120, 1280, 720, 1.0, 1.0),
                356,
                72,
                8);

            AssertEqual(562.0, position.Left, "client anchored left");
            AssertEqual(40.0, position.Top, "client anchored top");
        }

        private static void DoublesDefaultPanelWidthForBenchIcons()
        {
            var width = WindowPlacement.CalculateOverlayWidth(356);

            AssertEqual(712.0, width, "overlay width");
        }

        private static void ClampsPanelAboveClientToWorkAreaTop()
        {
            var position = WindowPlacement.CalculateAboveTopCenter(
                new WindowBounds(100, 20, 1280, 720, 1.0, 1.0),
                356,
                72,
                8,
                0);

            AssertEqual(0.0, position.Top, "clamped top");
        }

        private static void AllowsCertificateBypassOnlyForLocalLcuUrls()
        {
            AssertTrue(HttpLcuTransport.IsLocalLcuUrl("https://127.0.0.1:1234/lol-champ-select/v1/session"), "127.0.0.1 should be local LCU");
            AssertTrue(HttpLcuTransport.IsLocalLcuUrl("https://localhost:1234/lol-champ-select/v1/session"), "localhost should be local LCU");
            AssertFalse(HttpLcuTransport.IsLocalLcuUrl("https://example.com/lol-champ-select/v1/session"), "external host should not be local LCU");
        }

        private static void OverlayModeShowsStatusBeforeChampSelect()
        {
            var state = BenchWindowState.Decide("Matchmaking", null, DisplayMode.Overlay);

            AssertTrue(state.ShouldShow, "overlay mode should show status before champ select");
            AssertFalse(state.ShouldRenderBench, "status overlay should not render bench before champ select");
            AssertTrue(state.Status.Contains("Matchmaking"), "status should explain current phase");
        }

        private static void BenchOnlyModeHidesStatusBeforeChampSelect()
        {
            var state = BenchWindowState.Decide("Matchmaking", null, DisplayMode.BenchOnly);

            AssertFalse(state.ShouldShow, "bench-only mode should hide status before champ select");
            AssertFalse(state.ShouldRenderBench, "bench-only mode should not render bench before champ select");
            AssertTrue(state.Status.Contains("Matchmaking"), "tray status should explain current phase");
        }

        private static void OverlayModeShowsChampSelectWaitingStatus()
        {
            var session = new ChampSelectSession(true, true, 1, new BenchChampion[0]);

            var state = BenchWindowState.Decide("ChampSelect", session, DisplayMode.Overlay);

            AssertTrue(state.ShouldShow, "overlay mode should show champ select waiting status");
            AssertFalse(state.ShouldRenderBench, "waiting status should not render bench icons");
        }

        private static void BenchOnlyModeHidesChampSelectWaitingStatus()
        {
            var session = new ChampSelectSession(true, true, 1, new BenchChampion[0]);

            var state = BenchWindowState.Decide("ChampSelect", session, DisplayMode.BenchOnly);

            AssertFalse(state.ShouldShow, "bench-only mode should hide champ select waiting status");
            AssertFalse(state.ShouldRenderBench, "bench-only mode should not render bench icons");
        }

        private static void HidesOverlayDuringInProgressButKeepsStatusText()
        {
            var state = BenchWindowState.Decide("InProgress", null, DisplayMode.Overlay);

            AssertFalse(state.ShouldShow, "overlay should hide during game");
            AssertFalse(state.ShouldRenderBench, "overlay should not render bench during game");
            AssertTrue(state.Status.Contains("Game"), "tray status should explain current phase");
        }

        private static void ShowsOverlayOnlyForChampSelectBench()
        {
            var session = new ChampSelectSession(true, true, 1, new[] { new BenchChampion(31, false) });

            var state = BenchWindowState.Decide("ChampSelect", session, DisplayMode.BenchOnly);

            AssertTrue(state.ShouldShow, "overlay should show in champ select with bench");
            AssertTrue(state.ShouldRenderBench, "overlay should render bench in champ select");
        }

        private static void ParsesDisplayModePreference()
        {
            AssertEqual(DisplayMode.Overlay, DisplayModePreference.Parse(null), "null display mode default");
            AssertEqual(DisplayMode.Overlay, DisplayModePreference.Parse(""), "empty display mode default");
            AssertEqual(DisplayMode.Overlay, DisplayModePreference.Parse("Overlay"), "overlay display mode");
            AssertEqual(DisplayMode.BenchOnly, DisplayModePreference.Parse("BenchOnly"), "bench-only display mode");
            AssertEqual(DisplayMode.Overlay, DisplayModePreference.Parse("invalid"), "invalid display mode default");
            AssertEqual("BenchOnly", DisplayModePreference.Format(DisplayMode.BenchOnly), "bench-only display mode format");
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception(label + ": expected <" + expected + "> but got <" + actual + ">.");
            }
        }

        private static void AssertTrue(bool value, string label)
        {
            if (!value)
            {
                throw new Exception(label + ": expected true.");
            }
        }

        private static void AssertFalse(bool value, string label)
        {
            if (value)
            {
                throw new Exception(label + ": expected false.");
            }
        }
    }
}
