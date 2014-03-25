using System;
using System.Collections.Generic;
using System.Text;
using Regard.Query.Api;

namespace Regard.Query.Sql
{

    /// <summary>
    /// Representation of a query run against a SQL database
    /// </summary>
    class SqlQuery : IRegardQuery
    {
        /// <summary>
        /// The elements to this query
        /// </summary>
        private readonly List<SqlQueryElement> m_Elements = new List<SqlQueryElement>(); 

        /// <summary>
        /// Creates the 'all events' SQL query
        /// </summary>
        public SqlQuery(IQueryBuilder builder)
        {
            Builder = builder;
        }

        /// <summary>
        /// Creates a SQL query limited by a new element
        /// </summary>
        public SqlQuery(SqlQuery query, SqlQueryElement newElement)
        {
            Builder = query.Builder;
            m_Elements = new List<SqlQueryElement>(query.m_Elements);

            if (newElement != null)
            {
                m_Elements.Add(newElement);
            }
        }

        /// <summary>
        /// The object that built this query (and which can be used to refine it)
        /// </summary>
        public IQueryBuilder Builder
        {
            get; private set;
        }

        /// <summary>
        /// Generates the SQL query
        /// </summary>
        public string GenerateQuery()
        {
            // We build up the 4 parts of the query seperately
            StringBuilder selectPart    = new StringBuilder();
            StringBuilder fromPart      = new StringBuilder();
            StringBuilder wherePart     = new StringBuilder();
            StringBuilder groupPart     = new StringBuilder();

            // We always count the number of events
            selectPart.Append("COUNT(DISTINCT [ep1].EventId)");

            // Each element forms a new inner join
            for (int tableId = 0; tableId < m_Elements.Count; ++tableId)
            {
                // Get the table name (here's why overloading '+' to mean different things is a bad language design descision)
                var     element     = m_Elements[tableId];
                string  tableName   = "ep" + (tableId + 1);

                // Add to the from part
                if (tableId == 0)
                {
                    fromPart.Append("[EventPropertyValues] AS [" + tableName + "]");
                }
                else
                {
                    fromPart.Append("\nINNER JOIN [EventPropertyValues] AS [" + tableName + "] WHERE [ep1].[EventId] = [" + tableName + "].[EventId]");
                }

                // Build up the select part as needed
                if (element.Summarisation != null)
                {
                    foreach (var sum in element.Summarisation)
                    {
                        if (selectPart.Length > 0)
                        {
                            selectPart.Append(", ");
                        }
                        selectPart.Append(sum.ToQuery(tableName));
                    }
                }

                // Then the WHERE part
                if (element.Wheres != null)
                {
                    if (wherePart.Length > 0)
                    {
                        wherePart.Append("\n");
                    }
                    foreach (var where in element.Wheres)
                    {
                        if (wherePart.Length > 0)
                        {
                            wherePart.Append(" AND ");
                        }
                        wherePart.Append(where.ToQuery(tableName));
                    }
                }

                // Finally, the group part
                if (element.GroupBy != null)
                {
                    foreach (var group in element.GroupBy)
                    {
                        if (groupPart.Length > 0)
                        {
                            groupPart.Append(", ");
                        }
                        groupPart.Append("[" + tableName + "].[" + group + "]");
                    }
                }
            }

            // Fill in any blanks that need filling in
            if (fromPart.Length == 0)
            {
                fromPart.Append("[EventPropertyValues] AS ep1");
            }

            // Build up the final query
            StringBuilder finalQuery = new StringBuilder();

            finalQuery.Append("SELECT ");
            finalQuery.Append(selectPart);
            finalQuery.Append('\n');
            finalQuery.Append("FROM ");
            finalQuery.Append(fromPart);
            finalQuery.Append('\n');

            if (wherePart.Length > 0)
            {
                finalQuery.Append("WHERE ");
                finalQuery.Append(wherePart);
                finalQuery.Append('\n');
            }

            if (groupPart.Length > 0)
            {
                finalQuery.Append("GROUP BY ");
                finalQuery.Append(groupPart);
                finalQuery.Append('\n');
            }

            return finalQuery.ToString();
        }
    }
}
