# Build-ZFAll.ps1
# Builds ZombieForge (C#, x86) then BlackOpsMonitor (MSBuild, Win32).
# Usage: .\Build-ZFAll.ps1

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\..\"
$MSBuild  = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"

dotnet build "$RepoRoot\ZombieForge\ZombieForge.csproj" -c Debug -p:Platform=x86
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $MSBuild "$RepoRoot\BlackOpsMonitor\BlackOpsMonitor.vcxproj" `
    /p:Configuration=Debug /p:Platform=Win32 /nologo /v:minimal
exit $LASTEXITCODE
