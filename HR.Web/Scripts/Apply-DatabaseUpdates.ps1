#Requires -Version 5.0
<#
.SYNOPSIS
  DEPRECATED wrapper — use SQL-only schema workflow instead of EF Update-Database.

.DESCRIPTION
  This script now only runs VerifyDatabaseSchema.sql via sqlcmd when -Database is supplied.
  For full schema deploy use:
    1. Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql
    2. HR.Web\Scripts\Apply-MigrationsSql.ps1
    3. HR.Web\Migrations\Verify-ModelColumns.sql
  See Docs\IIS_DEPLOYMENT_GUIDE.md

.EXAMPLE
  .\Apply-DatabaseUpdates.ps1 -ServerInstance ".\SQLEXPRESS" -Database "HR_Local" -WindowsAuth
#>
param(
    [string] $ServerInstance = "localhost",
    [string] $Database = "",
    [switch] $WindowsAuth,
    [string] $UserName = "",
    [string] $Password = "",
    [switch] $ApplyOptionalSql
)

$ErrorActionPreference = "Stop"
$hrWebRoot = Split-Path $PSScriptRoot -Parent
$migrations = Join-Path $hrWebRoot "Migrations"
$verifyScript = Join-Path $PSScriptRoot "VerifyDatabaseSchema.sql"

Write-Host "=== HR.Web database updates (SQL-only) ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "EF Update-Database is deprecated. Deploy schema with:" -ForegroundColor Yellow
Write-Host "  1. Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql"
Write-Host "  2. HR.Web\Scripts\Apply-MigrationsSql.ps1"
Write-Host "  3. HR.Web\Migrations\Verify-ModelColumns.sql"
Write-Host ""

if ([string]::IsNullOrWhiteSpace($Database)) {
    Write-Host "No -Database supplied; nothing to run." -ForegroundColor DarkGray
    exit 0
}

$sqlcmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Warning "sqlcmd.exe not found on PATH. Run Verify-ModelColumns.sql in SSMS manually."
    exit 1
}

if (-not (Test-Path $verifyScript)) {
    throw "Missing file: $verifyScript"
}

if ($WindowsAuth) {
    $authArgs = @("-E")
}
elseif ($UserName -and $Password) {
    $authArgs = @("-U", $UserName, "-P", $Password)
}
else {
    throw "Provide -WindowsAuth or both -UserName and -Password for SQL authentication."
}

function Invoke-HrSqlFile {
    param([Parameter(Mandatory)][string] $Path)
    Write-Host "Running: $Path" -ForegroundColor Green
    $args = @("-S", $ServerInstance, "-d", $Database) + $authArgs + @("-b", "-i", $Path)
    & sqlcmd.exe @args
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed ($LASTEXITCODE): $Path"
    }
}

Invoke-HrSqlFile -Path $verifyScript

if ($ApplyOptionalSql) {
    Write-Host "Apply-MigrationsSql.ps1 applies all incremental HR.Web\Migrations\*.sql files." -ForegroundColor Yellow
    $applySql = Join-Path $PSScriptRoot "Apply-MigrationsSql.ps1"
    if (Test-Path $applySql) {
        & $applySql -ServerInstance $ServerInstance -Database $Database @(
            if ($WindowsAuth) { "-WindowsAuth" }
            if ($UserName) { "-UserName"; $UserName }
            if ($Password) { "-Password"; $Password }
        )
    }
}

Write-Host ""
Write-Host "Done. For missing columns, run Apply-MigrationsSql.ps1 or regenerate HR_CREATE_DATABASE_COMPLETE.sql." -ForegroundColor Cyan
