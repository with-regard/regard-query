using System;
using System.Collections.Generic;

namespace Regard.Query.Api
{
    /// <summary>
    /// The results stored in a single 'line' of a query result (data storage class)
    /// </summary>
    public class QueryResultLine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public QueryResultLine(long eventCount, IEnumerable<QueryResultColumn> columns)
        {
            if (columns == null) throw new ArgumentNullException("columns");

            EventCount  = eventCount;
            Columns     = columns;
        }

        /// <summary>
        /// The total number of events represented by this line
        /// </summary>
        public long EventCount { get; private set; }

        /// <summary>
        /// The columns that make up this result
        /// </summary>
        public IEnumerable<QueryResultColumn> Columns { get; private set; }
    }
}
