using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.DataAccessor
{
    /// <summary>
    /// Actions for a data store containing queries for an individual product
    /// </summary>
    class QueryDataStore
    {
        private readonly IKeyValueStore m_RawDataStore;

        public QueryDataStore(IKeyValueStore rawDataStore)
        {
            if (rawDataStore == null) throw new ArgumentNullException("rawDataStore");
            m_RawDataStore = rawDataStore;
        }

        /// <summary>
        /// Retrieves the queries registered for a particular node, or null if no queries are registered yet
        /// </summary>
        public Task<JObject> GetQueriesRegisteredByNode(string nodeName)
        {
            return m_RawDataStore.GetValue(new JArray(nodeName));
        }

        /// <summary>
        /// Sets the object represeting the registered queries for a particular node
        /// </summary>
        public async Task SetRegisteredQueries(string nodeName, JObject existingQuery)
        {
            await m_RawDataStore.SetValue(new JArray(nodeName), existingQuery);
        }

        /// <summary>
        /// Retrieves the active JSON definition of a query, or null if the query does not exist
        /// </summary>
        public async Task<JObject> GetJsonQueryDefinition(string queryName)
        {
            var projectQueries = m_RawDataStore.EnumerateAllValues();
            JObject queryDefinition = null;
            for (var query = await projectQueries.FetchNext(); query != null; query = await projectQueries.FetchNext())
            {
                JToken queryListToken;

                // Should contain a 'queries' element with this list of queries in it
                if (query.Item2.TryGetValue("Queries", out queryListToken))
                {
                    JObject queryList = queryListToken.Value<JObject>();

                    // The query exists if we can find the name in this object
                    JToken queryDataToken;
                    if (queryList.TryGetValue(queryName, out queryDataToken))
                    {
                        // TODO: pick the most recent definition of the query from all nodes
                        if (queryDataToken.Type == JTokenType.Object)
                        {
                            queryDefinition = queryDataToken.Value<JObject>();
                            break;
                        }
                    }
                }
            }

            return queryDefinition;
        }

        /// <summary>
        /// Retrieves all the queries for a product, as query names mapped to definitions
        /// </summary>
        public async Task<JObject> GetAllQueries()
        {
            JObject result = new JObject();

            var projectQueries = m_RawDataStore.EnumerateAllValues();
            for (var query = await projectQueries.FetchNext(); query != null; query = await projectQueries.FetchNext())
            {
                JToken queryListToken;

                // Should contain a 'queries' element with this list of queries in it
                if (query.Item2.TryGetValue("Queries", out queryListToken))
                {
                    JObject queryList = queryListToken.Value<JObject>();

                    // The query exists if we can find the name in this object
                    foreach (var singleQuery in queryList)
                    {
                        // TODO: if the query exists in multiple nodes, pick the one that is 'current'
                        result[singleQuery.Key] = singleQuery.Value["Query"];
                    }
                }
            }

            return result;
        }
    }
}
