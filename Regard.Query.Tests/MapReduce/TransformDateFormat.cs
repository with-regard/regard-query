using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.MapReduce;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce
{
    class TransformDateFormat
    {
        [Test]
        public void ThereShouldBe5ClickEvents()
        {
            var task = Task.Run(async () =>
            {
                // Query that turns the 'Date' field into
                var queryBuilder = new SerializableQueryBuilder(null);
                var onlyClicks = (SerializableQuery)queryBuilder.AllEvents().TransformDateFormat("Date", "DateDays", "Days").BrokenDownBy("DateDays", "DateDaysOut");
                var query = onlyClicks.GenerateMapReduce();

                // Generate a data store and an ingestor
                var resultStore = new MemoryKeyValueStore();
                var ingestor = new DataIngestor(query, resultStore);

                // Run the standard set of docs through
                ingestor.Ingest(JObject.FromObject(new
                {
                    // ISO-8601 format date
                    Date = "2014-07-01T05:00:14+00:00"
                }));
                await TestDataGenerator.Ingest12BasicDocuments(ingestor);

                // This should create a data store with one record indicating that there are 12 records 
                var reader = resultStore.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // The 'DateDaysOut' should contain the number of days since Jan 1, 1970 in UTC
                    var numDays = nextRecord.Item2["DateDaysOut"].Value<string>();
                    
                    // There are 16252 days between 1-1-1970 and our test date
                    Assert.AreEqual("16252", numDays);

                    // Should be only one event
                    Assert.AreEqual(1, nextRecord.Item2["Count"].Value<int>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            });

            task.Wait();
        }
    }
}
