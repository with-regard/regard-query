using System.Threading.Tasks;
using NUnit.Framework;

namespace Regard.Query.Tests.Api.ProductAdmin
{
    [TestFixture]
    public class Basic
    {
        [Test]
        public void CanCreateAProduct()
        {
            Task.Run(async () =>
            {
                var store = TestDataStoreFactory.CreateEmptyDataStore();
                await store.Products.CreateProduct("WithRegard", "Test");
            }).Wait();
        }

        [Test]
        public void CanRetrieveAProduct()
        {
            Task.Run(async () =>
            {
                var store = TestDataStoreFactory.CreateEmptyDataStore();
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
                var store = TestDataStoreFactory.CreateEmptyDataStore();

                var retrievedProduct = await store.Products.GetProduct("WithRegard", "Test");
                Assert.IsNull(retrievedProduct);
            }).Wait();
        }
    }
}
