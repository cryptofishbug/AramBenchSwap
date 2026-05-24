using System.Linq;

namespace AramBenchSwap.Core
{
    public sealed class LcuClient
    {
        private readonly LcuConnection _connection;
        private readonly ILcuTransport _transport;

        public LcuClient(LcuConnection connection, ILcuTransport transport)
        {
            _connection = connection;
            _transport = transport;
        }

        public ChampSelectSession GetChampSelectSession()
        {
            var response = _transport.Send("GET", _connection.BaseUrl + "/lol-champ-select/v1/session", _connection.Password, null);
            if (response.StatusCode != 200)
            {
                return new ChampSelectSession(false, false, -1, new BenchChampion[0]);
            }

            return ChampSelectSessionParser.Parse(response.Body);
        }

        public ChampSelectSession GetBenchAwareChampSelectSession()
        {
            var session = GetChampSelectSession();
            if (session == null || !session.IsAvailable || !session.BenchEnabled || session.BenchChampions.Count == 0)
            {
                return session;
            }

            var selectableChampionIds = GetSelectableChampionIds();
            if (selectableChampionIds == null)
            {
                return session;
            }

            return session.WithBenchChampions(
                session.BenchChampions.Select(champion => champion.WithSelectable(selectableChampionIds.Contains(champion.ChampionId))));
        }

        public string GetGameflowPhase()
        {
            var response = _transport.Send("GET", _connection.BaseUrl + "/lol-gameflow/v1/gameflow-phase", _connection.Password, null);
            if (response.StatusCode != 200)
            {
                return "None";
            }

            return (response.Body ?? string.Empty).Trim().Trim('"');
        }

        public SwapResult RefreshAndSwapBenchChampion(int championId)
        {
            return SwapBenchChampion(GetBenchAwareChampSelectSession(), championId);
        }

        public SwapResult SwapBenchChampion(ChampSelectSession session, int championId)
        {
            if (session == null || !session.IsAvailable || !session.BenchEnabled)
            {
                return SwapResult.Failure("No ARAM bench session is available.");
            }

            var benchChampion = session.BenchChampions.FirstOrDefault(champion => champion.ChampionId == championId);
            if (benchChampion == null)
            {
                return SwapResult.Failure("Champion is no longer on the bench.");
            }

            if (!benchChampion.IsSelectable)
            {
                return SwapResult.Failure("Champion is on the bench but is not selectable on this account.");
            }

            var url = _connection.BaseUrl + "/lol-champ-select/v1/session/bench/swap/" + championId;
            var response = _transport.Send("POST", url, _connection.Password, null);
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                return SwapResult.Ok();
            }

            return SwapResult.Failure("Bench swap failed with HTTP " + response.StatusCode + ".");
        }

        private int[] GetSelectableChampionIds()
        {
            var response = _transport.Send("GET", _connection.BaseUrl + "/lol-champions/v1/owned-champions-minimal", _connection.Password, null);
            if (response.StatusCode != 200)
            {
                return null;
            }

            var championIds = ChampionAvailabilityParser.ParseSelectableChampionIds(response.Body).Distinct().ToArray();
            return championIds.Length == 0 ? null : championIds;
        }
    }

    public interface ILcuTransport
    {
        LcuResponse Send(string method, string url, string password, string body);
    }
}
