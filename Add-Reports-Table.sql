-- Add Reports table
CREATE TABLE [dbo].[Reports] (
    [Id] INT IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (100) NOT NULL,
    [Type] NVARCHAR (50) NOT NULL,
    [Description] NVARCHAR (500) NULL,
    [CreatedDate] DATETIME2 NOT NULL,
    [GeneratedDate] DATETIME2 NULL,
    [GeneratedBy] NVARCHAR (MAX) NULL,
    [FilePath] NVARCHAR (500) NULL,
    [IsActive] BIT NOT NULL,
    [Parameters] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.Reports] PRIMARY KEY CLUSTERED ([Id] ASC)
);
