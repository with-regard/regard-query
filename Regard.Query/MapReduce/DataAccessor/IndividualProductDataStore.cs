using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.DataAccessor
{
    /// <summary>
    /// Represents a data store that contains data for an individual product
    /// </summary>
    class IndividualProductDataStore
    {
        private readonly IKeyValueStore m_RawDataStore;
        private readonly IKeyValueStore m_UserEvents;

        public IndividualProductDataStore(IKeyValueStore rawDataStore)
        {
            if (rawDataStore == null) throw new ArgumentNullException("rawDataStore");
            m_RawDataStore = rawDataStore;

            Users = new UserDataStore(m_RawDataStore.ChildStore(new JArray("users")));
            Queries = new QueryDataStore(m_RawDataStore.ChildStore(new JArray("queries")));

            m_UserEvents = m_RawDataStore.ChildStore(new JArray("user-events"));
        }

        public UserDataStore Users { get; private set; }
        public QueryDataStore Queries { get; private set; }

        /// <summary>
        /// Removes the current results for a particular query (on a particular node)
        /// </summary>
        public async Task DeleteQueryResults(string queryName, string nodeName)
        {
            await m_RawDataStore.DeleteChildStore(new JArray("query-results", queryName));
            var queryStatusStore = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));               // TODO: erase in other nodes too, I think
            await queryStatusStore.SetValue(new JArray(queryName), null);
        }

        /// <summary>
        /// Given a query as an IMapReduce implementation, returns a suitable data ingestor for a particular node
        /// </summary>
        public DataIngestor CreateIngestorForQuery(string queryName, string nodeName, IMapReduce mapReduceAlgorithm)
        {
            var thisQueryStore = m_RawDataStore.ChildStore(new JArray("query-results", queryName)).ChildStore(new JArray(nodeName));
            var ingestor = new DataIngestor(mapReduceAlgorithm, thisQueryStore);

            return ingestor;
        }

        /// <summary>
        /// Returns the currently set status of a particular query on a particular node
        /// </summary>
        public async Task<JObject> GetQueryStatus(string queryName, string nodeName)
        {
            var queryStatusStore    = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));
            var status              = await queryStatusStore.GetValue(new JArray(queryName));

            return status;
        }

        /// <summary>
        /// Sets the status of a particular query on a particular node
        /// </summary>
        public async Task SetQueryStatus(string queryName, string nodeName, JObject newStatus)
        {
            var queryStatusStore = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));
            await queryStatusStore.SetValue(new JArray(queryName), newStatus);
            await queryStatusStore.Commit();
        }

        /// <summary>
        /// Retrieves the raw event store (where incoming events are appended) for a particular node
        /// </summary>
        public IKeyValueStore GetRawEventStore(string nodeName)
        {
            return m_RawDataStore.ChildStore(new JArray("raw-events", nodeName));            
        }

        /// <summary>
        /// Marks that a particular user has received a particualr event
        /// </summary>
        public async Task AssociateEventWithUser(Guid user, long eventId, string nodeName, JObject eventData)
        {
            // Can just query everything beginning with the user prefix to get the complete list of events
            await m_UserEvents.SetValue(new JArray(user.ToString(), eventId, nodeName), eventData);
        }

        /// <summary>
        /// Returns an enumerator that loads the events for a particular user 
        /// </summary>
        public IKvStoreEnumerator GetEventEnumeratorForUser(Guid user)
        {
            return m_UserEvents.EnumerateValuesBeginningWithKey(new JArray(user.ToString()));
        }

        private const int c_DeleteBlockSize = 100;

        /// <summary>
        /// Deletes the store where an individual users events are kept
        /// </summary>
        public async Task DeleteEventStoreForUser(Guid userId)
        {
            // Send the events 100 at a time to be deleted
            List<JArray> userEventKeys = new List<JArray>(100);
            var eventEnum = GetEventEnumeratorForUser(userId);

            for (var userEvent = await eventEnum.FetchNext(); userEvent != null; userEvent = await eventEnum.FetchNext())
            {
                // Add the key for this event
                userEventKeys.Add(userEvent.Item1);

                // Delete an event set once we have enough
                if (userEventKeys.Count >= c_DeleteBlockSize)
                {
                    await m_UserEvents.DeleteKeys(userEventKeys);
                    userEventKeys = new List<JArray>();
                }
            }

            // Delete any remaining events
            if (userEventKeys.Count > 0)
            {
                await m_UserEvents.DeleteKeys(userEventKeys);
            }
        }

        /// <summary>
        /// Iterates through a user's event store and deletes any events found from the raw list for a particular node
        /// </summary>
        public async Task DeleteRawEventsForUser(Guid userId, string nodeName)
        {
            // Send the events 100 at a time to be deleted
            Dictionary<string, List<JArray>> eventsForNode = new Dictionary<string, List<JArray>>();
            var eventEnum = GetEventEnumeratorForUser(userId);

            // Iterate across all the user events
            for (var userEvent = await eventEnum.FetchNext(); userEvent != null; userEvent = await eventEnum.FetchNext())
            {
                // Treat each node individually when generating delete requests
                string eventNodeName = nodeName;
                if (userEvent.Item1.Count > 2)
                {
                    eventNodeName = userEvent.Item1[2].Value<string>();
                }

                List<JArray> userEventKeys;
                if (!eventsForNode.TryGetValue(eventNodeName, out userEventKeys))
                {
                    eventsForNode[eventNodeName] = userEventKeys = new List<JArray>();
                }

                // Add the key for this event
                userEventKeys.Add(new JArray(userEvent.Item1[1].Value<long>()));

                // Delete an event set once we have enough
                if (userEventKeys.Count >= c_DeleteBlockSize)
                {
                    var eventStore = m_RawDataStore.ChildStore(new JArray("raw-events", eventNodeName));
                    await eventStore.DeleteKeys(userEventKeys);
                    eventsForNode[eventNodeName] = new List<JArray>();
                }
            }

            // Delete any remaining events
            foreach (var nodeEvents in eventsForNode)
            {
                if (nodeEvents.Value.Count > 0)
                {
                    var eventStore = m_RawDataStore.ChildStore(new JArray("raw-events", nodeEvents.Key));
                    await eventStore.DeleteKeys(nodeEvents.Value);
                }
            }
        }

        /// <summary>
        /// Retrieves the currently calculated set of results for a particular query on a particular node
        /// </summary>
        public IKeyValueStore GetQueryResults(string queryName, string nodeName)
        {
            var results = m_RawDataStore.ChildStore(new JArray("query-results", queryName)).ChildStore(new JArray(nodeName));
            return results;
        }

        /// <summary>
        /// Ensures that data is committed for a particular node
        /// </summary>
        public async Task Commit(string nodeName)
        {
            await GetRawEventStore(nodeName).Commit();
            await m_UserEvents.Commit();
        }
    }
}
