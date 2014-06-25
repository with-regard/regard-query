using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Regard.Query.Api;
using Regard.Query.MapReduce;

namespace Regard.Query
{
    /// <summary>
    /// Convenience class for retrieving the data store for the current process
    /// </summary>
    public static class DataStoreFactory
    {
        /// <summary>
        /// The prefix used for the tables containing details about the organisations using Regard
        /// </summary>
        private const string c_TablePrefix = "UserData";

        /// <summary>
        /// The name of the test node (used while we don't have support for multi-node deployments)
        /// </summary>
        private const string c_TestNodeName = "TestNode";

        /// <summary>
        /// Creates the default data store for the current process
        /// </summary>
        /// <remarks>
        /// This will currently create a SQL server data store, using the Regard.Storage.SqlConnectionString value from Azure cloud settings
        /// or the Regard.SqlConnectionString from application settings.
        /// <para/>
        /// (SQL server has a number of problems as a data store for Regard when dealing with many/large projects so future versions will
        /// very likely use something different)
        /// </remarks>
        public static async Task<IRegardDataStore> CreateDefaultDataStore()
        {
            // Get the connection string for the Azure storage that is configured for this instance
            string storageConnectionString = CloudConfigurationManager.GetSetting("Regard.Storage.ConnectionString");

            if (!string.IsNullOrEmpty(storageConnectionString))
            {
                Trace.WriteLine("Retrieving data from Azure cloud storage using cloud settings");
                return await CreateAzureTableStore(storageConnectionString);
            }

            storageConnectionString = ConfigurationManager.AppSettings["Regard.StorageConnectionString"];
            if (!string.IsNullOrEmpty(storageConnectionString))
            {
                Trace.WriteLine("Retrieving data from Azure cloud storage using app settings");
                return await CreateAzureTableStore(storageConnectionString);
            }

            // No application settings let us find a data store
            // TODO: use a flat file or in-memory or something similar?
            throw new InvalidOperationException("No data store is available for this application");
        }

        private static object s_Sync = new object();
        private static Dictionary<string, IRegardDataStore> s_DataStoreForConnectionString = new Dictionary<string, IRegardDataStore>();

        /// <summary>
        /// Creates a data store using Azure storage
        /// </summary>
        public static Task<IRegardDataStore> CreateAzureTableStore(string connectionString)
        {
            lock (s_Sync)
            {
                IRegardDataStore result;
                if (s_DataStoreForConnectionString.TryGetValue(connectionString, out result))
                {
                    return Task.FromResult(result);
                }
                else
                {

                    // TODO: Currently we only support a single running node (actually a single ingestion node and a single query node)
                    // Eventually we should determine the node name by reading instance data
                    result = s_DataStoreForConnectionString[connectionString] = MapReduceDataStoreFactory.CreateAzureTableDataStore(connectionString, c_TablePrefix, c_TestNodeName);
                    return Task.FromResult(result);
                }
            }
        }
    }
}
