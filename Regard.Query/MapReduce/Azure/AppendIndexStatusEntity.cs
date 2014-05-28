using Microsoft.WindowsAzure.Storage.Table;

namespace Regard.Query.MapReduce.Azure
{
    /// <summary>
    /// Table entity used to store the most recently used index value used for appending to the table
    /// </summary>
    public class AppendIndexStatusEntity : TableEntity
    {
        public long LastAppendIndex { get; set; }
    }
}
