---
name: build-zfall
description: >-
  Builds both ZombieForge (C#, x86) and BlackOpsMonitor (C++, Win32) in Debug configuration.
  Use when asked to "build everything", "full build", "build ZombieForge and BlackOpsMonitor", or "run Build-ZFAll".
allowed-tools: shell
---

# Build-ZFAll

Builds the full ZombieForge solution: the C# WinUI 3 app first, then the BlackOpsMonitor C++ DLL.
BlackOpsMonitor is only built if the C# build succeeds.

---

## Step 1 — Run the full build

Run the helper script from this skill's base directory:

```powershell
.\Build-ZFAll.ps1
```

This runs in order:
1. `dotnet build ZombieForge\ZombieForge.csproj -c Debug -p:Platform=x86`
2. `MSBuild BlackOpsMonitor\BlackOpsMonitor.vcxproj /p:Configuration=Debug /p:Platform=Win32`

---

## Step 2 — Report result

- **Success:** Report "✅ Full build succeeded (ZombieForge + BlackOpsMonitor)."
- **C# failure:** Show dotnet errors. MSBuild step is skipped.
- **MSBuild failure:** Show MSBuild errors. Note that the C# build passed.
