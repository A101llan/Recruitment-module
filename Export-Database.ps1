# Export HR_Local database
$backupPath = "C:\temp\HR_Local.bak"
$serverName = "(localdb)\MSSQLLocalDB"
$databaseName = "HR_Local"

# Create temp directory if not exists
if (!(Test-Path "C:\temp")) {
    New-Item -ItemType Directory -Path "C:\temp"
}

# Backup the database
try {
    Write-Host "Backing up database $databaseName from $serverName..."
    
    $query = @"
BACKUP DATABASE [$databaseName] 
TO DISK = '$backupPath'
WITH FORMAT, INIT, NAME = '$databaseName-Full Database Backup',
SKIP, NOREWIND, NOUNLOAD, STATS = 10
"@
    
    sqlcmd -S "$serverName" -Q "$query"
    
    if (Test-Path $backupPath) {
        Write-Host "Backup created successfully at: $backupPath"
        Write-Host "File size: $((Get-Item $backupPath).Length / 1MB) MB"
    } else {
        Write-Error "Backup failed - file not created"
    }
}
catch {
    Write-Error "Backup failed: $_"
}
