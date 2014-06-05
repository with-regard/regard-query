using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.Azure
{
    /// <summary>
    /// A page from an Azure data store
    /// </summary>
    class SegmentedPage : IKeyValuePage
    {
        private readonly object m_Sync = new object();

        /// <summary>
        /// The table where the query is running
        /// </summary>
        private readonly CloudTable m_Table;

        /// <summary>
        /// The query to execute
        /// </summary>
        private readonly TableQuery<JsonTableEntity> m_Query;

        /// <summary>
        /// The segment to query
        /// </summary>
        private readonly TableContinuationToken m_ContinuationToken;

        /// <summary>
        /// null if we haven't retrieved the table segment, otherwise the last segment we retrieved
        /// </summary>
        private TableQuerySegment<JsonTableEntity> m_RetrievedSegment;

        /// <summary>
        /// Function used to decide if a result is included or excluded from the results, or null if all results should be included
        /// </summary>
        /// <remarks>
        /// Filterfuncs should ideally accept most items; performance will be hurt if a lot of irrelevant records are retrieved by the query
        /// </remarks>
        private readonly Func<JArray, JObject, bool> m_FilterFunc;

        public SegmentedPage(CloudTable table, TableQuery<JsonTableEntity> query, string segmentToken, Func<JArray, JObject, bool> filterFunc)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (query == null) throw new ArgumentNullException("query");

            m_Table             = table;
            m_Query             = query;
            m_FilterFunc        = filterFunc;

            if (segmentToken != null)
            {
                m_ContinuationToken = new TableContinuationToken();
                m_ContinuationToken.ReadXml(XmlReader.Create(new StringReader(segmentToken)));
            }
            else
            {
                m_ContinuationToken = null;
            }
        }

        private async Task<TableQuerySegment<JsonTableEntity>> GetCloudSegment()
        {
            // Return the existing segment if one exists
            lock (m_Sync)
            {
                if (m_RetrievedSegment != null) return m_RetrievedSegment;
            }

            // Retrieve a new segment
            // This might cause multiple requests if multiple threads try to access this page
            var newSegment = await m_Table.ExecuteQuerySegmentedAsync(m_Query, m_ContinuationToken);

            lock (m_Sync)
            {
                // In the event two threads did access the same page, we'll throw away the other result
                if (m_RetrievedSegment != null)
                {
                    return m_RetrievedSegment;
                }

                m_RetrievedSegment = newSegment;
                return newSegment;
            }
        }

        /// <summary>
        /// Retrieves the objects in this page
        /// </summary>
        public async Task<IEnumerable<Tuple<JArray, JObject>>> GetObjects()
        {
            // Get the current segment
            var currentSegment = await GetCloudSegment();

            return SegmentEnumerator(currentSegment);
        }

        /// <summary>
        /// Returns the contents of a segment
        /// </summary>
        private IEnumerable<Tuple<JArray, JObject>> SegmentEnumerator(TableQuerySegment<JsonTableEntity> currentSegment)
        {
            foreach (var nextResult in currentSegment.Results)
            {
                // ??? Not sure why this happens ??? Azure storage bug?
                if (nextResult.SerializedJson == null || nextResult.SerializedKey == null)
                {
                    continue;
                }

                // Parse this result
                var nextResultObj = JObject.Parse(nextResult.SerializedJson);
                var nextResultKey = JArray.Parse(nextResult.SerializedKey);

                // Exlcude it if filtered
                if (m_FilterFunc != null && !m_FilterFunc(nextResultKey, nextResultObj))
                {
                    // This object is filtered by the filterFunc: ignore it and get the next one
                    continue;
                }

                // Return the next object
                yield return new Tuple<JArray, JObject>(nextResultKey, nextResultObj);
            }
        }

        /// <summary>
        /// A token that can be used to retrieve the page that follows this one, or null if this is the last page.
        /// </summary>
        /// <remarks>
        /// Page tokens are persistent, so you can re-run the same enumeration and use the old page token to resume it from where the
        /// previous one left off.
        /// </remarks>
        public async Task<string> GetNextPageToken()
        {
            // Get the continuation token for this page
            var segment = await GetCloudSegment();
            var nextToken = segment.ContinuationToken;

            // Result is null if it doesn't exist
            if (nextToken == null)
            {
                return null;
            }

            // Convert to string (XML, annoyingly)
            // We assume that the token doesn't contain any security info. Things get really annoying if it does, because that makes
            // client-side paging really tricky to implement.
            var tokenXml        = new StringBuilder();
            var xmlStringWriter = new StringWriter(tokenXml);
            var xmlWriter       = XmlWriter.Create(xmlStringWriter);

            nextToken.WriteXml(xmlWriter);

            xmlWriter.Close();
            xmlStringWriter.Close();

            return tokenXml.ToString();
        }
    }
}
