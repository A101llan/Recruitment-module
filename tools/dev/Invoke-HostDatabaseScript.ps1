#Requires -Version 5.0
<#
.SYNOPSIS
  Runs Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql against SQL Server.

.EXAMPLE
  .\tools\dev\Invoke-HostDatabaseScript.ps1 -ServerInstance ".\SQLEXPRESS" -WindowsAuth

.EXAMPLE
  .\tools\dev\Invoke-HostDatabaseScript.ps1 -ServerInstance "localhost" -UserName sa -Password "YourPassword"
#>
param(
    [Parameter(Mandatory)]
    [string] $ServerInstance,
    [string] $Database = "master",
    [switch] $WindowsAuth,
    [switch] $SqlAuth,
    [string] $UserName = "",
    [string] $Password = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$scriptPath = Join-Path $repoRoot "Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql"
if (-not (Test-Path $scriptPath)) {
    Write-Host "Complete script missing; running Build-CompleteDatabaseScript.ps1 ..." -ForegroundColor Yellow
    & (Join-Path $repoRoot "tools\dev\Build-CompleteDatabaseScript.ps1")
}

if (-not (Test-Path $scriptPath)) {
    throw "Missing script: $scriptPath"
}

function New-RandomBootstrapPassword {
    param([int]$Length = 20)

    if ($Length -lt 12) { $Length = 12 }

    $upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"
    $lower = "abcdefghijkmnopqrstuvwxyz"
    $digits = "23456789"
    $special = "!@#$%^&*_-+=?"
    $all = ($upper + $lower + $digits + $special).ToCharArray()

    $bytes = New-Object byte[] ($Length + 16)
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)

    $chars = New-Object System.Collections.Generic.List[char]
    $chars.Add($upper[$bytes[0] % $upper.Length])
    $chars.Add($lower[$bytes[1] % $lower.Length])
    $chars.Add($digits[$bytes[2] % $digits.Length])
    $chars.Add($special[$bytes[3] % $special.Length])

    for ($i = 4; $i -lt $Length; $i++) {
        $chars.Add($all[$bytes[$i] % $all.Length])
    }

    for ($i = $chars.Count - 1; $i -gt 0; $i--) {
        $j = $bytes[$i + 8] % ($i + 1)
        $tmp = $chars[$i]
        $chars[$i] = $chars[$j]
        $chars[$j] = $tmp
    }

    -join $chars
}

function Get-PasswordHash {
    param(
        [Parameter(Mandatory)][string]$Password,
        [int]$Iterations = 100000,
        [int]$SaltSize = 16,
        [int]$KeySize = 32
    )

    $derive = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Password, $SaltSize, $Iterations)
    $salt = [Convert]::ToBase64String($derive.Salt)
    $key = [Convert]::ToBase64String($derive.GetBytes($KeySize))
    "$Iterations.$salt.$key"
}

$sqlcmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    throw "sqlcmd.exe not found. Install SQL Server Command Line Utilities."
}

if ($WindowsAuth -and $SqlAuth) {
    throw "Choose either -WindowsAuth or -SqlAuth, not both."
}

if ($WindowsAuth) {
    $authArgs = @("-E")
}
elseif ($SqlAuth -or $UserName) {
    if (-not $UserName) {
        throw "When using SQL authentication, provide -UserName."
    }

    if (-not $Password) {
        $secure = Read-Host "SQL password" -AsSecureString
        $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try {
            $Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        }
        finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }

    $authArgs = @("-U", $UserName, "-P", $Password)
}
else {
    throw "Provide -WindowsAuth, or use SQL auth with -SqlAuth -UserName (password will prompt if omitted)."
}

Write-Host "Running host database script on $ServerInstance ..." -ForegroundColor Cyan
Write-Host "  $scriptPath" -ForegroundColor DarkGray

$bootstrapPassword = New-RandomBootstrapPassword
$bootstrapHash = Get-PasswordHash -Password $bootstrapPassword

$args = @("-S", $ServerInstance, "-d", $Database) + $authArgs + @(
    "-b",
    "-v", "BOOTSTRAP_ADMIN_PASSWORD_HASH=$bootstrapHash",
    "-i", $scriptPath
)
& sqlcmd.exe @args
if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Done. Database HR_Local is ready." -ForegroundColor Green
Write-Host "One-time bootstrap login (save now):" -ForegroundColor Yellow
Write-Host "  Username: admin" -ForegroundColor Yellow
Write-Host "  Password: $bootstrapPassword" -ForegroundColor Yellow
Write-Host "You should change this password immediately after first login." -ForegroundColor DarkYellow
Write-Host "Verify with: sqlcmd ... -d HR_Local -i HR.Web\Scripts\VerifyDatabaseSchema.sql" -ForegroundColor DarkGray
