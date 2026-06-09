#Requires -Version 5.0
param(
    [string] $ServerInstance = "127.0.0.1",
    [string] $Database = "HR_Local",
    [string] $UserName = "admin",
    [string] $SqlUser = "sa",
    [string] $SqlPassword = "sa@123456",
    [string] $NewPassword = ""
)

$ErrorActionPreference = "Stop"

function New-RandomPassword {
    param([int]$Length = 16)

    $upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"
    $lower = "abcdefghijkmnopqrstuvwxyz"
    $digits = "23456789"
    $special = '!@#$%^&*_-+=?'
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
    return "$Iterations.$salt.$key"
}

if ([string]::IsNullOrWhiteSpace($NewPassword)) {
    $NewPassword = New-RandomPassword
}

$hash = Get-PasswordHash -Password $NewPassword
$escapedUser = $UserName.Replace("'", "''")
$query = @"
UPDATE Users
SET PasswordHash = N'$hash',
    RequirePasswordChange = 0
WHERE UserName = N'$escapedUser';

SELECT @@ROWCOUNT AS UpdatedRows;
"@

$output = sqlcmd -S $ServerInstance -U $SqlUser -P $SqlPassword -d $Database -Q $query -W -h -1
if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd failed with exit code $LASTEXITCODE"
}

if ($output -notmatch "1") {
    throw "User '$UserName' was not updated. sqlcmd output: $output"
}

Write-Host ""
Write-Host "SuperAdmin password reset complete." -ForegroundColor Green
Write-Host "  Username: $UserName"
Write-Host "  Password: $NewPassword" -ForegroundColor Yellow
Write-Host "  Login:    http://localhost:5002/Account/Login"
Write-Host ""
Write-Host "Save this password now; it cannot be recovered from the database." -ForegroundColor DarkYellow
