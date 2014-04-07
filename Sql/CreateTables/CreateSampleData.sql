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
DECLARE @productId bigint;
DECLARE @eventId bigint;
DECLARE @userId bigint;
DECLARE @sessionId bigint;
DECLARE @optInStateId int;

--
-- Clear the database (danger!)
--
DELETE FROM [EventPropertyValues];
DELETE FROM [Event];
DELETE FROM [Session];
DELETE FROM [OptInUser];
DELETE FROM [Product];

--
-- Product
--
INSERT INTO Product (Name, Organization) VALUES ('Test', 'WithRegard');
SET @productId = SCOPE_IDENTITY();
PRINT 'Product ID = ' + Convert(varchar(20), @productId);

SET @optInStateId = (SELECT StateID FROM [OptInState] WHERE Name = 'ShareWithDeveloper');

--
-- The test user
--
INSERT INTO [OptInUser] ([FullUserId], [ProductId], [OptInStateID]) VALUES ('F16CB994-00FF-4326-B0DB-F316F7EC2942', @productId, @optInStateId);

--
-- Sample opted-in user
--
INSERT INTO [OptInUser] ([FullUserId], [ProductId], [OptInStateID]) VALUES (NEWID(), @productId, @optInStateId);
SET @userId = SCOPE_IDENTITY();
PRINT 'User ID = ' + Convert(varchar(20), @userId);

--
-- Sample events
--

-- Day 1
INSERT INTO [Session] ([FullSessionId], [ShortUserId], [ProductId]) VALUES (NEWID(), @userId, @productId);
SET @sessionId = SCOPE_IDENTITY();

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Start');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '1');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'DoSomething');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '1');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'SessionId', '1');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Stop');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '1');

-- Day 2
INSERT INTO [Session] ([FullSessionId], [ShortUserId], [ProductId]) VALUES (NEWID(), @userId, @productId);
SET @sessionId = SCOPE_IDENTITY();

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Start');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '2');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'DoSomething');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '2');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'SessionId', '2');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'DoSomething');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '2');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'SessionId', '2');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Stop');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '2');

-- Day 3
INSERT INTO [Session] ([FullSessionId], [ShortUserId], [ProductId]) VALUES (NEWID(), @userId, @productId);
SET @sessionId = SCOPE_IDENTITY();

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Start');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '3');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Stop');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '3');

-- Day 4
INSERT INTO [Session] ([FullSessionId], [ShortUserId], [ProductId]) VALUES (NEWID(), @userId, @productId);
SET @sessionId = SCOPE_IDENTITY();

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Start');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '4');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'DoSomething');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '4');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'SessionId', '3');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Stop');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '4');

INSERT INTO [Session] ([FullSessionId], [ShortUserId], [ProductId]) VALUES (NEWID(), @userId, @productId);
SET @sessionId = SCOPE_IDENTITY();

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Start');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '4');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'DoSomething');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '4');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'SessionId', '4');

INSERT INTO Event (ShortSessionId) VALUES (@sessionId);
SET @eventId = SCOPE_IDENTITY();
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'EventType', 'Stop');
INSERT INTO EventPropertyValues (EventId, PropertyName, Value) VALUES (@eventId, 'Day', '4');

GO
