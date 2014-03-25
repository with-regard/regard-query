﻿using System.Collections;
using System.Collections.Generic;

namespace Regard.Query.Sql
{
    /// <summary>
    /// A SQL query consists of zero or more 'elements'.
    /// <para/>
    /// Each element generates a new inner join with the properties table (except the first)
    /// Each element may introduce a new part of a WHERE clause.
    /// Each element may introduce a new part of the GROUP BY clause.
    /// Each element may introduce a new summarisation function.
    /// WHERE and GROUP BY are omitted if no elements specify them.
    /// If there are no summarisation functions, then COUNT(DISTINCT ep1.EventId) is used
    /// </summary>
    class SqlQueryElement
    {
        /// <summary>
        /// Empty, or the Where items generated by this element. In this first version, these are all ANDed together.
        /// </summary>
        public IEnumerable<SqlQueryWhere> Wheres { get; set; }

        /// <summary>
        /// Empty, or the GROUP BY items generated by this element (fields in the event properties tables).
        /// </summary>
        public IEnumerable<string> GroupBy { get; set; }

        /// <summary>
        /// Empty, or the summarisation functions generated by this element
        /// </summary>
        public IEnumerable<SqlQuerySumFun> Summarisation { get; set; }
    }

}