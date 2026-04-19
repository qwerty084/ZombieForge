using System;
using System.IO;
using System.Linq;
using Xunit;
using ZombieForge.Models;
using ZombieForge.Services;

namespace ZombieForge.Tests.Services
{
    public class GameHistoryStoreTests : IDisposable
    {
        private readonly string _filePath;
        private readonly GameHistoryStore _store;

        public GameHistoryStoreTests()
        {
            _filePath = Path.Combine(Path.GetTempPath(), $"zf_test_{Guid.NewGuid()}.json");
            _store = new GameHistoryStore(_filePath);
        }

        public void Dispose()
        {
            try { File.Delete(_filePath); } catch { }
        }

        [Fact]
        public void GetEntries_WhenEmpty_ReturnsEmptyList()
        {
            var entries = _store.GetEntries();

            Assert.Empty(entries);
        }

        [Fact]
        public void AddOrUpdateEntry_AddsNewEntry()
        {
            var entry = CreateEntry("session-1");

            _store.AddOrUpdateEntry(entry);

            var entries = _store.GetEntries();
            Assert.Single(entries);
            Assert.Equal("session-1", entries[0].SessionId);
        }

        [Fact]
        public void AddOrUpdateEntry_UpdatesExistingEntry()
        {
            var entry = CreateEntry("session-1");
            _store.AddOrUpdateEntry(entry);

            entry.Kills = 42;
            _store.AddOrUpdateEntry(entry);

            var entries = _store.GetEntries();
            Assert.Single(entries);
            Assert.Equal(42, entries[0].Kills);
        }

        [Fact]
        public void SaveEntries_TrimsToMaxFiveCompletedEntries()
        {
            for (int i = 0; i < 8; i++)
            {
                _store.AddOrUpdateEntry(new GameHistoryEntry
                {
                    SessionId = $"session-{i}",
                    StartedAtUtc = DateTime.UtcNow.AddHours(-i),
                    IsRunning = false,
                });
            }

            var entries = _store.GetEntries();
            Assert.Equal(5, entries.Count);
        }

        [Fact]
        public void SaveEntries_KeepsRunningEntriesBeyondMax()
        {
            for (int i = 0; i < 5; i++)
            {
                _store.AddOrUpdateEntry(new GameHistoryEntry
                {
                    SessionId = $"completed-{i}",
                    StartedAtUtc = DateTime.UtcNow.AddHours(-i - 1),
                    IsRunning = false,
                });
            }

            var runningEntry = new GameHistoryEntry
            {
                SessionId = "running-1",
                StartedAtUtc = DateTime.UtcNow,
                IsRunning = true,
            };
            _store.AddOrUpdateEntry(runningEntry);

            var entries = _store.GetEntries();
            Assert.Equal(6, entries.Count);
            Assert.Contains(entries, e => e.SessionId == "running-1" && e.IsRunning);
        }

        [Fact]
        public void SaveEntries_SortsNewestFirst()
        {
            var older = new GameHistoryEntry
            {
                SessionId = "old",
                StartedAtUtc = DateTime.UtcNow.AddHours(-2),
                IsRunning = false,
            };
            var newer = new GameHistoryEntry
            {
                SessionId = "new",
                StartedAtUtc = DateTime.UtcNow.AddHours(-1),
                IsRunning = false,
            };

            _store.AddOrUpdateEntry(older);
            _store.AddOrUpdateEntry(newer);

            var entries = _store.GetEntries();
            Assert.Equal("new", entries[0].SessionId);
            Assert.Equal("old", entries[1].SessionId);
        }

        [Fact]
        public void Persistence_RoundTrip_SurvivesReload()
        {
            var entry = new GameHistoryEntry
            {
                SessionId = "persist-1",
                MapName = "Kino der Toten",
                StartedAtUtc = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                EndedAtUtc = new DateTime(2025, 1, 15, 11, 0, 0, DateTimeKind.Utc),
                IsRunning = false,
                FinalRound = 25,
                Points = 50000,
                Kills = 300,
                Downs = 2,
                Headshots = 150,
            };
            _store.AddOrUpdateEntry(entry);

            // Create new store from same file to simulate restart
            var reloadedStore = new GameHistoryStore(_filePath);
            var entries = reloadedStore.GetEntries();

            Assert.Single(entries);
            var loaded = entries[0];
            Assert.Equal("persist-1", loaded.SessionId);
            Assert.Equal("Kino der Toten", loaded.MapName);
            Assert.Equal(25, loaded.FinalRound);
            Assert.Equal(50000, loaded.Points);
            Assert.Equal(300, loaded.Kills);
            Assert.Equal(2, loaded.Downs);
            Assert.Equal(150, loaded.Headshots);
            Assert.False(loaded.IsRunning);
        }

        [Fact]
        public void LoadEntries_WithCorruptFile_ReturnsEmptyList()
        {
            File.WriteAllText(_filePath, "not valid json{{{");

            var store = new GameHistoryStore(_filePath);
            var entries = store.GetEntries();

            Assert.Empty(entries);
        }

        [Fact]
        public void GetEntries_ReturnsCopy()
        {
            _store.AddOrUpdateEntry(CreateEntry("s1"));

            var first = _store.GetEntries();
            first.Clear();

            var second = _store.GetEntries();
            Assert.Single(second);
        }

        private static GameHistoryEntry CreateEntry(string sessionId)
        {
            return new GameHistoryEntry
            {
                SessionId = sessionId,
                StartedAtUtc = DateTime.UtcNow,
                IsRunning = false,
            };
        }
    }
}
