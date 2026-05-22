using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace AramBenchSwap.Core
{
    public static class ChampSelectSessionParser
    {
        public static ChampSelectSession Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ChampSelectSession(false, false, -1, new BenchChampion[0]);
            }

            try
            {
                var serializer = new JavaScriptSerializer();
                var root = serializer.DeserializeObject(json) as IDictionary<string, object>;
                if (root == null)
                {
                    return new ChampSelectSession(false, false, -1, new BenchChampion[0]);
                }

                var benchEnabled = GetBool(root, "benchEnabled");
                var localPlayerCellId = GetLong(root, "localPlayerCellId", -1);
                var benchChampions = ParseBenchChampions(root);

                return new ChampSelectSession(true, benchEnabled, localPlayerCellId, benchChampions);
            }
            catch (ArgumentException)
            {
                return new ChampSelectSession(false, false, -1, new BenchChampion[0]);
            }
        }

        private static IEnumerable<BenchChampion> ParseBenchChampions(IDictionary<string, object> root)
        {
            object value;
            if (!root.TryGetValue("benchChampions", out value))
            {
                yield break;
            }

            var list = value as IEnumerable;
            if (list == null)
            {
                yield break;
            }

            foreach (var item in list)
            {
                var champion = item as IDictionary<string, object>;
                if (champion == null)
                {
                    continue;
                }

                var championId = (int)GetLong(champion, "championId", 0);
                if (championId <= 0)
                {
                    continue;
                }

                yield return new BenchChampion(championId, GetBool(champion, "isPriority"));
            }
        }

        private static bool GetBool(IDictionary<string, object> source, string key)
        {
            object value;
            if (!source.TryGetValue(key, out value) || value == null)
            {
                return false;
            }

            return value is bool && (bool)value;
        }

        private static long GetLong(IDictionary<string, object> source, string key, long defaultValue)
        {
            object value;
            if (!source.TryGetValue(key, out value) || value == null)
            {
                return defaultValue;
            }

            return Convert.ToInt64(value);
        }
    }
}
