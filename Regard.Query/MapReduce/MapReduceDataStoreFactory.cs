using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Factory methods for creating map/reduce data stores
    /// </summary>
    public static class MapReduceDataStoreFactory
    {
        /// <summary>
        /// Creates a map/reduce data store suitable for use for in-memory testing
        /// </summary>
        public static IRegardDataStore CreateInMemoryTemporaryDataStore()
        {
            // The node name would be based on the instance ID on Azure.
            // Here we use 'test-node' as there's only one
            return new DataStore(new MemoryKeyValueStore(), "test-node");
        }
    }
}
