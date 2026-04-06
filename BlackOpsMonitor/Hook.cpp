#include "Hook.h"
#include "SharedMemory.h"
#include <cstring>
#include <cstdio>

// ─── Game Addresses (Black Ops 1) ───────────────────────────────────────────
static constexpr uintptr_t VM_NOTIFY_ADDR       = 0x008A87C0;
static constexpr uintptr_t SL_CONVERT_TO_STRING  = 0x00687530;

// Prologue at VM_Notify (verified in x32dbg):
//   008A87C0  55        push ebp              ; 1 byte  (boundary 0)
//   008A87C1  8BEC      mov ebp, esp          ; 2 bytes (boundary 1)
//   008A87C3  83E4F8    and esp, 0xFFFFFFF8   ; 3 bytes (boundary 3)
//                                                        boundary 6 ← next instr
// We must steal 6 bytes (not 5) to land on a clean boundary.
static constexpr int STOLEN_BYTES = 6;
// ────────────────────────────────────────────────────────────────────────────

// SL_ConvertToString(unsigned int stringValue, unsigned int inst) → const char*
typedef const char* (__cdecl* SL_ConvertToString_t)(unsigned int, unsigned int);
static const auto GameSLConvertToString = (SL_ConvertToString_t)SL_CONVERT_TO_STRING;

// scrStringGlob — array of 2 pointers (one per script instance).
// Must be non-null before we call SL_ConvertToString.
static constexpr uintptr_t SCR_STRING_GLOB = 0x03067C00;

// ─── Detour plumbing ────────────────────────────────────────────────────────
static BYTE  g_origBytes[STOLEN_BYTES] = {};
static BYTE* g_trampoline = nullptr;
static bool  g_hooked     = false;

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
};

// ─── C++ handler (called from naked detour) ─────────────────────────────────
static volatile long g_callCount = 0;

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
            g_pState->lastEvent = g_events[i].type;
            SetEvent(g_hEvent);
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
void InstallHook()
{
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
        return;
    }

    memcpy(g_trampoline, g_origBytes, STOLEN_BYTES);
    g_trampoline[STOLEN_BYTES] = 0xE9;     // JMP rel32
    *(DWORD*)(g_trampoline + STOLEN_BYTES + 1) =
        (DWORD)((target + STOLEN_BYTES) - (g_trampoline + STOLEN_BYTES + 5));

    // 3. Patch target: 5-byte JMP + 1 NOP (fills the 6th stolen byte)
    DWORD oldProt;
    VirtualProtect(target, STOLEN_BYTES, PAGE_EXECUTE_READWRITE, &oldProt);

    target[0] = 0xE9;      // JMP rel32
    *(DWORD*)(target + 1) =
        (DWORD)((BYTE*)VM_Notify_Detour - (target + 5));
    target[5] = 0x90;      // NOP — pads the 6th byte we overwrote

    VirtualProtect(target, STOLEN_BYTES, oldProt, &oldProt);

    g_hooked = true;
    DllLog("InstallHook: stole %d bytes at 0x%X, trampoline at %p",
           STOLEN_BYTES, (unsigned)VM_NOTIFY_ADDR, g_trampoline);
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
