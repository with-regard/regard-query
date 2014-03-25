using Regard.Query.Api;

namespace Regard.Query.Samples
{
    /// <summary>
    /// Samples of how queries are built
    /// </summary>
    class BuildQuery
    {
        public void CountAllEvents(IQueryBuilder builder)
        {
            // Creates a query that counts all events
            var allEvents = builder.AllEvents();                    // Results are aggregated by default; we give a default aggregation of counting the number of events.
        }

        public void TotalUniqueSessions(IQueryBuilder builder)
        {
            // Ie, the number of times the app has been started
            var totalUniqueSessions = builder.AllEvents().Only("EventType", "NewSession");          // The default count aggregation is enough
        }

        public void SessionsPerDay(IQueryBuilder builder)
        {
            // Number of sessions per day
            // Assumes a 'day' field, due to the simplified API I'm using. I can think of several ways to enhance this so there just needs to be
            // a timestamp but don't want to implement any of them until we need to
            // Results ought to be suitable to shove straight into a graph
            var sessionsPerDay = builder.AllEvents().Only("EventType", "NewSession").BrokenDownBy("Day");
        }

        public void SessionsThatDidSomethingPerDay(IQueryBuilder builder)
        {
            // The number of sessions that perform an action at least once, broken down by day
            var actionsPerSessionPerDay = builder.AllEvents().Only("EventType", "DidSomething").CountUniqueValues("SessionId").BrokenDownBy("Day");
        }
    }
}
