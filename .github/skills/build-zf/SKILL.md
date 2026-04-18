---
name: build-zf
description: >-
  Builds the ZombieForge WinUI 3 C# project in Debug x86 configuration.
  Use when asked to "build ZombieForge", "build the C# project", or "run Build-ZF".
allowed-tools: shell
---

# Build-ZF

Builds only the ZombieForge C# project (not BlackOpsMonitor) in Debug x86.

---

## Step 1 — Run the build

Run the helper script from this skill's base directory:

```powershell
.\Build-ZF.ps1
```

This runs:
```
dotnet build ZombieForge\ZombieForge.csproj -c Debug -p:Platform=x86
```

---

## Step 2 — Report result

- **Success:** Report "✅ Build succeeded."
- **Failure:** Show the compiler errors and suggest a fix if obvious.
