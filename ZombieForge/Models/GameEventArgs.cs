using System;

namespace ZombieForge.Models
{
    /// <summary>
    /// Provides data for a game event emitted by the injected monitor.
    /// </summary>
    public class GameEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the event type that was emitted.
        /// </summary>
        public GameEventType Type      { get; init; }

        /// <summary>
        /// Gets the level-time timestamp, in milliseconds, when the event occurred.
        /// </summary>
        public int           Timestamp { get; init; }  // level time (ms) at the moment of the event
    }
}
