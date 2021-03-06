﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.MapReduce;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce
{
    [TestFixture]
    class IndexedBy
    {
        [Test]
        public void ThereAre3EventsInTheFirstSession()
        {
            // As for BrokenDownBy, but indexing by event type as well
            // ... but also 4 in the second, and 5 in the third. THis makes the method name rather long, though.
            // Not actually sure if these should be separate tests. I think not really as they are different failure conditions rather than different behaviours
            var task = Task.Run(async () =>
            {
                // Create the 'only clicks' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var brokenDownByDay = (SerializableQuery)queryBuilder.AllEvents().BrokenDownBy("SessionId", "WhichSession").IndexedBy("EventType");
                var query = brokenDownByDay.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    switch (nextRecord.Item2["WhichSession"].Value<string>())
                    {
                        case "1":
                            // There are 3 events in the first session
                            Assert.AreEqual(3, nextRecord.Item2["Count"].Value<int>());
                            break;

                        case "2":
                            // 4 events in the second
                            Assert.AreEqual(4, nextRecord.Item2["Count"].Value<int>());
                            break;

                        case "3":
                            // 5 events in the third
                            Assert.AreEqual(5, nextRecord.Item2["Count"].Value<int>());
                            break;

                        default:
                            // There are sessions one, two and three
                            Assert.Fail();
                            break;
                    }

                    recordCount++;
                }

                // Should be three records, one for each session
                Assert.AreEqual(3, recordCount);
            });

            task.Wait();
        }

        [Test]
        public void ThereAre2ClicksInTheSecondSession()
        {
            // As for BrokenDownBy, but indexing by event type as well
            // ... but also 4 in the second, and 5 in the third. THis makes the method name rather long, though.
            // Not actually sure if these should be separate tests. I think not really as they are different failure conditions rather than different behaviours
            var task = Task.Run(async () =>
            {
                // Create the 'only clicks' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var brokenDownByDay = (SerializableQuery)queryBuilder.AllEvents().BrokenDownBy("SessionId", "WhichSession").IndexedBy("EventType");
                var query = brokenDownByDay.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.ChildStore(new JArray("chain")).EnumerateValuesBeginningWithKey(new JArray("Click"));
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    switch (nextRecord.Item2["WhichSession"].Value<string>())
                    {
                        case "1":
                            // There are 0 click events in the first session
                            Assert.AreEqual(0, nextRecord.Item2["Count"].Value<int>());
                            break;

                        case "2":
                            // 2 events in the second
                            Assert.AreEqual(2, nextRecord.Item2["Count"].Value<int>());
                            break;

                        case "3":
                            // 3 events in the third
                            Assert.AreEqual(3, nextRecord.Item2["Count"].Value<int>());
                            break;

                        default:
                            // There are sessions one, two and three
                            Assert.Fail();
                            break;
                    }

                    recordCount++;
                }

                // Should be two records, one for each session (but the first session has no clicks, so it shouldn't have any records)
                Assert.AreEqual(2, recordCount);
            });

            task.Wait();
        }

        [Test]
        public void ThereAre2ClicksInTheSecondSessionWithDifferentOrdering()
        {
            // As for BrokenDownBy, but indexing by event type as well
            // ... but also 4 in the second, and 5 in the third. This makes the method name rather long, though.
            // Not actually sure if these should be separate tests. I think not really as they are different failure conditions rather than different behaviours
            var task = Task.Run(async () =>
            {
                // Create the 'only clicks' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var brokenDownByDay = (SerializableQuery)queryBuilder.AllEvents().IndexedBy("EventType").BrokenDownBy("SessionId", "WhichSession");
                var query = brokenDownByDay.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.ChildStore(new JArray("chain")).EnumerateValuesBeginningWithKey(new JArray("Click"));
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    switch (nextRecord.Item2["WhichSession"].Value<string>())
                    {
                        case "1":
                            // There are 0 click events in the first session
                            Assert.AreEqual(0, nextRecord.Item2["Count"].Value<int>());
                            break;

                        case "2":
                            // 2 events in the second
                            Assert.AreEqual(2, nextRecord.Item2["Count"].Value<int>());
                            break;

                        case "3":
                            // 3 events in the third
                            Assert.AreEqual(3, nextRecord.Item2["Count"].Value<int>());
                            break;

                        default:
                            // There are sessions one, two and three
                            Assert.Fail();
                            break;
                    }

                    recordCount++;
                }

                // Should be two records, one for each session (but the first session has no clicks, so it shouldn't have any records)
                Assert.AreEqual(2, recordCount);
            });

            task.Wait();
        }

        [Test]
        public void UnreductionResultsInNoDocuments()
        {
            // As for BrokenDownBy, but indexing by event type as well
            // ... but also 4 in the second, and 5 in the third. THis makes the method name rather long, though.
            // Not actually sure if these should be separate tests. I think not really as they are different failure conditions rather than different behaviours
            var task = Task.Run(async () =>
            {
                // Create the 'only clicks' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var brokenDownByDay = (SerializableQuery)queryBuilder.AllEvents().BrokenDownBy("SessionId", "WhichSession").IndexedBy("EventType");
                var query = brokenDownByDay.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through, then delete them
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);
                await TestDataGenerator.Uningest12BasicDocuments(ingestor);

                // This should leave an empty data store
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    Assert.AreEqual(0, nextRecord.Item2["Count"].Value<int>());

                    recordCount++;
                }

                // Should be three records, one for each session
                Assert.AreEqual(0, recordCount);
            });

            task.Wait();
        }

        [Test]
        public void UnreductionResultsInNoDocumentsWhenOrderIsReversed()
        {
            // As for BrokenDownBy, but indexing by event type as well
            // ... but also 4 in the second, and 5 in the third. THis makes the method name rather long, though.
            // Not actually sure if these should be separate tests. I think not really as they are different failure conditions rather than different behaviours
            var task = Task.Run(async () =>
            {
                // Create the 'only clicks' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var brokenDownByDay = (SerializableQuery)queryBuilder.AllEvents().IndexedBy("EventType").BrokenDownBy("SessionId", "WhichSession");
                var query = brokenDownByDay.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through, then delete them
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);
                await TestDataGenerator.Uningest12BasicDocuments(ingestor);

                // This should leave an empty data store
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    Assert.AreEqual(0, nextRecord.Item2["Count"].Value<int>());

                    recordCount++;
                }

                // Should be three records, one for each session
                Assert.AreEqual(0, recordCount);
            });

            task.Wait();
        }
    }
}
