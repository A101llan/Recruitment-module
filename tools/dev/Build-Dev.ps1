# Build HR.Web for local debugging (Debug config -> HR.Web\bin\).
# Run from repository root: .\tools\dev\Build-Dev.ps1

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$solution = Join-Path $repoRoot "HR.sln"
$dll = Join-Path $repoRoot "HR.Web\bin\HR.Web.dll"

Write-Host "Building Debug (HR.Web\bin\)..." -ForegroundColor Cyan
Push-Location $repoRoot
try {
    dotnet build $solution -c Debug -t:Rebuild -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

$item = Get-Item $dll
Write-Host "Debug build ready: $($item.FullName)" -ForegroundColor Green
Write-Host "  Modified: $($item.LastWriteTime)" -ForegroundColor Gray
Write-Host "Use Visual Studio configuration 'Debug' (not Release) when pressing F5." -ForegroundColor Yellow
