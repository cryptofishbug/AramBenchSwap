using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace AramBenchSwap.Core
{
    public static class ChampionAvailabilityParser
    {
        public static IEnumerable<int> ParseSelectableChampionIds(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                yield break;
            }

            object root;
            try
            {
                root = new JavaScriptSerializer().DeserializeObject(json);
            }
            catch (ArgumentException)
            {
                yield break;
            }

            var champions = root as IEnumerable;
            if (champions == null)
            {
                yield break;
            }

            foreach (var item in champions)
            {
                var champion = item as IDictionary<string, object>;
                if (champion == null)
                {
                    continue;
                }

                var championId = (int)GetLong(champion, "id", GetLong(champion, "championId", 0));
                if (championId <= 0 || !IsSelectable(champion))
                {
                    continue;
                }

                yield return championId;
            }
        }

        private static bool IsSelectable(IDictionary<string, object> champion)
        {
            if (HasBool(champion, "active") && !GetBool(champion, "active"))
            {
                return false;
            }

            if (GetBool(champion, "disabled"))
            {
                return false;
            }

            if (GetBool(champion, "freeToPlay"))
            {
                return true;
            }

            object ownershipValue;
            var ownership = champion.TryGetValue("ownership", out ownershipValue)
                ? ownershipValue as IDictionary<string, object>
                : null;
            if (ownership == null)
            {
                return false;
            }

            return GetBool(ownership, "owned") || GetBool(ownership, "freeToPlayReward");
        }

        private static bool HasBool(IDictionary<string, object> source, string key)
        {
            object value;
            return source.TryGetValue(key, out value) && value is bool;
        }

        private static bool GetBool(IDictionary<string, object> source, string key)
        {
            object value;
            return source.TryGetValue(key, out value) && value is bool && (bool)value;
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
