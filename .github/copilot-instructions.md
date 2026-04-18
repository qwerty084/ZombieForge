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

## How to Add a New Game
- Create `ZombieForge/Services/Games/<GameName>Handler.cs` and implement `IGameHandler`.
- Set `ProcessNames` to game executable names without `.exe` (include variants when needed).
- Keep offsets/address maps and game-specific memory logic inside the new handler (or helper types in `Services/Games/`), and implement `IGameHandler.ReadPlayerStats(...)` and `IGameHandler.ReadLevelTime(...)`.
- Keep handlers non-UI; any background callback updates must still be marshaled with `DispatcherQueue.TryEnqueue` in ViewModels/services.
- Wire the new handler into the existing game selection list (for example, `MainViewModel` handler/display entries) without changing core services.

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
- Memory address maps and hook reference data: see `docs/bo1-memory-map.md`
