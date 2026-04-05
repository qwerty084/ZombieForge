using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using ZombieForge.Services;

namespace ZombieForge.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ProcessWatcher _watcher = new();
        private readonly DispatcherQueue _dispatcher;
        private readonly ILogger<MainViewModel> _logger;
        private bool _isGameRunning;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsGameRunning
        {
            get => _isGameRunning;
            private set
            {
                if (_isGameRunning == value) return;
                _isGameRunning = value;
                _logger.LogInformation("Game state changed: {State}", value ? "Running" : "Not Detected");
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public string StatusText => IsGameRunning ? "Game Detected" : "Game Not Detected";

        public SolidColorBrush StatusBrush => IsGameRunning
            ? new SolidColorBrush(Colors.LimeGreen)
            : new SolidColorBrush(Colors.Gray);

        public MainViewModel(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
            _logger = App.LoggerFactory.CreateLogger<MainViewModel>();

            IsGameRunning = Process.GetProcessesByName("BlackOps").Length > 0;

            _watcher.ProcessStarted += (_, _) => _dispatcher.TryEnqueue(() => IsGameRunning = true);
            _watcher.ProcessStopped += (_, _) => _dispatcher.TryEnqueue(() => IsGameRunning = false);
            _watcher.Watch("BlackOps.exe");
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
