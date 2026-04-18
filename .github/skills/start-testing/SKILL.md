---
name: start-testing
description: >-
  Runs the full ZombieForge test workflow: builds the solution, launches ZombieForge,
  then launches Steam + Black Ops 1 directly into a solo zombie map via +devmap.
  Use when asked to "start testing", "launch the test environment", "build and launch",
  or "run start-testing".
allowed-tools: shell
---

# Start-Testing

Automates the full test loop: **build → launch ZombieForge → launch BO1 into zombies**.

---

## Step 1 — Run the script

Run the helper script from this skill's base directory:

```powershell
# Default (Kino der Toten, 3s delay, includes build):
.\Start-Testing.ps1

# Custom map:
.\Start-Testing.ps1 -Map zm_cosmodrome

# Skip build if already built:
.\Start-Testing.ps1 -SkipBuild

# All options:
.\Start-Testing.ps1 -Map <mapname> -ZFDelay <seconds> -SkipBuild
```

---

## Step 2 — What the script does

| # | Action | Detail |
|---|--------|--------|
| 1 | **Build** | Calls `build-zfall\Build-ZFAll.ps1`. Aborts if build fails. Skipped with `-SkipBuild`. |
| 2 | **Launch ZombieForge** | Finds the newest `ZombieForge.exe` under `ZombieForge\bin\x86\...\win-x86\`, runs it with admin elevation (`-Verb RunAs`), then waits `ZFDelay` seconds (default: 3s) for initialization. |
| 3 | **Launch Steam + BO1** | Resolves `steam.exe` from the Steam registry entry (with a standard install-path fallback), then runs `-applaunch 42700 +devmap $Map` to open solo zombies on the chosen map. |

ZombieForge will auto-detect `BlackOps.exe` via its ProcessWatcher once the game starts.

---

## Step 3 — Report result

- **Build failed:** Show errors, note that launch was aborted.
- **ZombieForge.exe not found:** Tell the user to run a build first.
- **Steam.exe not found:** Tell the user to install Steam or verify `HKCU:\Software\Valve\Steam`.
- **Success:** Report which map was launched and that ZombieForge will connect automatically.

---

## Available zombie maps

| Map ID | Map Name |
|--------|----------|
| `zm_prototype` | Nacht der Untoten |
| `zm_asylum` | Verrückt |
| `zm_sumpf` | Shi No Numa |
| `zm_factory` | Der Riese |
| `zm_theater` | Kino der Toten *(default)* |
| `zm_cosmodrome` | Ascension |
| `zm_coast` | Call of the Dead |
| `zm_temple` | Shangri-La |
| `zm_moon` | Moon |
