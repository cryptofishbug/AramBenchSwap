using System;

namespace AramBenchSwap.Core
{
    public static class CooldownRefreshDelay
    {
        private static readonly TimeSpan MinimumDelay = TimeSpan.FromMilliseconds(1);

        public static TimeSpan Calculate(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            return remaining < MinimumDelay ? MinimumDelay : remaining;
        }
    }
}
