using System;

namespace ZombieForge.Models
{
    /// <summary>
    /// Tracks timing state for a single zombies game (from round 1 through end game).
    /// </summary>
    public class GameSession
    {
        /// <summary>Level time (ms) when round 1 started. -1 when no session is active.</summary>
        public int StartTimestamp { get; private set; } = -1;

        /// <summary>Level time (ms) when the current round started. -1 if the round hasn't started yet.</summary>
        public int RoundStartTimestamp { get; private set; } = -1;

        /// <summary>
        /// Frozen elapsed ms captured at EndOfRound. When >= 0, FormatRoundTime returns this
        /// fixed value instead of computing against the live clock.
        /// </summary>
        private int _roundFrozenElapsedMs = -1;

        /// <summary>
        /// Gets the number of rounds observed in the current session.
        /// </summary>
        public uint CurrentRoundNumber { get; private set; } = 0;

        /// <summary>
        /// Gets a value that indicates whether a session is currently active.
        /// </summary>
        public bool IsActive => StartTimestamp != -1;

        /// <summary>
        /// Called on every StartOfRound event. Records the round start timestamp and, for the
        /// very first round of the session, also sets the game start timestamp.
        /// </summary>
        public void OnRoundStart(int timestamp)
        {
            if (timestamp <= 0)
                return;

            CurrentRoundNumber++;

            if (!IsActive)
                StartTimestamp = timestamp;

            RoundStartTimestamp    = timestamp;
            _roundFrozenElapsedMs  = -1;  // resume live counting
        }

        /// <summary>
        /// Called on EndOfRound. Freezes the round timer at the elapsed time at that moment.
        /// </summary>
        public void OnRoundEnd(int timestamp)
        {
            if (!IsActive || RoundStartTimestamp <= 0 || timestamp <= 0)
                return;

            int elapsed = timestamp - RoundStartTimestamp;
            _roundFrozenElapsedMs = elapsed >= 0 ? elapsed : 0;
        }

        /// <summary>Resets all state — call on EndGame or when the game process exits.</summary>
        public void Reset()
        {
            StartTimestamp        = -1;
            RoundStartTimestamp   = -1;
            _roundFrozenElapsedMs = -1;
            CurrentRoundNumber    = 0;
        }

        /// <summary>
        /// Returns the elapsed game time as "HH:MM:SS", or "--:--:--" when the session
        /// is not active or the current level time is not yet valid.
        /// </summary>
        public string FormatGameTime(int currentMs)
        {
            // BO1 level time is 0 while a map is still loading/restarting and can briefly lag behind
            // the first round event, so we show placeholders instead of negative or pre-session time.
            if (!IsActive || currentMs <= 0 || currentMs < StartTimestamp)
                return "--:--:--";

            return FormatHms(currentMs - StartTimestamp);
        }

        /// <summary>
        /// Returns the elapsed round time as "MM:SS", or "--:--" when no round is in progress.
        /// Returns the frozen value when the round has ended and the next hasn't started yet.
        /// </summary>
        public string FormatRoundTime(int currentMs)
        {
            if (!IsActive || RoundStartTimestamp <= 0)
                return "--:--";

            int elapsedMs = _roundFrozenElapsedMs >= 0
                ? _roundFrozenElapsedMs
                : currentMs - RoundStartTimestamp;

            if (elapsedMs < 0)
                return "--:--";

            TimeSpan ts = TimeSpan.FromMilliseconds(elapsedMs);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private static string FormatHms(int ms)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(ms);
            int hours = (int)ts.TotalHours;
            return $"{hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
