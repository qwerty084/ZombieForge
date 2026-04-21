using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Windows.Storage;
using ZombieForge.Models;
using ZombieForge.Services;
using ZombieForge.Services.Games;

namespace ZombieForge.ViewModels
{
    /// <summary>
    /// Coordinates game process detection, DLL monitor lifecycle, and top-level connection state.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private const string MonitorDllName = "BlackOpsMonitor.dll";
        private const string SettingKeyGame  = "SelectedGame";

        private static readonly IGameHandler[] _handlers =
        [
            new BlackOps1Handler(),
            new BlackOps2Handler(),
        ];

        // One ProcessWatcher per handler, indexed in sync with _handlers.
        private readonly ProcessWatcher[] _watchers;
        private readonly DispatcherQueue _dispatcher;
        private readonly ILogger<MainViewModel> _logger;
        private bool _isGameRunning;
        private GameCompatibilityState _compatibilityState = GameCompatibilityState.Unknown;
        private int  _selectedGameIndex;
        private int _connectionGeneration;
        private GameEventMonitor? _eventMonitor;

        /// <summary>
        /// Occurs when a bindable property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Occurs when a game event is received from the monitor.
        /// </summary>
        public event EventHandler<GameEventArgs>? GameEventReceived;

        // ── Stable display list ────────────────────────────────────────────────
        /// <summary>
        /// Gets the localized list of available game entries.
        /// </summary>
        public IReadOnlyList<string> AvailableGames { get; }

