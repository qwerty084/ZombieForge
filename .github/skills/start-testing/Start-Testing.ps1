# Start-Testing.ps1
# Builds ZombieForge + BlackOpsMonitor, then launches ZombieForge and BO1 into a zombie map.
# Usage: .\Start-Testing.ps1 [-Map <mapname>] [-ZFDelay <seconds>] [-SkipBuild]

param(
    [string]$Map       = "zm_theater",   # Kino der Toten by default
    [int]   $ZFDelay   = 3,              # Seconds between ZF launch and Steam+BO1 launch
    [switch]$SkipBuild                   # Skip the build step
)

$RepoRoot  = Resolve-Path "$PSScriptRoot\..\..\..\"
$BO1AppId  = 42700

function Resolve-ZombieForgeExe {
    param(
        [string]$RepoRoot
    )

    $searchRoot = Join-Path $RepoRoot "ZombieForge\bin"
    if (-not (Test-Path $searchRoot)) {
        throw "ZombieForge build output directory not found: $searchRoot"
    }

    $candidate = Get-ChildItem -Path $searchRoot -Filter "ZombieForge.exe" -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "\\bin\\x86\\.*\\win-x86\\ZombieForge\.exe$" } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1 -ExpandProperty FullName

    if (-not $candidate) {
        throw "ZombieForge.exe not found under $searchRoot. Run a build first, or check the output path."
    }

    return $candidate
}

function Resolve-SteamExe {
    $steamExe = (Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -Name SteamExe -ErrorAction SilentlyContinue).SteamExe
    if (-not $steamExe) {
        $steamPath = (Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -Name SteamPath -ErrorAction SilentlyContinue).SteamPath
        if ($steamPath) {
            $steamExe = Join-Path $steamPath "steam.exe"
        }
    }

    if (-not $steamExe) {
        $steamExe = "C:\Program Files (x86)\Steam\steam.exe"
    }

    if (-not (Test-Path $steamExe)) {
        throw "Steam.exe not found. Install Steam or verify HKCU:\Software\Valve\Steam points to a valid installation."
    }

    return (Resolve-Path $steamExe).Path
}

# ── Step 1: Build ────────────────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host "`n[1/3] Building ZombieForge + BlackOpsMonitor..." -ForegroundColor Cyan
    & "$PSScriptRoot\..\build-zfall\Build-ZFAll.ps1"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Aborting." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "Build succeeded." -ForegroundColor Green
} else {
    Write-Host "[1/3] Build skipped (-SkipBuild)." -ForegroundColor DarkGray
}

# ── Step 2: Launch ZombieForge ───────────────────────────────────────────────
Write-Host "`n[2/3] Launching ZombieForge..." -ForegroundColor Cyan
$ZFExe = Resolve-ZombieForgeExe -RepoRoot $RepoRoot
# ZombieForge requires administrator (requireAdministrator in app.manifest).
# -Verb RunAs triggers UAC elevation if the current shell is not already elevated.
Start-Process -FilePath $ZFExe -Verb RunAs

Write-Host "Waiting $ZFDelay seconds for ZombieForge to initialize..." -ForegroundColor DarkGray
Start-Sleep -Seconds $ZFDelay

# ── Step 3: Launch Steam + BO1 ───────────────────────────────────────────────
Write-Host "`n[3/3] Launching Steam + BO1 (map: $Map)..." -ForegroundColor Cyan
$SteamExe = Resolve-SteamExe
# -applaunch forwards to a running Steam instance or starts Steam if needed.
# +devmap loads directly into a solo zombie game on the specified map.
Start-Process -FilePath $SteamExe -ArgumentList "-applaunch $BO1AppId +devmap $Map"

Write-Host "`n✅ Done. Steam is launching BO1 into $Map." -ForegroundColor Green
Write-Host "   ZombieForge will auto-connect once BlackOps.exe is detected." -ForegroundColor DarkGray
