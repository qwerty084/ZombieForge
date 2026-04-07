using System.Collections.Generic;
using ZombieForge.Models;

namespace ZombieForge.Services.Games
{
    public static class BO1AddressProfiles
    {
        // Black Ops 1 — Global version, latest patch (1.3.0.780)
        // Points confirmed at BlackOps.exe+0x180A6C8
        public static AddressProfile GlobalLatest { get; } = new()
        {
            Version          = "Global (1.3.0.780)",
            BaseOffset       = 0x1808B40,
            Stride           = 7464,
            PointsOffset     = 7048,
            KillsOffset      = 7052,
            DownsOffset      = 7076,
            HeadshotsOffset  = 7084,
            LevelTimeAddress = 0x0286D014, // server/level time (ms) — confirmed in BO1_Address_Reference.md
        };

        public static IReadOnlyList<AddressProfile> All { get; } =
        [
            GlobalLatest,
            // Future profiles go here, e.g.:
            // SteamLegacy, etc.
        ];

        public static AddressProfile Default => GlobalLatest;
    }
}
