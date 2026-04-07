# ZombieForge — Copilot Instructions

## Project Overview
ZombieForge is a WinUI 3 desktop modding tool for Call of Duty: Black Ops 1 Zombies. It reads and writes game memory in real time. Black Ops 2 support is planned for the future.

## Tech Stack
- **UI:** WinUI 3 (Windows App SDK 1.8)
- **Framework:** .NET 8, C# 12
- **Pattern:** MVVM
- **Logging:** `Microsoft.Extensions.Logging` with Serilog (`WriteTo.Debug` + `WriteTo.File`)
- **Process detection:** WMI via `System.Management`
- **Game memory:** Win32 P/Invoke (`ReadProcessMemory` / `WriteProcessMemory`) — planned

## Project Structure
```
ZombieForge/
├── Converters/       # IValueConverter implementations for XAML bindings
├── Models/           # Plain data classes (game state, etc.)
├── Services/         # Non-UI logic (ProcessWatcher, MemoryService, game handlers)
│   └── Games/        # Per-game handlers implementing IGameHandler
├── ViewModels/       # MVVM ViewModels, implement INotifyPropertyChanged
└── MainWindow.xaml   # Single window, uses x:Bind to ViewModel
```

## Architecture Rules
- ViewModels expose data to the UI — no UI types (controls, brushes from XAML namespace) in services or models
- All WMI/background thread callbacks must marshal to the UI thread via `DispatcherQueue.TryEnqueue`
- Game-specific logic (memory offsets, process names) belongs in `Services/Games/` behind `IGameHandler`
- Adding support for a new game = new `IGameHandler` implementation only, no changes to core services

## Coding Conventions
- Use `x:Bind` (not `{Binding}`) for all XAML data binding with `Mode=OneWay` where applicable
- ViewModels implement `INotifyPropertyChanged` manually using `[CallerMemberName]`
- Loggers are created via `App.LoggerFactory.CreateLogger<T>()` — never instantiate `ILoggerFactory` elsewhere
- Use structured log messages: `_logger.LogInformation("State: {State}", value)` — no string interpolation in log calls
- Log levels: `Debug`/`Information` for normal flow, `Warning`+ for anything written to the log file

## Logging Setup
- **Debug sink:** All levels → VS Output window (dev only)
- **File sink:** `Warning`+ → `%LocalAppData%\ZombieForge\Logs\log-<date>.txt`, 7-day retention
- Log file is for production crash/error reporting only — do not log verbose data to file

## BO1 Game Details
- Process name: `BlackOps.exe` (with `.exe` for WMI), `BlackOps` (without for `Process.GetProcessesByName`)
- Memory access requires the app to run as administrator (needed for `WriteProcessMemory`)

## Memory Addresses
### Key Hook Addresses
- SCR_NOTIFY_ID_ADDR hook point: `0x483E2F` (Scr_NotifyNum detour site)
- SL_ConvertToString: `0x687530` (resolves string IDs to char*)
- STRING_TABLE_BASE (scrStringGlob): `0x03067C00` (array of 2 pointers, one per script instance)
- VM_Notify original: `0x8A87C0`
- VM_Notify hook sites: `0x41D2E5` and `0x8AB798`

### SL_ConvertToString Disassembly (confirmed via x32dbg)
```
00687530  mov eax, [esp+4]           ; eax = stringValue
00687534  test eax, eax
00687536  je 0068754B                ; if 0, return NULL
00687538  mov ecx, [esp+8]           ; ecx = inst (0 or 1)
0068753C  mov edx, [ecx*4+3067C00]   ; edx = scrStringGlob[inst]
00687543  shl eax, 4                 ; stringValue * 16 (entry size)
00687546  lea eax, [edx+eax+4]       ; string data at offset +4
0068754A  ret
```
Lookup formula: `tableBase = *(char**)(0x03067C00 + inst * 4); return tableBase + (id * 16) + 4;`

### Hook Mechanism
- At `0x483E2F`: 5-byte E9 JMP detour into Scr_NotifyNum. EDX register holds notification string ID (bit 0 = flag, EDX >> 8 = string table index)
- At `0x41D2E5`/`0x8AB798`: 4-byte CALL patches into VM_Notify handler. Calls SL_ConvertToString to resolve string, then strcmp against registered event names.

### Player State Memory
- Base: `0x01C08B40` (29395776), Stride: 7464 bytes per player
- Points offset: +7048, Kills: +7052, Downs: +7076, Headshots: +7084, Name: +6968, Noclip: +7180

### Game Function Addresses
- Cbuf_AddText: `0x0049B930`
- SV_GameSendServerCommand: `0x005441F0`
- Dvar_FindVar: `0x005AEA10`
- Scr_RegisterNotifyHandler: `0x00661400`
- Scr_GetString: `0x00567CB0`
- G_GetWeaponIndexForName: `0x005C2740`
- BG_GetWeaponName: `0x00450180`

### Entity Memory
- Base: `0x01A7E5F8`, Stride: 844 bytes. Health at +388, MaxHealth at +392, Flags at +368.

### DLL-Registered Script Events
- chest_accessed, leverDown, switch_activated, powerup_grabbed, power_on, safe_restart, saferestart

### Global Data
- Server/Level Time at `0x0286D014` (42389524)
