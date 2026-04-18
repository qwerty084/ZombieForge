# AGENTS.md

Practical guidance for contributors and coding agents working in this repository.

## Core boundaries (WinUI + MVVM)
- Keep UI concerns in XAML/views/viewmodels; keep services/models UI-agnostic.
- Do not introduce XAML UI types into `ZombieForge/Services` or `ZombieForge/Models`.
- Marshal WMI/background callbacks to the UI thread with `DispatcherQueue.TryEnqueue`.
- Keep game-specific process names, offsets, and memory logic behind `IGameHandler` in `ZombieForge/Services/Games`.

## Binding and ViewModel conventions
- Prefer `x:Bind` over `{Binding}` in XAML; use `Mode=OneWay` when data is not edited.
- Implement `INotifyPropertyChanged` in ViewModels with `[CallerMemberName]`.

## Logging conventions
- Create loggers via `App.LoggerFactory.CreateLogger<T>()`.
- Use structured logging placeholders (`"... {Value}"`) instead of string interpolation.
- `Warning` and above are persisted to `%LocalAppData%\ZombieForge\Logs`; keep noisy flow logging at `Debug`/`Information`.

## Process naming details
- BO1 process name conventions:
  - WMI watcher queries: `BlackOps.exe`
  - `Process.GetProcessesByName` / handler names: `BlackOps`
- BO2 handler currently uses `t6zm`.

## Key docs to consult
- `docs/bo1-memory-map.md` (BO1 memory map / offsets)
- `docs/bo1-zombies-facts.md` (gameplay reference facts)
- `docs/developer-console-commands.md` (console command references)
- `.github/copilot-instructions.md` (project-specific coding conventions)

## Build / validation commands
- C# app build: `dotnet build ZombieForge\ZombieForge.csproj -c Debug -p:Platform=x86`
- Full build: `dotnet build ZombieForge\ZombieForge.csproj -c Debug -p:Platform=x86` then
  `MSBuild BlackOpsMonitor\BlackOpsMonitor.vcxproj /p:Configuration=Debug /p:Platform=Win32`
- There is currently no dedicated test project in this worktree; run builds for validation.
