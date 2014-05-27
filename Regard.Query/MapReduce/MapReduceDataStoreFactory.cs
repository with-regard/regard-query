using Regard.Query.Api;
using Regard.Query.MapReduce.Azure;

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

        public static IRegardDataStore CreateAzureTableDataStore(string connectionName, string tableName, string nodeName)
        {
            return new DataStore(new AzureKeyValueStore(connectionName, tableName), nodeName);
        }
    }
}
