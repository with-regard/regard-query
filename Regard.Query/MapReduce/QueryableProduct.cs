﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Represents a product that can be queried using the map/reduce system
    /// </summary>
    class QueryableProduct : IQueryableProduct, IUserAdmin
    {
        /// <summary>
        /// The key/value store dedicated to this product
        /// </summary>
        private readonly IKeyValueStore m_ProductDataStore;

        /// <summary>
        /// The data store where information about users is stored
        /// </summary>
        private readonly IKeyValueStore m_UserDataStore;

        /// <summary>
        /// The data store where information about queries is stored
        /// </summary>
        private readonly IKeyValueStore m_QueryDataStore;

        /// <summary>
        /// The name of the node that this query represents
        /// </summary>
        private readonly string m_NodeName;

        public QueryableProduct(IKeyValueStore productDataStore, string nodeName)
        {
            if (productDataStore == null) throw new ArgumentNullException("productDataStore");

            m_NodeName          = nodeName;
            m_ProductDataStore  = productDataStore;
            m_UserDataStore     = m_ProductDataStore.ChildStore(new JArray("users"));
            m_QueryDataStore    = m_ProductDataStore.ChildStore(new JArray("queries"));
        }

        /// <summary>
        /// Creates a new query builder for this product
        /// </summary>
        public IQueryBuilder CreateQueryBuilder()
        {
            return new SerializableQueryBuilder(null);
        }

        /// <summary>
        /// Registers a query built by the query builder as active for this product
        /// </summary>
        /// <param name="queryName">A name for this query. If the query already exists then </param>
        /// <param name="query">A query generated by the query builder for this product (ie, the query builder created by <see cref="CreateQueryBuilder"></see>)</param>
        public async Task RegisterQuery(string queryName, IRegardQuery query)
        {
            var serializable = query as SerializableQuery;
            if (serializable == null) throw new ArgumentException("Invalid query object", "query");

            // Each running node should have a unique name
            JArray nodeQueryKey = new JArray(m_NodeName);

            // Generate data for this particular query
            JObject queryData = new JObject();

            // The 'when' field is used in case two queries wind up with the same name but created on separate nodes
            queryData["When"]   = (DateTime.UtcNow - DateTime.MinValue).TotalSeconds;
            queryData["Query"]  = serializable.ToJson();

            // The queries are all stored in a single data object
            // There is one data object per node to support scaling (no node needs to worry about overwriting another's data)
            JObject existingQuery = await m_QueryDataStore.GetValue(nodeQueryKey);
            JObject queryList;

            if (existingQuery == null)
            {
                existingQuery   = new JObject();
                queryList       = new JObject();
            }
            else
            {
                JToken queryListToken;
                if (existingQuery.TryGetValue("Queries", out queryListToken))
                {
                    queryList = queryListToken.Value<JObject>();
                }
                else
                {
                    queryList = new JObject();
                }
            }

            // Add to this query
            queryList[queryName] = queryData;

            // Erase any existing query data
            await m_ProductDataStore.DeleteChildStore(new JArray("query-results", queryName));

            // Store this item
            existingQuery["Queries"] = queryList;
            await m_QueryDataStore.SetValue(nodeQueryKey, existingQuery);
        }

        /// <summary>
        /// Causes the results for a query to be updated
        /// </summary>
        /// <param name="queryName">The name of the query that should be updated</param>
        /// <param name="queryDefinition"></param>
        private async Task UpdateQuery(string queryName, JObject queryDefinition)
        {
            // Get the query definition
            var queryJson = queryDefinition["Query"].Value<JObject>();

            // Turn into a map/reduce querty
            var mapReduce = MapReduceQueryFactory.GenerateMapReduce(queryJson);

            // Create an ingestor for this query
            // Update on a per-node basis
            //
            // One disadvantage of this technique is that multiple nodes need to run the query multiple times; you can't process some events on some nodes
            // and then aggregate the results later on. This is a scaling issue so it is not critical at this time.
            //
            // To fix this issue, we could send updated results around the nodes using a service bus
            var thisQueryStore  = m_ProductDataStore.ChildStore(new JArray("query-results", queryName, m_NodeName));
            var ingestor        = new DataIngestor(mapReduce, thisQueryStore);

            // Get the status of this query
            var queryStatusStore = m_ProductDataStore.ChildStore(new JArray("query-status", m_NodeName));
            var previousQueryStatus = await queryStatusStore.GetValue(new JArray(queryName));

            if (previousQueryStatus == null)
            {
                previousQueryStatus = JObject.FromObject(new {LastProcessedIndex = -1});
            }

            // Get the last processed index for this query
            var lastProcessedIndex = previousQueryStatus["LastProcessedIndex"].Value<long>();

            // Ingest any data that has arrived since the query was last processed
            // TODO: we want to ingest data for all nodes, not just this one. Right now we only need one node, so this is a stop-gap measure
            var eventStore              = m_ProductDataStore.ChildStore(new JArray("raw-events", m_NodeName));
            var dataSinceLastQuery      = eventStore.EnumerateValuesAppendedSince(lastProcessedIndex);
            var newLastProcessedIndex   = lastProcessedIndex;

            for (var newEvent = await dataSinceLastQuery.FetchNext(); newEvent != null; newEvent = await dataSinceLastQuery.FetchNext())
            {
                ingestor.Ingest(newEvent.Item2);

                // Make a note of the most recently processed event
                var thisIndex = newEvent.Item1[0].Value<long>();
                if (thisIndex > newLastProcessedIndex)
                {
                    newLastProcessedIndex = thisIndex;
                }
            }
            await ingestor.Commit();

            // Update the query status
            if (newLastProcessedIndex != lastProcessedIndex)
            {
                var updatedQueryStatus = JObject.FromObject(new {LastProcessedIndex = newLastProcessedIndex});
                await queryStatusStore.SetValue(new JArray(queryName), updatedQueryStatus);
            }
        }

        /// <summary>
        /// Runs the query with the specified name against the database
        /// </summary>
        public async Task<IResultEnumerator<QueryResultLine>> RunQuery(string queryName)
        {
            // Check if the query exists
            // TODO: performance would be improved considerably by some sort of caching scheme
            var projectQueries = m_QueryDataStore.EnumerateAllValues();
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

            // Result is null if no node has created this query
            // The KV data store is actually forgiving enough that we could return a result instead here
            if (queryDefinition == null)
            {
                return null;
            }

            // Ensure that the query results are up to date
            await UpdateQuery(queryName, queryDefinition);

            // Return the results
            // Currently, this will process the data for this node only (the initial version of the product only has a single consumer node so this is fine)
            // TODO: handle other nodes

            // The event recorder runs the query and puts the results in a child store
            var results         = m_ProductDataStore.ChildStore(new JArray("query-results", queryName, m_NodeName));

            // Fetch the entire set of results from the query
            var nodeEnumerator  = results.EnumerateAllValues();

            return new QueryResultEnumerator(nodeEnumerator);
        }

        // TODO: factor user admin functions out into a separate class

        /// <summary>
        /// Retrieves the object that can administer the users of this project
        /// </summary>
        public IUserAdmin Users { get { return this; } }

        /// <summary>
        /// Marks a specific user ID as being opted in to data collection for a specific product
        /// </summary>
        public async Task OptIn(Guid userId)
        {
            JObject userData = new JObject();

            userData["OptInState"] = "opt-in";

            await m_UserDataStore.SetValue(new JArray(userId.ToString()), userData);
        }

        /// <summary>
        /// Marks a specific user ID as being opted out from data collection for a specific product
        /// </summary>
        /// <remarks>
        /// This only opts out for future data collection. Any existing data will be retained.
        /// </remarks>
        public async Task OptOut(Guid userId)
        {
            JObject userData = new JObject();

            userData["OptInState"] = "opt-out";

            await m_UserDataStore.SetValue(new JArray(userId.ToString()), userData);
        }
    }
}
