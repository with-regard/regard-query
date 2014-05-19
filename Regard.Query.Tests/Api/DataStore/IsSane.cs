using NUnit.Framework;

namespace Regard.Query.Tests.Api.DataStore
{
    [TestFixture]
    class IsSane
    {
        [Test]
        public void ThereIsAnEventRecorder()
        {
            Assert.IsNotNull(TestDataStoreFactory.CreateEmptyDataStore().EventRecorder);
        }

        [Test]
        public void ThereIsAProductAdminInterface()
        {
            Assert.IsNotNull(TestDataStoreFactory.CreateEmptyDataStore().Products);
        }
    }
}
