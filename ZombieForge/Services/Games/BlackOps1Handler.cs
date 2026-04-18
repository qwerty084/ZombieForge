using System;
using ZombieForge.Models;

namespace ZombieForge.Services.Games
{
    public class BlackOps1Handler : IGameHandler
    {
        private readonly AddressProfile _profile;

        public BlackOps1Handler(AddressProfile? profile = null)
        {
            _profile = profile ?? BO1AddressProfiles.Default;
        }

        public string[] ProcessNames => ["BlackOps"];

        public bool TryReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex, out PlayerStats stats, out int win32Error)
        {
            long baseAddr = moduleBase + _profile.BaseOffset + (_profile.Stride * playerIndex);
            if (!MemoryService.TryReadInt32(processHandle, baseAddr + _profile.PointsOffset, out int points, out win32Error) ||
                !MemoryService.TryReadInt32(processHandle, baseAddr + _profile.KillsOffset, out int kills, out win32Error) ||
                !MemoryService.TryReadInt32(processHandle, baseAddr + _profile.DownsOffset, out int downs, out win32Error) ||
                !MemoryService.TryReadInt32(processHandle, baseAddr + _profile.HeadshotsOffset, out int headshots, out win32Error))
            {
                stats = new PlayerStats();
                return false;
            }

            stats = new PlayerStats
            {
                Points    = points,
                Kills     = kills,
                Downs     = downs,
                Headshots = headshots,
            };
            return true;
        }

        public bool TryReadLevelTime(IntPtr processHandle, out int levelTime, out int win32Error)
            => MemoryService.TryReadInt32(processHandle, _profile.LevelTimeAddress, out levelTime, out win32Error);
    }
}
