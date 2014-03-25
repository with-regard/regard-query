--
-- Creates the basic tables in the Regard database (from scratch)
--

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

--
-- Represents an individual product that can generate events
--
CREATE TABLE Product
	(
		[Id] int NOT NULL,
		[Name] NVARCHAR(256) NOT NULL,
		[Organisation] NVARCHAR(256) NOT NULL
	)

--
-- The Event table assigns IDs to events
--
CREATE TABLE Event
	(
		[Id] bigint NOT NULL IDENTITY(1,1) PRIMARY KEY,
		[ProductId] int NOT NULL
		
		-- TODO: product version?
		-- TODO: session identifier?
	)

--
-- The EventProperty table gives IDs to property names
--
CREATE TABLE EventProperty
	(
		[Id] int NOT NULL IDENTITY(1,1),
		[Name] NVARCHAR(256) NOT NULL PRIMARY KEY
	)
	
--
-- The Event property values table maps 
--
CREATE TABLE EventPropertyValues
	(
		[EventId] int NOT NULL,
		-- [PropertyId] int NOT NULL,
		[PropertyName] NVARCHAR(256) NOT NULL,		-- For simplicity, but at the cost of performance + DB size, avoid using the eventproperty table in the first iteration
		[Value] NVARCHAR(256) NOT NULL,
		
		PRIMARY KEY ([EventId], [PropertyName])
	)

GO