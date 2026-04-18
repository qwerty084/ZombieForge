#include "Hook.h"
#include "SharedMemory.h"
#include <cstring>
#include <cstdio>
#include <cstdint>

// ─── Game Addresses (Black Ops 1) ───────────────────────────────────────────
static constexpr uintptr_t VM_NOTIFY_ADDR       = 0x008A87C0;
static constexpr uintptr_t SL_CONVERT_TO_STRING  = 0x00687530;
static constexpr uintptr_t LEVEL_TIME_ADDR       = 0x0286D014; // server/level time (ms)

// Prologue at VM_Notify (verified in x32dbg):
//   008A87C0  55        push ebp              ; 1 byte  (boundary 0)
//   008A87C1  8BEC      mov ebp, esp          ; 2 bytes (boundary 1)
//   008A87C3  83E4F8    and esp, 0xFFFFFFF8   ; 3 bytes (boundary 3)
//                                                        boundary 6 ← next instr
// We must steal 6 bytes (not 5) to land on a clean boundary.
static constexpr int STOLEN_BYTES = 6;
static constexpr BYTE EXPECTED_VM_NOTIFY_PROLOGUE[STOLEN_BYTES] = { 0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8 };
// ────────────────────────────────────────────────────────────────────────────

// SL_ConvertToString(unsigned int stringValue, unsigned int inst) → const char*
typedef const char* (__cdecl* SL_ConvertToString_t)(unsigned int, unsigned int);
static const auto GameSLConvertToString = (SL_ConvertToString_t)SL_CONVERT_TO_STRING;

// scrStringGlob — array of 2 pointers (one per script instance).
// Must be non-null before we call SL_ConvertToString.
static constexpr uintptr_t SCR_STRING_GLOB = 0x03067C00;

