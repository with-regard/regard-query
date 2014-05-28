using System.Threading.Tasks;
using NUnit.Framework;

namespace Regard.Query.Tests.Api.ProductAdmin
{
    [TestFixture("InMemory")]
    [TestFixture("LocalAzureTableStore")]
    public class Basic
    {
        private string m_DataStoreType;

        public Basic(string dataStoreType)
        {
            m_DataStoreType = dataStoreType;
        }

        [Test]
        public void CanCreateAProduct()
        {
            Task.Run(async () =>
            {
                var store = TestDataStoreFactory.CreateEmptyDataStore(m_DataStoreType);
                await store.Products.CreateProduct("WithRegard", "Test");
            }).Wait();
        }

        [Test]
        public void CanRetrieveAProduct()
        {
            Task.Run(async () =>
            {
                var store = TestDataStoreFactory.CreateEmptyDataStore(m_DataStoreType);
                await store.Products.CreateProduct("WithRegard", "Test");

                var retrievedProduct = await store.Products.GetProduct("WithRegard", "Test");
                Assert.IsNotNull(retrievedProduct);
            }).Wait();
        }

        [Test]
        public void RetrieveNullIfProductDoesNotExist()
        {
            Task.Run(async () =>
            {
                var store = TestDataStoreFactory.CreateEmptyDataStore(m_DataStoreType);

                var retrievedProduct = await store.Products.GetProduct("WithRegard", "Test");
                Assert.IsNull(retrievedProduct);
            }).Wait();
        }
    }
}
