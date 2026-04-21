using System.Collections.Generic;
using ZombieForge.Models;

namespace ZombieForge.Services.Games
{
    /// <summary>
    /// Provides known BO1 memory address profiles.
    /// </summary>
    public static class BO1AddressProfiles
    {
        // Black Ops 1 — Global version, latest patch (1.3.0.780)
        // Points confirmed at BlackOps.exe+0x180A6C8
        /// <summary>
        /// Gets the latest global BO1 profile.
        /// </summary>
        public static AddressProfile GlobalLatest { get; } = new()
        {
            Version          = "Global (1.3.0.780)",
            BaseOffset       = 0x1808B40,
            Stride           = 7464,
            PointsOffset     = 7048,
            KillsOffset      = 7052,
            DownsOffset      = 7076,
            HeadshotsOffset  = 7084,
            LevelTimeAddress = 0x0286D014, // server/level time (ms) — confirmed in docs/bo1-memory-map.md
        };

        /// <summary>
        /// Gets all registered BO1 profiles.
        /// </summary>
        public static IReadOnlyList<AddressProfile> All { get; } =
        [
            GlobalLatest,
            // Future profiles go here, e.g.:
            // SteamLegacy, etc.
        ];

        /// <summary>
        /// Gets the default BO1 profile used by the app.
        /// </summary>
        public static AddressProfile Default => GlobalLatest;
    }
}
