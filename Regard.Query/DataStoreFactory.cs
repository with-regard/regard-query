using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Regard.Query.Api;
using Regard.Query.Sql;

namespace Regard.Query
{
    /// <summary>
    /// Convenience class for retrieving the data store for the current process
    /// </summary>
    public static class DataStoreFactory
    {
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
            // If we're running as an azure instance, the connection string will be availble from the cloud configuration manager
            string sqlConnectionString = CloudConfigurationManager.GetSetting("Regard.Storage.SqlConnectionString");
            if (!string.IsNullOrEmpty(sqlConnectionString))
            {
                // Will be non-null if it's set up
                Trace.WriteLine("Retrieving data from SQL server using cloud settings");
                return await CreateSqlServerStore(sqlConnectionString);
            }

            // Also look in the application configuration for a connection string
            sqlConnectionString = ConfigurationManager.AppSettings["Regard.SqlConnectionString"];
            if (!string.IsNullOrEmpty(sqlConnectionString))
            {
                Trace.WriteLine("Retrieving data from SQL server using application settings");
                return await CreateSqlServerStore(sqlConnectionString);
            }

            // No application settings let us find a data store
            // TODO: use a flat file or in-memory or something similar?
            throw new InvalidOperationException("No data store is available for this application");
        }

        /// <summary>
        /// Creates a SQL server data store
        /// </summary>
        public static async Task<IRegardDataStore> CreateSqlServerStore(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            return new SqlDataStore(connection);
        }
    }
}
