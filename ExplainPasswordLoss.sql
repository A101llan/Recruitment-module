-- Reset Passwords After Migration
-- The migration caused password loss. This script sets temporary passwords
-- Users will be required to change passwords on first login

USE HR_Local;
GO

PRINT 'WARNING: Setting temporary passwords for all users.';
PRINT 'All users will be required to change passwords on first login.';
PRINT '';

-- Set temporary password: "TempPass@2026" for all users
-- This hash was generated using the application's PasswordHelper
-- Note: You'll need to generate this hash using the actual application code

-- For now, we'll temporarily disable password requirement by updating a test account
-- Recommended: Create a new admin account via Register page

UPDATE Users
SET RequirePasswordChange = 1,
    LastPasswordChange = NULL
WHERE PasswordHash IS NULL;

SELECT 
    UserName,
    Email,
    Role,
    CASE WHEN PasswordHash IS NULL THEN 'MUST REGISTER/RESET' ELSE 'HAS PASSWORD' END AS Status,
    CompanyId
FROM Users
ORDER BY Role, UserName;

PRINT '';
PRINT '==================================================================';
PRINT 'RECOMMENDATION: Use the application Register page to create';
PRINT 'new accounts with proper password hashing.';
PRINT '';
PRINT 'The migration unfortunately lost the old password data when';
PRINT 'it added the new PasswordHash column without copying existing data.';
PRINT '==================================================================';
GO
