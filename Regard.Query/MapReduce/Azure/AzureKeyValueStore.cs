using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.Util;

namespace Regard.Query.MapReduce.Azure
{
    /// <summary>
    /// Key/value store that targets Azure data stores
    /// </summary>
    public class AzureKeyValueStore : IKeyValueStore
    {
        public const string InternalKeyPrefix = "---";

        private readonly object m_Sync = new object();

        /// <summary>
        /// -1 if we don't know the next append index, otherwise the append index for this table
        /// </summary>
        /// <remarks>
        /// This assumes that there is only one instance of a key/value store per table so we don't generate overlapping indexes. This isn't true at the moment: need to write some tests for this :-)
        /// </remarks>
        private long m_NextAppendIndex = -1;

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
        /// Creates a child store for an azure key/value store
        /// </summary>
        private AzureKeyValueStore(CloudTable table, string partition)
        {
            m_Table     = table;
            m_Partition = partition;
        }

        /// <summary>
        /// Prepares an entity to be stored in this table
        /// </summary>
        internal JsonTableEntity CreateEntity(JArray key, JObject data)
        {
            var newEntity               = new JsonTableEntity();
            var rowKeyString            = CreateKey(key);

            newEntity.RowKey            = rowKeyString;
            newEntity.PartitionKey      = m_Partition;
            newEntity.Index             = -1;
            newEntity.SerializedKey     = key.ToString(Formatting.None);
            newEntity.SerializedJson    = data.ToString(Formatting.None);

            return newEntity;
        }

