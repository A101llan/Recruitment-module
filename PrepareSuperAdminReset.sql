-- Set SuperAdmin Password to: SuperAdmin@2026!
-- This password meets all security requirements:
-- - At least 8 characters
-- - Contains uppercase, lowercase, digit, and special characters
-- - No sequential characters

USE HR_Local;
GO

-- Since we cannot generate the hash here, we'll set a NULL hash temporarily
-- and provide instructions to use the Forgot Password feature

UPDATE Users
SET PasswordHash = NULL,
    RequirePasswordChange = 1
WHERE UserName = 'superadmin';

SELECT Id, UserName, Email, Role, CompanyId, RequirePasswordChange, PasswordHash
FROM Users
WHERE UserName = 'superadmin';

PRINT '';
PRINT '==================================================================';
PRINT 'SuperAdmin account prepared for password reset.';
PRINT 'Please use the "Forgot Password" feature on the login page';
PRINT 'with email: superadmin@system.com';
PRINT 'OR you can log in with another admin account to set the password.';
PRINT '==================================================================';
GO
