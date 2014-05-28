using System.Threading.Tasks;
using Regard.Query.Api;

namespace Regard.Query.Tests.Api.EventRecorder
{
    /// <summary>
    /// Creates the event recorder test project (which has the WithRegard/Test product created and the well known test user opted in)
    /// </summary>
    static class SetupTestProject
    {
        public static async Task<IRegardDataStore> CreateEventRecorderTestProject(string dataStoreType)
        {
            var store = TestDataStoreFactory.CreateEmptyDataStore(dataStoreType);

            // Create the WithRegard/Test product
            await store.Products.CreateProduct("WithRegard", "Test");

            // Opt in the test user
            var product = await store.Products.GetProduct("WithRegard", "Test");
            await product.Users.OptIn(WellKnownUserIdentifier.TestUser);

            return store;
        }
    }
}
