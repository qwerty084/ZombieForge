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
    if (!g_pState) return false;

    ZeroMemory(g_pState, SHARED_MEM_SIZE);

    g_hEvent = CreateEventW(NULL, FALSE, FALSE, EVENT_NAME);
    if (!g_hEvent) return false;

    g_pState->dllReady = 1;
    DllLog("Shared memory + event created OK");
    return true;
}

static DWORD WINAPI InitThread(LPVOID)
{
    OpenLog();

    // Give the game time to fully initialize (string tables, etc.)
    // before we patch any code.
    Sleep(5000);
    DllLog("Init delay complete, proceeding");

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
        CreateThread(NULL, 0, InitThread, NULL, 0, NULL);
    }
    else if (reason == DLL_PROCESS_DETACH)
    {
        RemoveHook();
        DllLog("DLL detaching, cleanup done");
        CloseLog();
        if (g_pState)   UnmapViewOfFile(g_pState);
        if (g_hMapFile) CloseHandle(g_hMapFile);
        if (g_hEvent)   CloseHandle(g_hEvent);
    }
    return TRUE;
}
