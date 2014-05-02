using System;
using System.Data.SqlClient;
using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Builds queries for SQL server
    /// </summary>
    public class SqlQueryBuilder : IQueryBuilder
    {
        /// <summary>
        /// Creates a new query builder that will query events stored in a SQL server database
        /// </summary>
        /// <param name="connection">The connection to the database to use for queries created by this object</param>
        /// <param name="productId">The product that this query is for</param>
        /// <param name="userId">The ID of the user that this query is being run on behalf of (use <see cref="WellKnownUserIdentifier.ProductDeveloper"></see> for aggregation
        /// queries on behalf of the product developer)</param>
        /// <remarks>
        /// Note that query results for a specific user should only be displayed to that user. This library has no way to enforce this restriction.
        /// </remarks>
        public SqlQueryBuilder(SqlConnection connection, long productId, Guid userId)
        {
            Connection  = connection;
            ProductId   = productId;
            UserId      = userId;
        }

        /// <summary>
        /// The database connection that queries should run on
        /// </summary>
        public SqlConnection Connection { get; private set; }

        /// <summary>
        /// The identifier for the product that is being queried
        /// </summary>
        public long ProductId { get; private set; }

        /// <summary>
        /// The identifier for the user whose data is to be queried
        /// </summary>
        public Guid UserId { get; private set; }

        #region 'Raw' implementation

        /// <summary>
        /// Creates a query that counts all the events in the source
        /// </summary>
        /// <remarks>
        /// This is the basic query type: it retrieves all of the source event and aggregates them using the default
        /// aggregation operator (count)
        /// </remarks>
        public SqlQuery AllEvents()
        {
            return new SqlQuery(this);
        }

        /// <summary>
        /// Creates a query that takes the result of an existing query and removes any field that doesn't have the specified value
        /// </summary>
        /// <param name="query">The query that needs to be restricted</param>
        /// <param name="key">The key to test against</param>
        /// <param name="value">The value that the key must have in all the returned events</param>
        public SqlQuery Only(SqlQuery query, string key, string value)
        {
            // Where key = value
            // ... WHERE table.PropertyName = key AND table.Value = value
            var onlyElement = new SqlQueryElement
                {
                    Wheres = new[]
                             {
                                 new SqlQueryWhere
                                 {
                                     FieldName = "PropertyName",
                                     FieldValue = key
                                 },
                                 new SqlQueryWhere
                                 {
                                     FieldName = "Value",
                                     FieldValue = value
                                 },
                             }
                };

            return new SqlQuery(query, onlyElement);
        }

        /// <summary>
        /// Creates a query that splits the results into partitions by the value of a key
        /// </summary>
        /// <param name="query">The query that should be split</param>
        /// <param name="key">The key that this should break the results down using</param>
        /// <param name="name">The name to assign to the result</param>
        public SqlQuery BrokenDownBy(SqlQuery query, string key, string name)
        {
            // Where we've got the right property + group by its value
            // table.Value ... WHERE table.PropertyName = key ... GROUP BY table.Value
            var brokenDownElement = new SqlQueryElement
                {
                    Summarisation = new []
                        {
                            new SqlQuerySumFun
                            {
                                FieldName = "Value",
                                ResultName = name
                            }
                        },
                    Wheres = new []
                             {
                                 new SqlQueryWhere
                                 {
                                     FieldName = "PropertyName",
                                     FieldValue = key
                                 }
                             },
                    GroupBy = new[] { "Value" }
                };
            
            return new SqlQuery(query, brokenDownElement);
        }

        /// <summary>
        /// Given a key that exists in the database, sums the total of all its values (in each partition if there is more than one)
        /// </summary>
        /// <param name="query">The query to add a new sum to</param>
        /// <param name="key">The key to sum</param>
        /// <param name="name">The name to assign to the result</param>
        public IRegardQuery Sum(SqlQuery query, string key, string name)
        {
            // SELECT SUM(table.Value) ... WHERE table.PropertyName = key
            var countUniqueElement = new SqlQueryElement()
                                     {
                                         Summarisation = new []
                                                         {
                                                             new SqlQuerySumFun
                                                             {
                                                                 Distinct = false,
                                                                 FieldName = "Value",
                                                                 Function = "SUM",
                                                                 ResultName = name
                                                             }
                                                         },
                                        Wheres = new []
                                                 {
                                                     new SqlQueryWhere
                                                     {
                                                         FieldName = "PropertyName",
                                                         FieldValue = key
                                                     }
                                                 }
                                     };

            return new SqlQuery(query, countUniqueElement);
        }

        /// <summary>
        /// Creates a query that counts the number of unique values of a particular key
        /// </summary>
        /// <param name="query">The query to perform counting in</param>
        /// <param name="key">The key to count</param>
        /// <param name="name">The name to assign to the result</param>
        /// <returns>A query that counts the number of unique values in the specified key (in each partition if there is more than one)</returns>
        public IRegardQuery CountUniqueValues(SqlQuery query, string key, string name)
        {
            // SELECT COUNT(DISTINCT table.Value) ... WHERE table.PropertyName = key
            var countUniqueElement = new SqlQueryElement()
                                     {
                                         Summarisation = new []
                                                         {
                                                             new SqlQuerySumFun
                                                             {
                                                                 Distinct = true,
                                                                 FieldName = "Value",
                                                                 Function = "COUNT",
                                                                 ResultName = name
                                                             }
                                                         },
                                        Wheres = new []
                                                 {
                                                     new SqlQueryWhere
                                                     {
                                                         FieldName = "PropertyName",
                                                         FieldValue = key
                                                     }
                                                 }
                                     };

            return new SqlQuery(query, countUniqueElement);
        }

        #endregion

        #region IQueryBuilder implementation

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

        /// <summary>
        /// Creates a query that splits the results into partitions by the value of a key
        /// </summary>
        /// <param name="query">The query that should be split</param>
        /// <param name="key">The key that this should break the results down using</param>
        /// <param name="name">The name to assign to the result</param>
        public IRegardQuery BrokenDownBy(IRegardQuery query, string key, string name)
        {
            return BrokenDownBy((SqlQuery) query, key, name);
        }

        /// <summary>
        /// Creates a query that counts the number of unique values of a particular key
        /// </summary>
        /// <param name="query">The query to perform counting in</param>
        /// <param name="key">The key to count</param>
        /// <param name="name">The name to assign to the result</param>
        /// <returns>A query that counts the number of unique values in the specified key (in each partition if there is more than one)</returns>
        public IRegardQuery CountUniqueValues(IRegardQuery query, string key, string name)
        {
            return CountUniqueValues((SqlQuery) query, key, name);
        }

        /// <summary>
        /// Creates a query that takes the result of an existing query and removes any field that doesn't have the specified value
        /// </summary>
        /// <param name="query">The query that needs to be restricted</param>
        /// <param name="key">The key to test against</param>
        /// <param name="value">The value that the key must have in all the returned events</param>
        public IRegardQuery Only(IRegardQuery query, string key, string value)
        {
            return Only((SqlQuery) query, key, value);
        }

        /// <summary>
        /// Given a key that exists in the database, sums the total of all its values (in each partition if there is more than one)
        /// </summary>
        /// <param name="query">The query to add a new sum to</param>
        /// <param name="key">The key to sum</param>
        /// <param name="name">The name to assign to the result</param>
        public IRegardQuery Sum(IRegardQuery query, string key, string name)
        {
            return Sum((SqlQuery) query, key, name);
        }

        #endregion
    }
}
