-- Create Missing Security and Audit Tables
-- This script creates the LoginAttempts, AuditLogs, PasswordResets, and Reports tables

USE HR_Local;
GO

-- Create LoginAttempts table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoginAttempts')
BEGIN
    CREATE TABLE LoginAttempts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        Username NVARCHAR(100) NOT NULL,
        IPAddress NVARCHAR(50) NULL,
        AttemptTime DATETIME NOT NULL,
        IsSuccessful BIT NOT NULL,
        FailureReason NVARCHAR(200) NULL,
        CONSTRAINT FK_LoginAttempts_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id)
    );
    PRINT 'LoginAttempts table created.';
END
GO

-- Create AuditLogs table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        Username NVARCHAR(100) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(50) NULL,
        EntityId NVARCHAR(50) NULL,
        Timestamp DATETIME NOT NULL,
        IPAddress NVARCHAR(50) NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        WasSuccessful BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AuditLogs_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id)
    );
    PRINT 'AuditLogs table created.';
END
GO

-- Create PasswordResets table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResets')
BEGIN
    CREATE TABLE PasswordResets (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(100) NOT NULL,
        Token NVARCHAR(256) NOT NULL,
        ExpiryDate DATETIME NOT NULL,
        IsUsed BIT NOT NULL DEFAULT 0,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'PasswordResets table created.';
END
GO

-- Create Reports table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reports')
BEGIN
    CREATE TABLE Reports (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        Name NVARCHAR(100) NOT NULL,
        Type NVARCHAR(50) NOT NULL,
        GeneratedBy NVARCHAR(100) NOT NULL,
        GeneratedDate DATETIME NOT NULL,
        FilePath NVARCHAR(500) NULL,
        Parameters NVARCHAR(MAX) NULL,
        CONSTRAINT FK_Reports_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id)
    );
    PRINT 'Reports table created.';
END
GO

-- Create LicenseTransactions table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LicenseTransactions')
BEGIN
    CREATE TABLE LicenseTransactions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        ExecutedBy NVARCHAR(100) NOT NULL,
        TransactionDate DATETIME NOT NULL,
        PreviousExpiry DATETIME NULL,
        NewExpiry DATETIME NOT NULL,
        ExtendedByValue INT NOT NULL,
        ExtendedByUnit NVARCHAR(20) NULL,
        Notes NVARCHAR(MAX) NULL,
        CONSTRAINT FK_LicenseTransactions_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id)
    );
    PRINT 'LicenseTransactions table created.';
END
GO

-- Create ImpersonationRequests table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImpersonationRequests')
BEGIN
    CREATE TABLE ImpersonationRequests (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        RequestedBy NVARCHAR(100) NOT NULL,
        RequestedFrom NVARCHAR(100) NOT NULL,
        RequestDate DATETIME NOT NULL,
        Status INT NOT NULL DEFAULT 0, -- 0 = Pending
        Reason NVARCHAR(MAX) NULL,
        DecisionDate DATETIME NULL,
        AdminNotes NVARCHAR(MAX) NULL,
        ExpiryDate DATETIME NULL,
        CONSTRAINT FK_ImpersonationRequests_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id)
    );
    PRINT 'ImpersonationRequests table created.';
END
GO

PRINT 'All missing tables created successfully!';
