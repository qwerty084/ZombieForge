# Build-ZF.ps1
# Builds the ZombieForge C# project only (Debug, x86).
# Usage: .\Build-ZF.ps1

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\..\"

dotnet build "$RepoRoot\ZombieForge\ZombieForge.csproj" -c Debug -p:Platform=x86
exit $LASTEXITCODE
