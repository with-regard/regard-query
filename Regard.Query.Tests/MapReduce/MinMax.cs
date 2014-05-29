using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce
{
    [TestFixture]
    public class MinMax
    {
        [Test]
        public void MaxOfAllTheNumberValuesIs6()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.RunOnBasicDocuments((SerializableQuery)queryBuilder.AllEvents().Max("NumberValue", "MaxOfAllTheNumberValue"));

                // There should be one number, and it should be 1+2+...+5+6 = 21
                var reader = results.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 12 total events
                    Assert.AreEqual(12, nextRecord.Item2["Count"].Value<double>());

                    // The mean of the NumberValue field should be 3.5
                    // Note that only 6 of the records actually contain this field
                    Assert.AreEqual(6, nextRecord.Item2["MaxOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }

        [Test]
        public void MinOfAllTheNumberValuesIs1()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.RunOnBasicDocuments((SerializableQuery)queryBuilder.AllEvents().Min("NumberValue", "MinOfAllTheNumberValue"));

                // There should be one number, and it should be 1+2+...+5+6 = 21
                var reader = results.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 12 total events
                    Assert.AreEqual(12, nextRecord.Item2["Count"].Value<double>());

                    // The mean of the NumberValue field should be 3.5
                    // Note that only 6 of the records actually contain this field
                    Assert.AreEqual(1, nextRecord.Item2["MinOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }
    }
}
