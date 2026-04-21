namespace ZombieForge.Models
{
    /// <summary>
    /// Defines game-memory offsets used to read BO1 player and timer data.
    /// </summary>
    public class AddressProfile
    {
        /// <summary>
        /// Gets the display name for the address profile version.
        /// </summary>
        public required string Version { get; init; }

        /// <summary>
        /// Gets the module-relative base offset for player data.
        /// </summary>
        public required long BaseOffset { get; init; }

        /// <summary>
        /// Gets the byte stride between player data blocks.
        /// </summary>
        public required int Stride { get; init; }

        /// <summary>
        /// Gets the points field offset from the player base address.
        /// </summary>
        public required int PointsOffset { get; init; }

        /// <summary>
        /// Gets the kills field offset from the player base address.
        /// </summary>
        public required int KillsOffset { get; init; }

        /// <summary>
        /// Gets the downs field offset from the player base address.
        /// </summary>
        public required int DownsOffset { get; init; }

        /// <summary>
        /// Gets the headshots field offset from the player base address.
        /// </summary>
        public required int HeadshotsOffset { get; init; }

        /// <summary>
        /// Absolute (non-module-relative) address of the server/level time counter (int32, ms).
        /// Confirmed at 0x0286D014 for Global patch.
        /// </summary>
        public required long LevelTimeAddress { get; init; }
    }
}
