-- Drop legacy document CV paths (input-only collection via profile + questionnaire).
IF COL_LENGTH(N'dbo.Applications', N'ResumePath') IS NOT NULL
    ALTER TABLE dbo.Applications DROP COLUMN ResumePath;
IF COL_LENGTH(N'dbo.Applicants', N'ResumePath') IS NOT NULL
    ALTER TABLE dbo.Applicants DROP COLUMN ResumePath;
GO
