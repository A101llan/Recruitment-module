function Get-PBKDF2Hash($password) {
    $salt = [byte[]]::new(16)
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($salt)
    $iterations = 100000
    $keySize = 32
    $deriveBytes = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($password, $salt, $iterations)
    $key = $deriveBytes.GetBytes($keySize)
    $saltBase64 = [Convert]::ToBase64String($salt)
    $keyBase64 = [Convert]::ToBase64String($key)
    return "$iterations.$saltBase64.$keyBase64"
}

$pw1 = Read-Host "Enter password #1"
$pw2 = Read-Host "Enter password #2 (optional; press Enter to skip)"

if (-not [string]::IsNullOrWhiteSpace($pw1)) {
    $hash1 = Get-PBKDF2Hash $pw1
    Write-Host "Password #1 hash: $hash1"
}

if (-not [string]::IsNullOrWhiteSpace($pw2)) {
    $hash2 = Get-PBKDF2Hash $pw2
    Write-Host "Password #2 hash: $hash2"
}
