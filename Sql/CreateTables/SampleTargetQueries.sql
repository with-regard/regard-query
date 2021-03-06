-- builder.AllEvents();
SELECT	COUNT(DISTINCT ep.EventId) 
FROM	EventPropertyValues AS ep; -- TODO: we don't know a product ID so this isn't quite the query we'd run
GO

-- builder.AllEvents().Only("EventType", "Start");
SELECT	COUNT(DISTINCT ep.EventId) as NumEvents
FROM	EventPropertyValues AS ep 
WHERE	ep.PropertyName = 'EventType' AND ep.Value = 'Start'
GO

-- builder.AllEvents().Only("EventType", "Start").BrokenDownBy("Day");
-- This is the simple way but the results don't contain zeros for days with no events
SELECT		ep2.Value as Day, COUNT(DISTINCT ep1.EventId) as NumEvents
FROM		EventPropertyValues AS ep1
INNER JOIN	EventPropertyValues as ep2 ON (ep2.EventId = ep1.EventId)
WHERE		ep1.PropertyName = 'EventType' AND ep1.Value = 'Start'
AND			ep2.PropertyName = 'Day'
GROUP BY	ep2.Value
GO

-- builder.AllEvents().Only("EventType", "DoSomething").BrokenDownBy("Day");
-- Illustrates the problem outlined above, day 3 is missing as no events occur then
-- Possibly OK? Pretty easy to infer the value of missing items.
SELECT		ep2.Value as Day, COUNT(DISTINCT ep1.EventId) as NumEvents
FROM		EventPropertyValues AS ep1
INNER JOIN	EventPropertyValues as ep2 ON (ep2.EventId = ep1.EventId)
WHERE		ep1.PropertyName = 'EventType' AND ep1.Value = 'DoSomething'
AND			ep2.PropertyName = 'Day'
GROUP BY	ep2.Value
GO

-- builder.AllEvents().Only("EventType", "DoSomething").CountUniqueValues("SessionId")
SELECT		COUNT(DISTINCT ep2.Value) as NumSessions
FROM		EventPropertyValues AS ep1
INNER JOIN	EventPropertyValues AS ep2 ON (ep2.EventId = ep1.EventId)
WHERE		ep1.PropertyName = 'EventType' AND ep1.Value = 'DoSomething'
AND			ep2.PropertyName = 'SessionId'
GO

-- builder.AllEvents().CountUniqueValues("SessionId")
SELECT		COUNT(DISTINCT ep1.Value) as NumSessions
FROM		EventPropertyValues AS ep1
WHERE		ep1.PropertyName = 'SessionId'
GO

-- builder.AllEvents().Only("EventType", "DoSomething").CountUniqueValues("SessionId").BrokenDownBy("Day");
SELECT		ep3.Value as Day, COUNT(DISTINCT ep2.Value) as NumSessions
FROM		EventPropertyValues AS ep1
INNER JOIN	EventPropertyValues AS ep2 ON (ep2.EventId = ep1.EventId)
INNER JOIN	EventPropertyValues AS ep3 ON (ep3.EventId = ep2.EventId)
WHERE		ep1.PropertyName = 'EventType' AND ep1.Value = 'DoSomething'
AND			ep2.PropertyName = 'SessionId'
AND			ep3.PropertyName = 'Day'
GROUP BY	ep3.Value;
GO

-- builder.AllEvents().Only("EventType", "DoSomething").Only("SessionId", "2")
-- Dual 'Only' illustration. Session ID 2 is the only one with 2 events in our demo
SELECT		COUNT(DISTINCT ep1.EventId)
FROM		EventPropertyValues AS ep1
INNER JOIN	EventPropertyValues AS ep2 ON (ep2.EventId = ep1.EventId)
WHERE		ep1.PropertyName = 'EventType' AND ep1.Value = 'DoSomething'
AND			ep2.PropertyName = 'SessionId' AND ep2.Value = '2';
GO

-- builder.AllEvents().Only("EventType", "DoSomething").Only("SessionId", "1")
SELECT		COUNT(DISTINCT ep1.EventId)
FROM		EventPropertyValues AS ep1
INNER JOIN	EventPropertyValues AS ep2 ON (ep2.EventId = ep1.EventId)
WHERE		ep1.PropertyName = 'EventType' AND ep1.Value = 'DoSomething'
AND			ep2.PropertyName = 'SessionId' AND ep2.Value = '1';
GO
