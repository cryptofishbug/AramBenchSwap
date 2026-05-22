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
                BuildsBenchSwapRequestWithoutBody();
                RejectsBenchSwapWhenChampionIsNotOnBench();
                ReadsConnectionFromFirstExistingLockfile();
                ReadsLockfileWhileWriterKeepsItOpen();
                BuildsLcuBasicAuthHeader();
                HidesWaitingWindowUnlessManuallyOpened();
                KeepsManualWindowVisibleWhileWaiting();
                PositionsPanelAboveLeagueClientTopCenter();
                DoublesDefaultPanelWidthForBenchIcons();
                ClampsPanelAboveClientToWorkAreaTop();
                AllowsCertificateBypassOnlyForLocalLcuUrls();
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

        private sealed class RecordingTransport : ILcuTransport
        {
            public string LastMethod;
            public string LastUrl;
            public string LastBody;

            public LcuResponse Send(string method, string url, string password, string body)
            {
                LastMethod = method;
                LastUrl = url;
                LastBody = body;
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
