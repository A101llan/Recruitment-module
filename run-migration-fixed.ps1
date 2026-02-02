# Script to run Entity Framework migration
Add-Type -Path "C:\Users\allan\Documents\Examples\HR\packages\EntityFramework.6.4.4\lib\net45\EntityFramework.dll"
Add-Type -Path "C:\Users\allan\Documents\Examples\HR\HR.Web\bin\HR.Web.dll"

# Create configuration
$configuration = New-Object HR.Web.Migrations.Configuration

# Create migrator with configuration
$migrator = New-Object System.Data.Entity.Migrations.DbMigrator($configuration)

# Run the migration
$migrator.Update()

Write-Host "Migration completed successfully!"
