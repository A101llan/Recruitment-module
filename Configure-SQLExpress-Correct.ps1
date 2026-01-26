# Configure SQL Server Express using correct Registry paths
Write-Host "Configuring SQL Server Express via Registry..."

# Try MSSQL15.SQLEXPRESS first (SQL Server 2019)
$basePath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQLServer\SuperSocketNetLib"

# Enable TCP/IP
if (Test-Path "$basePath\Tcp") {
    Set-ItemProperty -Path "$basePath\Tcp" -Name "Enabled" -Value 1 -Force
    Write-Host "TCP/IP enabled for SQLEXPRESS"
}

# Enable Named Pipes
if (Test-Path "$basePath\Np") {
    Set-ItemProperty -Path "$basePath\Np" -Name "Enabled" -Value 1 -Force
    Write-Host "Named Pipes enabled for SQLEXPRESS"
}

# Set TCP Port to 1433
if (Test-Path "$basePath\Tcp\IPAll") {
    Set-ItemProperty -Path "$basePath\Tcp\IPAll" -Name "TcpPort" -Value "1433" -Force
    Set-ItemProperty -Path "$basePath\Tcp\IPAll" -Name "TcpDynamicPorts" -Value "" -Force
    Write-Host "TCP Port set to 1433"
}

# Restart SQL Server service
$serviceName = "MSSQL$SQLEXPRESS"
if (Get-Service $serviceName -ErrorAction SilentlyContinue) {
    Restart-Service -Name $serviceName -Force
    Write-Host "SQL Server SQLEXPRESS service restarted"
} else {
    Write-Host "Service $serviceName not found"
}

Write-Host "Configuration complete!"
