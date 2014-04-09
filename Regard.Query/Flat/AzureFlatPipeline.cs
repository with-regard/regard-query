using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Regard.Query.Util;

namespace Regard.Query.Flat
{
    /// <summary>
    /// Class that implements the pipeline actions for an azure flat table storage target
    /// </summary>
    public class AzureFlatPipeline : IPipelineAction
    {
        /// <summary>
        /// The table where the event will be written
        /// </summary>
        private readonly CloudTable m_Table;

        /// <summary>
        /// The product that this event is for
        /// </summary>
        private readonly string m_Product;

        /// <summary>
        /// The organization that this event is for
        /// </summary>
        private readonly string m_Organization;

        /// <summary>
        /// The ID of the query that this data is for
        /// </summary>
        private readonly string m_QueryId;

        /// <summary>
        /// The bucket that this event should be stored in
        /// </summary>
        private readonly List<string> m_Bucket = new List<string>();  

        /// <summary>
        /// The fields that this event should be counted in
        /// </summary>
        private readonly List<string> m_CountFields = new List<string>(); 

        /// <summary>
        /// The fields that this event should be counted with unique keys
        /// </summary>
        private readonly List<KeyValuePair<string, string>> m_CountUniqueFields = new List<KeyValuePair<string, string>>(); 

        /// <summary>
        /// True if the event has been dropped and shouldn't be stored
        /// </summary>
        private bool m_Dropped;

        /// <summary>
        /// Creates a pipeline for a new event
        /// </summary>
        public AzureFlatPipeline(CloudTable table, string product, string organization, string queryId)
        {
            m_Table         = table;
            m_Product       = product;
            m_Organization  = organization;
            m_QueryId       = queryId;
        }

        /// <summary>
        /// Drop the event (don't process any more)
        /// </summary>
        public void Drop()
        {
            m_Dropped = true;
        }

        /// <summary>
        /// Place the event in a named bucket
        /// </summary>
        public void Bucket(string name)
        {
            m_Bucket.Add(name);
        }

        /// <summary>
        /// Adds one to the value of a field in the current bucket
        /// </summary>
        /// <param name="fieldName">The field name within the bucket that </param>
        public void Count(string fieldName)
        {
            m_CountFields.Add(fieldName);
        }

        /// <summary>
        /// Adds one to the value of a field if a particular key has not been seen before within the current bucket
        /// </summary>
        /// <param name="fieldName">The field to add to</param>
        /// <param name="key">The key that should not have been seen before</param>
        public void CountUnique(string fieldName, string key)
        {
            m_CountUniqueFields.Add(new KeyValuePair<string, string>(key, fieldName));
        }

        /// <summary>
        /// Adds a value to a field in a bucket
        /// </summary>
        /// <param name="fieldName">The name of the field to add to</param>
        /// <param name="value">The value to add to the field</param>
        public void Sum(string fieldName, double value)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Computes the partition key for this event (without sanitising it)
        /// </summary>
        public string GetPartitionKey()
        {
            // Just the organization/product name
            return m_Organization + "/" + m_Product;
        }

        /// <summary>
        /// Computes the row key for this event (without sanitising it)
        /// </summary>
        public string GetRowKey()
        {
            StringBuilder keyBuilder = new StringBuilder();

            // Store under the query ID by default
            keyBuilder.Append(m_QueryId);

            // ... then under the bucket
            foreach (var bucket in m_Bucket)
            {
                keyBuilder.Append('/');
                keyBuilder.Append(bucket);
            }

            return keyBuilder.ToString();
        }

        /// <summary>
        /// Updates the values in a particular partion
        /// </summary>
        /// <param name="partitionKey">The (unsanitised) partition key</param>
        public async Task StoreInPartition(string partitionKey)
        {
            // Generate sanitised partition key
            var sanitisedPartitionKey = StorageUtil.SanitiseKey(partitionKey);

            // Retrieve the base of the bucket keys
            StringBuilder bucketBaseBuilder = new StringBuilder();
            foreach (var bucketPart in m_Bucket)
            {
                // Bucket items are separated by '*' characters
                bucketBaseBuilder.Append('*');
                bucketBaseBuilder.Append(StorageUtil.SanitiseKey(bucketPart));
            }
            string bucketBase = bucketBaseBuilder.ToString();

            // Update (or create) any count entities (these just contain the number of times this particular entity was encountered)
            foreach (var countField in m_CountFields)
            {
                // Get the row key for this field
                StringBuilder rowKeyBuilder = new StringBuilder(bucketBase);
                rowKeyBuilder.Append("**");
                rowKeyBuilder.Append(StorageUtil.SanitiseKey(countField));

                // Convert to a string
                var sanitisedRowKey = rowKeyBuilder.ToString();

                // Attempt to retrieve the row
                var retrieveOperation   = TableOperation.Retrieve<CountFieldEntity>(sanitisedPartitionKey, sanitisedRowKey);
                var retrieveRowResult   = await m_Table.ExecuteAsync(retrieveOperation);
                var existing            = retrieveRowResult.Result as CountFieldEntity;

                if (existing == null)
                {
                    // Create a new row
                    var newRow = new CountFieldEntity();

                    newRow.PartitionKey = sanitisedPartitionKey;
                    newRow.RowKey       = sanitisedRowKey;
                    newRow.Count        = 1;

                    await m_Table.ExecuteAsync(TableOperation.Insert(newRow));
                }
                else
                {
                    // Update an existing row
                    var toUpdate = existing;

                    ++toUpdate.Count;

                    await m_Table.ExecuteAsync(TableOperation.Replace(toUpdate));
                }

                // TODO: retry if another process updates the same entity
            }

            // TODO: unique counts need to work a little differently
        }

        /// <summary>
        /// Stores this event in the data store
        /// </summary>
        public async Task Store()
        {
            // Nothing to do if the event has been dropped
            if (m_Dropped) return;

            // TODO: summarise for the user as well as for the product

            // Summarise for the product
            await StoreInPartition(GetPartitionKey());
        }
    }
}
