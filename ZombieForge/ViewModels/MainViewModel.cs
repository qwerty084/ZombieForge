using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Windows.Storage;
using ZombieForge.Models;
using ZombieForge.Services;
using ZombieForge.Services.Games;

namespace ZombieForge.ViewModels
{
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
        private int  _selectedGameIndex;
        private GameEventMonitor? _eventMonitor;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<GameEventArgs>? GameEventReceived;

        // ── Stable display list ────────────────────────────────────────────────
        public IReadOnlyList<string> AvailableGames { get; }

        // ── Selected game ──────────────────────────────────────────────────────
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

        public IGameHandler ActiveHandler => _handlers[_selectedGameIndex];

        // ── Connection status ──────────────────────────────────────────────────
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

        public string StatusText => IsGameRunning
            ? Services.LocalizationService.GetString("StatusConnected")
            : Services.LocalizationService.GetString("StatusNotConnected");

        // ── Constructor ────────────────────────────────────────────────────────
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
                if (_handlers[i].ProcessNames.Any(n => Process.GetProcessesByName(n).Length > 0))
                {
                    _selectedGameIndex = i;
                    IsGameRunning = true;
                    _ = OnGameStartedAsync();
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
            _ = OnGameStartedAsync();
        }

        private async Task OnGameStartedAsync()
        {
            IsGameRunning = true;

            var handler   = ActiveHandler;
            var processes = handler.ProcessNames
                .SelectMany(n => Process.GetProcessesByName(n))
                .ToArray();
            if (processes.Length == 0) return;

            int pid = processes[0].Id;
            string dllPath = Path.Combine(AppContext.BaseDirectory, MonitorDllName);

            bool injected = await Task.Run(() => DllInjector.Inject(pid, dllPath, _logger));

            if (injected)
            {
                _eventMonitor = new GameEventMonitor();
                _eventMonitor.GameEventReceived += OnDllGameEvent;
                _eventMonitor.Start();
            }
        }

        private void OnGameStopped()
        {
            IsGameRunning = false;

            if (_eventMonitor != null)
            {
                _eventMonitor.GameEventReceived -= OnDllGameEvent;
                _eventMonitor.Dispose();
                _eventMonitor = null;
            }
        }

        private void OnDllGameEvent(object? sender, GameEventArgs args)
        {
            _dispatcher.TryEnqueue(() =>
            {
                _logger.LogInformation("Game event: {EventType} at {Timestamp}ms", args.Type, args.Timestamp);
                GameEventReceived?.Invoke(this, args);
            });
        }

        // ── Persistence ────────────────────────────────────────────────────────
        private static int LoadGameSelection()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue(SettingKeyGame, out var raw) && raw is int index
                && index >= 0 && index < _handlers.Length)
                return index;
            return 0;
        }

        private static void SaveGameSelection(int index)
            => ApplicationData.Current.LocalSettings.Values[SettingKeyGame] = index;

        // ── IDisposable ────────────────────────────────────────────────────────
        public void Dispose()
        {
            foreach (var w in _watchers)
                w.Dispose();

            if (_eventMonitor != null)
            {
                _eventMonitor.GameEventReceived -= OnDllGameEvent;
                _eventMonitor.Dispose();
                _eventMonitor = null;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
