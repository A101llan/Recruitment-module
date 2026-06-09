-- Idempotent: questionnaire template tables (merged from DatabaseSchemaEnsure / EF migration)
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
END
GO
