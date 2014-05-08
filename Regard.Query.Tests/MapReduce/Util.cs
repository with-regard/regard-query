using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.MapReduce;

namespace Regard.Query.Tests.MapReduce
{
    static class Util
    {
        /// <summary>
        /// Pass some basic documents into a Map/Reduce query processor
        /// </summary>
        public static async Task TestBasicDocuments(DataIngestor ingestor)
        {
            // Ingest some documents
            // 3 sessions, 6 clicks, 12 events total
            ingestor.Ingest(JObject.FromObject(new { SessionId = "1", Day = "1", EventType="Start" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "1", Day = "1", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "1", Day = "1", EventType = "Stop" }));

            ingestor.Ingest(JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Start" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "2", Day = "1", EventType = "Stop" }));

            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "1", EventType = "Start" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "1", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "1", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "1", EventType = "Click" }));
            ingestor.Ingest(JObject.FromObject(new { SessionId = "3", Day = "1", EventType = "Stop" }));

            // Finish the commit
            await ingestor.Commit();
        }
    }
}
