# ZombieForge — Copilot Instructions

## Project Overview

ZombieForge is a WinUI 3 desktop modding tool for Call of Duty: Black Ops 1 Zombies. It reads and writes game memory in real time via Win32 P/Invoke and receives in-game events through an injected C++ DLL (`BlackOpsMonitor`) over shared memory IPC. Black Ops 2 support is planned (handler exists as a stub).

## Tech Stack

- **UI:** WinUI 3 (Windows App SDK 1.8), `WinUISDKReferences=false`
- **Framework:** .NET 8, C# 12
- **Pattern:** MVVM with NavigationView + Frame page navigation
- **Logging:** `Microsoft.Extensions.Logging` with Serilog (`WriteTo.Debug` + `WriteTo.File`)
- **Process detection:** WMI via `System.Management`
- **Game memory:** Win32 P/Invoke (`ReadProcessMemory` / `WriteProcessMemory`)
- **DLL injection:** `DllInjector` — remote-thread injection into game process
- **In-game events:** `BlackOpsMonitor` C++ DLL → shared memory IPC → `GameEventMonitor`
- **Localization:** `.resw` resource files with `x:Uid` bindings (en-US, de-DE)

## Solution Layout

```
ZombieForge.slnx
├── ZombieForge/              # WinUI 3 C# app (x86)
│   ├── Commands/             # RelayCommand / RelayCommand<T> (ICommand implementations)
│   ├── Converters/           # BoolToColorConverter, BoolToVisibilityConverter
│   ├── Models/               # Plain data classes (PlayerStats, GameSession, BindEntry, etc.)
│   ├── Services/             # Non-UI logic
│   │   ├── BO1ConfigService  # Reads/writes BO1 config.cfg
│   │   ├── DllInjector       # Win32 remote-thread DLL injection
│   │   ├── GameEventMonitor  # Shared-memory IPC consumer (ring buffer)
│   │   ├── LocalizationService # Language selection + .resw resource loading
│   │   ├── MemoryService     # ReadProcessMemory / WriteProcessMemory wrappers
│   │   ├── ProcessWatcher    # WMI-based process start/stop detection
│   │   └── Games/            # Per-game handlers implementing IGameHandler
│   │       ├── IGameHandler.cs
│   │       ├── BlackOps1Handler.cs
│   │       ├── BlackOps2Handler.cs   (stub)
│   │       └── BO1AddressProfiles.cs
│   ├── Strings/              # Localization resources
│   │   ├── en-US/Resources.resw
│   │   └── de-DE/Resources.resw
│   ├── ViewModels/           # MainViewModel, HomeViewModel, ConfigViewModel, SettingsViewModel
│   ├── Views/                # Page views (NavigationView pages)
│   │   ├── HomePage.xaml     # Live stats display
│   │   ├── ConfigPage.xaml   # Keybind / config.cfg editor
│   │   ├── HistoryPage.xaml  # Game session history
│   │   └── SettingsPage.xaml # Language and app settings
│   └── MainWindow.xaml       # App shell: NavigationView + Frame + status pane
├── BlackOpsMonitor/          # C++ DLL injected into BO1 (Win32/x86)
│   ├── Hook.cpp/h            # Script-notify hook → event ring
│   ├── SharedMemory.h        # SharedGameState struct + IPC constants
│   └── dllmain.cpp           # DLL entry, shared-memory init
├── ZombieForge.Tests/        # xUnit test project
└── docs/                     # Reference documentation
```

## Architecture

### Navigation

MainWindow hosts a `NavigationView` with a `Frame`. Pages are navigated by tag:
- `"home"` → `HomePage` (live game stats)
- `"config"` → `ConfigPage` (keybind editor)
- `"history"` → `HistoryPage` (session log)
- Settings gear → `SettingsPage` (language, preferences)

Each page creates its own ViewModel. `MainViewModel` owns shared state (process detection, game selection) and is accessed by child pages via `App.MainWindow.ViewModel`.

### MVVM Boundaries

- **ViewModels** expose data to the UI — no UI types (controls, brushes) in services or models.
- **Services** are UI-agnostic. All WMI/background thread callbacks must marshal to the UI thread via `DispatcherQueue.TryEnqueue`.
- **Models** are plain data classes with no dependencies on UI or services.

### Game Handler Pattern

Game-specific logic (memory offsets, process names) lives behind `IGameHandler` in `Services/Games/`.

