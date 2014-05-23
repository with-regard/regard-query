using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.Util;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Key/value store that targets Azure data stores
    /// </summary>
    public class AzureKeyValueStore : IKeyValueStore
    {
        /// <summary>
        /// What we store in records for the table
        /// </summary>
        class JsonTableEntity : TableEntity
        {
            /// <summary>
            /// The serialized JSON representation of this entity
            /// </summary>
            public string SerializedJson { get; set; }
        }

        /// <summary>
        /// The table that this store will write to
        /// </summary>
        private readonly CloudTable m_Table;

        /// <summary>
        /// The partition represented by this table
        /// </summary>
        private readonly string m_Partition;

        /// <summary>
        /// Creates a new key value store using an Azure table
        /// </summary>
        public AzureKeyValueStore(string connectionString, string tableName)
        {
            // Connect to this table
            var storageAccount  = CloudStorageAccount.Parse(connectionString);
            var tableClient     = storageAccount.CreateCloudTableClient();
            m_Table             = tableClient.GetTableReference(tableName);

            // Ensure that the table exists
            m_Table.CreateIfNotExists();

            // The root partition is just the empty string
            m_Partition = "";
        }

        /// <summary>
        /// Converts a JArray to something suitable to use as a partition/row key
        /// </summary>
        private string CreateKey(JArray source)
        {
            if (source == null) return "";

            StringBuilder result = new StringBuilder();
            foreach (var element in source)
            {
                // Need to use key values that are valid in Azure table storage keys (it's really annoying that there are restrictions on this)
                switch (element.Type)
                {
                    case JTokenType.String:
                        result.Append(StorageUtil.SanitiseKey(element.Value<string>()));
                        break;

                    case JTokenType.Integer:
                        result.Append(StorageUtil.SanitiseKey(element.Value<long>().ToString(CultureInfo.InvariantCulture)));
                        break;

                    default:
                        result.Append(StorageUtil.SanitiseKey(element.ToString(Formatting.None)));
                        break;
                }
                result.Append("-");
            }

            // TODO: keys > 1024 characters will cause errors
            return result.ToString();
        }

        /// <summary>
        /// Retrieves a reference to a child key/value store with a particular key
        /// </summary>
        /// <remarks>
        /// When mapping/reducing data, we need a place to store the result; rather than sharing a 'single' data store (from the point of view of the caller), we allow
        /// for multiple stores.
        /// </remarks>
        public IKeyValueStore ChildStore(JArray key)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Stores a value in the database, indexed by a particular key
        /// </summary>
        public async Task SetValue(JArray key, JObject value)
        {
            var rowKeyString = CreateKey(key);

            if (value == null)
            {
                // Delete the existing entity
                // Azure Table Storage only actually uses the row/partition key with Delete but takes an entire entity anyway :-/
                var deleteCommand = TableOperation.Delete(new DynamicTableEntity(m_Partition, rowKeyString));
                await m_Table.ExecuteAsync(deleteCommand);
            }
            else
            {
                // Create a table entity
                var newEntity               = new JsonTableEntity();
                newEntity.RowKey            = rowKeyString;
                newEntity.PartitionKey      = m_Partition;
                newEntity.SerializedJson    = value.ToString(Formatting.None);

                var insertCommand = TableOperation.InsertOrReplace(newEntity);
                await m_Table.ExecuteAsync(insertCommand);
            }
        }

        /// <summary>
        /// Assigns a key that is unique to this child store and uses it as a key to store a value. The store guarantees that this will be unique within this process, but not 
        /// if the same child store is being accessed by multiple processes.
        /// </summary>
        /// <param name="value">The value to store</param>
        /// <returns>
        /// A long representing the key assigned to the value. The key for GetValue can be obtained by putting this result (alone) in a JArray.
        /// The result is guaranteed to be positive, and will always increase.
        /// </returns>
        public Task<long> AppendValue(JObject value)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Retrieves null or the value associated with a particular key
        /// </summary>
        public async Task<JObject> GetValue(JArray key)
        {
            // Generate a string for the row key
            var rowKeyString = CreateKey(key);

            // Create a query to retrieve this key
            TableOperation retrieve = TableOperation.Retrieve<JsonTableEntity>(m_Partition, rowKeyString);

            // Try to retrieve the entity from the table
            var retrieveResult = await m_Table.ExecuteAsync(retrieve);
            var entity = (JsonTableEntity) retrieveResult.Result;

            // Result is null if the row doesn't exist
            if (entity == null)
            {
                return null;
            }

            // Parse the serialized JSON to produce the final result
            return JObject.Parse(entity.SerializedJson);
        }

        /// <summary>
        /// Enumerates all of the values in this data store
        /// </summary>
        public IKvStoreEnumerator EnumerateAllValues()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Enumerates all the values appended after a particular key was generated by AppendValue
        /// </summary>
        /// <param name="appendKey">The key returned by AppendValue, or -1 to enumerate all values.</param>
        /// <returns>An enumerator</returns>
        /// <remarks>
        /// If values are appended during the enumeration, the implementation can either return the extra values or just the values at the time
        /// of the call. Values are not guaranteed to be returned in order.
        /// </remarks>
        public IKvStoreEnumerator EnumerateValuesAppendedSince(long appendKey)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Erases all of the values in a particular child store
        /// </summary>
        public Task DeleteChildStore(JArray key)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Waits for all of the pending SetValue requests to complete (if they are cached or otherwise write-through)
        /// </summary>
        public Task Commit()
        {
            throw new System.NotImplementedException();
        }
    }
}
