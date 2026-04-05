# ZombieForge

A WinUI 3 desktop modding tool for **Call of Duty: Black Ops 1 Zombies**. Reads and writes game memory in real time to enable in-game modifications.

> ⚠️ **For offline/solo use only.** Do not use in online or ranked matches.

---

## Features

- 🟢 Real-time game process detection (event-based, no polling)
- 🧠 Live game memory reading and writing *(in progress)*
- 📋 Structured logging — debug output in VS, warnings/errors to file

---

## Planned

- Round control
- Points editor
- Ammo editor
- Black Ops 2 support

---

## Requirements

- Windows 10 (1809+) or Windows 11
- [Windows App Runtime 1.8](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)
- **Run as Administrator** (required for game memory access)
- Call of Duty: Black Ops 1 (Steam)

---

## Tech Stack

| | |
|---|---|
| UI | WinUI 3 (Windows App SDK 1.8) |
| Framework | .NET 8, C# 12 |
| Pattern | MVVM |
| Logging | Microsoft.Extensions.Logging + Serilog |
| Process detection | WMI (`System.Management`) |
| Memory access | Win32 P/Invoke (`ReadProcessMemory` / `WriteProcessMemory`) |

---

## Project Structure

```
ZombieForge/
├── Converters/       # IValueConverter implementations for XAML bindings
├── Models/           # Plain data classes (game state, etc.)
├── Services/         # Non-UI logic
│   └── Games/        # Per-game handlers implementing IGameHandler
├── ViewModels/       # MVVM ViewModels
└── MainWindow.xaml   # Main window
```

---

## Logs

Runtime logs are written to:
```
%LocalAppData%\ZombieForge\Logs\log-<date>.txt
```
Only `Warning` level and above is written to file. Debug output is available in the Visual Studio Output window during development.

---

## License

[MIT](LICENSE)
