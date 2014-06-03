﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.MapReduce.DataAccessor;
using Regard.Query.Serializable;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Represents a product that can be queried using the map/reduce system
    /// </summary>
    class QueryableProduct : IQueryableProduct, IUserAdmin
    {
        private readonly object m_Sync = new object();

        private readonly IndividualProductDataStore m_ProductDataStore;
        private readonly QueryDataStore m_QueryDataStore;
        private readonly UserDataStore m_UserDataStore;

        /// <summary>
        /// List of queries that are in the process of being updated
        /// </summary>
        private readonly Dictionary<string, Task> m_UpdatingQueries = new Dictionary<string, Task>();

        /// <summary>
        /// The name of the node that this query represents
        /// </summary>
        private readonly string m_NodeName;

        public QueryableProduct(IndividualProductDataStore productDataStore, string nodeName)
        {
            if (productDataStore == null) throw new ArgumentNullException("productDataStore");

            m_NodeName          = nodeName;
            m_ProductDataStore  = productDataStore;
            m_UserDataStore     = m_ProductDataStore.Users;
            m_QueryDataStore    = m_ProductDataStore.Queries;
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
            // This counts as an update as far as the query is concerned
            var updateComplete = new TaskCompletionSource<bool>();
            Task activeUpdate = null;

            do
            {
                // Look for an existing update
                lock (m_Sync)
                {
                    if (!m_UpdatingQueries.TryGetValue(queryName, out activeUpdate))
                    {
                        // This becomes the active update
                        activeUpdate = null;

                        // This registration becomes the active update for this query once it starts
                        m_UpdatingQueries[queryName] = updateComplete.Task;
                    }
                }

                // If there was an update pending, wait for it to complete before registering the new query
                if (activeUpdate != null)
                {
                    await activeUpdate;
                }
            } while (activeUpdate != null);

            try
            {
                var serializable = query as SerializableQuery;
                if (serializable == null) throw new ArgumentException("Invalid query object", "query");

                // Ensure that the data is up to date for this node
                await m_ProductDataStore.Commit(m_NodeName);

                // Generate data for this particular query
                JObject queryData = new JObject();

                // The 'when' field is used in case two queries wind up with the same name but created on separate nodes
                queryData["When"] = (DateTime.UtcNow - DateTime.MinValue).TotalSeconds;
                queryData["Query"] = serializable.ToJson();

                // The queries are all stored in a single data object
                // There is one data object per node to support scaling (no node needs to worry about overwriting another's data)
                JObject existingQuery = await m_QueryDataStore.GetQueriesRegisteredByNode(m_NodeName);
                JObject queryList;

                if (existingQuery == null)
                {
                    existingQuery = new JObject();
                    queryList = new JObject();
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
                await m_ProductDataStore.DeleteQueryResults(queryName, m_NodeName);

                // Store this item
                existingQuery["Queries"] = queryList;
                await m_QueryDataStore.SetRegisteredQueries(m_NodeName, existingQuery);
            }
            finally
            {
                lock (m_Sync)
                {
                    // This is no longer updating the query
                    m_UpdatingQueries.Remove(queryName);
                }

                // Wake up any threads waiting for this registration to complete
                updateComplete.SetResult(true);
            }
        }

        /// <summary>
        /// Causes the results for a query to be updated
        /// </summary>
        /// <param name="queryName">The name of the query that should be updated</param>
        /// <param name="queryDefinition">The definition for the query as retrieved from the database</param>
        private async Task UpdateQuery(string queryName, JObject queryDefinition)
        {
            await m_ProductDataStore.Commit(m_NodeName);

            var updateComplete = new TaskCompletionSource<bool>();
            Task activeUpdate;

            lock (m_Sync)
            {
                if (!m_UpdatingQueries.TryGetValue(queryName, out activeUpdate))
                {
                    // This becomes the active update
                    activeUpdate = null;

                    // Other threads should just wait for this update to complete
                    m_UpdatingQueries[queryName] = updateComplete.Task;
                }
            }

            // If another thread is updating the query, just wait for it to finish
            if (activeUpdate != null)
            {
                await activeUpdate;
                return;
            }

            try
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
                var ingestor = m_ProductDataStore.CreateIngestorForQuery(queryName, m_NodeName, mapReduce);

                // Get the status of this query
                var previousQueryStatus = await m_ProductDataStore.GetQueryStatus(queryName, m_NodeName);

                if (previousQueryStatus == null)
                {
                    previousQueryStatus = JObject.FromObject(new {LastProcessedIndex = -1});
                }

                // Get the last processed index for this query
                var lastProcessedIndex = previousQueryStatus["LastProcessedIndex"].Value<long>();

                // Ingest any data that has arrived since the query was last processed
                // TODO: we want to ingest data for all nodes, not just this one. Right now we only need one node, so this is a stop-gap measure
                var eventStore = m_ProductDataStore.GetRawEventStore(m_NodeName);
                var dataSinceLastQuery = eventStore.EnumerateValuesAppendedSince(lastProcessedIndex);
                var newLastProcessedIndex = lastProcessedIndex;

                for (var newEvent = await dataSinceLastQuery.FetchNext();
                    newEvent != null;
                    newEvent = await dataSinceLastQuery.FetchNext())
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
                    await m_ProductDataStore.SetQueryStatus(queryName, m_NodeName, updatedQueryStatus);
                }
            }
            finally
            {
                lock (m_Sync)
                {
                    // We're no longer the active update
                    m_UpdatingQueries.Remove(queryName);
                }

                // Wake up any threads that might have been waiting on this update
                updateComplete.SetResult(true);
            }
        }

        /// <summary>
        /// Runs the query with the specified name against the database
        /// </summary>
        public async Task<IResultEnumerator<QueryResultLine>> RunQuery(string queryName)
        {
            // Check if the query exists
            // TODO: performance would be improved considerably by some sort of caching scheme
            JObject queryDefinition = await m_QueryDataStore.GetJsonQueryDefinition(queryName);

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
            IKeyValueStore results = m_ProductDataStore.GetQueryResults(queryName, m_NodeName);

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

            await m_UserDataStore.SetUserData(userId, userData);
        }

        /// <summary>
        /// Retrieves all of the raw events associated with a particular user ID
        /// </summary>
        /// <remarks>
        /// This is intended to support the user page: the results of running this query are meant to be displayed only to that user.
        /// <para/>
        /// One thought about a future version is that we might only want to store aggregate data, which would make this call redundant as
        /// we would no longer store data for a specific user.
        /// </remarks>
        public async Task<IResultEnumerator<JObject>> RetrieveEventsForUser(Guid userId)
        {
            return new KvObjectEnumerator(m_ProductDataStore.GetEventEnumeratorForUser(userId));
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

            await m_UserDataStore.SetUserData(userId, userData);
        }
    }
}
