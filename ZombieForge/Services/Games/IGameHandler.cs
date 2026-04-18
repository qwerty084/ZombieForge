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

        bool TryReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex, out PlayerStats stats, out int win32Error);

        /// <summary>
        /// Reads the game's internal level-time clock (milliseconds).
        /// Uses an absolute address — no module base required.
        /// Returns false when the read fails.
        /// </summary>
        bool TryReadLevelTime(IntPtr processHandle, out int levelTime, out int win32Error);
    }
}
