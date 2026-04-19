# AGENTS.md

Quick-reference for AI agents working in this repository.
For full project context, architecture, and conventions see `.github/copilot-instructions.md`.

## Build & Test Commands

```powershell
# C# app (x86 required — target games are 32-bit)
dotnet build ZombieForge\ZombieForge.csproj -c Debug -p:Platform=x86

# C++ DLL (requires MSBuild / VS Build Tools with v143)
msbuild BlackOpsMonitor\BlackOpsMonitor.vcxproj /p:Configuration=Debug /p:Platform=Win32

# Unit tests (xUnit)
dotnet test ZombieForge.Tests\ZombieForge.Tests.csproj -c Debug -p:Platform=x86

# Run tests via MSBuild target (alternative)
dotnet msbuild ZombieForge\ZombieForge.csproj -t:test -p:Configuration=Debug -p:Platform=x86
```

## Key Directories

| Path | What lives here |
|------|----------------|
| `ZombieForge/Views/` | Page views: HomePage, ConfigPage, HistoryPage, SettingsPage |
| `ZombieForge/ViewModels/` | MainViewModel, HomeViewModel, ConfigViewModel, SettingsViewModel |
| `ZombieForge/Services/` | ProcessWatcher, MemoryService, DllInjector, GameEventMonitor, LocalizationService, BO1ConfigService |
| `ZombieForge/Services/Games/` | `IGameHandler` + per-game handlers (BlackOps1Handler, BlackOps2Handler) |
| `ZombieForge/Models/` | PlayerStats, GameSession, BindEntry, AddressProfile, GameEventType, etc. |
| `ZombieForge/Strings/` | Localization `.resw` files (en-US, de-DE) |
| `ZombieForge/Converters/` | BoolToColorConverter, BoolToVisibilityConverter |
| `ZombieForge/Commands/` | RelayCommand, RelayCommand\<T\> |
| `BlackOpsMonitor/` | C++ DLL injected into BO1 — hooks script-notify, writes to shared memory |
| `ZombieForge.Tests/` | xUnit tests (links source files, not project reference, to avoid WinAppSDK runtime issues) |

## Critical Boundaries

- **No UI types in services or models.** Keep XAML controls, brushes, and UI namespace types out of `Services/` and `Models/`.
- **Marshal background callbacks.** All WMI/thread callbacks must reach the UI thread via `DispatcherQueue.TryEnqueue`.
- **Game logic stays behind `IGameHandler`.** Offsets, process names, and memory reads go in `Services/Games/`.
- **Localization via `x:Uid` only.** No hard-coded `Text`/`Content` strings in XAML.
- **IPC changes require both sides.** Update C++ (`SharedMemory.h`, `Hook.cpp`) and C# (`GameEventMonitor.cs`, `GameEventType.cs`) in the same PR. Keep enum values aligned.

## Process Names

| Context | BO1 | BO2 |
|---------|-----|-----|
| WMI queries | `BlackOps.exe` | `t6zm.exe` |
| Handler `ProcessNames` | `BlackOps` | `t6zm` |

## Docs to Consult

| When working on… | Read |
|-------------------|------|
| Memory addresses / offsets | `docs/bo1-memory-map.md` |
| IPC / shared memory / events | `docs/ipc-protocol.md` |
| Gameplay mechanics reference | `docs/bo1-zombies-facts.md` |
| Developer console commands | `docs/developer-console-commands.md` |
| Full coding conventions | `.github/copilot-instructions.md` |

## Git Workflow

1. Create a new git worktree + branch (`feat/` prefix) for each task.
2. Work and commit only inside that worktree.
3. Self-review your changes before opening a GitHub PR.
