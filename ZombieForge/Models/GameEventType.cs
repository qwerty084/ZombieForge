namespace ZombieForge.Models
{
    // IPC contract: keep values and order identical with BlackOpsMonitor::GameEventType.
    // Protocol documentation source: docs/ipc-protocol.md.
    public enum GameEventType : int
    {
        None = 0,
        StartOfRound = 1,
        EndOfRound = 2,
        PowerupGrabbed = 3,
        DogRound = 4,
        PowerOn = 5,
        EndGame = 6,
        PerkPurchased = 7,
    }
}
