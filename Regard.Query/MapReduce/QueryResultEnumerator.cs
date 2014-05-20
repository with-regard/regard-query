using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Iterates through the results of a query for a single node
    /// </summary>
    internal class QueryResultEnumerator : IResultEnumerator<QueryResultLine>
    {
        private readonly IKvStoreEnumerator m_Enumerator;

        public QueryResultEnumerator(IKvStoreEnumerator nodeEnumerator)
        {
            if (nodeEnumerator == null) throw new ArgumentNullException("nodeEnumerator");

            m_Enumerator = nodeEnumerator;

            // TODO: support multiple nodes (by iterating through the results from all the nodes and then reducing them)
        }

        public void Dispose()
        {
        }

        public async Task<QueryResultLine> FetchNext()
        {
            // Try to fetch the next result from the enumerator
            var nextResult = await m_Enumerator.FetchNext();
            if (nextResult == null)
            {
                return null;
            }

            // Convert from JSON into QueryResultLines
            // (This is kind of dumb as they wind up back as JSON later on; makes more sense for a DB format that isn't JSON inside)

            // Every entry should have a Count element (added by the QueryMapReduce class itself)
            var count   = nextResult.Item2["Count"].Value<long>();
            var columns = new List<QueryResultColumn>();

            foreach (var columnPair in nextResult.Item2)
            {
                //  Ignore the default count column
                if (columnPair.Key == "Count") continue;

                // Get the string value for this column pair
                string stringRepresentation;
                switch (columnPair.Value.Type)
                {
                    case JTokenType.Float:
                        stringRepresentation = columnPair.Value.Value<double>().ToString(CultureInfo.InvariantCulture);
                        break;

                    case JTokenType.Integer:
                        stringRepresentation = columnPair.Value.Value<long>().ToString(CultureInfo.InvariantCulture);
                        break;

                    case JTokenType.String:
                        stringRepresentation = columnPair.Value.Value<string>();
                        break;

                    default:
                        stringRepresentation = columnPair.Value.ToString();
                        break;
                }

                // Turn into a result column
                columns.Add(new QueryResultColumn { Name = columnPair.Key, Value = stringRepresentation });
            }

            // This forms the result line
            return new QueryResultLine(count, columns);
        }
    }
}