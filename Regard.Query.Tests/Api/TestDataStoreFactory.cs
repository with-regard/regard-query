using Regard.Query.Api;

namespace Regard.Query.Tests.Api
{
    /// <summary>
    /// Generates the data store we use for the tests (right now, this is an in-memory data store using the map/reduce algorithms)
    /// </summary>
    public static class TestDataStoreFactory
    {
        public static IRegardDataStore CreateEmptyDataStore()
        {
            return new MissingDataStore();
        }
    }
}
