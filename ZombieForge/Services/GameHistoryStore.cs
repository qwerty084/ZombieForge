using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZombieForge.Models;

namespace ZombieForge.Services
{
    /// <summary>
    /// Persists game history entries to a JSON file on disk and keeps an in-memory cache.
    /// Thread-safe; retains at most <see cref="MaxCompletedEntries"/> completed entries
    /// (running entries are always kept).
    /// </summary>
    public class GameHistoryStore
    {
        private const int MaxCompletedEntries = 5;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
        };

        private readonly ILogger<GameHistoryStore> _logger;
        private readonly string _filePath;
        private readonly object _lock = new();
        private List<GameHistoryEntry> _entries;

        /// <summary>
        /// Initializes a new <see cref="GameHistoryStore"/> using the default history file path
        /// (<c>%LocalAppData%\ZombieForge\history.json</c>).
        /// </summary>
        public GameHistoryStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZombieForge",
                "history.json"))
        {
        }

        /// <summary>
        /// Initializes a new <see cref="GameHistoryStore"/> with an explicit file path.
        /// Useful for testing with temporary files.
        /// </summary>
        /// <param name="filePath">Absolute path to the JSON history file.</param>
        public GameHistoryStore(string filePath)
        {
            ArgumentNullException.ThrowIfNull(filePath);

            _logger = App.LoggerFactory.CreateLogger<GameHistoryStore>();
            _filePath = filePath;
            _entries = LoadEntries();
        }

        /// <summary>
        /// Loads history entries from disk. Returns an empty list if the file does not exist
        /// or cannot be read.
        /// </summary>
        public List<GameHistoryEntry> LoadEntries()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_filePath))
                    {
                        return [];
                    }

                    string json = File.ReadAllText(_filePath);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return [];
                    }

                    return JsonSerializer.Deserialize<List<GameHistoryEntry>>(json, _jsonOptions) ?? [];
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize history from {FilePath}", _filePath);
                    return [];
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Failed to read history file {FilePath}", _filePath);
                    return [];
                }
            }
        }

        /// <summary>
        /// Saves the supplied entries to disk. Trims completed entries to the most recent
        /// <see cref="MaxCompletedEntries"/> before writing; running entries are always kept.
        /// </summary>
        /// <param name="entries">Entries to persist.</param>
        public void SaveEntries(List<GameHistoryEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            lock (_lock)
            {
                SaveEntriesLocked(entries);
            }
        }

        /// <summary>
        /// Adds a new entry or updates an existing one (matched by <see cref="GameHistoryEntry.SessionId"/>)
        /// in the in-memory list, then saves to disk.
        /// </summary>
        /// <param name="entry">The entry to add or update.</param>
        public void AddOrUpdateEntry(GameHistoryEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            lock (_lock)
            {
                int index = _entries.FindIndex(e => e.SessionId == entry.SessionId);

                if (index >= 0)
                {
                    _entries[index] = entry;
                }
                else
                {
                    _entries.Add(entry);
                }

                SaveEntriesLocked(_entries);
            }
        }

        private void SaveEntriesLocked(List<GameHistoryEntry> entries)
        {
            List<GameHistoryEntry> running = entries.Where(e => e.IsRunning).ToList();

            List<GameHistoryEntry> completed = entries
                .Where(e => !e.IsRunning)
                .OrderByDescending(e => e.StartedAtUtc)
                .Take(MaxCompletedEntries)
                .ToList();

            List<GameHistoryEntry> trimmed = [.. running, .. completed];
            trimmed.Sort((a, b) => b.StartedAtUtc.CompareTo(a.StartedAtUtc));

            _entries = trimmed;

            try
            {
                string? directory = Path.GetDirectoryName(_filePath);

                if (directory is not null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(trimmed, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to write history file {FilePath}", _filePath);
            }
        }

        /// <summary>
        /// Returns a copy of the current in-memory entry list.
        /// </summary>
        public List<GameHistoryEntry> GetEntries()
        {
            lock (_lock)
            {
                return [.. _entries];
            }
        }
    }
}
