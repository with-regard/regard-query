using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.MapReduce;

namespace Regard.Query.Tests.MapReduce
{
    [TestFixture("InMemory")]
    class BackingStore
    {
        private string m_DataStoreType;

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
                case "InMemory": return new MemoryKeyValueStore();
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
    }
}
