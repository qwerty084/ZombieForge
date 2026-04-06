using System;
using ZombieForge.Models;

namespace ZombieForge.Services.Games
{
    public interface IGameHandler
    {
        string ProcessName { get; }
        PlayerStats ReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex);

        /// <summary>
        /// Reads the game's internal level-time clock (milliseconds).
        /// Uses an absolute address — no module base required.
        /// Returns 0 on failure.
        /// </summary>
        int ReadLevelTime(IntPtr processHandle);
    }
}
