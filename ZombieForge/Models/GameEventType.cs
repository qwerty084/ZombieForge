namespace ZombieForge.Models
{
    // IPC contract: keep values and order identical with BlackOpsMonitor::GameEventType.
    // Protocol documentation source: docs/ipc-protocol.md.
    /// <summary>
    /// Enumerates event types sent through the shared-memory IPC ring.
    /// </summary>
    public enum GameEventType : int
    {
        /// <summary>
        /// Indicates that no event is present in the slot.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that a new round has started.
        /// </summary>
        StartOfRound = 1,

        /// <summary>
        /// Indicates that the current round has ended.
        /// </summary>
        EndOfRound = 2,

        /// <summary>
        /// Indicates that a power-up has been collected.
        /// </summary>
        PowerupGrabbed = 3,

        /// <summary>
        /// Indicates that a dog round has started.
        /// </summary>
        DogRound = 4,

        /// <summary>
        /// Indicates that the map power has been turned on.
        /// </summary>
        PowerOn = 5,

        /// <summary>
        /// Indicates that the game has ended.
        /// </summary>
        EndGame = 6,

        /// <summary>
        /// Indicates that a perk has been purchased.
        /// </summary>
        PerkPurchased = 7,
    }
}
