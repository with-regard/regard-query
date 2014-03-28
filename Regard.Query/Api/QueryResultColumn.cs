namespace Regard.Query.Api
{
    /// <summary>
    /// Data storage class representing a result stored in a column
    /// </summary>
    public class QueryResultColumn
    {
        /// <summary>
        /// The name for this column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The raw string value for this column
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// If this column represents a count of values, this is that count
        /// </summary>
        public long Count { get; set; }
    }
}
