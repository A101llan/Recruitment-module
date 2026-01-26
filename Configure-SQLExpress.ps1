# Enable SQL Server Express TCP/IP and Named Pipes
Import-Module SqlServer

# Load SQL Server WMI provider
$smo = [Microsoft.SqlServer.Management.Smo.Wmi.ManagedComputer]::LocalMachine

# Enable TCP/IP for SQLEXPRESS
$tcp = $smo.GetProtocol("SQLEXPRESS", "Tcp")
$tcp.IsEnabled = $true
$tcp.Alter()

# Enable Named Pipes for SQLEXPRESS
$np = $smo.GetProtocol("SQLEXPRESS", "NamedPipes")
$np.IsEnabled = $true
$np.Alter()

# Set TCP Port to 1433
$tcp.IPAddresses["IPAll"].IPAddressProperties["TcpPort"].Value = "1433"
$tcp.IPAddresses["IPAll"].IPAddressProperties["TcpDynamicPorts"].Value = ""
$tcp.Alter()

# Restart SQL Server service
Restart-Service -Name "MSSQL$SQLEXPRESS" -Force

Write-Host "SQL Server Express configured successfully!"
