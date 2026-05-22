using System;

namespace AramBenchSwap.Core
{
    public static class LockfileParser
    {
        public static LcuConnection Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Lockfile content is empty.", "content");
            }

            var parts = content.Trim().Split(':');
            if (parts.Length != 5)
            {
                throw new FormatException("Unexpected lockfile format.");
            }

            return new LcuConnection(
                parts[0],
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                parts[3],
                parts[4]);
        }
    }
}
