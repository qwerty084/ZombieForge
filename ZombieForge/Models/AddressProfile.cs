namespace ZombieForge.Models
{
    public class AddressProfile
    {
        public required string Version { get; init; }
        public required long BaseOffset { get; init; }
        public required int Stride { get; init; }
        public required int PointsOffset { get; init; }
        public required int KillsOffset { get; init; }
        public required int DownsOffset { get; init; }
        public required int HeadshotsOffset { get; init; }

        /// <summary>
        /// Absolute (non-module-relative) address of the server/level time counter (int32, ms).
        /// Confirmed at 0x0286D014 for Global patch.
        /// </summary>
        public required long LevelTimeAddress { get; init; }
    }
}
