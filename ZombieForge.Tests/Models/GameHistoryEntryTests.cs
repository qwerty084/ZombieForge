using System;
using Xunit;
using ZombieForge.Models;

namespace ZombieForge.Tests.Models
{
    public class GameHistoryEntryTests
    {
        [Fact]
        public void DefaultSessionId_IsNonEmpty()
        {
            var entry = new GameHistoryEntry();

            Assert.False(string.IsNullOrEmpty(entry.SessionId));
        }

        [Fact]
        public void DefaultMapName_IsUnknown()
        {
            var entry = new GameHistoryEntry();

            Assert.Equal("Unknown", entry.MapName);
        }

        [Fact]
        public void TwoEntries_HaveDifferentSessionIds()
        {
            var entry1 = new GameHistoryEntry();
            var entry2 = new GameHistoryEntry();

            Assert.NotEqual(entry1.SessionId, entry2.SessionId);
        }

        [Fact]
        public void EndedAtUtc_IsNullByDefault()
        {
            var entry = new GameHistoryEntry();

            Assert.Null(entry.EndedAtUtc);
        }

        [Fact]
        public void MutableProperties_CanBeUpdated()
        {
            var entry = new GameHistoryEntry
            {
                StartedAtUtc = DateTime.UtcNow,
                IsRunning = true,
            };

            entry.IsRunning = false;
            entry.EndedAtUtc = DateTime.UtcNow;
            entry.FinalRound = 15;
            entry.Points = 10000;
            entry.Kills = 200;
            entry.Downs = 3;
            entry.Headshots = 100;

            Assert.False(entry.IsRunning);
            Assert.NotNull(entry.EndedAtUtc);
            Assert.Equal(15, entry.FinalRound);
            Assert.Equal(10000, entry.Points);
            Assert.Equal(200, entry.Kills);
            Assert.Equal(3, entry.Downs);
            Assert.Equal(100, entry.Headshots);
        }
    }
}
