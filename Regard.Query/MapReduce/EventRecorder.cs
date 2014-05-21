using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Event recorder for the map/reduce query store
    /// </summary>
    internal class EventRecorder : IEventRecorder
    {
        /// <summary>
        /// The root data store where this will store its events
        /// </summary>
        private readonly IKeyValueStore m_RootDataStore;

        /// <summary>
        /// The name of this node
        /// </summary>
        private readonly string m_NodeName;

        /// <summary>
        /// Data store used to store information about active sessions
        /// </summary>
        /// <remarks>
        /// Session stores are shared across all instances: we assume that any given session will just be redefined the same way
        /// </remarks>
        private readonly IKeyValueStore m_SessionDataStore;

        /// <summary>
        /// The data store used for raw events (prior to any map/reduce operations being performed on it). Also the home of the 'last recorded' event ID, important if
        /// we need to replay events.
        /// </summary>
        private readonly IKeyValueStore m_EventDataStore;

        public EventRecorder(IKeyValueStore rootDataStore, string nodeName)
        {
            m_RootDataStore = rootDataStore;
            m_NodeName      = nodeName;

            // Sessions are stored in a data store shared across all nodes
            m_SessionDataStore = m_RootDataStore.ChildStore(new JArray("sessions"));

            // Raw events are stored only for this node, to avoid ingestion nodes needing to lock against one another
            m_EventDataStore = m_RootDataStore.ChildStore(new JArray("raw-events", nodeName));
        }

        /// <summary>
        /// Indicates that a new session has begun
        /// </summary>
        /// <param name="organization">The name of the organization that the session is for</param>
        /// <param name="product">The name of the product that the session is for</param>
        /// <param name="userId">A GUID that identifies the user that this session is for</param>
        /// <param name="sessionId">Should be Guid.Empty to indicate that the call should generate a session ID, otherwise it should be a session ID that has not been used before</param>
        /// <returns>A GUID that identifies this session, or Guid.Empty if the session can't be started (because the user is opted-out, for example)
        /// If sessionId is not Guid.Empty, then it will be the return value</returns>
        public async Task<Guid> StartSession(string organization, string product, Guid userId, Guid sessionId)
        {
            // TODO: do not start sessions for products that don't exist
            // TODO: do not start sessions for unknown user IDs
            // TODO: do not start sessions for opted-out user IDs

            // Generate a new session ID if the one passed in was empty
            if (sessionId == Guid.Empty)
            {
                sessionId = Guid.NewGuid();
            }

            // Add this session to the data store
            JObject sessionData = JObject.FromObject(new
            {
                Organization = organization,
                Product = product,
                UserId = userId,
            });

            // Create the session in the store
            // Overwrite it if it already exists: this should be OK provided that session IDs are never re-used
            await m_SessionDataStore.ChildStore(new JArray(organization, product)).SetValue(new JArray(sessionId.ToString()), sessionData);

            return sessionId;
        }

        /// <summary>
        /// Schedules a single event to be recorded by this object
        /// </summary>
        /// <param name="sessionId">The ID of the session (as returned by StartSession)</param>
        /// <param name="organization">The name of the organization that the session is for</param>
        /// <param name="product">The name of the product that the session is for</param>
        /// <param name="data">JSON data indicating the properties for this event</param>
        public async Task RecordEvent(Guid sessionId, string organization, string product, JObject data)
        {
            // TODO: do not record events for sessions that don't exist

            // Store in the raw events store
            await m_EventDataStore.AppendValue(data);

            // TODO: this needs quite a bit of work.
            // Right now, we fetch, decode and run the map/reduce algorithms individually on each event which is really inefficient
            // We can likely cache the decoded map/reduce algorithms
            // We could only calculate query results when they're actually requested
            // There's probably a missing class here: the same data format objects are used in the ProductAdmin class
            // If there are multiple threads in this process there are issues too

            // Get the product data store for this org/product
            var productDataStore = m_RootDataStore.ChildStore(new JArray("products")).ChildStore(ProductAdmin.KeyForProduct(organization, product));               // TODO: also defined in ProductAdmin
            var queryDataStore = productDataStore.ChildStore(new JArray("queries"));                                            // TODO: also defined in QueryableProduct

            // Get the queries for this org/product by merging all the queries from all the nodes
            // TODO: this code needs to be refactored :-)
            JObject queries = new JObject();
            var projectQueries = queryDataStore.EnumerateAllValues();
            for (var query = await projectQueries.FetchNext(); query != null; query = await projectQueries.FetchNext())
            {
                JToken queryListToken;

                // Should contain a 'queries' element with this list of queries in it
                if (query.Item2.TryGetValue("Queries", out queryListToken))
                {
                    JObject queryList = queryListToken.Value<JObject>();

                    // Add these queries to the queries object
                    // TODO: this just picks the last query: we should probably pick the most recent instead
                    foreach (var nodeQuery in queryList)
                    {
                        queries[nodeQuery.Key] = nodeQuery.Value;
                    }
                }
            }

            // Run all of the queries
            foreach (var queryToRun in queries)
            {
                // Get the query
                var queryName = queryToRun.Key;
                var queryJson = queryToRun.Value["Query"].Value<JObject>();

                // Turn into a map/reduce query
                var mapReduce = MapReduceQueryFactory.GenerateMapReduce(queryJson);

                // Create an ingestor for this query
                var thisQueryStore = productDataStore.ChildStore(new JArray("query-results", queryName));
                thisQueryStore = thisQueryStore.ChildStore(new JArray(m_NodeName));

                var ingestor = new DataIngestor(mapReduce, thisQueryStore);

                // Commit this query to it
                // TODO: ingesting one event at once is pretty inefficient
                ingestor.Ingest(data);
                await ingestor.Commit();
            }
        }
    }
}
