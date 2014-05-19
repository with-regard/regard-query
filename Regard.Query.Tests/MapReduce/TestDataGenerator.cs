using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.MapReduce;

namespace Regard.Query.Tests.MapReduce
{
    static class TestDataGenerator
    {
        /// <summary>
        /// Generates the 12 documents we use for basic testing
        /// </summary>
        public static IEnumerable<JObject> Generate12BasicDocuments()
        {
            // 3 sessions, 6 clicks, 12 events total
            // Spread across 2 days
            yield return JObject.FromObject(new { SessionId = "1", Day = "1", EventType = "Start" });
            yield return JObject.FromObject(new { SessionId = "1", Day = "1", EventType = "NotClick" });
            yield return JObject.FromObject(new { SessionId = "1", Day = "1", EventType = "Stop" });

            yield return JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Start" });
            yield return JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Click" });
            yield return JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Click" });
            yield return JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Stop" });

            yield return JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Start" });
            yield return JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Click" });
            yield return JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Click" });
            yield return JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Click" });
            yield return JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Stop" });
        }

        /// <summary>
        /// Pass some basic documents into a Map/Reduce query processor. We re-use this set and its particular format in numerous tests
        /// </summary>
        public static async Task Ingest12BasicDocuments(DataIngestor ingestor)
        {
            // Ingest some documents
            foreach (var doc in Generate12BasicDocuments())
            {
                ingestor.Ingest(doc);
            }

            // Finish the commit (this will cause reduce/re-reduce in the map/reduce table storage)
            await ingestor.Commit();
        }
    }
}
