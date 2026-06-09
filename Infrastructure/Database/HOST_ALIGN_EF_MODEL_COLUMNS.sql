-- Align legacy staging column names with current entity models (idempotent).
-- Run after FULL_STAGING_DEPLOYMENT.sql on fresh installs.

PRINT 'HOST_ALIGN_EF_MODEL_COLUMNS: aligning Users, Applications, Positions...';

IF COL_LENGTH(N'dbo.Users', N'UserName') IS NULL
   AND COL_LENGTH(N'dbo.Users', N'Username') IS NOT NULL
BEGIN
    EXEC sp_rename N'dbo.Users.Username', N'UserName', N'COLUMN';
    PRINT '   Renamed Users.Username -> UserName';
END
GO

IF COL_LENGTH(N'dbo.Applications', N'AppliedOn') IS NULL
   AND COL_LENGTH(N'dbo.Applications', N'AppliedDate') IS NOT NULL
BEGIN
    EXEC sp_rename N'dbo.Applications.AppliedDate', N'AppliedOn', N'COLUMN';
    PRINT '   Renamed Applications.AppliedDate -> AppliedOn';
END
GO

IF COL_LENGTH(N'dbo.Positions', N'IsOpen') IS NULL
BEGIN
    IF COL_LENGTH(N'dbo.Positions', N'IsActive') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Positions ADD IsOpen BIT NOT NULL CONSTRAINT DF_Positions_IsOpen DEFAULT (1);
        EXEC(N'UPDATE dbo.Positions SET IsOpen = IsActive');
        PRINT '   Added Positions.IsOpen from IsActive';
    END
    ELSE
    BEGIN
        ALTER TABLE dbo.Positions ADD IsOpen BIT NOT NULL CONSTRAINT DF_Positions_IsOpen DEFAULT (1);
        PRINT '   Added Positions.IsOpen';
    END
END
GO

IF COL_LENGTH(N'dbo.Positions', N'PostedOn') IS NULL
BEGIN
    IF COL_LENGTH(N'dbo.Positions', N'CreatedDate') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Positions ADD PostedOn DATETIME NOT NULL CONSTRAINT DF_Positions_PostedOn DEFAULT (GETUTCDATE());
        EXEC(N'UPDATE dbo.Positions SET PostedOn = CreatedDate WHERE PostedOn IS NULL OR PostedOn = ''19000101''');
        PRINT '   Added Positions.PostedOn from CreatedDate';
    END
    ELSE
    BEGIN
        ALTER TABLE dbo.Positions ADD PostedOn DATETIME NOT NULL CONSTRAINT DF_Positions_PostedOn DEFAULT (GETUTCDATE());
        PRINT '   Added Positions.PostedOn';
    END
END
GO

-- Applicants: legacy FirstName/LastName -> FullName (EF model)
IF COL_LENGTH(N'dbo.Applicants', N'FullName') IS NULL
BEGIN
    IF COL_LENGTH(N'dbo.Applicants', N'FirstName') IS NOT NULL
       AND COL_LENGTH(N'dbo.Applicants', N'LastName') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Applicants ADD FullName NVARCHAR(200) NULL;
        UPDATE dbo.Applicants
        SET FullName = LTRIM(RTRIM(ISNULL(FirstName, N'') + N' ' + ISNULL(LastName, N'')))
        WHERE FullName IS NULL;
        UPDATE dbo.Applicants SET FullName = N'Unknown' WHERE FullName IS NULL OR LTRIM(RTRIM(FullName)) = N'';
        ALTER TABLE dbo.Applicants ALTER COLUMN FullName NVARCHAR(200) NOT NULL;
        PRINT '   Added Applicants.FullName from FirstName/LastName';
    END
    ELSE
    BEGIN
        ALTER TABLE dbo.Applicants ADD FullName NVARCHAR(200) NOT NULL CONSTRAINT DF_Applicants_FullName DEFAULT (N'');
        PRINT '   Added Applicants.FullName';
    END
END
GO

-- Ensure admin seed uses UserName (FULL_STAGING may have inserted before rename).
IF EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'admin')
   AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Role = N'SuperAdmin')
BEGIN
    UPDATE dbo.Users SET Role = N'SuperAdmin' WHERE UserName = N'admin';
    PRINT '   Promoted admin user to SuperAdmin role';
END
GO

-- One-time bootstrap credential must be changed immediately on first login.
IF COL_LENGTH(N'dbo.Users', N'RequirePasswordChange') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'admin')
BEGIN
    UPDATE dbo.Users
    SET RequirePasswordChange = 1
    WHERE UserName = N'admin';
    PRINT '   Enforced RequirePasswordChange for admin';
END
GO

PRINT 'HOST_ALIGN_EF_MODEL_COLUMNS complete.';
GO
