using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce
{
    [TestFixture]
    class Mean
    {
        [Test]
        public void MeanOfAllTheNumberValuesIs3Point5()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.RunOnBasicDocuments((SerializableQuery)queryBuilder.AllEvents().Mean("NumberValue", "MeanOfAllTheNumberValue"));

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
                    Assert.AreEqual(3.5, nextRecord.Item2["MeanOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }

        [Test]
        public void MeanOfAllTheNumberValuesIsStill3Point5AfterRunningThroughTwice()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.RunOnBasicDocumentsTwice((SerializableQuery)queryBuilder.AllEvents().Mean("NumberValue", "MeanOfAllTheNumberValue"));

                // There should be one number, and it should be 1+2+...+5+6 = 21
                var reader = results.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 12 total events
                    Assert.AreEqual(24, nextRecord.Item2["Count"].Value<double>());

                    // The mean of the NumberValue field should be 3.5
                    // Note that only 6 of the records actually contain this field
                    Assert.AreEqual(3.5, nextRecord.Item2["MeanOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }

        [Test]
        public void MeanOfAllTheNumberValuesIsStill3Point5AfterDeletingExtras()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.AddBasicDocumentsTwiceThenDeleteOnce((SerializableQuery)queryBuilder.AllEvents().Mean("NumberValue", "MeanOfAllTheNumberValue"));

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
                    Assert.AreEqual(3.5, nextRecord.Item2["MeanOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }


        [Test]
        public void MeanOfAllTheNumberValuesIsNaNAfterDeletingThemAll()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.RunOnBasicDocumentsThenDeleteThem((SerializableQuery)queryBuilder.AllEvents().Mean("NumberValue", "MeanOfAllTheNumberValue"));

                // There should be one number, and it should be 1+2+...+5+6 = 21
                var reader = results.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 12 total events
                    Assert.AreEqual(0, nextRecord.Item2["Count"].Value<double>());

                    // The mean of the NumberValue field should be 3.5
                    // Note that only 6 of the records actually contain this field
                    Assert.AreEqual(Double.NaN, nextRecord.Item2["MeanOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }
    }
}