        // ── Selected game ──────────────────────────────────────────────────────
        /// <summary>
        /// Gets or sets the selected game index.
        /// </summary>
        public int SelectedGameIndex
        {
            get => _selectedGameIndex;
            set
            {
                if (_selectedGameIndex == value) return;
                _selectedGameIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveHandler));
                SaveGameSelection(value);
                _logger.LogInformation("Game selection changed to {Game}", AvailableGames[value]);
            }
        }

        /// <summary>
        /// Gets the currently active game handler.
        /// </summary>
        public IGameHandler ActiveHandler => _handlers[_selectedGameIndex];

        // ── Connection status ──────────────────────────────────────────────────
        /// <summary>
        /// Gets a value that indicates whether a supported game process is currently running and connected.
        /// </summary>
        public bool IsGameRunning
        {
            get => _isGameRunning;
            private set
            {
                if (_isGameRunning == value) return;
                _isGameRunning = value;
                _logger.LogInformation("Game state changed: {State}", value ? "Connected" : "Not Connected");
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        /// <summary>
        /// Gets the localized connection status text for the shell UI.
        /// </summary>
        public string StatusText
        {
            get
            {
                if (!IsGameRunning)
                    return Services.LocalizationService.GetString("StatusNotConnected");

                return _compatibilityState switch
                {
                    GameCompatibilityState.UnsupportedVersion or GameCompatibilityState.HookInstallFailed
                        => Services.LocalizationService.GetString("StatusUnsupportedVersion"),
                    _ => Services.LocalizationService.GetString("StatusConnected"),
                };
            }
        }

        // ── Constructor ────────────────────────────────────────────────────────
        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="dispatcher">The UI dispatcher queue used for main-thread callbacks.</param>
        public MainViewModel(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
            _logger = App.LoggerFactory.CreateLogger<MainViewModel>();

            AvailableGames =
            [
                LocalizationService.GetString("GameNameBlackOps1"),
                LocalizationService.GetString("GameNameBlackOps2"),
            ];

            _selectedGameIndex = LoadGameSelection();

            _watchers = new ProcessWatcher[_handlers.Length];
            for (int i = 0; i < _handlers.Length; i++)
            {
                int capturedIndex = i;
                var watcher = new ProcessWatcher();
                watcher.ProcessStarted += (_, _) => _dispatcher.TryEnqueue(() => OnDetectedGameStart(capturedIndex));
                watcher.ProcessStopped += (_, _) => _dispatcher.TryEnqueue(OnGameStopped);
                watcher.Watch(_handlers[i].ProcessNames);
                _watchers[i] = watcher;
            }

            // If any game is already running when the app starts, connect immediately.
            for (int i = 0; i < _handlers.Length; i++)
            {
                if (IsAnyProcessRunning(_handlers[i]))
                {
                    _selectedGameIndex = i;
                    RunBackground(() => OnGameStartedAsync(NextConnectionGeneration()), "OnGameStartedAsync");
                    break;
                }
            }
        }

        // ── Auto-switch when a game process starts ─────────────────────────────
        private void OnDetectedGameStart(int handlerIndex)
        {
            if (_selectedGameIndex != handlerIndex)
            {
                _selectedGameIndex = handlerIndex;
                OnPropertyChanged(nameof(SelectedGameIndex));
                OnPropertyChanged(nameof(ActiveHandler));
                SaveGameSelection(handlerIndex);
                _logger.LogInformation("Auto-switched to {Game}", AvailableGames[handlerIndex]);
            }
            RunBackground(() => OnGameStartedAsync(NextConnectionGeneration()), "OnGameStartedAsync");
        }

        private async Task OnGameStartedAsync(int generation)
        {
            if (generation != Volatile.Read(ref _connectionGeneration))
                return;

            SetCompatibilityState(GameCompatibilityState.Unknown);

            var handler   = ActiveHandler;
            var processes = handler.ProcessNames
                .SelectMany(n => Process.GetProcessesByName(n))
                .ToArray();
            if (processes.Length == 0)
            {
                IsGameRunning = false;
                return;
            }

            int pid;
            try
            {
                pid = processes[0].Id;
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
            }

            if (generation != Volatile.Read(ref _connectionGeneration))
            {
                IsGameRunning = false;
                return;
            }

            string dllPath = Path.Combine(AppContext.BaseDirectory, MonitorDllName);

            bool injected = await Task.Run(() => DllInjector.Inject(pid, dllPath, _logger));

            if (!injected)
            {
                IsGameRunning = false;
                return;
            }

            if (generation != Volatile.Read(ref _connectionGeneration))
            {
                IsGameRunning = false;
                return;
            }

            if (_eventMonitor != null)
            {
                IsGameRunning = true;
                return;
            }

            IsGameRunning = true;

            _eventMonitor = new GameEventMonitor();
            _eventMonitor.GameEventReceived += OnDllGameEvent;
            _eventMonitor.CompatibilityStateChanged += OnCompatibilityStateChanged;
            _eventMonitor.Start();
        }

        private void OnGameStopped()
        {
            Interlocked.Increment(ref _connectionGeneration);
            IsGameRunning = false;
            SetCompatibilityState(GameCompatibilityState.Unknown);
            StopEventMonitor();
        }

        private void OnDllGameEvent(object? sender, GameEventArgs args)
        {
            _dispatcher.TryEnqueue(() =>
            {
                _logger.LogInformation("Game event: {EventType} at {Timestamp}ms", args.Type, args.Timestamp);
                GameEventReceived?.Invoke(this, args);
            });
        }

        private void OnCompatibilityStateChanged(GameCompatibilityState compatibilityState)
        {
            _dispatcher.TryEnqueue(() => SetCompatibilityState(compatibilityState));
        }

        private void SetCompatibilityState(GameCompatibilityState compatibilityState)
        {
            if (_compatibilityState == compatibilityState) return;
            _compatibilityState = compatibilityState;
            _logger.LogInformation("Game compatibility state changed: {CompatibilityState}", compatibilityState);
            OnPropertyChanged(nameof(StatusText));
        }

        // ── Persistence ────────────────────────────────────────────────────────
        private int LoadGameSelection()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings;
                if (settings.Values.TryGetValue(SettingKeyGame, out var raw) && raw is int index
                    && index >= 0 && index < _handlers.Length)
                    return index;
            }
            catch (COMException ex)
            {
                _logger.LogWarning(ex, "Local settings are unavailable. Falling back to default game selection");
            }
            return 0;
        }

        private void SaveGameSelection(int index)
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values[SettingKeyGame] = index;
            }
            catch (COMException ex)
            {
                _logger.LogWarning(ex, "Failed to persist game selection in local settings");
            }
        }

        // ── IDisposable ────────────────────────────────────────────────────────
        /// <summary>
        /// Releases process watchers and monitor resources.
        /// </summary>
        public void Dispose()
        {
            Interlocked.Increment(ref _connectionGeneration);

            foreach (var w in _watchers)
                w.Dispose();

            StopEventMonitor();
        }

        private static bool IsAnyProcessRunning(IGameHandler handler)
        {
            foreach (string processName in handler.ProcessNames)
            {
                var processes = Process.GetProcessesByName(processName);
                try
                {
                    if (processes.Length > 0)
                        return true;
                }
                finally
                {
                    foreach (var process in processes)
                        process.Dispose();
                }
            }

            return false;
        }

        private int NextConnectionGeneration()
            => Interlocked.Increment(ref _connectionGeneration);

        private void StopEventMonitor()
        {
            if (_eventMonitor != null)
            {
                var monitor = _eventMonitor;
                _eventMonitor = null;

                monitor.GameEventReceived -= OnDllGameEvent;
                monitor.CompatibilityStateChanged -= OnCompatibilityStateChanged;

                _ = Task.Run(() =>
                {
                    try
                    {
                        monitor.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to dispose game event monitor");
                    }
                });
            }
        }

        private void RunBackground(Func<Task> operation, string operationName)
        {
            Task task;
            try
            {
                task = operation();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background operation {Operation} failed before scheduling", operationName);
                return;
            }

            _ = task.ContinueWith(
                t => _logger.LogWarning(t.Exception, "Background operation {Operation} failed", operationName),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
