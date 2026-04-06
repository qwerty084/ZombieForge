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
        private readonly GameSession _session = new();
        private readonly object _sessionLock = new();

        private int _points;
        private int _kills;
        private int _downs;
        private int _headshots;
        private string _gameTimer  = "--:--:--";
        private string _roundTimer = "--:--";

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

        /// <summary>Elapsed time since the game started, formatted as HH:MM:SS.</summary>
        public string GameTimer
        {
            get => _gameTimer;
            private set { if (_gameTimer != value) { _gameTimer = value; OnPropertyChanged(); } }
        }

        /// <summary>Elapsed time since the current round started, formatted as MM:SS.</summary>
        public string RoundTimer
        {
            get => _roundTimer;
            private set { if (_roundTimer != value) { _roundTimer = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<string> EventLog { get; } = [];

        public HomeViewModel(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
            _logger = App.LoggerFactory.CreateLogger<HomeViewModel>();
            _ = PollAsync(_cts.Token);
        }

        public void OnGameEvent(GameEventArgs args)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {args.Type}";
            EventLog.Insert(0, entry);
            while (EventLog.Count > MaxEventLogEntries)
                EventLog.RemoveAt(EventLog.Count - 1);

            lock (_sessionLock)
            {
                switch (args.Type)
                {
                    case GameEventType.StartOfRound:
                        _session.OnRoundStart(args.Timestamp);
                        break;
                    case GameEventType.EndOfRound:
                        _session.OnRoundEnd(args.Timestamp);
                        break;
                    case GameEventType.EndGame:
                        _session.Reset();
                        GameTimer  = "--:--:--";
                        RoundTimer = "--:--";
                        break;
                }
            }
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
            if (processes.Length == 0)
            {
                bool wasActive;
                lock (_sessionLock)
                {
                    wasActive = _session.IsActive;
                    if (wasActive) _session.Reset();
                }
                if (wasActive)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        GameTimer  = "--:--:--";
                        RoundTimer = "--:--";
                    });
                }
                return;
            }

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
                var stats     = _handler.ReadPlayerStats(handle, moduleBase, 0);
                int levelTime = _handler.ReadLevelTime(handle);

                string gameTimer;
                string roundTimer;
                lock (_sessionLock)
                {
                    gameTimer  = _session.FormatGameTime(levelTime);
                    roundTimer = _session.FormatRoundTime(levelTime);
                }

                _dispatcher.TryEnqueue(() =>
                {
                    Points     = stats.Points;
                    Kills      = stats.Kills;
                    Downs      = stats.Downs;
                    Headshots  = stats.Headshots;
                    GameTimer  = gameTimer;
                    RoundTimer = roundTimer;
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read game data");
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
