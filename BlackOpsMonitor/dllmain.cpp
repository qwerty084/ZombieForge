#include "SharedMemory.h"
#include "Hook.h"
#include <cstdio>
#include <cstdarg>

// ─── DLL file logger ─────────────────────────────────────────────────────────
static FILE* g_logFile = nullptr;

void DllLog(const char* fmt, ...)
{
    if (!g_logFile) return;
    va_list args;
    va_start(args, fmt);
    vfprintf(g_logFile, fmt, args);
    va_end(args);
    fprintf(g_logFile, "\n");
    fflush(g_logFile);
}

static void OpenLog()
{
    char path[MAX_PATH];
    GetTempPathA(MAX_PATH, path);
    strcat_s(path, "ZombieForge_dll.log");
    fopen_s(&g_logFile, path, "w");
    DllLog("=== BlackOpsMonitor DLL loaded ===");
}

static void CloseLog()
{
    if (g_logFile) { fclose(g_logFile); g_logFile = nullptr; }
}

// Global IPC handles — used by Hook.cpp via extern declarations in SharedMemory.h
static HANDLE           g_hMapFile = NULL;
SharedGameState*        g_pState   = NULL;
HANDLE                  g_hEvent   = NULL;

static bool InitSharedMemory()
{
    g_hMapFile = CreateFileMappingW(
        INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE,
        0, SHARED_MEM_SIZE, SHARED_MEM_NAME);
    if (!g_hMapFile) return false;

    g_pState = (SharedGameState*)MapViewOfFile(
        g_hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, SHARED_MEM_SIZE);
    if (!g_pState)
    {
        CloseHandle(g_hMapFile);
        g_hMapFile = NULL;
        return false;
    }

    ZeroMemory(g_pState, SHARED_MEM_SIZE);

    g_hEvent = CreateEventW(NULL, FALSE, FALSE, EVENT_NAME);
    if (!g_hEvent)
    {
        UnmapViewOfFile(g_pState);
        g_pState = NULL;
        CloseHandle(g_hMapFile);
        g_hMapFile = NULL;
        return false;
    }

    g_pState->protocolVersion = IPC_PROTOCOL_VERSION;
    g_pState->dllReady = 1;
    DllLog("Shared memory + event created OK");
    return true;
}

static DWORD WINAPI InitThread(LPVOID)
{
    OpenLog();

    constexpr DWORD WaitIntervalMs = 100;
    constexpr DWORD WaitTimeoutMs = 30000;
    DWORD waitedMs = 0;
    while (waitedMs < WaitTimeoutMs && !IsHookInstallReady())
    {
        Sleep(WaitIntervalMs);
        waitedMs += WaitIntervalMs;
    }

    if (IsHookInstallReady())
        DllLog("String table ready after %lu ms", waitedMs);
    else
        DllLog("String table still not ready after %lu ms, continuing", WaitTimeoutMs);

    if (InitSharedMemory())
        InstallHook();
    else
        DllLog("InitSharedMemory FAILED, GetLastError=%lu", GetLastError());
    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hModule);
        HANDLE hThread = CreateThread(NULL, 0, InitThread, NULL, 0, NULL);
        if (hThread) CloseHandle(hThread);
    }
    else if (reason == DLL_PROCESS_DETACH)
    {
        DllLog("DLL detaching; skipping RemoveHook in DllMain for safety");
        CloseLog();
        if (g_pState)   UnmapViewOfFile(g_pState);
        if (g_hMapFile) CloseHandle(g_hMapFile);
        if (g_hEvent)   CloseHandle(g_hEvent);
    }
    return TRUE;
}
