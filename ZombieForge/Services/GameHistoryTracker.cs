using System;
using Microsoft.Extensions.Logging;
using ZombieForge.Models;

namespace ZombieForge.Services
{
    /// <summary>
    /// Tracks the lifecycle of zombie game sessions independently of any particular page.
    /// Driven by game events and process state from <c>MainViewModel</c>.
    /// Creates, updates, and closes <see cref="GameHistoryEntry"/> records via
    /// <see cref="GameHistoryStore"/>.
    /// </summary>
    public class GameHistoryTracker
    {
        private readonly GameHistoryStore _store;
        private readonly ILogger<GameHistoryTracker> _logger;
        private readonly object _lock = new();
        private GameHistoryEntry? _currentEntry;

        /// <summary>
        /// Initializes a new <see cref="GameHistoryTracker"/> backed by the given store.
        /// Marks any previously-running entries from a prior app session as completed.
        /// </summary>
        public GameHistoryTracker(GameHistoryStore store)
        {
            ArgumentNullException.ThrowIfNull(store);

            _store = store;
            _logger = App.LoggerFactory.CreateLogger<GameHistoryTracker>();

            CloseOrphanedSessions();
        }

        /// <summary>The backing history store.</summary>
        public GameHistoryStore Store => _store;

        /// <summary>
        /// The currently active session entry, or <c>null</c> if no session is in progress.
        /// </summary>
        public GameHistoryEntry? CurrentEntry
        {
            get
            {
                lock (_lock)
                {
                    return _currentEntry;
                }
            }
        }

        /// <summary>
        /// Raised on the caller's thread whenever the history data changes (session started,
        /// updated, or completed). Listeners should marshal to the UI thread if needed.
        /// </summary>
        public event EventHandler? HistoryChanged;

        /// <summary>
        /// Called when the game process starts. Opens a new session if one is not already active.
        /// </summary>
        public void OnGameStarted()
        {
            lock (_lock)
            {
                if (_currentEntry is not null)
                {
                    return;
                }

                _currentEntry = new GameHistoryEntry
                {
                    StartedAtUtc = DateTime.UtcNow,
                    IsRunning = true,
                };

                _store.AddOrUpdateEntry(_currentEntry);
                _logger.LogInformation("History session started: {SessionId}", _currentEntry.SessionId);
            }

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Processes a game event to update the current session (round progression, end game).
        /// </summary>
        public void OnGameEvent(GameEventArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            bool changed = false;

            lock (_lock)
            {
                if (_currentEntry is null)
                {
                    return;
                }

                switch (args.Type)
                {
                    case GameEventType.StartOfRound:
                        _currentEntry.FinalRound++;
                        _store.AddOrUpdateEntry(_currentEntry);
                        changed = true;
                        break;

                    case GameEventType.EndGame:
                        CloseCurrentSessionLocked();
                        changed = true;
                        break;
                }
            }

            if (changed)
            {
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Updates the current session's player stats snapshot.
        /// Called periodically while a game is running.
        /// </summary>
        public void UpdateStats(PlayerStats stats)
        {
            ArgumentNullException.ThrowIfNull(stats);

            bool changed = false;

            lock (_lock)
            {
                if (_currentEntry is null)
                {
                    return;
                }

                if (_currentEntry.Points != stats.Points
                    || _currentEntry.Kills != stats.Kills
                    || _currentEntry.Downs != stats.Downs
                    || _currentEntry.Headshots != stats.Headshots)
                {
                    _currentEntry.Points = stats.Points;
                    _currentEntry.Kills = stats.Kills;
                    _currentEntry.Downs = stats.Downs;
                    _currentEntry.Headshots = stats.Headshots;

                    _store.AddOrUpdateEntry(_currentEntry);
                    changed = true;
                }
            }

            if (changed)
            {
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the game process stops. Closes the current session if one is active.
        /// </summary>
        public void OnGameStopped()
        {
            bool changed = false;

            lock (_lock)
            {
                if (_currentEntry is not null)
                {
                    CloseCurrentSessionLocked();
                    changed = true;
                }
            }

            if (changed)
            {
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Marks any sessions left as running from a previous app launch as completed.
        /// </summary>
        private void CloseOrphanedSessions()
        {
            var entries = _store.GetEntries();
            bool anyChanged = false;

            foreach (var entry in entries)
            {
                if (entry.IsRunning)
                {
                    entry.IsRunning = false;
                    entry.EndedAtUtc ??= DateTime.UtcNow;
                    _store.AddOrUpdateEntry(entry);
                    _logger.LogInformation("Closed orphaned history session: {SessionId}", entry.SessionId);
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CloseCurrentSessionLocked()
        {
            if (_currentEntry is null)
            {
                return;
            }

            _currentEntry.IsRunning = false;
            _currentEntry.EndedAtUtc = DateTime.UtcNow;
            _store.AddOrUpdateEntry(_currentEntry);
            _logger.LogInformation(
                "History session completed: {SessionId}, Round {Round}",
                _currentEntry.SessionId,
                _currentEntry.FinalRound);
            _currentEntry = null;
        }
    }
}
