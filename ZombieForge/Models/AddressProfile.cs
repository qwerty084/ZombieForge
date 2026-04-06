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
    }
}
