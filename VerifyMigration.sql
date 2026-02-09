-- Verify Multi-Tenant Migration
-- Run this in SSMS to verify the database migration was successful

USE HR_Local;
GO

PRINT '=== 1. Verify Companies Table ==='
SELECT * FROM Companies;
GO

PRINT '=== 2. Verify SuperAdmin User ==='
SELECT Id, UserName, Email, Role, CompanyId, RequirePasswordChange, LastPasswordChange
FROM Users
WHERE Role = 'SuperAdmin';
GO

PRINT '=== 3. Check CompanyId Columns in Key Tables ==='
SELECT 
    'Users' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(CompanyId) AS RowsWithCompany,
    COUNT(CASE WHEN CompanyId IS NULL THEN 1 END) AS RowsWithoutCompany
FROM Users
UNION ALL
SELECT 
    'Departments',
    COUNT(*),
    COUNT(CompanyId),
    COUNT(CASE WHEN CompanyId IS NULL THEN 1 END)
FROM Departments
UNION ALL
SELECT 
    'Positions',
    COUNT(*),
    COUNT(CompanyId),
    COUNT(CASE WHEN CompanyId IS NULL THEN 1 END)
FROM Positions
UNION ALL
SELECT 
    'Applications',
    COUNT(*),
    COUNT(CompanyId),
    COUNT(CASE WHEN CompanyId IS NULL THEN 1 END)
FROM Applications
UNION ALL
SELECT 
    'Applicants',
    COUNT(*),
    COUNT(CompanyId),
    COUNT(CASE WHEN CompanyId IS NULL THEN 1 END)
FROM Applicants;
GO

PRINT '=== 4. List All Users by Company ==='
SELECT 
    u.Id,
    u.UserName,
    u.Email,
    u.Role,
    u.CompanyId,
    c.Name AS CompanyName,
    c.Slug AS CompanySlug
FROM Users u
LEFT JOIN Companies c ON u.CompanyId = c.Id
ORDER BY u.CompanyId, u.Role, u.UserName;
GO

PRINT '=== 5. Check Foreign Keys ==='
SELECT 
    OBJECT_NAME(parent_object_id) AS TableName,
    name AS ForeignKeyName,
    OBJECT_NAME(referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys
WHERE name LIKE 'FK_%_Companies'
ORDER BY TableName;
GO

PRINT 'Migration verification complete!'
