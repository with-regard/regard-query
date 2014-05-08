using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.MapReduce;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce
{
    [TestFixture]
    class CountUnique
    {
        [Test]
        public void UniqueSessions()
        {
            var task = Task.Run(async () =>
            {
                // Create the 'only clicks' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var uniqueSessions = (SerializableQuery)queryBuilder.AllEvents().CountUniqueValues("SessionId", "NumSessions");
                var query = uniqueSessions.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through
                await Util.TestBasicDocuments(ingestor);

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 3 unique sessions
                    Assert.AreEqual(3, nextRecord.Item2["NumSessions"].Value<int>());

                    // There are 12 total events
                    Assert.AreEqual(12, nextRecord.Item2["Count"].Value<int>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            });

            task.Wait();
        }

        [Test]
        public void UniqueSessionsRereduce()
        {
            var task = Task.Run(async () =>
            {
                // Create the 'unique sessions' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var uniqueSessions = (SerializableQuery)queryBuilder.AllEvents().CountUniqueValues("SessionId", "NumSessions");
                var query = uniqueSessions.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through twice
                await Util.TestBasicDocuments(ingestor);
                await Util.TestBasicDocuments(ingestor);

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 3 unique sessions even when re-reducing
                    Assert.AreEqual(3, nextRecord.Item2["NumSessions"].Value<int>());

                    // There are 24 total events
                    Assert.AreEqual(24, nextRecord.Item2["Count"].Value<int>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            });

            task.Wait();
        }

        [Test]
        public void UniqueSessionsRereduceNew()
        {
            var task = Task.Run(async () =>
            {
                // Create the 'unique sessions' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var uniqueSessions = (SerializableQuery)queryBuilder.AllEvents().CountUniqueValues("SessionId", "NumSessions");
                var query = uniqueSessions.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through twice
                await Util.TestBasicDocuments(ingestor);
                await Util.TestBasicDocuments(ingestor);

                // Add a new session too
                ingestor.Ingest(JObject.FromObject(new { SessionId = "4" }));
                await ingestor.Commit();

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 3 unique sessions even when re-reducing
                    Assert.AreEqual(4, nextRecord.Item2["NumSessions"].Value<int>());

                    // There are 24 total events
                    Assert.AreEqual(25, nextRecord.Item2["Count"].Value<int>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            });

            task.Wait();
        }

        [Test]
        public void UniqueSessionsWithClick()
        {
            var task = Task.Run(async () =>
            {
                // Create the 'number of unique sessions with click' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var uniqueSessions = (SerializableQuery)queryBuilder.AllEvents().Only("EventType", "Click").CountUniqueValues("SessionId", "NumSessions");
                var query = uniqueSessions.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through
                await Util.TestBasicDocuments(ingestor);

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 3 unique sessions
                    Assert.AreEqual(3, nextRecord.Item2["NumSessions"].Value<int>());

                    // There are 12 total events
                    Assert.AreEqual(12, nextRecord.Item2["Count"].Value<int>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            });

            task.Wait();
        }
    }
}
