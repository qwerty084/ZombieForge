# Pull-ZFBuild.ps1
# Optionally switches to a branch, pulls, then runs a full build (ZombieForge + BlackOpsMonitor).
# Usage: .\Pull-ZFBuild.ps1 [-Branch <branch-name>]

param([string]$Branch = "")

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\..\"
$MSBuild  = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"

Push-Location $RepoRoot
try {
    if ($Branch) {
        git fetch origin
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        git checkout $Branch
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        git pull origin $Branch
    } else {
        git pull
    }
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet build "$RepoRoot\ZombieForge\ZombieForge.csproj" -c Debug -p:Platform=x86
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & $MSBuild "$RepoRoot\BlackOpsMonitor\BlackOpsMonitor.vcxproj" `
        /p:Configuration=Debug /p:Platform=Win32 /nologo /v:minimal
    exit $LASTEXITCODE
} finally {
    Pop-Location
}
