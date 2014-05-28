using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Regard.Query.Flat
{
    /// <summary>
    /// Class that batches up operations intended to update records in Azure flat tables
    /// </summary>
    /// <remarks>
    /// Azure flat tables are quite slow at individual operations but can be made to run much faster by running operations in batches of 100 or so
    /// </remarks>
    public class AzureTableBatch<TEntity> where TEntity : TableEntity
    {
        /// <summary>
        /// Synchronisation object
        /// </summary>
        private readonly object m_Sync = new object();

        /// <summary>
        /// The entities that are waiting to be updated
        /// </summary>
        private Dictionary<EntityKey, AzureTableBatchOperation<TEntity>> m_WaitingEntities = new Dictionary<EntityKey, AzureTableBatchOperation<TEntity>>();

        /// <summary>
        /// Prepares an update task on a particular entity. Many update tasks can be queued on a single entity if necessary.
        /// </summary>
        /// <param name="partitionKey">The parition key of the entity to update</param>
        /// <param name="rowKey">The row key of the entity to update</param>
        /// <param name="updateAction">The action to perform the update on the entity</param>
        /// <param name="commitAction">The action to p</param>
        public void UpdateOrCreate(string partitionKey, string rowKey, Action<TEntity> updateAction, Action commitAction = null)
        {
            lock (m_Sync)
            {
                AzureTableBatchOperation<TEntity> operation;
                var entityKey = new EntityKey(partitionKey, rowKey);

                // Get the existing operation, or create a new one
                if (!m_WaitingEntities.TryGetValue(entityKey, out operation))
                {
                    operation = new AzureTableBatchOperation<TEntity>(partitionKey, rowKey);
                    m_WaitingEntities[entityKey] = operation;
                }

                // Change the update and commit action action
                if (updateAction != null)
                {
                    operation.UpdateAction += updateAction;
                }

                if (commitAction != null)
                {
                    operation.CommitAction += commitAction;
                }
            }
        }

        /// <summary>
        /// Updates or inserts rows in a target table
        /// </summary>
        /// <param name="target">The table to be updated</param>
        public async Task RunBatch(CloudTable target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            // Grab the actions, and replace the table with an empty one so we can begin repopulating immediately
            Dictionary<EntityKey, AzureTableBatchOperation<TEntity>> actions;

            lock (m_Sync)
            {
                actions = m_WaitingEntities;
                m_WaitingEntities = new Dictionary<EntityKey, AzureTableBatchOperation<TEntity>>();
            }

            // Nothing to do if there are no actions waiting
            if (actions.Count == 0)
            {
                return;
            }

            // Perform a batch operation to retrieve the records
            TableBatchOperation retrieveBatch = new TableBatchOperation();

            foreach (var entity in actions.Keys)
            {
                retrieveBatch.Retrieve<TEntity>(entity.PartitionKey, entity.RowKey);
            }

            await target.ExecuteBatchAsync(retrieveBatch);
        }
    }
}
