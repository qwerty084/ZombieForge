# Build-ZFAll.ps1
# Builds ZombieForge (C#, x86) then BlackOpsMonitor (MSBuild, Win32).
# Usage: .\Build-ZFAll.ps1

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\..\"
$VsWherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $VsWherePath)) {
    Write-Error "MSBuild discovery failed: vswhere.exe was not found at '$VsWherePath'."
    exit 1
}

$MSBuild = & $VsWherePath -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($MSBuild) -or -not (Test-Path $MSBuild)) {
    Write-Error "MSBuild discovery failed: no MSBuild.exe installation was found via vswhere."
    exit 1
}

dotnet build "$RepoRoot\ZombieForge\ZombieForge.csproj" -c Debug -p:Platform=x86
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $MSBuild "$RepoRoot\BlackOpsMonitor\BlackOpsMonitor.vcxproj" `
    /p:Configuration=Debug /p:Platform=Win32 /nologo /v:minimal
exit $LASTEXITCODE
