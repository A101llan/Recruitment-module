#Requires -RunAsAdministrator
param(
    [string] $SiteName = "Hirehub",
    [string] $AppPoolName = "Hirehub_Pool",
    [string] $PhysicalPath = "C:\inetpub\wwwroot\Hirehub",
    [int] $Port = 5002
)

$ErrorActionPreference = "Continue"
$log = Join-Path $PhysicalPath "deploy-diagnostic.log"
"=== $(Get-Date -Format o) ===" | Out-File $log

function Log($msg) {
    Write-Host $msg
    $msg | Out-File $log -Append
}

Import-Module WebAdministration -ErrorAction SilentlyContinue
if (-not (Get-Module WebAdministration)) {
    Log "WebAdministration module unavailable."
}

Log "Site:"
Get-Website -Name $SiteName -ErrorAction SilentlyContinue | Format-List * | Out-String | Out-File $log -Append

Log "App pool:"
Get-ItemProperty "IIS:\AppPools\$AppPoolName" -ErrorAction SilentlyContinue | Format-List * | Out-String | Out-File $log -Append

Restart-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

try {
    $r = Invoke-WebRequest -Uri "http://localhost:$Port/Account/Login" -UseBasicParsing -TimeoutSec 20
    Log "HTTP $($r.StatusCode) OK"
}
catch {
    Log "HTTP error: $($_.Exception.Message)"
}

Get-WinEvent -FilterHashtable @{LogName='Application'; Level=2,3; StartTime=(Get-Date).AddMinutes(-10)} -MaxEvents 10 -ErrorAction SilentlyContinue |
    ForEach-Object { Log $_.Message.Substring(0, [Math]::Min(1200, $_.Message.Length)) }

Log "Diagnostic log: $log"