bool IsHookInstallReady()
{
    __try
    {
        return *(uintptr_t*)SCR_STRING_GLOB != 0;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
}

// ─── Detour plumbing ────────────────────────────────────────────────────────
static BYTE  g_origBytes[STOLEN_BYTES] = {};
static BYTE* g_trampoline = nullptr;
static bool  g_hooked     = false;

struct ModuleLayout
{
    uintptr_t base;
    uintptr_t end;
};

// ─── Event mapping table ────────────────────────────────────────────────────
struct EventMapping { const char* name; GameEventType type; };
static const EventMapping g_events[] =
{
    { "start_of_round",     GameEventType::StartOfRound   },
    { "end_of_round",       GameEventType::EndOfRound     },
    { "powerup_grabbed",    GameEventType::PowerupGrabbed },
    { "dog_round_starting", GameEventType::DogRound       },
    { "power_on",           GameEventType::PowerOn        },
    { "end_game",           GameEventType::EndGame        },
    { "perk_bought",        GameEventType::PerkPurchased  },
};

// ─── C++ handler (called from naked detour) ─────────────────────────────────
static volatile long g_callCount = 0;

static void SetCompatibilityState(HookCompatibilityState state)
{
    if (!g_pState)
        return;

    g_pState->compatibilityState = static_cast<int>(state);
    if (g_hEvent)
        SetEvent(g_hEvent);
}

static bool TryGetMainModuleLayout(ModuleLayout& layout)
{
    HMODULE module = GetModuleHandleW(nullptr);
    if (!module)
    {
        DllLog("VerifyHook: GetModuleHandleW(NULL) failed, err=%lu", GetLastError());
        return false;
    }

    auto* dos = reinterpret_cast<const IMAGE_DOS_HEADER*>(module);
    if (dos->e_magic != IMAGE_DOS_SIGNATURE)
    {
        DllLog("VerifyHook: DOS signature mismatch");
        return false;
    }

    if (dos->e_lfanew < static_cast<LONG>(sizeof(IMAGE_DOS_HEADER)) || dos->e_lfanew > 0x1000000)
    {
        DllLog("VerifyHook: invalid e_lfanew=%ld", dos->e_lfanew);
        return false;
    }

    auto* ntAddress = reinterpret_cast<const BYTE*>(module) + dos->e_lfanew;
    MEMORY_BASIC_INFORMATION ntMbi = {};
    if (VirtualQuery(ntAddress, &ntMbi, sizeof(ntMbi)) == 0)
    {
        DllLog("VerifyHook: VirtualQuery for NT headers failed, err=%lu", GetLastError());
        return false;
    }

    uintptr_t ntStart = reinterpret_cast<uintptr_t>(ntAddress);
    uintptr_t ntEnd = ntStart + sizeof(IMAGE_NT_HEADERS);
    uintptr_t mbiStart = reinterpret_cast<uintptr_t>(ntMbi.BaseAddress);
    uintptr_t mbiEnd = mbiStart + ntMbi.RegionSize;
    if (ntEnd < ntStart || ntStart < mbiStart || ntEnd > mbiEnd)
    {
        DllLog("VerifyHook: NT headers out of readable region");
        return false;
    }

    if (ntMbi.State != MEM_COMMIT || (ntMbi.Protect & (PAGE_NOACCESS | PAGE_GUARD)) != 0)
    {
        DllLog("VerifyHook: NT headers region is not safely readable");
        return false;
    }

    auto* nt = reinterpret_cast<const IMAGE_NT_HEADERS*>(
        ntAddress);
    if (nt->Signature != IMAGE_NT_SIGNATURE)
    {
        DllLog("VerifyHook: NT signature mismatch");
        return false;
    }

    uintptr_t base = reinterpret_cast<uintptr_t>(module);
    uintptr_t size = static_cast<uintptr_t>(nt->OptionalHeader.SizeOfImage);
    uintptr_t end = base + size;
    if (size == 0 || end < base)
    {
        DllLog("VerifyHook: invalid module image size");
        return false;
    }

    layout.base = base;
    layout.end = end;
    return true;
}

static bool IsRangeInsideModule(uintptr_t address, size_t length, const ModuleLayout& module)
{
    uintptr_t end = address + static_cast<uintptr_t>(length);
    if (end < address)
        return false;

    return address >= module.base && end <= module.end;
}

static bool IsExecutableMemory(const void* address)
{
    MEMORY_BASIC_INFORMATION mbi = {};
    if (VirtualQuery(address, &mbi, sizeof(mbi)) == 0)
        return false;

    const DWORD executableMask =
        PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY;
    return mbi.State == MEM_COMMIT && (mbi.Protect & executableMask) != 0;
}

static bool VerifyHookCompatibility()
{
    ModuleLayout module = {};
    if (!TryGetMainModuleLayout(module))
    {
        SetCompatibilityState(HookCompatibilityState::HookInstallFailed);
        return false;
    }

    struct AddressRangeCheck
    {
        const char* label;
        uintptr_t address;
        size_t length;
    };

    const AddressRangeCheck checks[] =
    {
        { "VM_NOTIFY_ADDR",      VM_NOTIFY_ADDR,        STOLEN_BYTES },
        { "SL_CONVERT_TO_STRING", SL_CONVERT_TO_STRING, 1 },
        { "LEVEL_TIME_ADDR",      LEVEL_TIME_ADDR,      sizeof(int) },
        { "SCR_STRING_GLOB",      SCR_STRING_GLOB,      sizeof(uintptr_t) },
    };

    for (const auto& check : checks)
    {
        if (!IsRangeInsideModule(check.address, check.length, module))
        {
            DllLog("VerifyHook: %s out of module range (addr=0x%p module=[0x%p,0x%p))",
                   check.label,
                   reinterpret_cast<void*>(check.address),
                   reinterpret_cast<void*>(module.base),
                   reinterpret_cast<void*>(module.end));
            SetCompatibilityState(HookCompatibilityState::UnsupportedVersion);
            return false;
        }
    }

    auto* target = reinterpret_cast<const BYTE*>(VM_NOTIFY_ADDR);
    if (!IsExecutableMemory(target))
    {
        DllLog("VerifyHook: VM_NOTIFY_ADDR is not executable memory");
        SetCompatibilityState(HookCompatibilityState::UnsupportedVersion);
        return false;
    }

    if (memcmp(target, EXPECTED_VM_NOTIFY_PROLOGUE, STOLEN_BYTES) != 0)
    {
        DllLog("VerifyHook: VM_Notify prologue mismatch. Expected=%02X %02X %02X %02X %02X %02X Actual=%02X %02X %02X %02X %02X %02X",
               EXPECTED_VM_NOTIFY_PROLOGUE[0], EXPECTED_VM_NOTIFY_PROLOGUE[1], EXPECTED_VM_NOTIFY_PROLOGUE[2],
               EXPECTED_VM_NOTIFY_PROLOGUE[3], EXPECTED_VM_NOTIFY_PROLOGUE[4], EXPECTED_VM_NOTIFY_PROLOGUE[5],
               target[0], target[1], target[2], target[3], target[4], target[5]);
        SetCompatibilityState(HookCompatibilityState::UnsupportedVersion);
        return false;
    }

    return true;
}

static void QueueEvent(GameEventType eventType, int timestamp, int value)
{
    if (!g_pState || !g_hEvent)
        return;

    int head = g_pState->eventHead;
    int tail = g_pState->eventTail;

    if (head < tail)
    {
        DllLog("QueueEvent: invalid ring indices (head=%d, tail=%d), resetting tail", head, tail);
        g_pState->eventTail = head;
        tail = head;
    }

    if ((head - tail) >= EVENT_RING_CAPACITY)
    {
        InterlockedIncrement(reinterpret_cast<volatile LONG*>(&g_pState->droppedEvents));
        return;
    }

    const int slotIndex = head % EVENT_RING_CAPACITY;
    g_pState->eventRing[slotIndex].eventType = static_cast<int>(eventType);
    g_pState->eventRing[slotIndex].eventTimestamp = timestamp;
    g_pState->eventRing[slotIndex].eventValue = value;
    MemoryBarrier();

    g_pState->eventHead = head + 1;
    SetEvent(g_hEvent);
}

static void __cdecl HandleNotify(unsigned int stringValue)
{
    // Skip zero / obviously invalid string IDs
    if (stringValue == 0)
        return;

    // Check that the string table for instance 0 is initialised
    uintptr_t tableBase = *(uintptr_t*)SCR_STRING_GLOB;
    if (!tableBase)
        return;

    long count = InterlockedIncrement(&g_callCount);

    // Call SL_ConvertToString under SEH so an invalid stringValue
    // can never crash the game.
    const char* name = nullptr;
    __try
    {
        name = GameSLConvertToString(stringValue, 0);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        DllLog("HandleNotify #%ld: sv=%u EXCEPTION in SL_ConvertToString", count, stringValue);
        return;
    }

    DllLog("HandleNotify #%ld: sv=%u name=%s", count, stringValue,
           name ? name : "(null)");

    if (!name || !g_pState || !g_hEvent)
        return;

    for (int i = 0; i < _countof(g_events); ++i)
    {
        if (strcmp(name, g_events[i].name) == 0)
        {
            DllLog(">>> EVENT MATCHED: %s -> type %d", name, (int)g_events[i].type);
            int levelTime = 0;
            __try
            {
                levelTime = *(volatile int*)LEVEL_TIME_ADDR;
            }
            __except (EXCEPTION_EXECUTE_HANDLER)
            {
                DllLog("HandleNotify #%ld: EXCEPTION reading level time", count);
                return;
            }

            QueueEvent(g_events[i].type, levelTime, 0);
            return;
        }
    }
}

// ─── Naked detour ───────────────────────────────────────────────────────────
// Reached via 5-byte JMP at VM_Notify entry.  Stack layout is identical to the
// original function entry (JMP does not push a return address):
//   EAX           = notifyListOwnerId  (register arg, __usercall)
//   [esp+0]       = return address      (pushed by the original CALL to VM_Notify)
//   [esp+4]       = top                 (1st stack arg)
//   [esp+8]       = stringValue         (2nd stack arg)
//
// After our handler we jump to the trampoline which executes the stolen
// prologue bytes and then continues at VM_Notify+6.
// ────────────────────────────────────────────────────────────────────────────
static __declspec(naked) void VM_Notify_Detour()
{
    __asm
    {
        test eax, eax
        jnz  skip_handler

        pushad                          // +32 bytes on stack
        pushfd                          // + 4 bytes on stack  (total +36 = 0x24)

        // original [esp+8] is now at [esp + 8 + 0x24] = [esp + 0x2C]
        push dword ptr [esp + 0x2C]     // push stringValue arg for HandleNotify
        call HandleNotify
        add  esp, 4                     // clean up __cdecl arg

        popfd
        popad

    skip_handler:
        // Execute stolen prologue → continue at VM_Notify+6
        jmp  dword ptr [g_trampoline]
    }
}

// ─── Install / Remove ───────────────────────────────────────────────────────
bool InstallHook()
{
    if (g_hooked)
        return true;

    if (!VerifyHookCompatibility())
        return false;

    auto target = (BYTE*)VM_NOTIFY_ADDR;

    // 1. Save original bytes
    memcpy(g_origBytes, target, STOLEN_BYTES);

    // 2. Build trampoline: stolen prologue + JMP rel32 back to target+STOLEN_BYTES
    //    Layout: [6 bytes: original prologue] [5 bytes: E9 rel32]
    g_trampoline = (BYTE*)VirtualAlloc(NULL, 32,
                                       MEM_COMMIT | MEM_RESERVE,
                                       PAGE_EXECUTE_READWRITE);
    if (!g_trampoline)
    {
        DllLog("VirtualAlloc for trampoline FAILED");
        SetCompatibilityState(HookCompatibilityState::HookInstallFailed);
        return false;
    }

    memcpy(g_trampoline, g_origBytes, STOLEN_BYTES);
    g_trampoline[STOLEN_BYTES] = 0xE9;     // JMP rel32
    *(DWORD*)(g_trampoline + STOLEN_BYTES + 1) =
        (DWORD)((target + STOLEN_BYTES) - (g_trampoline + STOLEN_BYTES + 5));

    // 3. Patch target: the detour itself is only 5 bytes, so we NOP the 6th byte we stole
    //    to keep execution aligned with the same instruction boundary as the trampoline copy.
    DWORD oldProt;
    if (!VirtualProtect(target, STOLEN_BYTES, PAGE_EXECUTE_READWRITE, &oldProt))
    {
        DllLog("VirtualProtect RWX failed, err=%lu", GetLastError());
        VirtualFree(g_trampoline, 0, MEM_RELEASE);
        g_trampoline = nullptr;
        SetCompatibilityState(HookCompatibilityState::HookInstallFailed);
        return false;
    }

    target[0] = 0xE9;      // JMP rel32
    *(DWORD*)(target + 1) =
        (DWORD)((BYTE*)VM_Notify_Detour - (target + 5));
    target[5] = 0x90;      // NOP — pads the 6th byte we overwrote

    if (!VirtualProtect(target, STOLEN_BYTES, oldProt, &oldProt))
    {
        DllLog("VirtualProtect restore failed, err=%lu; continuing with hook installed", GetLastError());
    }

    g_hooked = true;
    SetCompatibilityState(HookCompatibilityState::Compatible);
    DllLog("InstallHook: stole %d bytes at 0x%X, trampoline at %p",
           STOLEN_BYTES, (unsigned)VM_NOTIFY_ADDR, g_trampoline);
    return true;
}

void RemoveHook()
{
    if (!g_hooked) return;

    auto target = (BYTE*)VM_NOTIFY_ADDR;

    DWORD oldProt;
    VirtualProtect(target, STOLEN_BYTES, PAGE_EXECUTE_READWRITE, &oldProt);
    memcpy(target, g_origBytes, STOLEN_BYTES);
    VirtualProtect(target, STOLEN_BYTES, oldProt, &oldProt);

    VirtualFree(g_trampoline, 0, MEM_RELEASE);
    g_trampoline = nullptr;
    g_hooked = false;

    DllLog("RemoveHook: restored original bytes");
}
