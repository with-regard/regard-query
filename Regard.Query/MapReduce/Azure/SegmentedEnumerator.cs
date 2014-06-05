﻿using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.Azure
{
    /// <summary>
    /// Returns results generated by an Azure segmented query
    /// </summary>
    internal class SegmentedEnumerator : IKvStoreEnumerator
    {
        /// <summary>
        /// The table where the query is running
        /// </summary>
        private readonly CloudTable m_Table;

        /// <summary>
        /// The query to execute
        /// </summary>
        private readonly TableQuery<JsonTableEntity> m_Query;

        /// <summary>
        /// null, or the task that will retrieve the next segment of the query
        /// </summary>
        private Task<TableQuerySegment<JsonTableEntity>> m_NextSegmentTask;

        /// <summary>
        /// null, or the current query segment
        /// </summary>
        private TableQuerySegment<JsonTableEntity> m_CurrentSegment;

        /// <summary>
        /// The index of the next result to return
        /// </summary>
        private int m_NextResultIndex;

        /// <summary>
        /// Function used to decide if a result is included or excluded from the results, or null if all results should be included
        /// </summary>
        /// <remarks>
        /// Filterfuncs should ideally accept most items; performance will be hurt if a lot of irrelevant records are retrieved by the query
        /// </remarks>
        private readonly Func<JArray, JObject, bool> m_FilterFunc;

        public SegmentedEnumerator(CloudTable table, TableQuery<JsonTableEntity> query, Func<JArray, JObject, bool> filterFunc)
        {
            if (table == null) throw new ArgumentNullException("table");
            if (query == null) throw new ArgumentNullException("query");

            m_Table = table;
            m_Query = query;
            m_FilterFunc = filterFunc;

            // Start fetching the initial set of data
            m_NextSegmentTask = m_Table.ExecuteQuerySegmentedAsync(query, null);
        }

        public async Task<Tuple<JArray, JObject>> FetchNext()
        {
            while (m_CurrentSegment == null || m_NextResultIndex >= m_CurrentSegment.Results.Count)
            {
                // We either haven't retrieved a segment, or the current segment is out of date
                if (m_NextSegmentTask == null)
                {
                    // There are no result or no more results
                    return null;
                }

                // Fetch the next segment
                m_CurrentSegment = await m_NextSegmentTask;
                m_NextSegmentTask = null;
                m_NextResultIndex = 0;

                if (m_CurrentSegment == null)
                {
                    // No more segments
                    return null;
                }

                if (m_CurrentSegment.ContinuationToken != null)
                {
                    // Start fetching the next segment
                    m_NextSegmentTask = m_Table.ExecuteQuerySegmentedAsync(m_Query, m_CurrentSegment.ContinuationToken);
                }
                else
                {
                    // There are no results after this segment
                    m_NextSegmentTask = null;
                }
            }

            // Fetch the next result
            // m_NextResultIndex should be valid once we exit the while loop above
            var nextResult = m_CurrentSegment.Results[m_NextResultIndex];

            m_NextResultIndex++;

            // Internal keys begin with '---' and should never be returned by one of these enumerators
            if (nextResult.RowKey.StartsWith(AzureKeyValueStore.InternalKeyPrefix))
            {
                return await FetchNext();
            }

            // Parse the Json/key for this result
            var nextResultObj = JObject.Parse(nextResult.SerializedJson);
            var nextResultKey = JArray.Parse(nextResult.SerializedKey);

            if (m_FilterFunc != null && !m_FilterFunc(nextResultKey, nextResultObj))
            {
                // This object is filtered by the filterFunc: ignore it and get the next one
                return await FetchNext();
            }

            // Return this as the result
            return new Tuple<JArray, JObject>(nextResultKey, nextResultObj);
        }

        /// <summary>
        /// Retrieves a page of objects from the list
        /// </summary>
        /// <param name="pageToken">null to retrieve the first page in the list, otherwise a value returned by IKeyValuePage.NextPageToken</param>
        public async Task<IKeyValuePage> FetchPage(string pageToken)
        {
            // TODO: can probably remove the 'Task' qualifier here...
            return new SegmentedPage(m_Table, m_Query, pageToken, m_FilterFunc);
        }
    }
}