```csharp
public interface IGameHandler
{
    string[] ProcessNames { get; }
    bool TryReadPlayerStats(IntPtr processHandle, long moduleBase, int playerIndex,
                            out PlayerStats stats, out int win32Error);
    bool TryReadLevelTime(IntPtr processHandle, out int levelTime, out int win32Error);
}
```

Adding a new game = new `IGameHandler` implementation + wire into `MainViewModel` game list. No changes to core services.

### BlackOpsMonitor IPC

The C++ DLL hooks BO1's script-notify system and writes game events to a shared-memory ring buffer. `GameEventMonitor` (C#) consumes events via `WaitHandle` + ring drain. Protocol details: see `docs/ipc-protocol.md`.

**Critical rule:** When extending IPC, update both C++ and C# sides in the same change. Keep `GameEventType` enum values aligned across both languages. Bump `IPC_PROTOCOL_VERSION` for breaking changes.

## Coding Conventions

### XAML Binding
- Use `x:Bind` (not `{Binding}`) with `Mode=OneWay` where applicable.
- **Known exception:** The status indicator `Ellipse` in `MainWindow.xaml` uses `{Binding}` with a converter because `x:Bind` with converters at the Window root generates invalid code. Child page elements should always use `x:Bind`.

### Localization
- All user-visible XAML text uses `x:Uid` with `.resw` resource files — no hard-coded `Text`/`Content` strings.
- `LocalizationService.Initialize()` runs in `App.OnLaunched` before `MainWindow` is created. It reads `ApplicationLanguages.PrimaryLanguageOverride` from `LocalSettings["LanguageOverride"]`.
- Adding a new language: add a `Strings/<locale>/Resources.resw` file and register the `LanguageOption` in `LocalizationService._namedLanguages`.

### ViewModels
- Implement `INotifyPropertyChanged` manually with `[CallerMemberName]`.
- Use `RelayCommand` / `RelayCommand<T>` from `Commands/` for `ICommand` bindings.

### Logging
- Loggers are created via `App.LoggerFactory.CreateLogger<T>()` — never instantiate `ILoggerFactory` elsewhere.
- Use structured log messages: `_logger.LogInformation("State: {State}", value)` — no string interpolation.
- Log levels: `Debug`/`Information` for normal flow, `Warning`+ for file persistence.
- **Debug sink:** All levels → VS Output window (dev only).
- **File sink:** `Warning`+ → `%LocalAppData%\ZombieForge\Logs\log-<date>.txt`, 7-day retention.

### WinUI 3 Platform Notes
- `WinUISDKReferences=false` — `ContentDialog.ShowAsync()` cannot be directly awaited. Use event-based pattern: `dialog.PrimaryButtonClick += ...; _ = dialog.ShowAsync();`
- DLL injection requires architecture match: app builds x86 because target games are 32-bit.

## How to Add a New Game

1. Create `ZombieForge/Services/Games/<GameName>Handler.cs` implementing `IGameHandler`.
2. Set `ProcessNames` to executable names **without** `.exe` (include variants if needed).
3. Implement `TryReadPlayerStats(...)` and `TryReadLevelTime(...)` with game-specific offsets.
4. Keep handlers non-UI; marshal any background callbacks in ViewModels/services with `DispatcherQueue.TryEnqueue`.
5. Wire into `MainViewModel` game selection list — no changes to core services.

## AI Git Workflow (Required)

- Use `feat/` prefix for branch names (e.g., `feat/add-bo2-support`).
- Before making changes, create a new git worktree and a new branch for that task.
- Perform all edits and commits only inside that task-specific worktree/branch.
- When implementation is complete, review your own changes before opening a PR.
- If the changes look correct, create a GitHub pull request from that branch.

## Process Naming Conventions

| Context | BO1 | BO2 |
|---------|-----|-----|
| WMI queries (with `.exe`) | `BlackOps.exe` | `t6zm.exe` |
| `Process.GetProcessesByName` / handler `ProcessNames` | `BlackOps` | `t6zm` |

## Key Documentation

| File | Contents |
|------|----------|
| `docs/bo1-memory-map.md` | BO1 memory addresses, offsets, hook reference |
| `docs/ipc-protocol.md` | SharedGameState layout, event ring protocol, extension rules |
| `docs/bo1-zombies-facts.md` | Gameplay reference (round mechanics, perk costs, etc.) |
| `docs/developer-console-commands.md` | BO1 developer console command reference |
| `docs/codebase-review.md` | Architecture review with known issues and recommendations |
