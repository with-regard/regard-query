using System.Collections;
using System.Collections.Generic;
using Regard.Query.Api;

namespace Regard.Query.WebAPI
{
    /// <summary>
    /// Model object representing the response to 
    /// </summary>
    class QueryResponseModel
    {
        /// <summary>
        /// The results for this query
        /// </summary>
        public IEnumerable<QueryResultLine> Results { get; set; }
    }
}
