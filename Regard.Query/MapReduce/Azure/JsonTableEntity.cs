using Microsoft.WindowsAzure.Storage.Table;

namespace Regard.Query.MapReduce.Azure
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
}