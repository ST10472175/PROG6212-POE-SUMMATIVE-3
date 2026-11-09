-- RaceDay Part 3 schema update for an existing Part 2 SQL Server database.
-- Run this against the database referenced by ConnectionStrings:DefaultConnection.
-- It is safe to run more than once.

IF COL_LENGTH(N'dbo.Events', N'BannerImageUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Events ADD BannerImageUrl nvarchar(2048) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Participant', N'ProfilePictureUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Participant ADD ProfilePictureUrl nvarchar(2048) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Assignment', N'Status') IS NULL
BEGIN
    ALTER TABLE dbo.Assignment
        ADD Status nvarchar(20) NOT NULL
            CONSTRAINT DF_Assignment_Status DEFAULT N'Pending';
END;
GO
