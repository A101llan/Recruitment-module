# DEPRECATED — EF code migrations are no longer used for deployment.
# Use SQL-only schema workflow instead:
#   1. Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql (or Invoke-HostDatabaseScript.ps1)
#   2. HR.Web\Scripts\Apply-MigrationsSql.ps1
#   3. HR.Web\Migrations\Verify-ModelColumns.sql
# See Docs\IIS_DEPLOYMENT_GUIDE.md
Write-Host "Run-EfMigrate.ps1 is deprecated. Use Apply-MigrationsSql.ps1 and HR_CREATE_DATABASE_COMPLETE.sql instead." -ForegroundColor Yellow
exit 1
