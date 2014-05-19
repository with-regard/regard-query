using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.MapReduce;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce
{
    [TestFixture]
    class AllEvents
    {
        [Test]
        public void WeShouldCount12BasicDocuments()
        {
            var task = Task.Run(async () =>
            {
                // == SETUP ==

                // Create the 'all events query'
                var queryBuilder = new SerializableQueryBuilder(null);
                var allEvents = queryBuilder.AllEvents();
                var query = allEvents.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through
                // As there are no documents in the data store currently, this will reduce but not re-reduce
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);

                // == ASSERT ==

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // Should contain a count of 12
                    Assert.AreEqual(12, nextRecord.Item2["Count"].Value<int>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            });

            task.Wait();
        }

        [Test]
        public void IfWeReReduceAgainThereShouldBe24Documents()
        {
            var task = Task.Run(async () =>
            {
                // Create the 'all events query'
                var queryBuilder = new SerializableQueryBuilder(null);
                var allEvents = queryBuilder.AllEvents();
                var query = allEvents.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through twice (forcing a re-reduce)
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // Should contain a count of 24
                    Assert.AreEqual(24, nextRecord.Item2["Count"].Value<int>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            });

            task.Wait();
        }
    }
}
