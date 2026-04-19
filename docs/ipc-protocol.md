# BO1 IPC Protocol (BlackOpsMonitor ↔ ZombieForge)

This document defines the **current** inter-process contract between:

- `BlackOpsMonitor` (injected C++ DLL)
- `ZombieForge` (`GameEventMonitor` in C#)

## Named IPC objects and basic flow

| Item | C++ definition | C# definition | Current value |
|---|---|---|---|
| Shared memory name | `SHARED_MEM_NAME` | `SharedMemName` | `BO1MonitorSharedMem` |
| Event name | `EVENT_NAME` | `EventName` | `BO1MonitorEvent` |
| Shared memory size | `SHARED_MEM_SIZE` | `SharedMemSize` | `4096` bytes |

Current startup/event flow:

1. DLL initializes in `dllmain.cpp` (`InitThread`), polls readiness of `scrStringGlob` (100 ms interval, bounded timeout), then creates/open-maps shared memory and creates event.
2. DLL zeroes the mapping, sets `compatibilityState` and `protocolVersion = IPC_PROTOCOL_VERSION`, then sets `dllReady = 1`.
3. C# `GameEventMonitor` retries `OpenExisting(...)` every 1 second until both objects exist, `dllReady == 1`, and `protocolVersion` matches.
4. Hook code maps script notify names to `GameEventType`, appends entries to `eventRing[]`, advances `eventHead`, and calls `SetEvent`.
5. C# waits with `WaitHandle.WaitAny(...)`, drains all pending ring entries from `eventTail` to `eventHead`, updates `eventTail`, and raises `GameEventReceived` per slot.

## `SharedGameState` layout and packing assumptions

`BlackOpsMonitor\SharedMemory.h` defines:

```cpp
#pragma pack(push, 1)
struct SharedGameState
{
    volatile int dllReady;            // offset 0
    volatile int compatibilityState;  // offset 4
    volatile int eventHead;           // offset 8
    volatile int eventTail;           // offset 12
    volatile int droppedEvents;       // offset 16
    volatile int protocolVersion;     // offset 20
    SharedEventSlot eventRing[64];
};
#pragma pack(pop)
```

Current field contract:

| Offset | Size | Field | Meaning |
|---|---:|---|---|
| 0 | 4 | `dllReady` | `1` when DLL IPC init is complete |
| 4 | 4 | `compatibilityState` | `HookCompatibilityState` used by UI |
| 8 | 4 | `eventHead` | Producer sequence (DLL write cursor) |
| 12 | 4 | `eventTail` | Consumer sequence (C# read cursor) |
| 16 | 4 | `droppedEvents` | Count of dropped events when ring is full |
| 20 | 4 | `protocolVersion` | Must equal `IPC_PROTOCOL_VERSION` on both sides |
| 24 | 768 | `eventRing[64]` | Fixed-size event slots (`eventType`, `eventTimestamp`, `eventValue`) |

Notes:

- C# does **offset-based** reads/writes (`ReadInt32`/`Write`) and currently uses offsets `0`, `4`, `8`, `12`, `16`, `20`, and `24+`.
- Ring slots store `eventValue`; the app currently logs it and can extend handling later.
- Struct data currently occupies 792 bytes (24-byte header + 64 * 12-byte slots); mapping is 4096 bytes.
- Assumes Windows little-endian and 4-byte `int`/enum backing (`enum class GameEventType : int`).

## `GameEventType` mapping contract (C++ ↔ C#)

> **Event semantics** (what each event means in-game, when it fires, known-but-uncaptured notify strings) are documented in [game-events.md](game-events.md).


Numeric values must match exactly between:

- `BlackOpsMonitor\SharedMemory.h` (`enum class GameEventType : int`)
- `ZombieForge\Models\GameEventType.cs` (`enum GameEventType : int`)

| Value | Enum member |
|---:|---|
| 0 | `None` |
| 1 | `StartOfRound` |
| 2 | `EndOfRound` |
| 3 | `PowerupGrabbed` |
| 4 | `DogRound` |
| 5 | `PowerOn` |
| 6 | `EndGame` |
| 7 | `PerkPurchased` |

Current notify-string mapping in `BlackOpsMonitor\Hook.cpp`:

- `start_of_round` → `StartOfRound`
- `end_of_round` → `EndOfRound`
- `powerup_grabbed` → `PowerupGrabbed`
- `dog_round_starting` → `DogRound`
- `power_on` → `PowerOn`
- `end_game` → `EndGame`
- `perk_bought` → `PerkPurchased`

## Event semantics and ring-buffer behavior

- DLL creates the event with `CreateEventW(NULL, FALSE, FALSE, EVENT_NAME)`: **auto-reset**, initial state non-signaled.
- C# waits with `WaitHandle.WaitAny` against event + cancellation token and drains the ring when signaled.
- Event signals are not counted; multiple `SetEvent` calls can collapse into one wakeup, which is fine because the ring preserves queued entries.
- Protocol uses a **64-slot ring buffer** with `eventHead`/`eventTail`; C# drains all pending entries each wakeup.
- If the ring is full, DLL increments `droppedEvents`; C# reports increases as warnings.

## Rules for safely extending this protocol

1. **Update both implementations in the same change**  
   - C++: `SharedMemory.h`, relevant writer logic (`Hook.cpp`/`dllmain.cpp`).
   - C#: `GameEventMonitor.cs`, `GameEventType.cs`, and any consumers (`GameEventArgs`, viewmodels).
2. **Keep enum numeric values aligned** across C++/C# (do not reorder existing values).
3. **If adding fields**, update documented offsets and all C# offset constants together.
4. **If changing semantics** (event behavior, reset strategy, payload meaning), update both sides and this doc in the same PR.
5. **Protocol version is mandatory**; update `IPC_PROTOCOL_VERSION` and C# `ExpectedProtocolVersion` together for intentional breaking IPC changes.
