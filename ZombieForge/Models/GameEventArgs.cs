using System;

namespace ZombieForge.Models
{
    public class GameEventArgs : EventArgs
    {
        public GameEventType Type      { get; init; }
        public int           Timestamp { get; init; }  // level time (ms) at the moment of the event
    }
}
