using System;

namespace AramBenchSwap.Core
{
    public static class DisplayModePreference
    {
        public static DisplayMode Parse(string value)
        {
            if (string.Equals(value, "BenchOnly", StringComparison.OrdinalIgnoreCase))
            {
                return DisplayMode.BenchOnly;
            }

            return DisplayMode.Overlay;
        }

        public static string Format(DisplayMode mode)
        {
            return mode == DisplayMode.BenchOnly ? "BenchOnly" : "Overlay";
        }
    }
}
