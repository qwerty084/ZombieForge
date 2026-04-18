#pragma once
#include <windows.h>

#define SHARED_MEM_NAME  L"BO1MonitorSharedMem"
#define EVENT_NAME       L"BO1MonitorEvent"
#define SHARED_MEM_SIZE  4096

// IPC contract: keep values and order identical with ZombieForge.Models.GameEventType.
// Protocol documentation source: docs/ipc-protocol.md.
enum class GameEventType : int
{
    None           = 0,
    StartOfRound   = 1,
    EndOfRound     = 2,
    PowerupGrabbed = 3,
    DogRound       = 4,
    PowerOn        = 5,
    EndGame        = 6,
    PerkPurchased  = 7,
};

enum class HookCompatibilityState : int
{
    Unknown            = 0,
    Compatible         = 1,
    UnsupportedVersion = 2,
    HookInstallFailed  = 3,
};

static constexpr int EVENT_RING_CAPACITY = 64;

#pragma pack(push, 1)
struct SharedEventSlot
{
    int eventType;        // GameEventType (int)
    int eventTimestamp;   // game level time (ms) at time of event
    int eventValue;       // extra data (e.g. player slot), 0 if unused
};

struct SharedGameState
{
    volatile int dllReady;             // offset 0  — set to 1 by DLL when initialized
    volatile int compatibilityState;   // offset 4  — HookCompatibilityState
    volatile int eventHead;            // offset 8  — next write sequence (DLL producer)
    volatile int eventTail;            // offset 12 — next read sequence (C# consumer)
    volatile int droppedEvents;        // offset 16 — number of dropped events due full ring
    SharedEventSlot eventRing[EVENT_RING_CAPACITY];
};
#pragma pack(pop)

// Globals — defined in dllmain.cpp, used by Hook.cpp
extern SharedGameState* g_pState;
extern HANDLE           g_hEvent;

// Simple file logger for diagnostics (defined in dllmain.cpp)
void DllLog(const char* fmt, ...);
