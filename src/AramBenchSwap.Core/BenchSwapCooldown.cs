using System;

namespace AramBenchSwap.Core
{
    public sealed class BenchSwapCooldown
    {
        private readonly TimeSpan _duration;
        private bool _champSelectWasActive;
        private DateTime _cooldownUntilUtc;

        public BenchSwapCooldown(TimeSpan duration)
        {
            _duration = duration;
            _cooldownUntilUtc = DateTime.MinValue;
        }

        public void Update(string gameflowPhase, ChampSelectSession session, DateTime nowUtc)
        {
            var champSelectActive = gameflowPhase == "ChampSelect";

            if (!champSelectActive)
            {
                _champSelectWasActive = false;
                _cooldownUntilUtc = DateTime.MinValue;
                return;
            }

            if (!_champSelectWasActive)
            {
                _cooldownUntilUtc = nowUtc.Add(_duration);
            }

            _champSelectWasActive = true;
        }

        public bool IsActive(DateTime nowUtc)
        {
            return nowUtc < _cooldownUntilUtc;
        }

        public TimeSpan Remaining(DateTime nowUtc)
        {
            return IsActive(nowUtc) ? _cooldownUntilUtc - nowUtc : TimeSpan.Zero;
        }
    }
}
