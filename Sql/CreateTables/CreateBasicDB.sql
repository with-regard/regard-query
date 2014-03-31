--
-- Creates the basic tables in the Regard database (from scratch)
--

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

--
-- Delete the existing tables (danger!)
--
DROP TABLE [Product];
DROP TABLE [Event];
DROP TABLE [EventProperty];
DROP TABLE [EventPropertyValues];
DROP TABLE [OptInUser]
DROP TABLE [Session]

GO

--
-- Represents an individual product that can generate events
--
CREATE TABLE [Product]
	(
		[Id] bigint NOT NULL IDENTITY(1,1) PRIMARY KEY,
		[Name] NVARCHAR(256) NOT NULL,
		[Organization] NVARCHAR(256) NOT NULL
	)
	;

--
-- The Event table assigns IDs to events
--
CREATE TABLE [Event]
	(
		[Id] bigint NOT NULL IDENTITY(1,1) PRIMARY KEY,
		[ProductId] bigint NOT NULL,
		[ShortSessionId] bigint NOT NULL
				
		-- TODO: product version?
		-- TODO: session identifier?
	)
	;

--
-- The EventProperty table gives IDs to property names
--
CREATE TABLE [EventProperty]
	(
		[Id] int NOT NULL IDENTITY(1,1),
		[Name] NVARCHAR(256) NOT NULL PRIMARY KEY
	)
	;
	
--
-- The Event property values table maps 
--
CREATE TABLE [EventPropertyValues]
	(
		[EventId] bigint NOT NULL,
		-- [PropertyId] int NOT NULL,
		[PropertyName] NVARCHAR(256) NOT NULL,		-- For simplicity, but at the cost of performance + DB size, avoid using the eventproperty table in the first iteration
		[Value] NVARCHAR(256) NOT NULL,
		
		PRIMARY KEY ([EventId], [PropertyName])
	)
	;

--
-- The users table identifies opted-in users
--
-- Events MUST NOT be recorded for users who are not in this table.
--
CREATE TABLE [OptInUser]
	(
		[FullUserID] uniqueidentifier PRIMARY KEY,						-- How the user identifies to us
		[ShortUserID] bigint NOT NULL IDENTITY(1,1)						-- How the user is identified within the database
	)
	;

--
-- The session table indicate runs through the application
--
-- Sessions MUST NOT be recorded for users who are not in the opt in table
--
CREATE TABLE [Session]
	(
		[FullSessionID] uniqueidentifier PRIMARY KEY,					-- How the user's app identifies the session to us
		[ShortSessionID] bigint NOT NULL IDENTITY(1,1),					-- How the session is identified within the database
		[ShortUserID] bigint NOT NULL									-- Identifies the user that the session is for
	)
	;

GO
