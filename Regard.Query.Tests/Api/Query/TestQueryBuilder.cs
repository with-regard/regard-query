using System;
using System.Threading.Tasks;
using Regard.Query.Api;
using Regard.Query.Tests.MapReduce;

namespace Regard.Query.Tests.Api.Query
{
    static class TestQueryBuilder
    {
        /// <summary>
        /// Creates an empty data store with the 'WithRegard/Test' project and the test user opted-in
        /// </summary>
        public static async Task<IRegardDataStore> CreateEmptyDataStore()
        {
            var store = TestDataStoreFactory.CreateEmptyDataStore();

            await store.Products.CreateProduct("WithRegard", "Test");

            var testProduct = await store.Products.GetProduct("WithRegard", "Test");
            await testProduct.Users.OptIn(WellKnownUserIdentifier.TestUser);

            return store;
        }

        /// <summary>
        /// Sends the 12 test documents to the test project in a data store (we put them all in a single session, which will do
        /// for testing the query engine, but isn't quite realistic)
        /// </summary>
        public static async Task IngestBasic12TestDocuments(IRegardDataStore target)
        {
            // We use a fixed session ID for each test
            var sessionId = new Guid("CB2FC120-237C-4B5C-B29F-803DF5CE0FB2");
            await target.EventRecorder.StartSession("WithRegard", "Test", WellKnownUserIdentifier.TestUser, sessionId);

            foreach (var doc in TestDataGenerator.Generate12BasicDocuments())
            {
                await target.EventRecorder.RecordEvent(sessionId, doc);
            }
        }
    }
}
