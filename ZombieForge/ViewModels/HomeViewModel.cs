using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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

        private IGameHandler _handler;
        private readonly CancellationTokenSource _cts = new();
        private readonly ILogger<HomeViewModel> _logger;
        private readonly DispatcherQueue _dispatcher;
        private readonly GameSession _session = new();
        private readonly object _sessionLock = new();
        private readonly HashSet<string> _loggedReadFailureWarningKeys = [];

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

        public HomeViewModel(DispatcherQueue dispatcher, IGameHandler handler)
        {
            _dispatcher = dispatcher;
            _handler    = handler;
            _logger = App.LoggerFactory.CreateLogger<HomeViewModel>();
            _ = PollAsync(_cts.Token);
        }

        /// <summary>Switches the active handler when the user changes the selected game.</summary>
        public void SetHandler(IGameHandler handler)
        {
            _handler = handler;
        }

        public void OnGameEvent(GameEventArgs args)
        {
            string eventName = Services.LocalizationService.GetString($"EventType_{args.Type}");
            string entry = $"[{DateTime.Now:HH:mm:ss}] {eventName}";
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
            var handler   = _handler; // snapshot to avoid race if SetHandler is called mid-poll
            var processes = handler.ProcessNames
                .SelectMany(n => Process.GetProcessesByName(n))
                .ToArray();
            if (processes.Length == 0)
            {
                bool wasActive;
                lock (_sessionLock)
                {
                    // Session lifetime is owned here: if no selected game process is running,
                    // the run is over, whereas a zero/invalid level timer in GameSession only suppresses display output.
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
                if (!handler.TryReadPlayerStats(handle, moduleBase, 0, out var stats, out int statsReadError))
                {
                    LogReadFailure("player stats", proc.Id, handler, statsReadError);
                    return;
                }

                if (!handler.TryReadLevelTime(handle, out int levelTime, out int levelTimeReadError))
                {
                    LogReadFailure("level time", proc.Id, handler, levelTimeReadError);
                    return;
                }

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

        private void LogReadFailure(string dataType, int pid, IGameHandler handler, int win32Error)
        {
            if (win32Error == 0)
            {
                _logger.LogDebug(
                    "Read unavailable for DataType={DataType}, PID={Pid}, Handler={Handler}",
                    dataType,
                    pid,
                    handler.GetType().Name);
                return;
            }

            string warningKey = $"{dataType}|{handler.GetType().FullName}|{win32Error}";
            if (_loggedReadFailureWarningKeys.Add(warningKey))
            {
                _logger.LogWarning(
                    "Failed to read DataType={DataType}, PID={Pid}, Handler={Handler}, Win32Error={Error}",
                    dataType,
                    pid,
                    handler.GetType().Name,
                    win32Error);
                return;
            }

            _logger.LogDebug(
                "Suppressed repeated read failure DataType={DataType}, PID={Pid}, Handler={Handler}, Win32Error={Error}",
                dataType,
                pid,
                handler.GetType().Name,
                win32Error);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
