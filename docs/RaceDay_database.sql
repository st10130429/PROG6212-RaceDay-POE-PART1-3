-- RaceDay Database - SQL Server
-- Part 1: System Planning and Database

CREATE DATABASE RaceDayDB;
GO
USE RaceDayDB;
GO

CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Organiser','Participant')),
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Events (
    EventID INT PRIMARY KEY IDENTITY(1,1),
    OrganiserID INT NOT NULL FOREIGN KEY REFERENCES Users(UserID),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000),
    Location NVARCHAR(200) NOT NULL,
    EventDate DATETIME NOT NULL,
    ImageUrl NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    EventID INT NOT NULL FOREIGN KEY REFERENCES Events(EventID) ON DELETE CASCADE,
    Name NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    MaxParticipants INT DEFAULT 1000
);

CREATE TABLE Enrolments (
    EnrolmentID INT PRIMARY KEY IDENTITY(1,1),
    ParticipantID INT NOT NULL FOREIGN KEY REFERENCES Users(UserID),
    CategoryID INT NOT NULL FOREIGN KEY REFERENCES Categories(CategoryID),
    EnrolmentDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) DEFAULT 'Confirmed' CHECK (Status IN ('Confirmed','Cancelled','Completed')),
    UNIQUE(ParticipantID, CategoryID)
);

CREATE TABLE Results (
    ResultID INT PRIMARY KEY IDENTITY(1,1),
    EnrolmentID INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Enrolments(EnrolmentID),
    FinishTime TIME NOT NULL,
    Position INT,
    RecordedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE RouteInformation (
    RouteID INT PRIMARY KEY IDENTITY(1,1),
    CategoryID INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Categories(CategoryID),
    DistanceKM DECIMAL(5,2) NOT NULL,
    ElevationGain DECIMAL(6,2),
    MapUrl NVARCHAR(500)
);

-- SEED DATA

INSERT INTO Users (Name, Email, PasswordHash, Role) VALUES
('Thabo Organiser', 'thabo@raceday.co.za', 'hashed_pwd_1', 'Organiser'),
('Nandi Organiser', 'nandi@raceday.co.za', 'hashed_pwd_2', 'Organiser'),
('Sipho Participant', 'sipho@gmail.com', 'hashed_pwd_3', 'Participant'),
('Lerato Participant', 'lerato@gmail.com', 'hashed_pwd_4', 'Participant');

INSERT INTO Events (OrganiserID, Name, Description, Location, EventDate) VALUES
(1, 'Soweto Marathon 2026', 'Annual Soweto marathon through historic Soweto', 'Soweto, Johannesburg', '2026-11-01'),
(1, 'Cape Town Cycle Tour', 'Biggest cycle tour in Africa', 'Cape Town', '2026-03-08'),
(2, 'Comrades Training Run', 'Community training run', 'Pretoria', '2026-05-10');

INSERT INTO Categories (EventID, Name, DistanceKm, Price) VALUES
(1, 'Full Marathon', 42.2, 350.00),
(1, 'Half Marathon', 21.1, 250.00),
(1, '10km Fun Run', 10.0, 150.00),
(2, '109km Cycle', 109.0, 500.00),
(3, '21km Training', 21.1, 100.00);

INSERT INTO Enrolments (ParticipantID, CategoryID) VALUES
(3, 1), (3, 4), (4, 2), (4, 3);

INSERT INTO Results (EnrolmentID, FinishTime, Position) VALUES
(3, '02:15:30', 45), (4, '00:58:10', 12);

INSERT INTO RouteInformation (CategoryID, DistanceKM, ElevationGain, MapUrl) VALUES
(1, 42.2, 320.5, 'https://maps.example.com/soweto-full'),
(2, 21.1, 180.0, 'https://maps.example.com/soweto-half'),
(3, 10.0, 60.0, 'https://maps.example.com/soweto-10k'),
(4, 109.0, 950.0, 'https://maps.example.com/ct-cycle-109'),
(5, 21.1, 140.0, 'https://maps.example.com/comrades-training');

-- VERIFICATION QUERIES
SELECT * FROM Users;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;
SELECT * FROM RouteInformation;
