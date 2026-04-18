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
2. DLL zeroes the mapping, sets `protocolVersion = IPC_PROTOCOL_VERSION`, then sets `dllReady = 1`.
3. C# `GameEventMonitor` retries `OpenExisting(...)` every 1 second until both objects exist, `dllReady == 1`, and `protocolVersion` matches.
4. Hook code maps script notify names to `GameEventType`, writes `eventTimestamp` and `lastEvent`, then calls `SetEvent`.
5. C# waits on the event (`WaitOne` timeout 5s), reads `lastEvent`/`eventTimestamp`, writes `lastEvent = None`, and raises `GameEventReceived`.

## `SharedGameState` layout and packing assumptions

`BlackOpsMonitor\SharedMemory.h` defines:

```cpp
#pragma pack(push, 1)
struct SharedGameState
{
    volatile GameEventType  lastEvent;      // offset 0
    volatile int            eventTimestamp; // offset 4
    volatile int            eventValue;     // offset 8
    volatile int            dllReady;       // offset 12
    volatile int            protocolVersion; // offset 16
};
#pragma pack(pop)
```

Current field contract:

| Offset | Size | Field | Meaning |
|---|---:|---|---|
| 0 | 4 | `lastEvent` | Latest event type (`GameEventType` as `int`) |
| 4 | 4 | `eventTimestamp` | BO1 level/server time in ms at event time |
| 8 | 4 | `eventValue` | Extra payload slot (currently not consumed by C#) |
| 12 | 4 | `dllReady` | `1` when DLL IPC init is complete |
| 16 | 4 | `protocolVersion` | Must equal `IPC_PROTOCOL_VERSION` on both sides |

Notes:

- C# does **offset-based** reads/writes (`ReadInt32`/`Write`) and currently uses offsets `0`, `4`, `12`, and `16`.
- `eventValue` exists in memory layout but is not currently read by C#.
- Struct data currently occupies 20 bytes; mapping is 4096 bytes (remaining bytes unused by this protocol).
- Assumes Windows little-endian and 4-byte `int`/enum backing (`enum class GameEventType : int`).

## `GameEventType` mapping contract (C++ ↔ C#)

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

## Event/reset semantics and single-slot limitations

- DLL creates the event with `CreateEventW(NULL, FALSE, FALSE, EVENT_NAME)`: **auto-reset**, initial state non-signaled.
- C# waits with `WaitOne`; no explicit `ResetEvent` call is required for auto-reset behavior.
- Event signals are not counted. Multiple `SetEvent` calls before consumer processing can collapse into one wakeup.
- Protocol is currently **single-slot** (`lastEvent` + one timestamp/value). A newer event can overwrite an older unread event.
- C# resets by writing `lastEvent = None` after reading. Without sequence/versioning, races can still lose events under bursty traffic.

## Rules for safely extending this protocol

1. **Update both implementations in the same change**  
   - C++: `SharedMemory.h`, relevant writer logic (`Hook.cpp`/`dllmain.cpp`).
   - C#: `GameEventMonitor.cs`, `GameEventType.cs`, and any consumers (`GameEventArgs`, viewmodels).
2. **Keep enum numeric values aligned** across C++/C# (do not reorder existing values).
3. **If adding fields**, update documented offsets and all C# offset constants together.
4. **If changing semantics** (event behavior, reset strategy, payload meaning), update both sides and this doc in the same PR.
5. **Protocol version is mandatory**; update `IPC_PROTOCOL_VERSION` and C# `ExpectedProtocolVersion` together for intentional breaking IPC changes.
