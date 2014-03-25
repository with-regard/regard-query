--
-- Creates some sample data, for development purposes
--

SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO

--
-- Variables
--
DECLARE @productId int;
DECLARE @eventId int;

--
-- Clear the database (danger!)
--
DELETE FROM Product;
DELETE FROM EventPropertyValues;

--
-- Product
--
INSERT INTO Product (Name, Organization) VALUES ('Test', 'WithRegard');
SET @productId = SCOPE_IDENTITY();
PRINT 'Product ID = ' + Convert(varchar(20), @productId);

--
-- Sample events
--

-- Day 1
INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Start');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '1');

INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'DoSomething');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '1');

INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Stop');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '1');

-- Day 2
INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Start');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '2');

INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'DoSomething');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '2');

INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'DoSomething');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '2');

INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Stop');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '2');

-- Day 3
INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Start');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '3');

INSERT INTO Event (ProductId) VALUES (@productId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Stop');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '3');

GO
