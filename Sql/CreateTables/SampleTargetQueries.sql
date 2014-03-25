-- builder.AllEvents();
SELECT	COUNT(DISTINCT ep.EventId) 
FROM	EventPropertyValues AS ep; -- TODO: we don't know a product ID so this isn't quite the query we'd run
GO

-- builder.AllEvents().Only("EventType", "Start");
SELECT	COUNT(DISTINCT ep.EventId) 
FROM	EventPropertyValues AS ep 
WHERE	ep.PropertyName = 'EventType' AND ep.Value = 'Start'
GO

-- builder.AllEvents().Only("EventType", "Start").BrokenDownBy("Day");
-- This is the simple way but the results don't contain zeros for days with no events
SELECT		ep2.Value as Day, COUNT(DISTINCT ep1.EventId) 
FROM		EventPropertyValues AS ep1
INNER JOIN	EventPropertyValues as ep2 ON (ep2.EventId = ep1.EventId AND ep1.PropertyName = 'EventType' AND ep1.Value = 'Start' AND ep2.PropertyName = 'Day')
GROUP BY	ep2.Value
GO

-- builder.AllEvents().Only("EventType", "DoSomething").BrokenDownBy("Day");
-- Illustrates the problem outlined above, day 3 is missing
SELECT		ep2.Value as Day, COUNT(DISTINCT ep1.EventId) 
FROM		EventPropertyValues AS ep1
INNER JOIN	EventPropertyValues as ep2 ON (ep2.EventId = ep1.EventId AND ep1.PropertyName = 'EventType' AND ep1.Value = 'DoSomething' AND ep2.PropertyName = 'Day')
GROUP BY	ep2.Value
GO

-- builder.AllEvents().Only("EventType", "DoSomething").CountUniqueValues("SessionId").BrokenDownBy("Day");
