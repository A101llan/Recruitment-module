# Working migration script using EF tools
$toolsPath = "C:\Users\allan\Documents\Examples\Recruitment\packages\EntityFramework.5.0.0\tools"
$projectPath = "C:\Users\allan\Documents\Examples\Recruitment\HR.Web"

# Import EF module (EF5; project targets .NET Framework 4.0)
Import-Module "$toolsPath\EntityFramework.psm1"

# Change to project directory
Set-Location $projectPath

# Run database update
Update-Database -ProjectName HR.Web -Verbose

Write-Host "Migration completed!"
