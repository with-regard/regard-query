namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that can build a query against a database
    /// </summary>
    public interface IQueryBuilder
    {
        /// <summary>
        /// Creates a query that counts all the events in the source
        /// </summary>
        /// <remarks>
        /// This is the basic query type: it retrieves all of the source event and aggregates them using the default
        /// aggregation operator (count)
        /// </remarks>
        IRegardQuery AllEvents();

        /// <summary>
        /// Creates a query that takes the result of an existing query and removes any record that either does not contain the field, or that contains the field but
        /// it is not the specified value.
        /// </summary>
        /// <param name="query">The query that needs to be restricted</param>
        /// <param name="key">The name of the field to test against</param>
        /// <param name="value">The value that the field must have in all the returned events</param>
        IRegardQuery Only(IRegardQuery query, string key, string value);                // TODO: I think a final version of this will need to do ranges and other comparisons rather than straight-up matches

        /// <summary>
        /// Creates a query that splits the results into partitions by the value of a key. Records that do not contain that field will not be included in the results.
        /// </summary>
        /// <param name="query">The query that should be split</param>
        /// <param name="key">The key that this should break the results down using</param>
        /// <param name="name">The name to assign to the result</param>
        IRegardQuery BrokenDownBy(IRegardQuery query, string key, string name);                      // TODO: I think a final version of this will need to do ranges and other comparisons

        /// <summary>
        /// Creates an index that can be used to retrieve a sub-query
        /// </summary>
        /// <param name="query">The query to index</param>
        /// <param name="key">The key to index by</param>
        /// <param name="name">The name of the field where the indexed value is stored</param>
        /// <remarks>
        /// It's possible that the name of this method needs to be changed to something more sensible.
        /// <para/>
        /// Indexing has no effect on the top-level results, but makes it possible to break them down further. For example, indexing
        /// by user ID makes it possible to ask the query engine to return the results for a single user ID as well as for the
        /// entire data set.
        /// </remarks>
        IRegardQuery IndexedBy(IRegardQuery query, string key, string name);

        /// <summary>
        /// Given a key that exists in the database, sums the total of all its values (in each partition if there is more than one)
        /// </summary>
        /// <param name="query">The query to add a new sum to</param>
        /// <param name="key">The key to sum</param>
        /// <param name="name">The name to assign to the result</param>
        IRegardQuery Sum(IRegardQuery query, string key, string name);

        /// <summary>
        /// Finds the average (mean) of a paticular field (parsed as a double) across all of the events matched by a query
        /// </summary>
        /// <param name="query">The query to add a new mean to</param>
        /// <param name="key">The field to average</param>
        /// <param name="name">The name to assign to the result</param>
        IRegardQuery Mean(IRegardQuery query, string key, string name);

        /// <summary>
        /// Finds the minimum value of a particular field (parsed as a double)
        /// </summary>
        IRegardQuery Min(IRegardQuery query, string key, string name);

        /// <summary>
        /// Finds the maximum value of a particular field (parsed as a double)
        /// </summary>
        IRegardQuery Max(IRegardQuery query, string key, string name);

        /// <summary>
        /// Creates a query that counts the number of unique values of a particular key
        /// </summary>
        /// <param name="query">The query to perform counting in</param>
        /// <param name="key">The key to count</param>
        /// <param name="name">The name to assign to the result</param>
        /// <returns>A query that counts the number of unique values in the specified key (in each partition if there is more than one)</returns>
        IRegardQuery CountUniqueValues(IRegardQuery query, string key, string name);
    }
}
