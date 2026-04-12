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

        public PlayerStats ReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex)
        {
            long baseAddr = moduleBase + _profile.BaseOffset + (_profile.Stride * playerIndex);
            return new PlayerStats
            {
                Points    = MemoryService.ReadInt32(processHandle, baseAddr + _profile.PointsOffset),
                Kills     = MemoryService.ReadInt32(processHandle, baseAddr + _profile.KillsOffset),
                Downs     = MemoryService.ReadInt32(processHandle, baseAddr + _profile.DownsOffset),
                Headshots = MemoryService.ReadInt32(processHandle, baseAddr + _profile.HeadshotsOffset),
            };
        }

        public int ReadLevelTime(IntPtr processHandle)
            => MemoryService.ReadInt32(processHandle, _profile.LevelTimeAddress);
    }
}
