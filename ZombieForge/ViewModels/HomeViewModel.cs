using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using ZombieForge.Models;
using ZombieForge.Services;
using ZombieForge.Services.Games;

namespace ZombieForge.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged, IDisposable
    {
        private const int MaxEventLogEntries = 50;

        private readonly BlackOps1Handler _handler = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly ILogger<HomeViewModel> _logger;
        private readonly DispatcherQueue _dispatcher;

        private int _points;
        private int _kills;
        private int _downs;
        private int _headshots;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Points
        {
            get => _points;
            private set { if (_points != value) { _points = value; OnPropertyChanged(); } }
        }

        public int Kills
        {
            get => _kills;
            private set { if (_kills != value) { _kills = value; OnPropertyChanged(); } }
        }

        public int Downs
        {
            get => _downs;
            private set { if (_downs != value) { _downs = value; OnPropertyChanged(); } }
        }

        public int Headshots
        {
            get => _headshots;
            private set { if (_headshots != value) { _headshots = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<string> EventLog { get; } = [];

        public HomeViewModel(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
            _logger = App.LoggerFactory.CreateLogger<HomeViewModel>();
            _ = PollAsync(_cts.Token);
        }

        public void OnGameEvent(GameEventType eventType)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {eventType}";
            EventLog.Insert(0, entry);
            while (EventLog.Count > MaxEventLogEntries)
                EventLog.RemoveAt(EventLog.Count - 1);
        }

        private async Task PollAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                    Poll();
            }
            catch (OperationCanceledException) { }
        }

        private void Poll()
        {
            var processes = Process.GetProcessesByName(_handler.ProcessName);
            if (processes.Length == 0) return;

            var proc = processes[0];

            long moduleBase;
            try { moduleBase = (long)proc.MainModule!.BaseAddress; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read module base for PID={Pid}", proc.Id);
                return;
            }

            var handle = MemoryService.OpenGameProcess(proc.Id);
            if (handle == IntPtr.Zero)
            {
                _logger.LogWarning("OpenProcess failed for PID={Pid}, Win32Error={Error}", proc.Id, Marshal.GetLastWin32Error());
                return;
            }

            try
            {
                var stats = _handler.ReadPlayerStats(handle, moduleBase, 0);
                _dispatcher.TryEnqueue(() =>
                {
                    Points = stats.Points;
                    Kills = stats.Kills;
                    Downs = stats.Downs;
                    Headshots = stats.Headshots;
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read player stats");
            }
            finally
            {
                MemoryService.CloseGameProcess(handle);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
