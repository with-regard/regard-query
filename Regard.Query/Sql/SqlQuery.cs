using System;
using System.Collections.Generic;
using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Describes a result we want in the query answer
    /// </summary>
    struct SqlResultDescription
    {
        public SqlResultDescription(string query, string name)
        {
            QueryPart   = query;
            NamePart    = name;
        }

        /// <summary>
        /// The query (in SQL: eg COUNT(DISTINCT property))
        /// </summary>
        public string QueryPart;

        /// <summary>
        /// The name it should appear as 
        /// </summary>
        public string NamePart;
    }

    /// <summary>
    /// Describes a filter to add to the query
    /// </summary>
    struct SqlFilterDescription
    {
        /// <summary>
        /// The name of the property to filter on
        /// </summary>
        public string PropertyName;

        /// <summary>
        /// The value that the property must be equal to in order to be included in the results
        /// </summary>
        public string PropertyValue;
    }

    /// <summary>
    /// Representation of a query run against a SQL database
    /// </summary>
    class SqlQuery : IRegardQuery
    {
        /// <summary>
        /// The computed results (a list of name/query part pairs)
        /// </summary>
        private readonly List<SqlResultDescription> m_Results = new List<SqlResultDescription>(); 

        /// <summary>
        /// Things to break down the query by
        /// </summary>
        private readonly List<string> m_GroupBy = new List<string>();

        /// <summary>
        /// The list of queries to perform
        /// </summary>
        private readonly List<SqlFilterDescription> m_Query = new List<SqlFilterDescription>(); 

        public SqlQuery()
        {
            // Default result is everything in a field called 'count'
            m_Results.Add(new SqlResultDescription("count", "Count(*)"));
        }

        /// <summary>
        /// The object that built this query (and which can be used to refine it)
        /// </summary>
        public IQueryBuilder Builder
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Generates the SQL query
        /// </summary>
        public string GenerateQuery()
        {
            throw new NotImplementedException();
        }
    }
}
