using System;
using Regard.Query.Api;

namespace Regard.Query.Serializable
{
    /// <summary>
    /// A query builder that can produce a serializable query (which has a well-known format that can be read by code and can be replayed to 
    /// </summary>
    public class SerializableQueryBuilder : IQueryBuilder
    {
        /// <summary>
        /// Creates a new serializable query builder
        /// </summary>
        /// <param name="targetQueryBuilder">
        /// A query builder that can be used to create a 'live' query, or null if the query should produce no results
        /// </param>
        public SerializableQueryBuilder(IQueryBuilder targetQueryBuilder)
        {
            TargetQueryBuilder = targetQueryBuilder;
        }

        /// <summary>
        /// Returns the query builder used to generate the 'live' queries. This can be null, in which case this should 
        /// produce no data.
        /// </summary>
        public IQueryBuilder TargetQueryBuilder { get; private set; }

        /// <summary>
        /// Creates a query that counts all the events in the source
        /// </summary>
        /// <remarks>
        /// This is the basic query type: it retrieves all of the source event and aggregates them using the default
        /// aggregation operator (count)
        /// </remarks>
        public SerializableQuery AllEvents()
        {
            return new SerializableQuery(this) { Verb = QueryVerbs.AllEvents };
        }

        /// <summary>
        /// Creates a query that takes the result of an existing query and removes any field that doesn't have the specified value
        /// </summary>
        /// <param name="query">The query that needs to be restricted</param>
        /// <param name="key">The key to test against</param>
        /// <param name="value">The value that the key must have in all the returned events</param>
        public SerializableQuery Only(SerializableQuery query, string key, string value)
        {
            return new SerializableQuery(this) { AppliesTo = query, Verb = QueryVerbs.Only, Key = key, Value = value };
        }

        /// <summary>
        /// Creates a query that splits the results into partitions by the value of a key
        /// </summary>
        /// <param name="query">The query that should be split</param>
        /// <param name="key">The key that this should break the results down using</param>
        /// <param name="name">The name to assign to the result</param>
        public SerializableQuery BrokenDownBy(SerializableQuery query, string key, string name)
        {
            return new SerializableQuery(this) { AppliesTo = query, Verb = QueryVerbs.BrokenDownBy, Key = key, Name = name };
        }

        /// <summary>
        /// Given a key that exists in the database, sums the total of all its values (in each partition if there is more than one)
        /// </summary>
        /// <param name="query">The query to add a new sum to</param>
        /// <param name="key">The key to sum</param>
        /// <param name="name">The name to assign to the result</param>
        public SerializableQuery Sum(SerializableQuery query, string key, string name)
        {
            return new SerializableQuery(this) { AppliesTo = query, Verb = QueryVerbs.Sum, Key = key, Name = name };
        }

        /// <summary>
        /// Creates a query that counts the number of unique values of a particular key
        /// </summary>
        /// <param name="query">The query to perform counting in</param>
        /// <param name="key">The key to count</param>
        /// <param name="name">The name to assign to the result</param>
        /// <returns>A query that counts the number of unique values in the specified key (in each partition if there is more than one)</returns>
        public SerializableQuery CountUniqueValues(SerializableQuery query, string key, string name)
        {
            return new SerializableQuery(this) { AppliesTo = query, Verb = QueryVerbs.CountUniqueValues, Key = key, Name = name };
        }

        /// <summary>
        /// Finds the average (mean) of a paticular field across all of the events matched by a particuila
        /// </summary>
        /// <param name="query">The query to add a new mean to</param>
        /// <param name="key">The field to average</param>
        /// <param name="name">The name to assign to the result</param>
        public SerializableQuery Mean(SerializableQuery query, string key, string name)
        {
            return new SerializableQuery(this) { AppliesTo = query, Verb = QueryVerbs.Mean, Key = key, Name = name };
        }

        public SerializableQuery Min(SerializableQuery query, string key, string name)
        {
            throw new NotImplementedException();
        }

        public SerializableQuery Max(SerializableQuery query, string key, string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a query that counts all the events in the source
        /// </summary>
        /// <remarks>
        /// This is the basic query type: it retrieves all of the source event and aggregates them using the default
        /// aggregation operator (count)
        /// </remarks>
        IRegardQuery IQueryBuilder.AllEvents()
        {
            return AllEvents();
        }

        public IRegardQuery Only(IRegardQuery query, string key, string value)
        {
            return Only((SerializableQuery) query, key, value);
        }

        IRegardQuery IQueryBuilder.BrokenDownBy(IRegardQuery query, string key, string name)
        {
            return BrokenDownBy((SerializableQuery) query, key, name);
        }

        IRegardQuery IQueryBuilder.Sum(IRegardQuery query, string key, string name)
        {
            return Sum((SerializableQuery) query , key, name);
        }

        IRegardQuery IQueryBuilder.CountUniqueValues(IRegardQuery query, string key, string name)
        {
            return CountUniqueValues((SerializableQuery) query, key, name);
        }

        public IRegardQuery Mean(IRegardQuery query, string key, string name)
        {
            return Mean((SerializableQuery) query, key, name);
        }

        public IRegardQuery Min(IRegardQuery query, string key, string name)
        {
            return Min((SerializableQuery) query, key, name);
        }

        public IRegardQuery Max(IRegardQuery query, string key, string name)
        {
            return Max((SerializableQuery) query, key, name);
        }
    }
}
