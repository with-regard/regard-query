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
DROP TABLE [Event];
DROP TABLE [EventProperty];
DROP TABLE [EventPropertyValues];
DROP TABLE [Session];
DROP TABLE [OptInUser];
DROP TABLE [OptInState];
DROP TABLE [Product];

GO

--
-- Represents an individual product that can generate events
--
CREATE TABLE [Product]
	(
		[Id] bigint NOT NULL IDENTITY(1,1) PRIMARY KEY,
		[Name] NVARCHAR(200) NOT NULL,
		[Organization] NVARCHAR(200) NOT NULL
	)
	;

CREATE UNIQUE NONCLUSTERED INDEX [IDX_ProductName] ON [Product] ([Organization], [Name]) ;

--
-- The Event table assigns IDs to events
--
CREATE TABLE [Event]
	(
		[Id] bigint NOT NULL IDENTITY(1,1),
		[ShortSessionId] bigint NOT NULL,
		
		PRIMARY KEY ([Id])
		-- TODO: product version?
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
		[NumericValue] bigint
		
		PRIMARY KEY ([EventId], [PropertyName])
	)
	;

--
-- Possible opt-in states
--
-- Users may be opted in but have chosen not to share their data with the developer
--
-- There is no 'opted-out' state. Users who are opted out are not present in the database.
--
-- That is, users can see their data, but the developer must not be able to see it or have it influence the
-- results of their queries.
--
CREATE TABLE [OptInState]
	(
		[StateId] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
		[Name] varchar(20) NOT NULL
	)
	;

INSERT INTO OptInState (Name) VALUES ('UserOnly');
INSERT INTO OptInState (Name) VALUES ('ShareWithDeveloper');

--
-- The users table identifies opted-in users
--
-- Events MUST NOT be recorded for users who are not in this table.
--
CREATE TABLE [OptInUser]
	(
		[FullUserId] uniqueidentifier PRIMARY KEY NONCLUSTERED,			-- How the user identifies to us
		[ShortUserId] bigint NOT NULL IDENTITY(1,1) UNIQUE,				-- How the user is identified within the database
		[OptInStateId] int NOT NULL,

		CONSTRAINT [FK_OptInState] FOREIGN KEY ([OptInStateId]) REFERENCES [OptInState] ([StateId])
	)
	;

-- For developer queries we want to quickly look up the users who are opted in
CREATE CLUSTERED INDEX [IDX_OptInState] ON [OptInUser] ([OptInStateId], [FullUserId]) ;

--
-- The session table indicate runs through the application
--
-- Sessions MUST NOT be recorded for users who are not in the opt in table
--
CREATE TABLE [Session]
	(
		[FullSessionId] uniqueidentifier,								-- How the user's app identifies the session to use
		[ShortSessionId] bigint NOT NULL IDENTITY(1,1) UNIQUE,			-- How the session is identified within the database
		[ShortUserId] bigint NOT NULL,									-- Identifies the user that the session is for
		[ProductId] bigint NOT NULL,									-- Identifies the product that this session is for

		PRIMARY KEY NONCLUSTERED ([FullSessionId]),
		CONSTRAINT [FK_SessionProduct] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([Id]),
		CONSTRAINT [FK_SessionUser] FOREIGN KEY ([ShortUserId]) REFERENCES [OptInUser] ([ShortUserId]),
	)
	;

-- Typically we want to run queries like 'get all the sessions for an individual user'
CREATE CLUSTERED INDEX [IDX_Session] ON [Session] ([ProductId], [ShortUserId], [FullSessionId]);

GO
