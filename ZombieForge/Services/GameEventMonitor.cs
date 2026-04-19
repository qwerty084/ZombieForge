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
        private const int EventRingCapacity = 64;
        private const int EventSlotSize = 12;

        // SharedGameState field offsets (must match C++ #pragma pack(push, 1) layout)
        private const int DllReadyOffset = 0;
        private const int CompatibilityStateOffset = 4;
        private const int EventHeadOffset = 8;
        private const int EventTailOffset = 12;
        private const int DroppedEventsOffset = 16;
        private const int ProtocolVersionOffset = 20;
        private const int EventRingOffset = 24;
        private const int ExpectedProtocolVersion = 1;

        // SharedEventSlot field offsets
        private const int EventTypeInSlotOffset = 0;
        private const int EventTimestampInSlotOffset = 4;
        private const int EventValueInSlotOffset = 8;

        private readonly ILogger<GameEventMonitor> _logger;
        private readonly CancellationTokenSource _cts = new();
        private Task _runTask = Task.CompletedTask;
        private int _lastDroppedEvents;
        private GameCompatibilityState _lastCompatibilityState = GameCompatibilityState.Unknown;

        private MemoryMappedFile? _mmf;
        private MemoryMappedViewAccessor? _accessor;
        private EventWaitHandle? _event;

        public event EventHandler<GameEventArgs>? GameEventReceived;
        public event Action<GameCompatibilityState>? CompatibilityStateChanged;

        public GameEventMonitor()
        {
            _logger = App.LoggerFactory.CreateLogger<GameEventMonitor>();
        }

        public void Start()
        {
            if (!_runTask.IsCompleted)
                return;

            _logger.LogInformation("Game event monitor starting");
            _runTask = Task.Run(() => RunAsync(_cts.Token));
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
                        PublishCompatibilityStateIfChanged();
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                }

                if (ct.IsCancellationRequested || _event is null || _accessor is null)
                {
                    return;
                }

                WaitHandle[] waitHandles = [_event, ct.WaitHandle];
                while (!ct.IsCancellationRequested)
                {
                    int signaledHandle = WaitHandle.WaitAny(waitHandles, 250);
                    if (signaledHandle == 1)
                    {
                        break;
                    }

                    if (signaledHandle == 0)
                    {
                        DrainPendingEvents();
                    }

                    PublishCompatibilityStateIfChanged();
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
                _lastDroppedEvents = _accessor.ReadInt32(DroppedEventsOffset);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Shared memory not available yet: {Message}", ex.Message);
                ReleaseIpc();
                return false;
            }
        }

        private void DrainPendingEvents()
        {
            if (_accessor == null)
                return;

            int head = _accessor.ReadInt32(EventHeadOffset);
            int tail = _accessor.ReadInt32(EventTailOffset);

            if (tail > head || head - tail > EventRingCapacity)
            {
                _logger.LogWarning("Invalid shared-memory ring state detected (head={Head}, tail={Tail}); resyncing", head, tail);
                _accessor.Write(EventTailOffset, head);
                tail = head;
            }

            while (tail < head)
            {
                int slotIndex = tail % EventRingCapacity;
                int slotOffset = EventRingOffset + (slotIndex * EventSlotSize);

                var eventType = (GameEventType)_accessor.ReadInt32(slotOffset + EventTypeInSlotOffset);
                int timestamp = _accessor.ReadInt32(slotOffset + EventTimestampInSlotOffset);
                int eventValue = _accessor.ReadInt32(slotOffset + EventValueInSlotOffset);

                if (eventType != GameEventType.None)
                {
                    _logger.LogInformation(
                        "Game event received: {EventType} at {Timestamp}ms with value {EventValue}",
                        eventType, timestamp, eventValue);
                    GameEventReceived?.Invoke(this, new GameEventArgs { Type = eventType, Timestamp = timestamp });
                }

                tail++;
            }

            _accessor.Write(EventTailOffset, tail);

            int droppedEvents = _accessor.ReadInt32(DroppedEventsOffset);
            if (droppedEvents > _lastDroppedEvents)
            {
                _logger.LogWarning(
                    "Native event ring dropped {DroppedSinceLastCheck} events ({DroppedTotal} total)",
                    droppedEvents - _lastDroppedEvents,
                    droppedEvents);
            }

            _lastDroppedEvents = droppedEvents;
        }

        private void PublishCompatibilityStateIfChanged()
        {
            if (_accessor == null)
                return;

            int rawState = _accessor.ReadInt32(CompatibilityStateOffset);
            GameCompatibilityState state = rawState switch
            {
                (int)GameCompatibilityState.Unknown => GameCompatibilityState.Unknown,
                (int)GameCompatibilityState.Compatible => GameCompatibilityState.Compatible,
                (int)GameCompatibilityState.UnsupportedVersion => GameCompatibilityState.UnsupportedVersion,
                (int)GameCompatibilityState.HookInstallFailed => GameCompatibilityState.HookInstallFailed,
                _ => GameCompatibilityState.Unknown,
            };

            if (state == _lastCompatibilityState)
                return;

            _lastCompatibilityState = state;
            _logger.LogInformation("Game compatibility state changed: {CompatibilityState}", state);
            CompatibilityStateChanged?.Invoke(state);
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
            _event?.Set();
            try
            {
                _runTask.Wait();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerException is OperationCanceledException)
            {
            }

            ReleaseIpc();
            _cts.Dispose();
        }
    }
}
