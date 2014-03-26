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
        string Name { get; set; }

        /// <summary>
        /// The raw string value for this column
        /// </summary>
        string Value { get; set; }

        /// <summary>
        /// If this column represents a count of values, this is that count
        /// </summary>
        long Count { get; set; }
    }
}
