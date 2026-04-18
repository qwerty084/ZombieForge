using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ZombieForge.Models;
using ZombieForge.Services;

namespace ZombieForge.ViewModels
{
    public class ConfigViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<ConfigViewModel> _logger;

        private ConfigData? _configData;
        private string      _configPath    = string.Empty;
        private bool        _configFound;
        private string      _statusMessage = string.Empty;
        private bool        _hasUnsavedChanges;

        // Dvar backing fields
        private int    _fov         = 65;
        private int    _maxFps      = 85;
        private double _sensitivity = 5.0;
        private bool   _fullscreen;
        private string _resolution  = "1920x1080";
        private string _aspectRatio = "auto";
        private bool   _isSaving;

        public event PropertyChangedEventHandler? PropertyChanged;

        // ──────────────────────────────────────────────
        //  Status
        // ──────────────────────────────────────────────

        public bool ConfigFound
        {
            get => _configFound;
            private set { if (_configFound != value) { _configFound = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConfigNotFound)); } }
        }

        public bool ConfigNotFound => !_configFound;

        public string ConfigPath
        {
            get => _configPath;
            private set { if (_configPath != value) { _configPath = value; OnPropertyChanged(); } }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set { if (_hasUnsavedChanges != value) { _hasUnsavedChanges = value; OnPropertyChanged(); } }
        }

        // ──────────────────────────────────────────────
        //  Dvars
        // ──────────────────────────────────────────────

        public int Fov
        {
            get => _fov;
            set { if (_fov != value) { _fov = value; OnPropertyChanged(); MarkDirty(); } }
        }

        public int MaxFps
        {
            get => _maxFps;
            set { if (_maxFps != value) { _maxFps = value; OnPropertyChanged(); MarkDirty(); } }
        }

        public double Sensitivity
        {
            get => _sensitivity;
            set { if (Math.Abs(_sensitivity - value) > 0.001) { _sensitivity = value; OnPropertyChanged(); MarkDirty(); } }
        }

        public bool Fullscreen
        {
            get => _fullscreen;
            set { if (_fullscreen != value) { _fullscreen = value; OnPropertyChanged(); MarkDirty(); } }
        }

        public string Resolution
        {
            get => _resolution;
            set { if (_resolution != value) { _resolution = value; OnPropertyChanged(); MarkDirty(); } }
        }

        public string AspectRatio
        {
            get => _aspectRatio;
            set { if (_aspectRatio != value) { _aspectRatio = value; OnPropertyChanged(); MarkDirty(); } }
        }

        // ──────────────────────────────────────────────
        //  Combo sources
        // ──────────────────────────────────────────────

        public IReadOnlyList<string> Resolutions { get; } =
        [
            "800x600", "1024x768", "1280x720", "1280x800", "1280x1024",
            "1360x768", "1366x768", "1440x900", "1600x900",
            "1920x1080", "1920x1200", "2560x1440", "3840x2160",
        ];

        public IReadOnlyList<string> AspectRatios { get; } =
        [
            "auto", "4:3", "16:9", "16:10",
        ];

        public IReadOnlyList<string> BindableKeys { get; } =
        [
            "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
            "KP_1","KP_2","KP_3","KP_4","KP_5","KP_6","KP_7","KP_8","KP_9","KP_0",
            "KP_PLUS","KP_MINUS","KP_STAR","KP_SLASH","KP_ENTER","KP_DEL","KP_INS",
            "INS","DEL","HOME","END","PGUP","PGDN",
            "UP","DOWN","LEFT","RIGHT",
            "0","1","2","3","4","5","6","7","8","9",
            "MINUS","EQUALS","LBRACKET","RBRACKET","SEMICOLON","APOSTROPHE",
            "COMMA","PERIOD","SLASH","BACKSLASH","BACKSPACE","PAUSE",
        ];

        // ──────────────────────────────────────────────
        //  Binds
        // ──────────────────────────────────────────────

        public ObservableCollection<BindEntry> CurrentBinds { get; } = [];

        // ──────────────────────────────────────────────
        //  Presets
        // ──────────────────────────────────────────────

        public IReadOnlyList<PresetCommand> Presets { get; } =
        [
            // Kino Der Toten teleport spots
            new("Kino Der Toten", "Fire Trap",      "setviewpos -1250 -630 200"),
            new("Kino Der Toten", "Alley",           "setviewpos -1490 220 100"),
            new("Kino Der Toten", "Boiler Room",     "setviewpos -1400 990 170"),
            new("Kino Der Toten", "Stage",           "setviewpos -40 1830 20"),
            new("Kino Der Toten", "Theater",         "setviewpos 40 140 80"),
            new("Kino Der Toten", "Dressing Room",   "setviewpos 1350 1300 60"),
            new("Kino Der Toten", "Kitchen",         "setviewpos 1650 700 60"),
            new("Kino Der Toten", "Balcony",         "setviewpos 900 -630 390"),
            // Map launches
            new("Map Launch", "Kino Der Toten",      "map zombie_theater"),
            new("Map Launch", "Ascension",           "map zombietron"),
            new("Map Launch", "Call of the Dead",    "map zombie_cosmodrome"),
            new("Map Launch", "Shangri-La",          "map zombie_shino"),
            new("Map Launch", "Moon",                "map zombie_moon"),
            new("Map Launch", "Five",                "map zombie_pentagon"),
            // Utility
            new("Utility", "Unlock FPS",             "com_maxfps 1000"),
            new("Utility", "Lock FPS 60",            "com_maxfps 60"),
            new("Utility", "FOV 90",                 "cg_fov 90"),
            new("Utility", "FOV 65 (default)",       "cg_fov 65"),
        ];

        // ──────────────────────────────────────────────
        //  Commands
        // ──────────────────────────────────────────────

        public ICommand SaveCommand       { get; }
        public ICommand RemoveBindCommand { get; }
        public ICommand OpenFolderCommand { get; }

        // ──────────────────────────────────────────────
        //  Constructor
        // ──────────────────────────────────────────────

        public ConfigViewModel()
        {
            _logger = App.LoggerFactory.CreateLogger<ConfigViewModel>();

            SaveCommand = new RelayCommand(ExecuteSave, () => ConfigFound);
            RemoveBindCommand = new RelayCommand<BindEntry>(RemoveBind);
            OpenFolderCommand = new RelayCommand(OpenFolder, () => ConfigFound);

            LoadConfig();
        }

        // ──────────────────────────────────────────────
        //  Load
        // ──────────────────────────────────────────────

        private void LoadConfig()
        {
            ConfigFound = BO1ConfigService.TryFindConfigPath(out var path);
            ConfigPath  = path;

            if (!ConfigFound)
            {
                _logger.LogWarning("BO1 config not found at {Path}", path);
                return;
            }

            try
            {
                _configData = BO1ConfigService.Load(path);
                ApplyDvars(_configData.Dvars);
                ApplyBinds(_configData.Binds);
                HasUnsavedChanges = false;
                StatusMessage = string.Empty;
                _logger.LogInformation("Loaded config from {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load config from {Path}", path);
                StatusMessage = $"Error loading config: {ex.Message}";
            }
        }

        private void ApplyDvars(Dictionary<string, string> dvars)
        {
            if (dvars.TryGetValue("cg_fov_default", out var fovStr) && int.TryParse(fovStr, out var fov))
                _fov = Math.Clamp(fov, 50, 120);

            if (dvars.TryGetValue("com_maxfps", out var fpsStr) && int.TryParse(fpsStr, out var fps))
                _maxFps = Math.Clamp(fps, 1, 1000);

            if (dvars.TryGetValue("sensitivity", out var senStr) && double.TryParse(senStr,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var sen))
                _sensitivity = Math.Clamp(sen, 0.1, 30.0);

            if (dvars.TryGetValue("r_fullscreen", out var fsStr))
                _fullscreen = fsStr == "1";

            if (dvars.TryGetValue("r_mode", out var res))
                _resolution = NormaliseResolution(res);

            if (dvars.TryGetValue("r_aspectRatio", out var ar))
                _aspectRatio = ar;

            // Fire change notifications after bulk load
            OnPropertyChanged(nameof(Fov));
            OnPropertyChanged(nameof(MaxFps));
            OnPropertyChanged(nameof(Sensitivity));
            OnPropertyChanged(nameof(Fullscreen));
            OnPropertyChanged(nameof(Resolution));
            OnPropertyChanged(nameof(AspectRatio));
        }

        private void ApplyBinds(Dictionary<string, string> binds)
        {
            CurrentBinds.Clear();
            foreach (var kv in binds)
                CurrentBinds.Add(new BindEntry(kv.Key, kv.Value));
        }

        // ──────────────────────────────────────────────
        //  Save
        // ──────────────────────────────────────────────

        private void ExecuteSave()
        {
            if (_isSaving || _configData is null) return;
            _isSaving = true;
            try
            {
                // Push local values back into the config data
                _configData.Dvars["cg_fov_default"] = _fov.ToString();
                _configData.Dvars["com_maxfps"]     = _maxFps.ToString();
                _configData.Dvars["sensitivity"]    = _sensitivity.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
                _configData.Dvars["r_fullscreen"]   = _fullscreen ? "1" : "0";
                _configData.Dvars["r_mode"]         = _resolution;
                _configData.Dvars["r_aspectRatio"]  = _aspectRatio;

                // Rebuild binds from ObservableCollection
                _configData.Binds.Clear();
                foreach (var entry in CurrentBinds)
                    _configData.Binds[entry.Key.ToUpperInvariant()] = entry.Command;

                BO1ConfigService.Save(ConfigPath, _configData);

                // Reload from disk so OriginalLines and RemovedBindKeys are in sync
                _configData = BO1ConfigService.Load(ConfigPath);

                HasUnsavedChanges = false;
                StatusMessage = $"Saved at {DateTime.Now:HH:mm:ss}. Restart the game for changes to take effect.";
                _logger.LogInformation("Config saved to {Path}", ConfigPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save config to {Path}", ConfigPath);
                StatusMessage = $"Error saving: {ex.Message}";
            }
            finally
            {
                _isSaving = false;
            }
        }

        // ──────────────────────────────────────────────
        //  Bind helpers
        // ──────────────────────────────────────────────

        public void AddOrUpdateBind(string key, string command)
        {
            key = key.ToUpperInvariant().Trim();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command))
                return;

            // Replace existing entry for this key
            for (int i = 0; i < CurrentBinds.Count; i++)
            {
                if (string.Equals(CurrentBinds[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    CurrentBinds[i] = new BindEntry(key, command);
                    MarkDirty();
                    return;
                }
            }

            CurrentBinds.Add(new BindEntry(key, command));
            MarkDirty();
        }

        private void RemoveBind(BindEntry? entry)
        {
            if (entry is null) return;
            _configData?.RemovedBindKeys.Add(entry.Key);
            CurrentBinds.Remove(entry);
            MarkDirty();
        }

        private void OpenFolder()
        {
            try
            {
                var folder = System.IO.Path.GetDirectoryName(ConfigPath);
                if (folder is not null)
                    System.Diagnostics.Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to open folder {Folder}", folder);
            }
        }

        // ──────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────

        private void MarkDirty() => HasUnsavedChanges = true;

        private static string NormaliseResolution(string raw)
        {
            // Config stores e.g. "1920x1080" already — normalise case / whitespace
            return raw.Replace(" ", "").ToLowerInvariant() switch
            {
                "800x600"   => "800x600",
                "1024x768"  => "1024x768",
                "1280x720"  => "1280x720",
                "1280x800"  => "1280x800",
                "1280x1024" => "1280x1024",
                "1360x768"  => "1360x768",
                "1366x768"  => "1366x768",
                "1440x900"  => "1440x900",
                "1600x900"  => "1600x900",
                "1920x1080" => "1920x1080",
                "1920x1200" => "1920x1200",
                "2560x1440" => "2560x1440",
                "3840x2160" => "3840x2160",
                _           => raw,
            };
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ──────────────────────────────────────────────────────────────
    //  Minimal ICommand helpers (no MVVM framework dependency)
    // ──────────────────────────────────────────────────────────────

    internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => execute();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    internal sealed class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => execute((T?)parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
