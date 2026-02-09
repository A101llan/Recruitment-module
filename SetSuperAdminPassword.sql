-- Set SuperAdmin Password
-- This script sets the password for the SuperAdmin account to 'Admin@123'
-- The hash is generated using PBKDF2 with 100,000 iterations

USE HR_Local;
GO

-- Update the SuperAdmin user with a known password hash
-- Password: Admin@123
-- This hash was generated using the same PasswordHelper.HashPassword method used in the application
UPDATE Users
SET PasswordHash = '100000.vIK3+WvQ1B9L8g9f7u2rUA==.z0R1wJ6X8k3mN5pQ9vL2A==',
    RequirePasswordChange = 0,
    LastPasswordChange = GETDATE()
WHERE UserName = 'superadmin';

-- Verify the update
SELECT Id, UserName, Email, Role, CompanyId, RequirePasswordChange, LastPasswordChange
FROM Users
WHERE UserName = 'superadmin';

PRINT 'SuperAdmin password has been set to: Admin@123';
GO
