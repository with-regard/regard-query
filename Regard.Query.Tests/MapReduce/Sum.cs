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
                    // There are 12 total events
                    Assert.AreEqual(12, nextRecord.Item2["Count"].Value<double>());

                    // The sum of the NumberValue should be 21
                    // Note that only 6 of the records actually contain this field
                    Assert.AreEqual(21, nextRecord.Item2["SumOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }

        [Test]
        public void DoubleSumOfAllTheNumberValuesIs42()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.RunOnBasicDocumentsTwice((SerializableQuery)queryBuilder.AllEvents().Sum("NumberValue", "SumOfAllTheNumberValue"));

                // There should be one number, and it should be (1+2+...+5+6)*2 = 42
                var reader = results.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 12 total events
                    Assert.AreEqual(24, nextRecord.Item2["Count"].Value<double>());

                    // The sum of the NumberValue should be 21
                    // Note that only 6 of the records actually contain this field
                    Assert.AreEqual(42, nextRecord.Item2["SumOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }

        [Test]
        public void SumOfAllTheNumberValuesIsStill21AfterDeletingExtras()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.AddBasicDocumentsTwiceThenDeleteOnce((SerializableQuery)queryBuilder.AllEvents().Sum("NumberValue", "SumOfAllTheNumberValue"));

                // There should be one number, and it should be 1+2+...+5+6 = 21
                var reader = results.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 12 total events
                    Assert.AreEqual(12, nextRecord.Item2["Count"].Value<double>());

                    // The sum of the NumberValue should be 21
                    // Note that only 6 of the records actually contain this field
                    Assert.AreEqual(21, nextRecord.Item2["SumOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be only one record
                Assert.AreEqual(1, recordCount);
            }).Wait();
        }


        [Test]
        public void SumOfAllTheNumberValuesIs0AfterDeletingThemAll()
        {
            Task.Run(async () =>
            {
                var queryBuilder = new SerializableQueryBuilder(null);
                var results = await RunMapReduce.RunOnBasicDocumentsThenDeleteThem((SerializableQuery)queryBuilder.AllEvents().Sum("NumberValue", "SumOfAllTheNumberValue"));

                // There should be one number, and it should be 1+2+...+5+6 = 21
                var reader = results.EnumerateAllValues();
                int recordCount = 0;

                Tuple<JArray, JObject> nextRecord;
                while ((nextRecord = await reader.FetchNext()) != null)
                {
                    // There are 12 total events
                    Assert.AreEqual(0, nextRecord.Item2["Count"].Value<double>());

                    // The sum of the NumberValue should be 21
                    // Note that only 6 of the records actually contain this field
                    Assert.AreEqual(0, nextRecord.Item2["SumOfAllTheNumberValue"].Value<double>());
                    recordCount++;
                }

                // Should be no records
                Assert.AreEqual(0, recordCount);
            }).Wait();
        }
    }
}
