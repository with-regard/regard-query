using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.Sql;

namespace Regard.Query.Samples
{
    public static class Run
    {
        /// <summary>
        /// Auto-generates data for load testing
        /// </summary>
        public static async Task GenerateData(IEventRecorder recorder, Guid userId, int numSessions, int numEvents, int randomSeed)
        {
            Random rng = new Random(randomSeed);

            // Repetition makes certain events more likely
            string[] eventTypes = new[] {"Click", "Click", "Click", "Click", "Drag", "Drag", "Rock", "Roll"};

            DateTime lastTick = DateTime.Now;
            DateTime start = DateTime.Now;
            int eventsSinceLastTick = 0;

            for (int session = 0; session < numSessions; ++session)
            {
                var sessionId = await recorder.StartSession("WithRegard", "Test", userId, Guid.Empty);

                var day = rng.Next(256);

                await recorder.RecordEvent(sessionId,
                    JObject.FromObject(new
                    {
                        Day         = day,
                        SessionId   = session,
                        EventType   = "BeginSession"
                    }));

                ++eventsSinceLastTick;

                // TODO: different numbers of events for different sessions?
                for (int evt = 1; evt < numEvents; ++evt)
                {
                    await recorder.RecordEvent(sessionId, JObject.FromObject(new
                        {
                            Day         = day,
                            SessionId   = session,
                            EventType   = eventTypes[rng.Next(eventTypes.Length)]
                        }));

                    DateTime now = DateTime.Now;
                    if ((now - lastTick) > TimeSpan.FromSeconds(1))
                    {
                        Console.WriteLine("{0}/{1} sessions ({2} events per second)", session, numSessions, ((double)eventsSinceLastTick) / (now - lastTick).TotalSeconds);

                        lastTick = now;
                        eventsSinceLastTick = 0;
                    }

                    ++eventsSinceLastTick;
                }
            }

            Console.WriteLine("Total: {0} seconds", (DateTime.Now - start).TotalSeconds);
        }

        [STAThread]
        public static void Main()
        {
            // We need to use asynchronous stuff and there's no way to make an entry point async in C# (though there really should be), so run as a task
            var runIt = Task.Run(async () => 
            {
                Console.WriteLine(@"builder.AllEvents().Only(""EventType"", ""DoSomething"").CountUniqueValues(""SessionId"").BrokenDownBy(""Day"");");

                // Create the database connection
                var connection = new SqlConnection("");
                await connection.OpenAsync();

                var dataStore = new SqlDataStore(connection);

                // Generate some data
                var recorder = dataStore.EventRecorder;
                var sessionId = await recorder.StartSession("WithRegard", "Test", WellKnownUserIdentifier.TestUser, Guid.Empty);

                await recorder.RecordEvent(sessionId, JObject.FromObject(new
                    {
                        Day = 1,
                        Test = "Hello"
                    }));

                await recorder.RecordEvent(sessionId, JObject.FromObject(new
                {
                    Day = 203,
                    EventType = "DoSomething",
                    SessionId = 8
                }));

                // Try recording an event for a non-existent session
                await recorder.RecordEvent(Guid.NewGuid(), JObject.FromObject(new
                {
                    Day = 3478,
                    EventType = "BadEvent",
                    SessionId = 8
                }));

                // 10000 sessions of 100 events each.
                // 10000 sessions is likely from a medium-sized open-source project
                // 100 events per session is on the high side but not necessarily unreasonable
                await GenerateData(recorder, WellKnownUserIdentifier.TestUser, 10000, 100, 1);

                // Try querying the database
                var testWithRegard = await dataStore.Products.GetProduct("WithRegard", "Test");

                // Opt in a random user
                await testWithRegard.Users.OptIn(Guid.NewGuid());

                var builder = testWithRegard.CreateQueryBuilder();
                IRegardQuery result = builder.AllEvents();
                result = result.Only("EventType", "Click").CountUniqueValues("SessionId", "NumSessions").BrokenDownBy("Day", "Day");

                await testWithRegard.RegisterQuery("ClicksByDay", result);

                Console.WriteLine("Press enter to run the query...");
                Console.ReadLine();

                var queryResult = await testWithRegard.RunQuery("ClicksByDay");

                for (var nextLine = await queryResult.FetchNext(); nextLine != null; nextLine = await queryResult.FetchNext())
                {
                    Console.WriteLine("=== NEW LINE: {0} events", nextLine.EventCount);

                    foreach (var column in nextLine.Columns)
                    {
                        Console.WriteLine("  {0} = {1}", column.Name, column.Value);
                    }
                }

                Console.ReadLine();
            });

            runIt.Wait();
        }
    }
}
