using System;
using ZombieForge.Models;

namespace ZombieForge.Services.Games
{
    public interface IGameHandler
    {
        /// <summary>
        /// All process names (without .exe) that identify this game.
        /// Supports multiple executables, e.g. vanilla + Plutonium builds.
        /// </summary>
        string[] ProcessNames { get; }

        PlayerStats ReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex);

        /// <summary>
        /// Reads the game's internal level-time clock (milliseconds).
        /// Uses an absolute address — no module base required.
        /// Returns 0 on failure.
        /// </summary>
        int ReadLevelTime(IntPtr processHandle);
    }
}
