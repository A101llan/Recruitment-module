-- Tables and columns required by HR.Web but not in FULL_STAGING_DEPLOYMENT.sql
-- (idempotent — safe to re-run).

PRINT 'HOST_SCHEMA_EXTENSIONS: supplemental tables and columns...';

-- Positions (EF model fields beyond base staging script)
IF COL_LENGTH(N'dbo.Positions', N'Responsibilities') IS NULL
    ALTER TABLE dbo.Positions ADD Responsibilities NVARCHAR(3000) NULL;
IF COL_LENGTH(N'dbo.Positions', N'Qualifications') IS NULL
    ALTER TABLE dbo.Positions ADD Qualifications NVARCHAR(3000) NULL;
IF COL_LENGTH(N'dbo.Positions', N'Currency') IS NULL
    ALTER TABLE dbo.Positions ADD Currency NVARCHAR(10) NULL;
IF COL_LENGTH(N'dbo.Positions', N'SalaryMin') IS NULL
    ALTER TABLE dbo.Positions ADD SalaryMin DECIMAL(18, 2) NULL;
IF COL_LENGTH(N'dbo.Positions', N'SalaryMax') IS NULL
    ALTER TABLE dbo.Positions ADD SalaryMax DECIMAL(18, 2) NULL;
IF COL_LENGTH(N'dbo.Positions', N'ExpiryDate') IS NULL
    ALTER TABLE dbo.Positions ADD ExpiryDate DATETIME NULL;
IF COL_LENGTH(N'dbo.Positions', N'IsTechnical') IS NULL
    ALTER TABLE dbo.Positions ADD IsTechnical BIT NOT NULL CONSTRAINT DF_Positions_IsTechnical_HostExt DEFAULT (0);
IF COL_LENGTH(N'dbo.Positions', N'PassMark') IS NULL
    ALTER TABLE dbo.Positions ADD PassMark DECIMAL(18, 2) NULL;
IF COL_LENGTH(N'dbo.Positions', N'PassMarksByStageJson') IS NULL
    ALTER TABLE dbo.Positions ADD PassMarksByStageJson NVARCHAR(4000) NULL;
IF COL_LENGTH(N'dbo.Positions', N'QuestionnaireStageCount') IS NULL
    ALTER TABLE dbo.Positions ADD QuestionnaireStageCount INT NOT NULL CONSTRAINT DF_Positions_QStageCount_HostExt DEFAULT (1);
IF COL_LENGTH(N'dbo.Companies', N'LogoPath') IS NULL
    ALTER TABLE dbo.Companies ADD LogoPath NVARCHAR(260) NULL;
IF COL_LENGTH(N'dbo.Users', N'Phone') IS NULL
    ALTER TABLE dbo.Users ADD Phone NVARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.Users', N'DateOfBirth') IS NULL
    ALTER TABLE dbo.Users ADD DateOfBirth DATETIME NULL;
GO

-- Applications (legacy document CV path removed)
IF COL_LENGTH(N'dbo.Applications', N'ResumePath') IS NOT NULL
    ALTER TABLE dbo.Applications DROP COLUMN ResumePath;
IF COL_LENGTH(N'dbo.Applicants', N'ResumePath') IS NOT NULL
    ALTER TABLE dbo.Applicants DROP COLUMN ResumePath;
IF COL_LENGTH(N'dbo.Applications', N'WorkExperienceLevel') IS NULL
    ALTER TABLE dbo.Applications ADD WorkExperienceLevel NVARCHAR(30) NULL;
IF COL_LENGTH(N'dbo.Applications', N'ScoreReason') IS NULL
    ALTER TABLE dbo.Applications ADD ScoreReason NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'dbo.Applications', N'CoverLetter') IS NULL
    ALTER TABLE dbo.Applications ADD CoverLetter NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'dbo.Applications', N'CurrentStage') IS NULL
    ALTER TABLE dbo.Applications ADD CurrentStage INT NOT NULL CONSTRAINT DF_Applications_CurrentStage_HostExt DEFAULT (1);
IF COL_LENGTH(N'dbo.Applications', N'PendingQuestionnaireStage') IS NULL
    ALTER TABLE dbo.Applications ADD PendingQuestionnaireStage INT NULL;
IF COL_LENGTH(N'dbo.Applications', N'LastCompletedQuestionnaireStage') IS NULL
    ALTER TABLE dbo.Applications ADD LastCompletedQuestionnaireStage INT NOT NULL CONSTRAINT DF_Applications_LastCompletedQ_HostExt DEFAULT (0);
IF COL_LENGTH(N'dbo.Applications', N'QuestionnaireInvitedOn') IS NULL
    ALTER TABLE dbo.Applications ADD QuestionnaireInvitedOn DATETIME NULL;
