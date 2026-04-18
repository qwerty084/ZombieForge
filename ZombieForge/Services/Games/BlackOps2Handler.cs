using System;
using ZombieForge.Models;

namespace ZombieForge.Services.Games
{
    /// <summary>
    /// Stub handler for Black Ops 2 Zombies. Memory offsets are not yet implemented.
    /// </summary>
    public class BlackOps2Handler : IGameHandler
    {
        // t6zm.exe = standard BO2 Zombies. Additional variants (e.g. Plutonium) can be added here.
        public string[] ProcessNames => ["t6zm"];

        public bool TryReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex, out PlayerStats stats, out int win32Error)
        {
            stats = new PlayerStats();
            win32Error = 0;
            return false;
        }

        public bool TryReadLevelTime(IntPtr processHandle, out int levelTime, out int win32Error)
        {
            levelTime = 0;
            win32Error = 0;
            return false;
        }
    }
}
