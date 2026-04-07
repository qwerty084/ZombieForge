using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ZombieForge.Models;
using ZombieForge.Services;
using ZombieForge.Services.Games;

namespace ZombieForge.ViewModels
{
    public class ConfigViewModel : INotifyPropertyChanged
    {
        private readonly ConfigService _configService = new();
        private readonly ILogger<ConfigViewModel> _logger;

        private BO1Config? _config;
        private string _configFilePath = string.Empty;
        private bool _isFileLoaded;
        private string? _loadError;
        private bool _hasUnsavedChanges;
        private bool _gameIsRunning;

        public event PropertyChangedEventHandler? PropertyChanged;

        // ── Commands ──────────────────────────────────────────────────────────

        public ICommand AutoDetectCommand { get; }
        public ICommand BrowseCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DiscardCommand { get; }

        // ── Properties ────────────────────────────────────────────────────────

        public string ConfigFilePath
        {
            get => _configFilePath;
            set { if (_configFilePath == value) return; _configFilePath = value; OnPropertyChanged(); }
        }

        public bool IsFileLoaded
        {
            get => _isFileLoaded;
            private set
            {
                if (_isFileLoaded == value) return;
                _isFileLoaded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanDiscard));
            }
        }

        public string? LoadError
        {
            get => _loadError;
            private set { if (_loadError == value) return; _loadError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLoadError)); }
        }

        public bool HasLoadError => LoadError is not null;

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                if (_hasUnsavedChanges == value) return;
                _hasUnsavedChanges = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanDiscard));
            }
        }

        public bool CanSave => HasUnsavedChanges && IsFileLoaded;
        public bool CanDiscard => HasUnsavedChanges && IsFileLoaded;

        /// <summary>True when BlackOps.exe is running; triggers the "save while closed" warning.</summary>
        public bool GameIsRunning
        {
            get => _gameIsRunning;
            private set { if (_gameIsRunning == value) return; _gameIsRunning = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BindEntryViewModel> Bindings { get; } = new();
        public ObservableCollection<CvarEntryViewModel> CvarSettings { get; } = new();

        // Bind collections per category
        public ObservableCollection<BindEntryViewModel> MovementBindings { get; } = new();
        public ObservableCollection<BindEntryViewModel> CombatBindings { get; } = new();
        public ObservableCollection<BindEntryViewModel> InteractionBindings { get; } = new();
        public ObservableCollection<BindEntryViewModel> CommunicationBindings { get; } = new();
        public ObservableCollection<BindEntryViewModel> UtilityBindings { get; } = new();

        // Filtered CVar collections per tab — rebuilt on each load
        public ObservableCollection<CvarEntryViewModel> GraphicsPerformanceCVars { get; } = new();
        public ObservableCollection<CvarEntryViewModel> MouseCVars { get; } = new();
        public ObservableCollection<CvarEntryViewModel> AudioCVars { get; } = new();

        /// <summary>All raw lines in the currently loaded file, for the Advanced tab.</summary>
        public string RawFileText { get; private set; } = string.Empty;

        // ── Constructor ───────────────────────────────────────────────────────

        public ConfigViewModel()
        {
            _logger = App.LoggerFactory.CreateLogger<ConfigViewModel>();

            AutoDetectCommand = new RelayCommand(AutoDetect);
            BrowseCommand = new RelayCommand<Action<string?>>(ExecuteBrowse);
            SaveCommand = new RelayCommand(Save, () => HasUnsavedChanges && IsFileLoaded);
            DiscardCommand = new RelayCommand(Discard, () => HasUnsavedChanges && IsFileLoaded);
        }

        // ── Public API called by code-behind ──────────────────────────────────

        public void AutoDetect()
        {
            CheckGameRunning();
            var path = _configService.FindConfigPath();
            if (path is null)
            {
                LoadError = "config.cfg not found at the default location. Use Browse to locate it manually.";
                _logger.LogInformation("Auto-detect found no config.cfg at default path");
                return;
            }
            LoadFile(path);
        }

        public void LoadFile(string path)
        {
            CheckGameRunning();
            try
            {
                _config = _configService.Load(path);
                ConfigFilePath = path;
                LoadError = null;
                IsFileLoaded = true;
                HasUnsavedChanges = false;
                PopulateViewModels();
                _logger.LogInformation("Loaded config from {Path}", path);
            }
            catch (Exception ex)
            {
                LoadError = $"Failed to load file: {ex.Message}";
                IsFileLoaded = false;
                _logger.LogWarning(ex, "Failed to load config from {Path}", path);
            }
        }

        public void Save()
        {
            if (_config is null || string.IsNullOrEmpty(ConfigFilePath)) return;
            try
            {
                _configService.Save(ConfigFilePath, _config);
                HasUnsavedChanges = false;
                LoadError = null;
                _logger.LogInformation("Saved config to {Path}", ConfigFilePath);
            }
            catch (Exception ex)
            {
                LoadError = $"Failed to save file: {ex.Message}";
                _logger.LogWarning(ex, "Failed to save config to {Path}", ConfigFilePath);
            }
        }

        public void Discard()
        {
            if (string.IsNullOrEmpty(ConfigFilePath)) return;
            LoadFile(ConfigFilePath);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void CheckGameRunning()
            => GameIsRunning = Process.GetProcessesByName("BlackOps").Length > 0;

        private void PopulateViewModels()
        {
            if (_config is null) return;

            Bindings.Clear();
            MovementBindings.Clear();
            CombatBindings.Clear();
            InteractionBindings.Clear();
            CommunicationBindings.Clear();
            UtilityBindings.Clear();
            CvarSettings.Clear();

            // Build bind ViewModels from the catalog; pre-fill from loaded config
            foreach (var def in BO1ConfigCatalog.Binds)
            {
                // Find which key is currently mapped to this command (first match)
                var currentKey = _config.Entries
                    .Where(e => e.EntryType == EntryType.Bind &&
                                string.Equals(e.Value, def.Command, StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.Key)
                    .FirstOrDefault() ?? string.Empty;

                var vm = new BindEntryViewModel
                {
                    FriendlyName = def.FriendlyName,
                    Description  = def.Description,
                    Category     = def.Category,
                    Command      = def.Command,
                };

                // Set without triggering the change event yet
                vm.SelectedKey = string.IsNullOrEmpty(currentKey) ? "(Unbound)" : currentKey;

                vm.KeyChanged += OnBindKeyChanged;
                Bindings.Add(vm);

                // Route to per-category collections
                var target = def.Category switch
                {
                    "Movement"      => MovementBindings,
                    "Combat"        => CombatBindings,
                    "Interaction"   => InteractionBindings,
                    "Communication" => CommunicationBindings,
                    "Utility"       => UtilityBindings,
                    _               => null
                };
                target?.Add(vm);
            }

            // Build CVar ViewModels
            GraphicsPerformanceCVars.Clear();
            MouseCVars.Clear();
            AudioCVars.Clear();

            foreach (var def in BO1ConfigCatalog.CVars)
            {
                var rawValue = _config.GetCVar(def.CvarName) ?? def.DefaultValue ?? string.Empty;

                var vm = new CvarEntryViewModel
                {
                    FriendlyName  = def.FriendlyName,
                    Description   = def.Description,
                    Category      = def.Category,
                    CvarName      = def.CvarName,
                    EditorType    = def.EditorType,
                    Min           = def.Min,
                    Max           = def.Max,
                    Step          = def.Step,
                    AllowedValues = def.AllowedValues,
                };

                vm.Value = rawValue;
                vm.ValueChanged += OnCvarValueChanged;
                CvarSettings.Add(vm);

                // Also route to per-tab collections
                if (def.Category is "Graphics" or "Performance" or "HUD" or "Console")
                    GraphicsPerformanceCVars.Add(vm);
                else if (def.Category == "Mouse")
                    MouseCVars.Add(vm);
                else if (def.Category == "Audio")
                    AudioCVars.Add(vm);
            }

            RebuildRawText();
            OnPropertyChanged(nameof(RawFileText));
        }

        private void OnBindKeyChanged(object? sender, string newKey)
        {
            if (_config is null || sender is not BindEntryViewModel vm) return;

            if (newKey == "(Unbound)" || string.IsNullOrEmpty(newKey))
            {
                // Find and remove any bind entry for this command
                var existing = _config.Entries.FirstOrDefault(e =>
                    e.EntryType == EntryType.Bind &&
                    string.Equals(e.Value, vm.Command, StringComparison.OrdinalIgnoreCase));
                if (existing is not null)
                    _config.RemoveBind(existing.Key);
            }
            else
            {
                _config.SetBind(newKey, vm.Command);
            }

            HasUnsavedChanges = true;
            RebuildRawText();
            OnPropertyChanged(nameof(RawFileText));
        }

        private void OnCvarValueChanged(object? sender, string newValue)
        {
            if (_config is null || sender is not CvarEntryViewModel vm) return;
            _config.SetCVar(vm.CvarName, newValue);
            HasUnsavedChanges = true;
            RebuildRawText();
            OnPropertyChanged(nameof(RawFileText));
        }

        private void RebuildRawText()
        {
            if (_config is null) { RawFileText = string.Empty; return; }
            RawFileText = string.Join(Environment.NewLine, _config.Entries.Select(e => e.RawLine));
        }

        // ── Placeholder for browse (actual picker is async; called from code-behind) ──
        private void ExecuteBrowse(Action<string?>? callback) { /* handled by code-behind */ }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Minimal ICommand helpers ───────────────────────────────────────────────

    internal sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    internal sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute(parameter is T t ? t : default);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
