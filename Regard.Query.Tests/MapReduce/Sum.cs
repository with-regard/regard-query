using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce
{
    [TestFixture]
    class Sum
    {
        [Test]
        public void SumOfAllTheNumberValuesIs21()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.RunOnBasicDocuments((SerializableQuery) queryBuilder.AllEvents().Sum("NumberValue", "SumOfAllTheNumberValue"));

                // There should be one number, and it should be 1+2+...+5+6 = 21
                var reader = results.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 5 click events
                    Assert.AreEqual(21, nextRecord.Item2["SumOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }
    }
}
