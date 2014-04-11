using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Regard.Query.Flat
{
    /// <summary>
    /// Represents a single batch operation on a table (updating a single row)
    /// </summary>
    class AzureTableBatchOperation<TEntity> where TEntity : ITableEntity
    {
        /// <summary>
        /// The partition key of this row
        /// </summary>
        private readonly string m_PartitionKey;

        /// <summary>
        /// The row key of this row
        /// </summary>
        private readonly string m_RowKey;

        /// <summary>
        /// The action to take to update the entity
        /// </summary>
        private Action<TEntity> m_UpdateAction;

        /// <summary>
        /// The action to take after the updated entity has been committed to the database
        /// </summary>
        private Action m_CommitAction;

        /// <summary>
        /// Creates a new batch operation for a particular partition/row key
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="rowKey"></param>
        public AzureTableBatchOperation(string partitionKey, string rowKey)
        {
            m_PartitionKey  = partitionKey;
            m_RowKey        = rowKey;
        }

        /// <summary>
        /// The partition key of the item to update
        /// </summary>
        public string PartitionKey
        {
            get { return m_PartitionKey; }
        }

        /// <summary>
        /// The row key of the item to update
        /// </summary>
        public string RowKey
        {
            get { return m_RowKey; }
        }

        /// <summary>
        /// The action used to update the element
        /// </summary>
        public Action<TEntity> UpdateAction
        {
            get { return m_UpdateAction; }
            set { m_UpdateAction = value; }
        }

        /// <summary>
        /// The action to take after the entity has been committed to the database
        /// </summary>
        public Action CommitAction
        {
            get { return m_CommitAction; }
            set { m_CommitAction = value; }
        }
    }
}
