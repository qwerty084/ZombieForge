using System;
using System.IO;
using System.Linq;
using Xunit;
using ZombieForge.Models;
using ZombieForge.Services;

namespace ZombieForge.Tests.Services
{
    public class GameHistoryTrackerTests : IDisposable
    {
        private readonly string _filePath;
        private readonly GameHistoryStore _store;
        private readonly GameHistoryTracker _tracker;
        private int _historyChangedCount;

        public GameHistoryTrackerTests()
        {
            _filePath = Path.Combine(Path.GetTempPath(), $"zf_tracker_test_{Guid.NewGuid()}.json");
            _store = new GameHistoryStore(_filePath);
            _tracker = new GameHistoryTracker(_store);
            _tracker.HistoryChanged += (_, _) => _historyChangedCount++;
        }

        public void Dispose()
        {
            try { File.Delete(_filePath); } catch { }
        }

        [Fact]
        public void OnGameStarted_CreatesNewSession()
        {
            _tracker.OnGameStarted();

            Assert.NotNull(_tracker.CurrentEntry);
            Assert.True(_tracker.CurrentEntry!.IsRunning);

            var entries = _store.GetEntries();
            Assert.Single(entries);
            Assert.True(entries[0].IsRunning);
        }

        [Fact]
        public void OnGameStarted_WhenAlreadyRunning_DoesNotCreateDuplicate()
        {
            _tracker.OnGameStarted();
            var firstId = _tracker.CurrentEntry!.SessionId;

            _tracker.OnGameStarted();

            Assert.Equal(firstId, _tracker.CurrentEntry!.SessionId);
            Assert.Single(_store.GetEntries());
        }

        [Fact]
        public void OnGameEvent_StartOfRound_IncrementsRound()
        {
            _tracker.OnGameStarted();

            _tracker.OnGameEvent(new GameEventArgs { Type = GameEventType.StartOfRound, Timestamp = 1000 });

            Assert.Equal(1, _tracker.CurrentEntry!.FinalRound);

            _tracker.OnGameEvent(new GameEventArgs { Type = GameEventType.StartOfRound, Timestamp = 60000 });

            Assert.Equal(2, _tracker.CurrentEntry!.FinalRound);
        }

        [Fact]
        public void OnGameEvent_EndGame_ClosesSession()
        {
            _tracker.OnGameStarted();

            _tracker.OnGameEvent(new GameEventArgs { Type = GameEventType.EndGame, Timestamp = 120000 });

            Assert.Null(_tracker.CurrentEntry);

            var entries = _store.GetEntries();
            Assert.Single(entries);
            Assert.False(entries[0].IsRunning);
            Assert.NotNull(entries[0].EndedAtUtc);
        }

        [Fact]
        public void OnGameStopped_ClosesActiveSession()
        {
            _tracker.OnGameStarted();

            _tracker.OnGameStopped();

            Assert.Null(_tracker.CurrentEntry);

            var entries = _store.GetEntries();
            Assert.Single(entries);
            Assert.False(entries[0].IsRunning);
        }

        [Fact]
        public void OnGameStopped_WhenNoSession_DoesNothing()
        {
            _tracker.OnGameStopped();

            Assert.Null(_tracker.CurrentEntry);
            Assert.Empty(_store.GetEntries());
        }

        [Fact]
        public void UpdateStats_SetsPlayerStatsOnCurrentEntry()
        {
            _tracker.OnGameStarted();

            _tracker.UpdateStats(new PlayerStats
            {
                Points = 1000,
                Kills = 50,
                Downs = 1,
                Headshots = 30,
            });

            Assert.Equal(1000, _tracker.CurrentEntry!.Points);
            Assert.Equal(50, _tracker.CurrentEntry.Kills);
            Assert.Equal(1, _tracker.CurrentEntry.Downs);
            Assert.Equal(30, _tracker.CurrentEntry.Headshots);
        }

        [Fact]
        public void UpdateStats_WhenNoSession_DoesNothing()
        {
            _tracker.UpdateStats(new PlayerStats
            {
                Points = 1000,
                Kills = 50,
                Downs = 1,
                Headshots = 30,
            });

            Assert.Null(_tracker.CurrentEntry);
        }

        [Fact]
        public void UpdateStats_WhenStatsUnchanged_DoesNotRaiseEvent()
        {
            _tracker.OnGameStarted();
            var stats = new PlayerStats { Points = 500, Kills = 10, Downs = 0, Headshots = 5 };
            _tracker.UpdateStats(stats);

            int countAfterFirstUpdate = _historyChangedCount;

            // Same stats again — should not raise event
            _tracker.UpdateStats(new PlayerStats { Points = 500, Kills = 10, Downs = 0, Headshots = 5 });

            Assert.Equal(countAfterFirstUpdate, _historyChangedCount);
        }

        [Fact]
        public void ConstructorClosesOrphanedSessions()
        {
            // Create a store with a running entry
            var entry = new GameHistoryEntry
            {
                SessionId = "orphan",
                StartedAtUtc = DateTime.UtcNow.AddHours(-1),
                IsRunning = true,
            };
            _store.AddOrUpdateEntry(entry);

            // Creating a new tracker should close orphaned sessions
            var newTracker = new GameHistoryTracker(_store);

            var entries = _store.GetEntries();
            Assert.Single(entries);
            Assert.False(entries[0].IsRunning);
            Assert.NotNull(entries[0].EndedAtUtc);
        }

        [Fact]
        public void HistoryChanged_RaisedOnGameStarted()
        {
            int before = _historyChangedCount;

            _tracker.OnGameStarted();

            Assert.True(_historyChangedCount > before);
        }

        [Fact]
        public void HistoryChanged_RaisedOnGameStopped()
        {
            _tracker.OnGameStarted();
            int before = _historyChangedCount;

            _tracker.OnGameStopped();

            Assert.True(_historyChangedCount > before);
        }

        [Fact]
        public void FullSessionLifecycle_CreateUpdateClose()
        {
            _tracker.OnGameStarted();
            var sessionId = _tracker.CurrentEntry!.SessionId;

            _tracker.OnGameEvent(new GameEventArgs { Type = GameEventType.StartOfRound, Timestamp = 1000 });
            _tracker.UpdateStats(new PlayerStats { Points = 500, Kills = 10, Downs = 0, Headshots = 5 });

            _tracker.OnGameEvent(new GameEventArgs { Type = GameEventType.StartOfRound, Timestamp = 60000 });
            _tracker.UpdateStats(new PlayerStats { Points = 2000, Kills = 50, Downs = 1, Headshots = 25 });

            _tracker.OnGameEvent(new GameEventArgs { Type = GameEventType.EndGame, Timestamp = 120000 });

            Assert.Null(_tracker.CurrentEntry);

            var entries = _store.GetEntries();
            Assert.Single(entries);
            var completed = entries[0];
            Assert.Equal(sessionId, completed.SessionId);
            Assert.Equal(2, completed.FinalRound);
            Assert.Equal(2000, completed.Points);
            Assert.Equal(50, completed.Kills);
            Assert.Equal(1, completed.Downs);
            Assert.Equal(25, completed.Headshots);
            Assert.False(completed.IsRunning);
        }
    }
}
