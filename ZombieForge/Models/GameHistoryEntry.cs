using System;

namespace ZombieForge.Models
{
    /// <summary>
    /// Records the stats and metadata for a single completed or in-progress zombies game session.
    /// </summary>
    public class GameHistoryEntry
    {
        /// <summary>Unique identifier for this session, generated once at creation.</summary>
        public string SessionId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>Map name the session was played on.</summary>
        public string MapName { get; init; } = "Unknown";

        /// <summary>UTC timestamp when the session started.</summary>
        public DateTime StartedAtUtc { get; init; }

        /// <summary>UTC timestamp when the session ended, or <c>null</c> while still running.</summary>
        public DateTime? EndedAtUtc { get; set; }

        /// <summary>Whether this session is still in progress.</summary>
        public bool IsRunning { get; set; }

        /// <summary>Highest round reached during the session.</summary>
        public int FinalRound { get; set; }

        /// <summary>Player points at the end (or current snapshot) of the session.</summary>
        public int Points { get; set; }

        /// <summary>Total kills during the session.</summary>
        public int Kills { get; set; }

        /// <summary>Total downs during the session.</summary>
        public int Downs { get; set; }

        /// <summary>Total headshots during the session.</summary>
        public int Headshots { get; set; }
    }
}
