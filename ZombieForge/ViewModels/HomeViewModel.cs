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
        private readonly GameHistoryTracker? _historyTracker;
        private readonly object _sessionLock = new();
        private readonly object _processHandleLock = new();
        private readonly HashSet<string> _loggedReadFailureWarningKeys = [];
        private readonly Task _pollTask;
        private int _disposeState;
        private IntPtr _gameProcessHandle = IntPtr.Zero;
        private int _gameProcessId = -1;
        private DateTime _gameProcessStartTimeUtc = DateTime.MinValue;
        private long _gameProcessModuleBase;

        private int _points;
        private int _kills;
        private int _downs;
        private int _headshots;
        private string _gameTimer = "--:--:--";
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

        public HomeViewModel(DispatcherQueue dispatcher, IGameHandler handler, GameHistoryTracker? historyTracker = null)
        {
            _dispatcher = dispatcher;
            _handler = handler;
            _historyTracker = historyTracker;
            _logger = App.LoggerFactory.CreateLogger<HomeViewModel>();
            _pollTask = RunBackground(() => PollAsync(_cts.Token), "PollAsync");
        }

        /// <summary>Switches the active handler when the user changes the selected game.</summary>
        public void SetHandler(IGameHandler handler)
        {
            _handler = handler;
            CloseGameProcessHandle();
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
                        GameTimer = "--:--:--";
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Polling loop failed unexpectedly");
            }
        }

        private void Poll()
        {
            var handler = _handler; // snapshot to avoid race if SetHandler is called mid-poll
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
                        GameTimer = "--:--:--";
                        RoundTimer = "--:--";
                    });
                }
                CloseGameProcessHandle();
                return;
            }

            int processId;
            DateTime processStartTimeUtc;
            long moduleBase;
            try
            {
                var proc = processes[0];
                processId = proc.Id;
                processStartTimeUtc = proc.StartTime.ToUniversalTime();
                moduleBase = (long)proc.MainModule!.BaseAddress;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to inspect selected game process");
                CloseGameProcessHandle();
                return;
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
            }

            PlayerStats stats;
            int levelTime;
            try
            {
                lock (_processHandleLock)
                {
                    if (!EnsureGameProcessLocked(processId, processStartTimeUtc, moduleBase))
                        return;

                    var handle = _gameProcessHandle;
                    if (!handler.TryReadPlayerStats(handle, moduleBase, 0, out stats, out int statsReadError))
                    {
                        LogReadFailure("player stats", processId, handler, statsReadError);
                        return;
                    }

                    if (!handler.TryReadLevelTime(handle, out levelTime, out int levelTimeReadError))
                    {
                        LogReadFailure("level time", processId, handler, levelTimeReadError);
                        return;
                    }
                }

                string gameTimer;
                string roundTimer;
                lock (_sessionLock)
                {
                    gameTimer = _session.FormatGameTime(levelTime);
                    roundTimer = _session.FormatRoundTime(levelTime);
                }

                _dispatcher.TryEnqueue(() =>
                {
                    Points = stats.Points;
                    Kills = stats.Kills;
                    Downs = stats.Downs;
                    Headshots = stats.Headshots;
                    GameTimer = gameTimer;
                    RoundTimer = roundTimer;
                });

                _historyTracker?.UpdateStats(stats);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read game data");
                CloseGameProcessHandle();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeState, 1) == 1)
                return;

            _cts.Cancel();
            _ = _pollTask.ContinueWith(
                t =>
                {
                    try
                    {
                        if (t.IsFaulted)
                            _logger.LogWarning(t.Exception, "Polling task failed during shutdown");

                        CloseGameProcessHandle();
                    }
                    finally
                    {
                        _cts.Dispose();
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        private bool EnsureGameProcessLocked(int processId, DateTime processStartTimeUtc, long moduleBase)
        {
            if (_gameProcessHandle != IntPtr.Zero
                && _gameProcessId == processId
                && _gameProcessStartTimeUtc == processStartTimeUtc
                && _gameProcessModuleBase == moduleBase)
            {
                return true;
            }

            CloseGameProcessHandleNoLock();

            var handle = MemoryService.OpenGameProcess(processId);
            if (handle == IntPtr.Zero)
            {
                _logger.LogWarning("OpenProcess failed for PID={Pid}, Win32Error={Error}", processId, Marshal.GetLastWin32Error());
                return false;
            }

            _gameProcessHandle = handle;
            _gameProcessId = processId;
            _gameProcessStartTimeUtc = processStartTimeUtc;
            _gameProcessModuleBase = moduleBase;
            return true;
        }

        private void CloseGameProcessHandle()
        {
            lock (_processHandleLock)
                CloseGameProcessHandleNoLock();
        }

        private void CloseGameProcessHandleNoLock()
        {
            if (_gameProcessHandle != IntPtr.Zero)
            {
                MemoryService.CloseGameProcess(_gameProcessHandle);
                _gameProcessHandle = IntPtr.Zero;
            }

            _gameProcessId = -1;
            _gameProcessStartTimeUtc = DateTime.MinValue;
            _gameProcessModuleBase = 0;
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

        private Task RunBackground(Func<Task> operation, string operationName)
        {
            Task task;
            try
            {
                task = operation();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background operation {Operation} failed before scheduling", operationName);
                return Task.CompletedTask;
            }

            _ = task.ContinueWith(
                t => _logger.LogWarning(t.Exception, "Background operation {Operation} failed", operationName),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            return task;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