        private string CreateKeyComponent(long value)
        {
            // Using a fixed-length string for the key value ensures we can use it successfully in queries
            return value.ToString("X16", CultureInfo.InvariantCulture);
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
                        result.Append(CreateKeyComponent(element.Value<long>()));
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
        /// Creates an internal key (which can be easily excluded from enumerations and which can never match a user-created key)
        /// </summary>
        private string CreateInternalKey(string value)
        {
            // Internal keys begin '---'
            return InternalKeyPrefix + StorageUtil.SanitiseKey(value);
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
            var extraPartitionKey = CreateKey(key);

            // Use '--' to separate child stores to prevent clashes between similar names
            return new AzureKeyValueStore(m_Table, m_Partition + "--" + extraPartitionKey);
        }

        /// <summary>
        /// Stores a value in the database, indexed by a particular key
        /// </summary>
        public async Task SetValue(JArray key, JObject value)
        {
            if (value == null)
            {
                var rowKeyString = CreateKey(key);

                // Delete the existing entity
                // Azure Table Storage only actually uses the row/partition key with Delete but takes an entire entity anyway :-/
                // Setting the ETag to * means we don't need to read the record before deleting it
                var deleteCommand = TableOperation.Delete(new DynamicTableEntity(m_Partition, rowKeyString) { ETag = "*" });

                try
                {
                    await m_Table.ExecuteAsync(deleteCommand);
                }
                catch (StorageException e)
                {
                    // A 404 indicates that the record didn't exist in the first place
                    if (e.RequestInformation.HttpStatusCode != 404)
                    {
                        throw;
                    }
                }
            }
            else
            {
                // Create a table entity
                var newEntity               = CreateEntity(key, value);

                var insertCommand = TableOperation.InsertOrReplace(newEntity);
                await m_Table.ExecuteAsync(insertCommand);
            }
        }

        /// <summary>
        /// Tries to retrieve the latest append index value from the table
        /// </summary>
        private async Task<long> RetrieveLatestAppendIndex()
        {
            lock (m_Sync)
            {
                if (m_NextAppendIndex >= 0)
                {
                    // We have an in-memory value to use instead
                    var result = m_NextAppendIndex;
                    m_NextAppendIndex++;
                    return result;
                }
            }

            var indexKey = CreateInternalKey("AppendIndex");

            // Read the last key from the table
            var retrieveOperation = TableOperation.Retrieve<AppendIndexStatusEntity>(m_Partition, indexKey);
            var storedValue = await m_Table.ExecuteAsync(retrieveOperation);

            lock (m_Sync)
            {
                if (m_NextAppendIndex < 0)
                {
                    // We're the first thread to retrieve the value. Use existing values if a different thread got there first
                    // We add 10000 to the last result to avoid the case where the index was written out of order. We assume this can't happen over 10000 records
                    // (This works around the fragility of this technique. If Azure tables supported counting then we could use a much more robust algorithm here)
                    // TODO: could probe for entities after this one instead of this, which avoids 'gaps'
                    if (storedValue != null && storedValue.Result != null)
                    {
                        // Existing item
                        m_NextAppendIndex = ((AppendIndexStatusEntity) storedValue.Result).LastAppendIndex + 10000;
                    }
                    else
                    {
                        // First item in the table
                        m_NextAppendIndex = 0;
                    }
                }

                // Generate the final result
                var result = m_NextAppendIndex;
                m_NextAppendIndex++;
                return result;
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
        public async Task<long> AppendValue(JObject value)
        {
            var indexKey = CreateInternalKey("AppendIndex");

            long nextValue = -1;
            lock (m_Sync)
            {
                if (m_NextAppendIndex >= 0)
                {
                    // Assign a value if we already know an append index
                    nextValue = m_NextAppendIndex;
                    m_NextAppendIndex++;
                }
                else
                {
                    // We don't already know an append index
                    nextValue = -1;
                }
            }

            // If we don't have an append index, we need to fetch the last index
            if (nextValue == -1)
            {
                // Query the table to find the append index
                nextValue = await RetrieveLatestAppendIndex();
            }

            // Generate a key for this value and store in the table
            JArray valueKey = JArray.FromObject(new[] {nextValue});

            // Store this as the latest key
            Task storeLatest = null;
            lock (m_Sync)
            {
                if (nextValue + 1 == m_NextAppendIndex)
                {
                    // This is the latest key
                    var keyEntity = new AppendIndexStatusEntity
                    {
                        RowKey = indexKey,
                        PartitionKey = m_Partition,
                        LastAppendIndex = m_NextAppendIndex
                    };
                    var storeLatestOperation = TableOperation.InsertOrReplace(keyEntity);

                    storeLatest = m_Table.ExecuteAsync(storeLatestOperation);
                }
            }

            // Store the value associated with this key
            var storeTask = SetValue(valueKey, value);

            // Wait for the tasks to complete
            if (storeLatest != null)
            {
                await Task.WhenAll(storeLatest, storeTask);
            }
            else
            {
                await storeTask;
            }

            // Return the appended value
            return nextValue;
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
            // Get everything from the partition
            var query = new TableQuery<JsonTableEntity>();
            query.Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, m_Partition)
                );

            return new SegmentedEnumerator(m_Table, query, null);
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
            // Get the initial key
            var initialKey = CreateKeyComponent(appendKey);

            // Create a query that should find all of the keys greater than this value
            // We use 16-digit strings so that the query won't match 100, 1, 2 but will actually find truly greater values
            var findKeysQuery = new TableQuery<JsonTableEntity>();

            if (appendKey >= 0)
            {
                findKeysQuery.Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, m_Partition),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, initialKey + "-"))
                    );
            }
            else
            {
                findKeysQuery.Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, m_Partition),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, CreateKeyComponent(0) + "-"))
                    );
            }

            // Execute the query using an enumerator
            return new SegmentedEnumerator(m_Table, findKeysQuery, (key, obj) =>
            {
                // Must be an item with an integer key, which is greater than the target key
                if (key == null)                        return false;
                if (key.Count != 1)                     return false;
                if (key[0].Type != JTokenType.Integer)  return false;

                return key[0].Value<long>() > appendKey;
            });
        }

        /// <summary>
        /// Erases all of the values in a particular child store
        /// </summary>
        public async Task DeleteChildStore(JArray key)
        {
            // Ugh, here's where the limitations of Azure's query language really show themselves
            // There's no 'startswith' query for strings, even though that ought to be really easy to implement if you can do greater-than.
            // There's no way to just delete the results of a query. You need to read the records then batch up a deletion operation

            // Using the technique here: http://www.dotnetsolutions.co.uk/blog/starts-with-query-pattern---windows-azure-table-design-patterns
            // to work around the first limitation
            var extraPartitionKey = CreateKey(key);

            // Use '--' to separate child stores to prevent clashes between similar names
            var childStoreKey       = m_Partition + "--" + extraPartitionKey;
            var childStoreNextKey   = childStoreKey.Remove(childStoreKey.Length-1) + (childStoreKey[childStoreKey.Length - 1] + 1);                 // Ie, the item lexographically after childStoreKey

            var allKeysQuery = new TableQuery<JsonTableEntity>();
            allKeysQuery.Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, childStoreKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThan, childStoreNextKey)
                    )
                );

            // Select all the items and then batch delete them
            // See http://wintellect.com/blogs/jlane/deleting-entities-in-windows-azure-table-storage for the basic technique used here
            Dictionary<string, TableBatchOperation> batches = new Dictionary<string, TableBatchOperation>();

            var currentSegment = await m_Table.ExecuteQuerySegmentedAsync(allKeysQuery, null);

            while (currentSegment != null && currentSegment.Results != null)
            {
                // Batch up the items in this segment
                foreach (var value in currentSegment.Results)
                {
                    // Find or create the batch operation for this partition
                    TableBatchOperation partitionBatchOp;
                    if (!batches.TryGetValue(value.PartitionKey, out partitionBatchOp))
                    {
                        partitionBatchOp = batches[value.PartitionKey] = new TableBatchOperation();
                    }

                    // Delete this item
                    partitionBatchOp.Add(TableOperation.Delete(value));

                    if (partitionBatchOp.Count >= 100)
                    {
                        // Execute this batch when it gets large enough
                        await m_Table.ExecuteBatchAsync(partitionBatchOp);
                        batches[value.PartitionKey] = new TableBatchOperation();
                    }
                }
            }

            // Execute all of the remaining batch operations to finish up
            foreach (var batchOp in batches)
            {
                // Sometimes the batches might be empty
                if (batchOp.Value.Count <= 0) continue;

                await m_Table.ExecuteBatchAsync(batchOp.Value);
            }
        }

        /// <summary>
        /// Waits for all of the pending SetValue requests to complete (if they are cached or otherwise write-through)
        /// </summary>
        public async Task Commit()
        {
            // Right now, this is a no-op. If we ever support caching or batching operations - and we should - this should clear out the current batch
            // I won't suppress the warning so that we get some kind of reminder this would be a good idea
        }
    }
}
