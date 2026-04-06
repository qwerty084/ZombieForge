#pragma once
#include <windows.h>

#define SHARED_MEM_NAME  L"BO1MonitorSharedMem"
#define EVENT_NAME       L"BO1MonitorEvent"
#define SHARED_MEM_SIZE  4096

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

#pragma pack(push, 1)
struct SharedGameState
{
    volatile GameEventType  lastEvent;       // offset 0  — DLL writes, C# reads & resets to None
    volatile int            eventTimestamp;   // offset 4  — game level time (ms) at time of event
    volatile int            eventValue;       // offset 8  — extra data (e.g. player slot), 0 if unused
    volatile int            dllReady;         // offset 12 — set to 1 by DLL when initialized
};
#pragma pack(pop)

// Globals — defined in dllmain.cpp, used by Hook.cpp
extern SharedGameState* g_pState;
extern HANDLE           g_hEvent;

// Simple file logger for diagnostics (defined in dllmain.cpp)
void DllLog(const char* fmt, ...);
