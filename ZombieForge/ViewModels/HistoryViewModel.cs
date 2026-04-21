using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ZombieForge.Models;
using ZombieForge.Services;

namespace ZombieForge.ViewModels
{
    /// <summary>
    /// ViewModel for the History page. Provides a filtered, searchable list of
    /// game history entries and exposes map filter options.
    /// </summary>
    public class HistoryViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly GameHistoryTracker _tracker;
        private readonly ILogger<HistoryViewModel> _logger;
        private string _searchText = string.Empty;
        private string _selectedMapFilter = string.Empty;
        private List<GameHistoryEntry> _filteredEntries = [];
        private List<string> _mapFilterOptions = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new <see cref="HistoryViewModel"/> that listens for history changes.
        /// </summary>
        public HistoryViewModel(GameHistoryTracker tracker)
        {
            ArgumentNullException.ThrowIfNull(tracker);

            _tracker = tracker;
            _logger = App.LoggerFactory.CreateLogger<HistoryViewModel>();

            _tracker.HistoryChanged += OnHistoryChanged;

            Refresh();
        }

        /// <summary>Search text applied across map name, round, and stats fields.</summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        /// <summary>
        /// The currently selected map filter. Empty string means "All".
        /// </summary>
        public string SelectedMapFilter
        {
            get => _selectedMapFilter;
            set
            {
                if (_selectedMapFilter == value) return;
                _selectedMapFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        /// <summary>Available map names for the filter ComboBox, including "All" as the first option.</summary>
        public List<string> MapFilterOptions
        {
            get => _mapFilterOptions;
            private set
            {
                _mapFilterOptions = value;
                OnPropertyChanged();
            }
        }

        /// <summary>The filtered and sorted list of history entries to display.</summary>
        public List<GameHistoryEntry> FilteredEntries
        {
            get => _filteredEntries;
            private set
            {
                _filteredEntries = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Reloads data from the store/tracker and recomputes filters.</summary>
        public void Refresh()
        {
            RebuildMapFilterOptions();
            ApplyFilter();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _tracker.HistoryChanged -= OnHistoryChanged;
        }

        private void OnHistoryChanged(object? sender, EventArgs e)
        {
            Refresh();
        }

        private void RebuildMapFilterOptions()
        {
            var entries = GetAllEntries();
            var maps = entries
                .Select(e => e.MapName)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var options = new List<string>(maps.Count + 1)
            {
                string.Empty, // "All" option
            };
            options.AddRange(maps);

            MapFilterOptions = options;

            if (!options.Contains(_selectedMapFilter, StringComparer.OrdinalIgnoreCase))
            {
                _selectedMapFilter = string.Empty;
                OnPropertyChanged(nameof(SelectedMapFilter));
            }
        }

        private void ApplyFilter()
        {
            var entries = GetAllEntries();

            // Apply map filter
            if (!string.IsNullOrEmpty(_selectedMapFilter))
            {
                entries = entries
                    .Where(e => string.Equals(e.MapName, _selectedMapFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply search text across multiple fields
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string search = _searchText.Trim();
                entries = entries
                    .Where(e => MatchesSearch(e, search))
                    .ToList();
            }

            // Sort by newest first, running entries at the top
            entries.Sort((a, b) =>
            {
                if (a.IsRunning != b.IsRunning)
                    return a.IsRunning ? -1 : 1;
                return b.StartedAtUtc.CompareTo(a.StartedAtUtc);
            });

            FilteredEntries = entries;
        }

        private List<GameHistoryEntry> GetAllEntries()
        {
            return _tracker.Store.GetEntries();
        }

        private static bool MatchesSearch(GameHistoryEntry entry, string search)
        {
            if (entry.MapName.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (entry.FinalRound.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (entry.Kills.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (entry.Points.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (entry.Downs.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (entry.Headshots.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (entry.StartedAtUtc.ToLocalTime().ToString("g").Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
