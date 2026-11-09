/* Run this against the existing RaceDayDb database once before starting the integrated Part 3 API.
   Each column is added only if it does not already exist. Back up the database first. */
IF COL_LENGTH('dbo.Events', 'EventType') IS NULL
    ALTER TABLE dbo.Events ADD EventType nvarchar(max) NOT NULL CONSTRAINT DF_Events_EventType_Part3 DEFAULT ('Running');
IF COL_LENGTH('dbo.Events', 'DistanceKm') IS NULL
    ALTER TABLE dbo.Events ADD DistanceKm decimal(6,2) NOT NULL CONSTRAINT DF_Events_DistanceKm_Part3 DEFAULT (0);
IF COL_LENGTH('dbo.Events', 'BannerUrl') IS NULL
    ALTER TABLE dbo.Events ADD BannerUrl nvarchar(max) NULL;
IF COL_LENGTH('dbo.Users', 'ProfilePictureUrl') IS NULL
    ALTER TABLE dbo.Users ADD ProfilePictureUrl nvarchar(max) NULL;
IF COL_LENGTH('dbo.Enrolments', 'Status') IS NULL
    ALTER TABLE dbo.Enrolments ADD Status nvarchar(max) NOT NULL CONSTRAINT DF_Enrolments_Status_Part3 DEFAULT ('Pending');
