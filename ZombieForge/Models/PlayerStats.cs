namespace ZombieForge.Models
{
    /// <summary>
    /// Snapshot of the tracked zombies stats for a single player.
    /// </summary>
    public class PlayerStats
    {
        /// <summary>Gets the player's current points.</summary>
        public int Points { get; init; }

        /// <summary>Gets the player's total kills.</summary>
        public int Kills { get; init; }

        /// <summary>Gets the player's total downs.</summary>
        public int Downs { get; init; }

        /// <summary>Gets the player's total headshots.</summary>
        public int Headshots { get; init; }
    }
}
