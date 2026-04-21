using System;
using ZombieForge.Models;

namespace ZombieForge.Services.Games
{
    /// <summary>
    /// Defines game-specific process detection and memory-read operations.
    /// </summary>
    public interface IGameHandler
    {
        /// <summary>
        /// All process names (without .exe) that identify this game.
        /// Supports multiple executables, e.g. vanilla + Plutonium builds.
        /// </summary>
        string[] ProcessNames { get; }

        /// <summary>
        /// Reads player statistics for a specific player index.
        /// </summary>
        /// <param name="processHandle">A handle to the target game process.</param>
        /// <param name="moduleBase">The base address of the target process main module.</param>
        /// <param name="playerIndex">The zero-based player index to read.</param>
        /// <param name="stats">When this method returns, contains the player stats when the read succeeds. This parameter is treated as uninitialized.</param>
        /// <param name="win32Error">When this method returns, contains the last Win32 error when the read fails; otherwise, <c>0</c>. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true" /> if stats were read successfully; otherwise, <see langword="false" />.</returns>
        bool TryReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex, out PlayerStats stats, out int win32Error);

        /// <summary>
        /// Reads the game's internal level-time clock (milliseconds).
        /// Uses an absolute address — no module base required.
        /// Returns false when the read fails.
        /// </summary>
        bool TryReadLevelTime(IntPtr processHandle, out int levelTime, out int win32Error);
    }
}
