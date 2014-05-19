using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.MapReduce;

namespace Regard.Query.Tests.MapReduce
{
    static class TestDataGenerator
    {
        /// <summary>
        /// Pass some basic documents into a Map/Reduce query processor. We re-use this set and its particular format in numerous tests
        /// </summary>
        public static async Task Ingest12BasicDocuments(DataIngestor ingestor)
        {
            // Ingest some documents
            // 3 sessions, 6 clicks, 12 events total
            // Spread across 2 days
            ingestor.Ingest(JObject.FromObject(new { SessionId = "1", Day = "1", EventType="Start" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "1", Day = "1", EventType = "NotClick" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "1", Day = "1", EventType = "Stop" }));

            ingestor.Ingest(JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Start" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Stop" }));

            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Start" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "2", EventType = "Stop" }));

            // Finish the commit
            await ingestor.Commit();
        }
    }
}
