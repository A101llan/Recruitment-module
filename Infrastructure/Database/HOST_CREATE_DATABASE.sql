-- ═══════════════════════════════════════════════════════════════════════════════
-- HR Recruitment Module — HOST / IIS database bootstrap
-- ═══════════════════════════════════════════════════════════════════════════════
--
-- PREFERRED: Run the single generated file (no :r includes required):
--   Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql
--
-- Regenerate that file after schema changes:
--   .\tools\dev\Build-CompleteDatabaseScript.ps1
--
-- sqlcmd:
--   sqlcmd -S YOUR_SERVER -E -b -i Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql
--
-- Or use: .\tools\dev\Invoke-HostDatabaseScript.ps1 -ServerInstance YOUR_SERVER -WindowsAuth
--
-- Bootstrap login username: admin
-- Bootstrap password: generated one-time by Invoke-HostDatabaseScript.ps1
-- Verify: HR.Web\Migrations\Verify-ModelColumns.sql
-- ═══════════════════════════════════════════════════════════════════════════════

PRINT 'Redirecting: execute HR_CREATE_DATABASE_COMPLETE.sql instead of this wrapper.';
PRINT '  Path: Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql';
PRINT '  Or run: .\tools\dev\Invoke-HostDatabaseScript.ps1';
GO

-- Legacy :r orchestrator (SSMS only, paths relative to this folder):
:r HR_CREATE_DATABASE_COMPLETE.sql
GO
