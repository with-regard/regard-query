﻿using System;
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

        public async Task DeleteQueryResults(string queryName, string nodeName)
        {
            await m_RawDataStore.DeleteChildStore(new JArray("query-results", queryName));
            var queryStatusStore = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));               // TODO: erase in other nodes too, I think
            await queryStatusStore.SetValue(new JArray(queryName), null);
        }

        public DataIngestor CreateIngestorForQuery(string queryName, string nodeName, IMapReduce mapReduceAlgorithm)
        {
            var thisQueryStore = m_RawDataStore.ChildStore(new JArray("query-results", queryName)).ChildStore(new JArray(nodeName));
            var ingestor = new DataIngestor(mapReduceAlgorithm, thisQueryStore);

            return ingestor;
        }

        public async Task<JObject> GetQueryStatus(string queryName, string nodeName)
        {
            var queryStatusStore    = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));
            var status              = await queryStatusStore.GetValue(new JArray(queryName));

            return status;
        }

        public async Task SetQueryStatus(string queryName, string nodeName, JObject newStatus)
        {
            var queryStatusStore = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));
            await queryStatusStore.SetValue(new JArray(queryName), newStatus);
        }

        public IKeyValueStore GetRawEventStore(string nodeName)
        {
            return m_RawDataStore.ChildStore(new JArray("raw-events", nodeName));            
        }

        public async Task AssociateEventWithUser(Guid user, long eventId, JObject eventData)
        {
            // Can just query everything beginning with the user prefix to get the complete list of events
            await m_UserEvents.SetValue(new JArray(user.ToString(), eventId), eventData);
        }

        public IKvStoreEnumerator GetEventEnumeratorForUser(Guid user)
        {
            return m_UserEvents.EnumerateValuesBeginningWithKey(new JArray(user.ToString()));
        }

        private const int c_DeleteBlockSize = 100;

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

        public async Task DeleteRawEventsForUser(Guid userId, string nodeName)
        {
            var eventStore = m_RawDataStore.ChildStore(new JArray("raw-events", nodeName));

            // Send the events 100 at a time to be deleted
            List<JArray> userEventKeys = new List<JArray>(100);
            var eventEnum = GetEventEnumeratorForUser(userId);

            for (var userEvent = await eventEnum.FetchNext(); userEvent != null; userEvent = await eventEnum.FetchNext())
            {
                // Add the key for this event
                userEventKeys.Add(new JArray(userEvent.Item1[1].Value<long>()));

                // Delete an event set once we have enough
                if (userEventKeys.Count >= c_DeleteBlockSize)
                {
                    await eventStore.DeleteKeys(userEventKeys);
                    userEventKeys = new List<JArray>();
                }
            }

            // Delete any remaining events
            if (userEventKeys.Count > 0)
            {
                await eventStore.DeleteKeys(userEventKeys);
            }
        }

        public IKeyValueStore GetQueryResults(string queryName, string nodeName)
        {
            var results = m_RawDataStore.ChildStore(new JArray("query-results", queryName)).ChildStore(new JArray(nodeName));
            return results;
        }

        public async Task Commit(string nodeName)
        {
            await GetRawEventStore(nodeName).Commit();
        }
    }
}
