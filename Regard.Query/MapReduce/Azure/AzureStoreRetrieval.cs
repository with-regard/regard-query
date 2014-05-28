using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.Azure
{
    /// <summary>
    /// Retrieves the root key/value store for a particular product stored in an Azure table
    /// </summary>
    class AzureStoreRetrieval : IProductStoreRetrieval
    {
        private readonly object m_Sync = new object();
        private readonly string m_ConnectionString;
        private readonly string m_TablePrefix;
        private readonly CloudTableClient m_TableClient;

        private readonly Regex m_ValidNames = new Regex("^[A-Za-z][A-Za-z0-9]{2,62}$");

        private readonly Dictionary<Tuple<string, string>, IKeyValueStore> m_ExistingStores = new Dictionary<Tuple<string, string>, IKeyValueStore>();

        /// <summary>
        /// Creates a new Azure key/value store retrieval object
        /// </summary>
        /// <param name="connectionString">The connection string for the Azure table storage to use</param>
        /// <param name="tablePrefix">A prefix for the table names that this will use</param>
        public AzureStoreRetrieval(string connectionString, string tablePrefix)
        {
            if (connectionString == null)   throw new ArgumentNullException("connectionString");
            if (tablePrefix == null)        throw new ArgumentNullException("tablePrefix");

            var storageAccount  = CloudStorageAccount.Parse(connectionString);
            m_TableClient       = storageAccount.CreateCloudTableClient();

            m_ConnectionString  = connectionString;
            m_TablePrefix       = tablePrefix;
        }

        private string TableNameForOrganization(string organization)
        {
            StringBuilder result = new StringBuilder();
            result.Append(m_TablePrefix);

            result.Append(organization);

            if (!m_ValidNames.Match(result.ToString()).Success)
            {
                throw new InvalidOperationException("Invalid organization/product name");
            }

            return result.ToString();
        }

        public async Task<IKeyValueStore> GetStoreForProduct(string organization, string product)
        {
            if (string.IsNullOrEmpty(organization)) throw new ArgumentNullException("organization");
            if (string.IsNullOrEmpty(product)) throw new ArgumentNullException("product");

            // Try to retrieve an existing store if there is one
            lock (m_Sync)
            {
                IKeyValueStore result;
                if (m_ExistingStores.TryGetValue(new Tuple<string, string>(organization, product), out result))
                {
                    return result;
                }
            }

            // Get the name of the table where the data for this organization is stored
            // We currently use one table per organization
            // This makes it fast to delete organizations but slow to delete products within an organization due to how Azure works
            var tableName = TableNameForOrganization(organization);

            // Get the table for this organization
            var table = m_TableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();

            // Create a KV store for this table
            var store           = new AzureKeyValueStore(table);
            var productStore    = store.ChildStore(JArray.FromObject(new[] { organization, product }));

            // Remember the store
            lock (m_Sync)
            {
                // Deal with the case where two threads created the table simultaneously and this one lost the race
                IKeyValueStore result;
                if (m_ExistingStores.TryGetValue(new Tuple<string, string>(organization, product), out result))
                {
                    return result;
                }

                // This is the first thread to get the store for this product. Remember for later
                result = m_ExistingStores[new Tuple<string, string>(organization, product)] = productStore;
                return result;
            }
        }
    }
}
