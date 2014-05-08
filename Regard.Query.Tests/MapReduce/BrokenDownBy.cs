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
    class BrokenDownBy
    {
        [Test]
        public void Session()
        {
            var task = Task.Run(async () =>
            {
                // Create the 'only clicks' query
                var queryBuilder = new SerializableQueryBuilder(null);
                var brokenDownByDay = (SerializableQuery) queryBuilder.AllEvents().BrokenDownBy("SessionId", "WhichSession");
                var query = brokenDownByDay.GenerateMapReduce();

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
                    switch (nextRecord.Item2["WhichSession"].Value<string>())
                    {
                        case "1":
                            // There are 3 events in the first session
                            Assert.AreEqual(3, nextRecord.Item2["Count"].Value<int>());
                            break;

                        case "2":
                            Assert.AreEqual(4, nextRecord.Item2["Count"].Value<int>());
                            break;

                        case "3":
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
    }
}
