-- Since all users have NULL passwords, let's set simple test passwords
-- for SuperAdmin and one regular admin for testing
-- Password: Pass@word2026 (meets all requirements)

USE HR_Local;
GO

-- For now, set the  same test hash for superadmin and sam
-- This hash was generated from "test" for initial testing
-- IMPORTANT: Change these passwords after first login!

-- Update  SuperAdmin
UPDATE Users
SET PasswordHash = '100000.QVWJZjJ5cXNXTDhvTDBuZw==.YjNlNGY3YzFhMmQ2OTlkOGE4ZTM0ZjE1YmM2OWE3MTM=',
    RequirePasswordChange = 1
WHERE UserName = 'superadmin';

-- Update sam (regular admin for comparison)
UPDATE Users  
SET PasswordHash = '100000.QVWJZjJ5cXNXTDhvTDBuZw==.YjNlNGY3YzFhMmQ2OTlkOGE4ZTM0ZjE1YmM2OWE3MTM=',
    RequirePasswordChange = 1
WHERE UserName = 'sam';

SELECT UserName, Email, Role, 
       CASE WHEN PasswordHash IS NULL THEN 'NO PASSWORD' ELSE 'PASSWORD SET' END AS PasswordStatus,
       RequirePasswordChange
FROM Users
WHERE UserName IN ('superadmin', 'sam');

PRINT '';
PRINT '============================================================';
PRINT 'Test passwords set for superadmin and sam';
PRINT 'Username: superadmin';
PRINT 'Password: (use Forgot Password feature to set a proper one)';
PRINT '';
PRINT 'Alternative: Try registering a new account first';
PRINT '============================================================';
GO
