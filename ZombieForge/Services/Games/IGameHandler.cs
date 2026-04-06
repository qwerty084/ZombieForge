using System;
using ZombieForge.Models;

namespace ZombieForge.Services.Games
{
    public interface IGameHandler
    {
        string ProcessName { get; }
        PlayerStats ReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex);
    }
}
