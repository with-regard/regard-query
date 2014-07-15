using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Regard.Query.Tests.Api.DataStore
{
    [TestFixture("InMemory")]
    [TestFixture("LocalAzureTableStore")]
    class IsSane
    {
        private string m_DataStoreType;

        public IsSane(string dataStoreType)
        {
            m_DataStoreType = dataStoreType;
        }

        [Test]
        public void ThereIsAnEventRecorder()
        {
            Assert.IsNotNull(TestDataStoreFactory.CreateEmptyDataStore(m_DataStoreType).EventRecorder);
        }

        [Test]
        public void ThereIsAProductAdminInterface()
        {
            Assert.IsNotNull(TestDataStoreFactory.CreateEmptyDataStore(m_DataStoreType).Products);
        }

        [Test]
        public void ThereCanOnlyBeOneAzureStorePerProcess()
        {
            // Technically per node
            Task.Run(async () =>
            {
                var aDataStore = await DataStoreFactory.CreateAzureTableStore(TestDataStoreFactory.GetTestConnectionString(), "TestStore", "TestNode");
                var anotherDataStore = await DataStoreFactory.CreateAzureTableStore(TestDataStoreFactory.GetTestConnectionString(), "TestStore", "TestNode");

                Assert.IsTrue(ReferenceEquals(aDataStore, anotherDataStore));
            }).Wait();
        }
    }
}
