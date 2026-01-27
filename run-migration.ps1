# Script to run Entity Framework migration
Add-Type -Path "C:\Users\allan\Documents\Examples\HR\packages\EntityFramework.6.4.4\tools\EntityFramework.dll"

$context = New-Object HR.Web.Data.HrContext
$configuration = New-Object HR.Web.Migrations.Configuration

# Enable migrations if not already enabled
[System.Data.Entity.Database]::SetInitializer($null)

# Run the migration
$migrator = New-Object System.Data.Entity.Migrations.DbMigrator($configuration)
$migrator.Update()

Write-Host "Migration completed successfully!"
