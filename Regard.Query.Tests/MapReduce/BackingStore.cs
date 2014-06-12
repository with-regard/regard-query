using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.MapReduce;
using Regard.Query.MapReduce.Azure;
using Regard.Query.Tests.Api;

namespace Regard.Query.Tests.MapReduce
{
    // TODO: deleting a child store doesn't delete similar stores

    [TestFixture("InMemory")]
    [TestFixture("LocalAzureTableStore")]
    class BackingStore
    {
        private string m_DataStoreType;
        private MemoryKeyValueStore m_LastMemoryStore;
        private string m_LastStoreKey;

        public BackingStore(string storeType)
        {
            m_DataStoreType = storeType;
        }

        /// <summary>
        /// Creates a backing store store of the given type
        /// </summary>
        private IKeyValueStore CreateStoreToTest()
        {
            switch (m_DataStoreType)
            {
                case "InMemory": 
                    m_LastMemoryStore = new MemoryKeyValueStore();
                    return m_LastMemoryStore;

                case "LocalAzureTableStore":
                    m_LastStoreKey = new Random().Next(int.MaxValue).ToString();
                    return new AzureKeyValueStore(TestDataStoreFactory.GetTestConnectionString(), "TestTable" + m_LastStoreKey);

                default:
                    Assert.Fail();
                    return null;
            }
        }

        /// <summary>
        /// Creates a new connection to the same store that was last returned from CreateStoreToTest
        /// </summary>
        /// <returns></returns>
        private IKeyValueStore RecreateStoreToTest()
        {
            switch (m_DataStoreType)
            {
                case "InMemory":
                    return m_LastMemoryStore;

                case "LocalAzureTableStore":
                    return new AzureKeyValueStore(TestDataStoreFactory.GetTestConnectionString(), "TestTable" + m_LastStoreKey);

                default:
                    Assert.Fail();
                    return null;
            }
        }

        [Test]
        public void CanCreateAnEmptyStore()
        {
            CreateStoreToTest();
        }

