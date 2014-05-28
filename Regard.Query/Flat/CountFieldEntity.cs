using Microsoft.WindowsAzure.Storage.Table;

namespace Regard.Query.Flat
{
    /// <summary>
    /// Table entity representing a field containing a count
    /// </summary>
    class CountFieldEntity : TableEntity
    {
        /// <summary>
        /// The number of times this entity has been matched
        /// </summary>
        public long Count { get; set; }
    }
}
