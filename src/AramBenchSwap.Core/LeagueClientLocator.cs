using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AramBenchSwap.Core
{
    public static class LeagueClientLocator
    {
        public static bool TryReadConnection(out LcuConnection connection)
        {
            return TryReadConnection(GetDefaultLockfileCandidates(), out connection);
        }

        public static bool TryReadConnection(IEnumerable<string> lockfileCandidates, out LcuConnection connection)
        {
            foreach (var candidate in lockfileCandidates)
            {
                if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
                {
                    continue;
                }

                try
                {
                    connection = LockfileParser.Parse(ReadShared(candidate));
                    return true;
                }
                catch
                {
                    continue;
                }
            }

            connection = null;
            return false;
        }

        private static IEnumerable<string> GetDefaultLockfileCandidates()
        {
            var env = Environment.GetEnvironmentVariable("LCU_LOCKFILE");
            if (!string.IsNullOrWhiteSpace(env))
            {
                yield return env;
            }

            foreach (var path in GetProcessLockfiles())
            {
                yield return path;
            }

            yield return @"C:\Riot Games\League of Legends\lockfile";
            yield return @"C:\Program Files\Riot Games\League of Legends\lockfile";
            yield return @"C:\Program Files (x86)\Riot Games\League of Legends\lockfile";
        }

        private static string ReadShared(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static IEnumerable<string> GetProcessLockfiles()
        {
            foreach (var processName in new[] { "LeagueClientUx", "LeagueClient" })
            {
                Process[] processes;
                try
                {
                    processes = Process.GetProcessesByName(processName);
                }
                catch
                {
                    continue;
                }

                foreach (var process in processes)
                {
                    using (process)
                    {
                        string fileName;
                        try
                        {
                            fileName = process.MainModule.FileName;
                        }
                        catch
                        {
                            continue;
                        }

                        var directory = Path.GetDirectoryName(fileName);
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            yield return Path.Combine(directory, "lockfile");
                        }
                    }
                }
            }
        }
    }
}
