using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Builds queries for SQL server
    /// </summary>
    class SqlQueryBuilder : IQueryBuilder
    {
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
        public SqlQuery BrokenDownBy(SqlQuery query, string key)
        {
            // Where we've got the right property + group by its value
            // table.Value ... WHERE table.PropertyName = key ... GROUP BY table.Value
            var brokenDownElement = new SqlQueryElement
                {
                    Summarisation = new []
                        {
                            new SqlQuerySumFun
                            {
                                FieldName = "Value"
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
        public SqlQuery Sum(SqlQuery query, string key)
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
                                                                 Function = "SUM"
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
        /// <returns>A query that counts the number of unique values in the specified key (in each partition if there is more than one)</returns>
        public SqlQuery CountUniqueValues(SqlQuery query, string key)
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
                                                                 Function = "COUNT"
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
        public IRegardQuery BrokenDownBy(IRegardQuery query, string key)
        {
            return BrokenDownBy((SqlQuery) query, key);
        }

        /// <summary>
        /// Creates a query that counts the number of unique values of a particular key
        /// </summary>
        /// <param name="query">The query to perform counting in</param>
        /// <param name="key">The key to count</param>
        /// <returns>A query that counts the number of unique values in the specified key (in each partition if there is more than one)</returns>
        public IRegardQuery CountUniqueValues(IRegardQuery query, string key)
        {
            return CountUniqueValues((SqlQuery) query, key);
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
        public IRegardQuery Sum(IRegardQuery query, string key)
        {
            return Sum((SqlQuery) query, key);
        }

        #endregion
    }
}