IF COL_LENGTH(N'dbo.Applications', N'LastQuestionnaireScore') IS NULL
    ALTER TABLE dbo.Applications ADD LastQuestionnaireScore DECIMAL(18, 2) NULL;
IF COL_LENGTH(N'dbo.Applications', N'FailedCandidateEmailSentAt') IS NULL
    ALTER TABLE dbo.Applications ADD FailedCandidateEmailSentAt DATETIME NULL;
GO

-- Users (email verification and panelist support beyond MFA scripts)
IF COL_LENGTH(N'dbo.Users', N'IsEmailVerified') IS NULL
    ALTER TABLE dbo.Users ADD IsEmailVerified BIT NOT NULL CONSTRAINT DF_Users_IsEmailVerified DEFAULT (0);
IF COL_LENGTH(N'dbo.Users', N'EmailVerificationCode') IS NULL
    ALTER TABLE dbo.Users ADD EmailVerificationCode NVARCHAR(10) NULL;
IF COL_LENGTH(N'dbo.Users', N'EmailVerificationExpiry') IS NULL
    ALTER TABLE dbo.Users ADD EmailVerificationExpiry DATETIME NULL;
IF COL_LENGTH(N'dbo.Users', N'IsPanelist') IS NULL
    ALTER TABLE dbo.Users ADD IsPanelist BIT NOT NULL CONSTRAINT DF_Users_IsPanelist DEFAULT (0);
IF COL_LENGTH(N'dbo.Users', N'RequirePasswordChange') IS NULL
    ALTER TABLE dbo.Users ADD RequirePasswordChange BIT NOT NULL CONSTRAINT DF_Users_RequirePasswordChange DEFAULT (0);
IF COL_LENGTH(N'dbo.Users', N'LastPasswordChange') IS NULL
    ALTER TABLE dbo.Users ADD LastPasswordChange DATETIME NULL;
IF COL_LENGTH(N'dbo.Users', N'PrivacyAcceptedAt') IS NULL
    ALTER TABLE dbo.Users ADD PrivacyAcceptedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.Users', N'TermsAcceptedAt') IS NULL
    ALTER TABLE dbo.Users ADD TermsAcceptedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.Users', N'PrivacyVersion') IS NULL
    ALTER TABLE dbo.Users ADD PrivacyVersion NVARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.Users', N'TermsVersion') IS NULL
    ALTER TABLE dbo.Users ADD TermsVersion NVARCHAR(20) NULL;
GO

-- Applicants (legal acceptance + email verification)
IF COL_LENGTH(N'dbo.Applicants', N'IsEmailVerified') IS NULL
    ALTER TABLE dbo.Applicants ADD IsEmailVerified BIT NOT NULL CONSTRAINT DF_Applicants_IsEmailVerified DEFAULT (0);
IF COL_LENGTH(N'dbo.Applicants', N'PrivacyAcceptedAt') IS NULL
    ALTER TABLE dbo.Applicants ADD PrivacyAcceptedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.Applicants', N'TermsAcceptedAt') IS NULL
    ALTER TABLE dbo.Applicants ADD TermsAcceptedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.Applicants', N'PrivacyVersion') IS NULL
    ALTER TABLE dbo.Applicants ADD PrivacyVersion NVARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.Applicants', N'TermsVersion') IS NULL
    ALTER TABLE dbo.Applicants ADD TermsVersion NVARCHAR(20) NULL;
GO

-- PasswordResets (IP binding for reset links)
IF COL_LENGTH(N'dbo.PasswordResets', N'RequestingIP') IS NULL
    ALTER TABLE dbo.PasswordResets ADD RequestingIP NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.PasswordResets', N'CompletedIP') IS NULL
    ALTER TABLE dbo.PasswordResets ADD CompletedIP NVARCHAR(100) NULL;
GO

-- SystemSettings (key/value store for email templates and app config)
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings (
        SettingKey NVARCHAR(128) NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
        SettingValue NVARCHAR(MAX) NOT NULL,
        Description NVARCHAR(200) NULL,
        IsEncrypted BIT NOT NULL CONSTRAINT DF_SystemSettings_IsEncrypted DEFAULT (0)
    );
    PRINT '   Created dbo.SystemSettings';
END
GO

-- TemporaryCredentials (secure company admin credential handoff)
IF OBJECT_ID(N'dbo.TemporaryCredentials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TemporaryCredentials (
        Id INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_TemporaryCredentials PRIMARY KEY,
        Token NVARCHAR(100) NOT NULL,
        EncryptedData NVARCHAR(MAX) NOT NULL,
        ExpiryDate DATETIME NOT NULL,
        IsUsed BIT NOT NULL CONSTRAINT DF_TemporaryCredentials_IsUsed DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_TemporaryCredentials_CreatedDate DEFAULT (GETUTCDATE()),
        CredentialType NVARCHAR(50) NULL
    );
    CREATE UNIQUE INDEX IX_TemporaryCredentials_Token ON dbo.TemporaryCredentials (Token);
    PRINT '   Created dbo.TemporaryCredentials';