        [Test]
        public void KeysDefaultToNull()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var value = await store.GetValue(JArray.FromObject(new[] { "test-key" }));
                Assert.IsNull(value);
            }).Wait();
        }

        [Test]
        public void CanStoreSomething()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var key = JArray.FromObject(new[] { "test-key" });
                var storeValue = JObject.FromObject(new { SomeValue = "hello" });

                await store.SetValue(key, storeValue);
            }).Wait();
        }

        [Test]
        public void CanStoreAndOverwriteSomething()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var key = JArray.FromObject(new[] { "test-key" });
                var storeValue = JObject.FromObject(new { SomeValue = "hello" });
                var overwriteValue = JObject.FromObject(new { SomeValue = "goodbye" });

                await store.SetValue(key, storeValue);
                await store.SetValue(key, overwriteValue);
            }).Wait();
        }

        [Test]
        public void CanStoreAndRetrieveADocument()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var key = JArray.FromObject(new[] { "test-key" });
                var storeValue = JObject.FromObject(new {SomeValue = "hello" });

                await store.SetValue(key, storeValue);
                var value = await store.GetValue(key);

                Assert.IsNotNull(value);
                Assert.AreEqual("hello", value["SomeValue"].Value<string>());
            }).Wait();
        }

        [Test]
        public void CanStoreAndRetrieveADocumentByEnumeration()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var key = JArray.FromObject(new[] { "test-key" });
                var storeValue = JObject.FromObject(new { SomeValue = "hello" });

                await store.SetValue(key, storeValue);

                var enumerator = store.EnumerateAllValues();
                var value = await enumerator.FetchNext();

                Assert.IsNotNull(value);
                Assert.AreEqual("hello", value.Item2["SomeValue"].Value<string>());
            }).Wait();
        }

        [Test]
        public void CanResetADocumentToNull()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var key = JArray.FromObject(new[] { "test-key" });
                var storeValue = JObject.FromObject(new { SomeValue = "hello" });

                await store.SetValue(key, storeValue);
                await store.Commit();
                var value = await store.GetValue(key);

                Assert.IsNotNull(value);

                await store.SetValue(key, null);
                await store.Commit();
                value = await store.GetValue(key);

                Assert.IsNull(value);
            }).Wait();
        }

        [Test]
        public void CanSetANonexistentDocumentToNull()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var key = JArray.FromObject(new[] { "test-key" });

                await store.SetValue(key, null);
                var value = await store.GetValue(key);

                Assert.IsNull(value);
            }).Wait();
        }

        [Test]
        public void NullDocumentsAreNotReturnedDuringEnumeration()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var key = JArray.FromObject(new[] { "test-key" });
                var storeValue = JObject.FromObject(new { SomeValue = "hello" });

                await store.SetValue(key, storeValue);
                var value = await store.GetValue(key);

                Assert.IsNotNull(value);

                await store.SetValue(key, null);

                var enumerator = store.EnumerateAllValues();
                for (var shouldNotBeNull = await enumerator.FetchNext(); shouldNotBeNull != null; shouldNotBeNull = await enumerator.FetchNext())
                {
                    Assert.IsNotNull(shouldNotBeNull.Item2);
                }
            }).Wait();
        }

        [Test]
        public void CanRetrieveAChildStore()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var key = JArray.FromObject(new[] { "test-key" });

                var child = store.ChildStore(key);

                Assert.IsNotNull(child);
            }).Wait();
        }

        [Test]
        public void ValuesAreDifferentInChildFromParent()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var childStoreKey = JArray.FromObject(new[] { "test-store-key" });
                var documentKey = JArray.FromObject(new[] { "test-key" });

                var child = store.ChildStore(childStoreKey);
                Assert.IsFalse(ReferenceEquals(child, store));

                await store.SetValue(documentKey, JObject.FromObject(new { SomeValue = "MainStore" }));
                await child.SetValue(documentKey, JObject.FromObject(new { SomeValue = "ChildStore" }));

                await store.Commit();
                await child.Commit();

                var mainValue = await store.GetValue(documentKey);
                var childValue = await child.GetValue(documentKey);

                Assert.IsNotNull(mainValue);
                Assert.IsNotNull(childValue);

                Assert.AreEqual("MainStore", mainValue["SomeValue"].Value<string>());
                Assert.AreEqual("ChildStore", childValue["SomeValue"].Value<string>());
            }).Wait();
        }

        [Test]
        public void ValueKeysDoNotClashWithChildStoreKeys()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var childStoreKey = new JArray("child-store");
                var documentKey = new JArray("child-store");

                Assert.IsNull(await store.GetValue(documentKey));
                Assert.IsNull(await store.ChildStore(childStoreKey).GetValue(documentKey));

                await store.ChildStore(childStoreKey).SetValue(documentKey, JObject.FromObject(new { Something = "Hello" }));
                await store.ChildStore(childStoreKey).Commit();

                Assert.IsNull(await store.GetValue(documentKey));
                Assert.IsNotNull(await store.ChildStore(childStoreKey).GetValue(documentKey));
            }).Wait();
        }

        [Test]
        public void DeletingAChildStoreRemovesItsKeys()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var childStoreKey = new JArray("child-store");
                var documentKey = new JArray("document");

                Assert.IsNull(await store.ChildStore(childStoreKey).GetValue(documentKey));
                await store.ChildStore(childStoreKey).SetValue(documentKey, JObject.FromObject(new { Something = "Hello" }));
                Assert.IsNotNull(await store.ChildStore(childStoreKey).GetValue(documentKey));

                await store.DeleteChildStore(childStoreKey);
                Assert.IsNull(await store.ChildStore(childStoreKey).GetValue(documentKey));
            }).Wait();
        }

        [Test]
        public void DeletingAChildStoreIsRecursive()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var childStoreKey = new JArray("child-store");
                var documentKey = new JArray("document");

                Assert.IsNull(await store.ChildStore(childStoreKey).GetValue(documentKey));
                await store.ChildStore(childStoreKey).SetValue(documentKey, JObject.FromObject(new { Something = "Hello" }));
                await store.ChildStore(childStoreKey).ChildStore(childStoreKey).SetValue(documentKey, JObject.FromObject(new { Something = "Hello" }));
                Assert.IsNotNull(await store.ChildStore(childStoreKey).GetValue(documentKey));
                Assert.IsNotNull(await store.ChildStore(childStoreKey).ChildStore(childStoreKey).GetValue(documentKey));

                await store.DeleteChildStore(childStoreKey);
                Assert.IsNull(await store.ChildStore(childStoreKey).GetValue(documentKey));
                Assert.IsNull(await store.ChildStore(childStoreKey).ChildStore(childStoreKey).GetValue(documentKey));
            }).Wait();
        }

        [Test]
        public void DeletingAChildStoreRemoves200Keys()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var childStoreKey = new JArray("child-store");
                var documentKey = new JArray("document");

                await AppendData(store.ChildStore(childStoreKey), 200, -1);
                Assert.IsNotNull(await store.ChildStore(childStoreKey).EnumerateAllValues().FetchNext());

                await store.DeleteChildStore(childStoreKey);
                Assert.IsNull(await store.ChildStore(childStoreKey).EnumerateAllValues().FetchNext());
            }).Wait();
        }

        [Test]
        public void CanAppendAResult()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var keyValue = await store.AppendValue(JObject.FromObject(new { Something = "Hello " }));
                Assert.That(keyValue >= 0);
            }).Wait();
        }

        [Test]
        public void AppendedResultsGenerateValidKey()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var keyValue = await store.AppendValue(JObject.FromObject(new { Something = "Hello" }));
                await store.Commit();

                var retrievedObject = await store.GetValue(new JArray(keyValue));
                Assert.IsNotNull(retrievedObject);
                Assert.AreEqual("Hello", retrievedObject["Something"].Value<string>());
            }).Wait();
        }

        /// <summary>
        /// Writes a number of data items (each specifying its index) to a key value store using the AppendValue operator
        /// </summary>
        private static async Task<long> AppendData(IKeyValueStore store, int numData, int returnKeyIndex)
        {
            long appendKeyIndex = -1;

            for (int x = 0; x < numData; ++x)
            {
                var thisKeyIndex = await store.AppendValue(JObject.FromObject(new { Index = x }));
                if (x == returnKeyIndex)
                {
                    appendKeyIndex = thisKeyIndex;
                }
            }

            await store.Commit();

            return appendKeyIndex;
        }

        /// <summary>
        /// Checks that an enumeration contains all the elements between lowerIndex (inclusive) and upperIndex (exclusive)
        /// </summary>
        /// <param name="enumerator"></param>
        /// <param name="lowerIndex"></param>
        /// <param name="upperIndex"></param>
        /// <returns></returns>
        private static async Task CheckEnumerationContainsAllIndexes(IKvStoreEnumerator enumerator, int lowerIndex, int upperIndex)
        {
            var foundItems = new HashSet<int>();

            for (var value = await enumerator.FetchNext(); value != null; value = await enumerator.FetchNext())
            {
                // Every value must have an index
                var index = value.Item2["Index"].Value<int>();

                // Must be in the specified range
                Assert.That(index >= lowerIndex, index + " < " + lowerIndex);
                Assert.That(index < upperIndex, index + " >= " + upperIndex);

                Assert.That(!foundItems.Contains(index), index + " duplicated");

                foundItems.Add(index);
            }

            Assert.AreEqual(upperIndex - lowerIndex, foundItems.Count, "Only found " + foundItems + " keys");
        }

        /// <summary>
        /// Checks that an enumeration contains all the elements between lowerIndex (inclusive) and upperIndex (exclusive)
        /// </summary>
        /// <param name="enumerator"></param>
        /// <param name="lowerIndex"></param>
        /// <param name="upperIndex"></param>
        /// <returns></returns>
        private static async Task CheckEnumerationContainsAllIndexesUsingPages(Func<IKvStoreEnumerator> enumeratorGenerator, int lowerIndex, int upperIndex)
        {
            var foundItems = new HashSet<int>();

            for (var page = await enumeratorGenerator().FetchPage(null); page != null; page = await enumeratorGenerator().FetchPage(await page.GetNextPageToken()))
            {
                foreach (var value in await page.GetObjects())
                {
                    // Every value must have an index
                    var index = value.Item2["Index"].Value<int>();

                    // Must be in the specified range
                    Assert.That(index >= lowerIndex, index + " is lower than " + lowerIndex);
                    Assert.That(index < upperIndex, index + " is greater than " + upperIndex);

                    Assert.That(!foundItems.Contains(index), index + " was duplicated");

                    foundItems.Add(index);
                }

                // Reached the end if the next token is null
                if (await page.GetNextPageToken() == null) break;
            }

            Assert.AreEqual(upperIndex - lowerIndex, foundItems.Count, "Expected " + (upperIndex - lowerIndex) + " items but found " + foundItems.Count);
        }

        [Test]
        public void CanEnumerateOver100Items()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();
                await AppendData(store, 100, -1);
                await CheckEnumerationContainsAllIndexes(store.EnumerateAllValues(), 0, 100);
            }).Wait();
        }

        [Test]
        public void CanEnumerateOver200Items()
        {
            // 100 happens to be a magic number for azure table storage, so try 200 too.
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();
                await AppendData(store, 200, -1);
                await CheckEnumerationContainsAllIndexes(store.EnumerateAllValues(), 0, 200);
            }).Wait();
        }

        [Test]
        public void CanEnumerateOver2000ItemsUsingPaging()
        {
            // 1000 is a magic number for Azure pages.
            // The test data set is fairly annoying and time-consuming in an Azure table to generate, though :-(
            // (MS claim 20k transactions per second, which should mean this takes 100ms. It actaully takes several minutes even using query batches :-/)
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();
                await AppendData(store, 2000, -1);
                await CheckEnumerationContainsAllIndexesUsingPages(store.EnumerateAllValues, 0, 2000);
            }).Wait();
        }

        [Test]
        public void CanEnumerateFromStartUsingAppendedSince()
        {
            Task.Run(async () =>
            {
                // The key '-1' can be used to enumerate over all of the data
                var store = CreateStoreToTest();
                await AppendData(store, 100, -1);
                await CheckEnumerationContainsAllIndexes(store.EnumerateValuesAppendedSince(-1), 0, 100);
            }).Wait();
        }

        [Test]
        public void CanEnumerateFromHalfWayUsingAnAppendKey()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var halfWay = await AppendData(store, 100, 50);
                await CheckEnumerationContainsAllIndexes(store.EnumerateValuesAppendedSince(halfWay), 51, 100);
            }).Wait();
        }

        [Test]
        public void EnumerationDoesNotHitKeysAddedWithoutAppendValue()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                // Generate data through AppendValue
                await AppendData(store, 100, -1);

                // Add an extra value which doesn't have an AppendValue style key
                // Behaviour is undefined if you use an AppendValue key here
                await store.SetValue(new JArray("ShouldntEnumerate"), JObject.FromObject(new {Index = 50}));

                // This key would appear if EnumerateAllValues is here instead
                await CheckEnumerationContainsAllIndexes(store.EnumerateValuesAppendedSince(-1), 0, 100);
            }).Wait();
        }

        [Test]
        public void RetrievingChildTwiceStoreDoesNotCauseAppendClashes()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();

                var aChildStore = store.ChildStore(JArray.FromObject(new[] {"test"}));
                var shouldBeTheSameChildStore = store.ChildStore(JArray.FromObject(new[] {"test"}));

                // Slightly dependent on a race condition. If the stores are separate, we can get the same value because they don't know about each other
                var taskOne = aChildStore.AppendValue(new JObject());
                var taskTwo = shouldBeTheSameChildStore.AppendValue(new JObject());

                await Task.WhenAll(taskOne, taskTwo);

                long one = await taskOne;
                long two = await taskTwo;
                long three = await aChildStore.AppendValue(new JObject());
                long four = await shouldBeTheSameChildStore.AppendValue(new JObject());

                Assert.AreNotEqual(one, two);
                Assert.AreNotEqual(two, three);
                Assert.AreNotEqual(three, four);
                Assert.AreNotEqual(one, three);
                Assert.AreNotEqual(one, four);
                Assert.AreNotEqual(two, four);
            }).Wait();
        }

        [Test]
        public void AppendValueDoesntClashAfterRecreatingStore()
        {
            Task.Run(async () =>
            {
                var initialStore = CreateStoreToTest();
                var one = await initialStore.AppendValue(new JObject());
                await initialStore.Commit();

                initialStore = null;

                var recreatedStore = RecreateStoreToTest();
                var two = await recreatedStore.AppendValue(new JObject());
                await recreatedStore.Commit();

                Assert.AreNotEqual(one, two);
            }).Wait();
        }

        [Test]
        public void CanRetrieveValuesByPrefix()
        {
            Task.Run(async () =>
            {
                var initialStore = CreateStoreToTest();

                // Append 12 values with a particular prefix, and 12 with a preceding and a following prefix
                for (int x = 0; x < 12; ++x)
                {
                    await initialStore.SetValue(new JArray("prefix", x), JObject.FromObject(new { Val = x }));
                    await initialStore.SetValue(new JArray("qrefix", x), JObject.FromObject(new { Following = x }));
                    await initialStore.SetValue(new JArray("orefix", x), JObject.FromObject(new { Preceding = x }));
                    await initialStore.SetValue(new JArray("prefix2", x), JObject.FromObject(new { Following2 = x }));
                    await initialStore.SetValue(new JArray("prefix2-", x), JObject.FromObject(new { Following3 = x }));
                }

                // Commit values so we can retrieve them
                await initialStore.Commit();

                // Should be able to retrieve 12 values with this prefix
                var prefixEnum = initialStore.EnumerateValuesBeginningWithKey(new JArray("prefix"));
                int count = 0;
                for (var obj = await prefixEnum.FetchNext(); obj != null; obj = await prefixEnum.FetchNext())
                {
                    ++count;
                }

                Assert.AreEqual(12, count);
            }).Wait();
        }

        private static IEnumerable<JArray> KeysFrom(int lower, int upper)
        {
            for (long x = lower; x <= upper; ++x)
            {
                yield return new JArray(x);
            }
        }

        [Test]
        public void CanDeleteTheLowerSetOfKeys()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();
                await AppendData(store, 100, -1);
                await store.DeleteKeys(KeysFrom(0, 49));
                await CheckEnumerationContainsAllIndexes(store.EnumerateAllValues(), 50, 100);
            }).Wait();
        }

        [Test]
        public void CanDeleteTheUpperSetOfKeys()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();
                await AppendData(store, 99, -1);
                await store.DeleteKeys(KeysFrom(50, 98));
                await CheckEnumerationContainsAllIndexes(store.EnumerateAllValues(), 0, 50);
            }).Wait();
        }

        [Test]
        public void DeletingNonExistentKeysIsNotAnError()
        {
            Task.Run(async () =>
            {
                var store = CreateStoreToTest();
                await AppendData(store, 50, -1);
                await store.DeleteKeys(KeysFrom(25, 99));
                await CheckEnumerationContainsAllIndexes(store.EnumerateAllValues(), 0, 25);
            }).Wait();
        }
    }
}
