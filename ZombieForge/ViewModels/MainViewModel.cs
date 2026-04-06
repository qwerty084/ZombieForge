using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using ZombieForge.Models;
using ZombieForge.Services;

namespace ZombieForge.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private const string MonitorDllName = "BlackOpsMonitor.dll";

        private readonly ProcessWatcher _watcher = new();
        private readonly DispatcherQueue _dispatcher;
        private readonly ILogger<MainViewModel> _logger;
        private bool _isGameRunning;
        private GameEventMonitor? _eventMonitor;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<GameEventArgs>? GameEventReceived;

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
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public string StatusText => IsGameRunning ? "Game Connected" : "Game Not Connected";

        public SolidColorBrush StatusBrush => IsGameRunning
            ? new SolidColorBrush(Colors.LimeGreen)
            : new SolidColorBrush(Colors.Gray);

        public MainViewModel(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
            _logger = App.LoggerFactory.CreateLogger<MainViewModel>();

            _watcher.ProcessStarted += (_, _) => _dispatcher.TryEnqueue(() => _ = OnGameStartedAsync());
            _watcher.ProcessStopped += (_, _) => _dispatcher.TryEnqueue(OnGameStopped);
            _watcher.Watch("BlackOps.exe");

            // If the game is already running when the app starts, inject immediately.
            if (Process.GetProcessesByName("BlackOps").Length > 0)
            {
                IsGameRunning = true;
                _ = OnGameStartedAsync();
            }
        }

        private async Task OnGameStartedAsync()
        {
            IsGameRunning = true;

            var processes = Process.GetProcessesByName("BlackOps");
            if (processes.Length == 0) return;

            int pid = processes[0].Id;
            string dllPath = Path.Combine(AppContext.BaseDirectory, MonitorDllName);

            // Run injection on a background thread — WaitForSingleObject(INFINITE) must not block the UI thread.
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

        public void Dispose()
        {
            _watcher.Dispose();
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
