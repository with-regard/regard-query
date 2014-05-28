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
            return new DataStore(new MemoryStoreRetrieval(), "test-node");
        }

        /// <summary>
        /// Creates a map/reduce data store that is backed by an Azure table
        /// </summary>
        /// <param name="connectionString">The connection string for the Azure storage instance</param>
        /// <param name="tableName">The name of the table where data should be written</param>
        /// <param name="nodeName">The name of the current node. Every running instance must have a unique node name so that data is always written to an independent partition</param>
        /// <returns>A data store for the specified table</returns>
        public static IRegardDataStore CreateAzureTableDataStore(string connectionString, string tableName, string nodeName)
        {
            return new DataStore(new AzureStoreRetrieval(connectionString, tableName), nodeName);
        }
    }
}
