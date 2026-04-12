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

        public PlayerStats ReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex)
            => new();

        public int ReadLevelTime(IntPtr processHandle)
            => 0;
    }
}
