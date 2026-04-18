---
name: pull-zfbuild
description: >-
  Pulls the latest changes (optionally switching to a specific branch first) then runs a full build
  of ZombieForge and BlackOpsMonitor. Use when asked to "pull and build", "pull branch X and build",
  or "run Pull-ZFBuild".
allowed-tools: shell
---

# Pull-ZFBuild

Pulls (and optionally checks out) a branch, then runs the full build (ZombieForge + BlackOpsMonitor).

---

## Step 1 — Determine the branch

If the user provided a branch name, pass it with `-Branch`. Otherwise omit it to pull the current branch.

```powershell
# With a specific branch:
.\Pull-ZFBuild.ps1 -Branch <branch-name>

# Current branch:
.\Pull-ZFBuild.ps1
```

---

## Step 2 — What the script does

1. If `-Branch` is given: `git fetch origin` → `git checkout <branch>` → `git pull origin <branch>`
2. If no branch: `git pull`
3. On pull success: `dotnet build ZombieForge\ZombieForge.csproj -c Debug -p:Platform=x86`
4. On C# success: `MSBuild BlackOpsMonitor\BlackOpsMonitor.vcxproj /p:Configuration=Debug /p:Platform=Win32`

Each step only runs if the previous one succeeded.

---

## Step 3 — Report result

- **Success:** Report "✅ Pulled and built successfully." and the branch that was built.
- **Git failure:** Show the git error. Build steps are skipped.
- **Build failure:** Show the compiler errors. Note which stage failed.
