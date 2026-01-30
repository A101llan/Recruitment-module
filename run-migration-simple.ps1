# Simple migration script using EF tools
Add-Type -Path "C:\Users\allan\Documents\Examples\HR\packages\EntityFramework.6.4.4\tools\EntityFramework.dll"

# Set the project directory
$projectPath = "C:\Users\allan\Documents\Examples\HR\HR.Web"
$toolsPath = "C:\Users\allan\Documents\Examples\HR\packages\EntityFramework.6.4.4\tools"

# Change to project directory
Set-Location $projectPath

# Import EF module
Import-Module "$toolsPath\EntityFramework.psd1"

# Run database update
Update-Database -ProjectName HR.Web -Verbose

Write-Host "Migration completed!"
