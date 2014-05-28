using System;
using Regard.Query.Api;
using Regard.Query.MapReduce;

namespace Regard.Query.Tests.Api
{
    /// <summary>
    /// Generates the data store we use for the tests (right now, this is an in-memory data store using the map/reduce algorithms)
    /// </summary>
    public static class TestDataStoreFactory
    {
        public static string GetTestConnectionString()
        {
            // Try to read a connection string from an environment variable
            var environmentString = Environment.GetEnvironmentVariable("REGARDTESTCONNECTIONSTRING");
            if (environmentString != null)
            {
                return environmentString;
            }

            // Use the local development storage if no connection string is set
            return "UseDevelopmentStorage=true";
        }

        public static IRegardDataStore CreateEmptyDataStore(string dataStoreType)
        {
            switch (dataStoreType)
            {
                case "InMemory":
                    // Use an in-memory data store for testing purposes (will check that the algorithms work independently of needing actual backing store/server capacity)
                    return MapReduceDataStoreFactory.CreateInMemoryTemporaryDataStore();

                case "LocalAzureTableStore":
                    // Use an in-memory data store for testing purposes (will check that the algorithms work independently of needing actual backing store/server capacity)
                    var storeKey = new Random().Next(int.MaxValue).ToString();
                    return MapReduceDataStoreFactory.CreateAzureTableDataStore(GetTestConnectionString(), "TestDataStore" + storeKey, "TestNode");

                default:
                    throw new InvalidOperationException("Unknown data store type");
            }
        }
    }
}
