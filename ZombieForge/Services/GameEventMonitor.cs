using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZombieForge.Models;

namespace ZombieForge.Services
{
    public sealed class GameEventMonitor : IDisposable
    {
        private const string SharedMemName = "BO1MonitorSharedMem";
        private const string EventName = "BO1MonitorEvent";
        private const int SharedMemSize = 4096;

        // SharedGameState field offsets (must match C++ #pragma pack(push, 1) layout)
        private const int LastEventOffset      = 0;
        private const int EventTimestampOffset = 4;
        private const int DllReadyOffset       = 12;
        private const int ProtocolVersionOffset = 16;
        private const int ExpectedProtocolVersion = 1;

        private readonly ILogger<GameEventMonitor> _logger;
        private readonly CancellationTokenSource _cts = new();
        private Task _runTask = Task.CompletedTask;

        private MemoryMappedFile? _mmf;
        private MemoryMappedViewAccessor? _accessor;
        private EventWaitHandle? _event;

        public event EventHandler<GameEventArgs>? GameEventReceived;

        public GameEventMonitor()
        {
            _logger = App.LoggerFactory.CreateLogger<GameEventMonitor>();
        }

        public void Start()
        {
            _logger.LogInformation("Game event monitor starting");
            _runTask = RunAsync(_cts.Token);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (TryConnect())
                    {
                        _logger.LogInformation("Connected to DLL shared memory");
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }

                while (!ct.IsCancellationRequested)
                {
                    bool signaled = await Task.Run(
                        () => _event!.WaitOne(TimeSpan.FromSeconds(5)), ct);
                    if (!signaled) continue;

                    var eventType = (GameEventType)_accessor!.ReadInt32(LastEventOffset);
                    if (eventType != GameEventType.None)
                    {
                        int timestamp = _accessor.ReadInt32(EventTimestampOffset);
                        _logger.LogInformation("Game event received: {EventType} at {Timestamp}ms", eventType, timestamp);
                        _accessor.Write(LastEventOffset, (int)GameEventType.None);
                        GameEventReceived?.Invoke(this, new GameEventArgs { Type = eventType, Timestamp = timestamp });
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Game event monitor encountered an error");
            }
        }

        private bool TryConnect()
        {
            try
            {
                _mmf = MemoryMappedFile.OpenExisting(SharedMemName);
                _accessor = _mmf.CreateViewAccessor(0, SharedMemSize);
                _event = EventWaitHandle.OpenExisting(EventName);

                int dllReady = _accessor.ReadInt32(DllReadyOffset);
                if (dllReady != 1)
                {
                    _logger.LogDebug("Shared memory found but DLL not ready yet");
                    ReleaseIpc();
                    return false;
                }

                int protocolVersion = _accessor.ReadInt32(ProtocolVersionOffset);
                if (protocolVersion != ExpectedProtocolVersion)
                {
                    _logger.LogWarning(
                        "IPC protocol version mismatch. Expected={ExpectedVersion}, Found={FoundVersion}",
                        ExpectedProtocolVersion,
                        protocolVersion);
                    ReleaseIpc();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Shared memory not available yet: {Message}", ex.Message);
                ReleaseIpc();
                return false;
            }
        }

        private void ReleaseIpc()
        {
            _accessor?.Dispose();
            _accessor = null;
            _mmf?.Dispose();
            _mmf = null;
            _event?.Dispose();
            _event = null;
        }

        public void Dispose()
        {
            _cts.Cancel();
            // Wait for RunAsync to exit before releasing IPC handles it may still be using.
            _runTask.Wait();
            ReleaseIpc();
            _cts.Dispose();
        }
    }
}