END
GO

-- Questionnaire templates (also in HR.Web\Migrations\202605240000015_AddQuestionnaireTemplates.sql)
IF OBJECT_ID(N'dbo.QuestionnaireTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuestionnaireTemplates (
        Id INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_QuestionnaireTemplates PRIMARY KEY,
        CompanyId INT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        StageCount INT NOT NULL CONSTRAINT DF_QuestionnaireTemplates_StageCount DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_QuestionnaireTemplates_IsActive DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_QuestionnaireTemplates_CreatedOn DEFAULT (GETUTCDATE()),
        UpdatedOn DATETIME NULL,
        CONSTRAINT FK_QuestionnaireTemplates_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies (Id)
    );
    CREATE NONCLUSTERED INDEX IX_QuestionnaireTemplates_CompanyId ON dbo.QuestionnaireTemplates (CompanyId);
    PRINT '   Created dbo.QuestionnaireTemplates';
END
GO

IF OBJECT_ID(N'dbo.QuestionnaireTemplateQuestions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuestionnaireTemplateQuestions (
        Id INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_QuestionnaireTemplateQuestions PRIMARY KEY,
        TemplateId INT NOT NULL,
        QuestionId INT NOT NULL,
        [Order] INT NOT NULL,
        Weight DECIMAL(18, 2) NULL,
        IsRequired BIT NOT NULL CONSTRAINT DF_QuestionnaireTemplateQuestions_IsRequired DEFAULT (1),
        StageNumber INT NOT NULL CONSTRAINT DF_QuestionnaireTemplateQuestions_StageNumber DEFAULT (1),
        CONSTRAINT FK_QuestionnaireTemplateQuestions_Templates FOREIGN KEY (TemplateId) REFERENCES dbo.QuestionnaireTemplates (Id) ON DELETE CASCADE,
        CONSTRAINT FK_QuestionnaireTemplateQuestions_Questions FOREIGN KEY (QuestionId) REFERENCES dbo.Questions (Id)
    );
    CREATE NONCLUSTERED INDEX IX_QuestionnaireTemplateQuestions_TemplateId ON dbo.QuestionnaireTemplateQuestions (TemplateId);
    CREATE NONCLUSTERED INDEX IX_QuestionnaireTemplateQuestions_QuestionId ON dbo.QuestionnaireTemplateQuestions (QuestionId);
    PRINT '   Created dbo.QuestionnaireTemplateQuestions';
END
GO

-- MCP image classification tables
IF OBJECT_ID(N'dbo.ImageClassifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImageClassifications (
        Id INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ImageClassifications PRIMARY KEY,
        OriginalFileName NVARCHAR(255) NOT NULL,
        SavedFileName NVARCHAR(255) NOT NULL,
        ImagePath NVARCHAR(500) NOT NULL,
        Description NVARCHAR(1000) NULL,
        UploadedAt DATETIME NOT NULL,
        ProcessedAt DATETIME NOT NULL,
        Success BIT NOT NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        UploadedByUserId INT NULL,
        TenantId INT NOT NULL,
        CONSTRAINT FK_ImageClassifications_Users FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_ImageClassifications_Companies FOREIGN KEY (TenantId) REFERENCES dbo.Companies (Id)
    );
    CREATE INDEX IX_ImageClassifications_UploadedByUserId ON dbo.ImageClassifications (UploadedByUserId);
    CREATE INDEX IX_ImageClassifications_TenantId ON dbo.ImageClassifications (TenantId);
    PRINT '   Created dbo.ImageClassifications';
END
GO

IF OBJECT_ID(N'dbo.ImageDetections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImageDetections (
        Id INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ImageDetections PRIMARY KEY,
        ObjectType NVARCHAR(50) NOT NULL,
        Confidence DECIMAL(18, 4) NOT NULL,
        BoundingBoxX INT NOT NULL,
        BoundingBoxY INT NOT NULL,
        BoundingBoxWidth INT NOT NULL,
        BoundingBoxHeight INT NOT NULL,
        DetectedAt DATETIME NOT NULL,
        ImageClassificationId INT NOT NULL,
        CONSTRAINT FK_ImageDetections_ImageClassifications FOREIGN KEY (ImageClassificationId) REFERENCES dbo.ImageClassifications (Id)
    );
    CREATE INDEX IX_ImageDetections_ImageClassificationId ON dbo.ImageDetections (ImageClassificationId);
    PRINT '   Created dbo.ImageDetections';
END
GO

PRINT 'HOST_SCHEMA_EXTENSIONS complete.';
GO
