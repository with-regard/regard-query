using System.Threading.Tasks;
using NUnit.Framework;
using Regard.Query.Api;

namespace Regard.Query.Tests.Api.ProductAdmin.UserAdmin
{
    static class TestUserAdminFactory
    {
        public static async Task<IUserAdmin> CreateUserAdminForTestProduct(string dataStoreType)
        {
            var store = TestDataStoreFactory.CreateEmptyDataStore(dataStoreType);

            await store.Products.CreateProduct("WithRegard", "Test");
            var product = await store.Products.GetProduct("WithRegard", "Test");
            Assert.IsNotNull(product);

            return product.Users;
        }
    }
}
