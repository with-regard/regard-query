using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.MapReduce;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce.Bugs
{
    [TestFixture]
    class CountUnique
    {
        /// <summary>
        /// Performs various actions on the data store and tracks state so we can verify that they worked OK
        /// </summary>
        private class UserCreepTester
        {
            private readonly IKeyValueStore m_Store;
            private readonly SerializableQuery m_Query; 
            private DataIngestor m_Ingestor;

            private Dictionary<string, List<JObject>> m_EventsForUser = new Dictionary<string, List<JObject>>();

            public UserCreepTester(SerializableQuery query, IKeyValueStore store)
            {
                m_Store     = store;
                m_Query     = query;
                m_Ingestor  = new DataIngestor(query.GenerateMapReduce(), store);
            }

            /// <summary>
            /// Creates a new user with no events
            /// </summary>
            public void CreateNewUser(int eventCount = 0)
            {
                // An improvement would be to use a known GUID sequence
                Guid someUserGuid = new Guid();
                var userEvents = m_EventsForUser[someUserGuid.ToString()] = new List<JObject>();

                for (int anEvent = 0; anEvent < eventCount; ++anEvent)
                {
                    JObject evtObject = new JObject();

                    evtObject["user-id"] = someUserGuid.ToString();
                    evtObject["action"] = "testing";
                    evtObject["sequenceCount"] = anEvent;

                    m_Ingestor.Ingest(evtObject);
                    userEvents.Add(evtObject);
                }
            }

            public void RecreateIngestor()
            {
                m_Ingestor = new DataIngestor(m_Query.GenerateMapReduce(), m_Store);
            }

            public void AddEventsForAllUsers(int eventCount)
            {
                // Just add a bunch of events
                foreach (string userId in m_EventsForUser.Keys)
                {
                    var userEvents = m_EventsForUser[userId];

                    for (int anEvent = 0; anEvent < eventCount; ++anEvent)
                    {
                        JObject evtObject = new JObject();

                        evtObject["user-id"]        = userId;
                        evtObject["action"]         = "testing";
                        evtObject["sequenceCount"]  = anEvent;

                        m_Ingestor.Ingest(evtObject);
                        userEvents.Add(evtObject);
                    }
                }
            }

            public void DeleteEventsForSomeUser()
            {
                // Do nothing if there are no users
                if (!m_EventsForUser.Any())
                {
                    return;
                }

                // Not especially random, but I don't think it matters for this test
                var userId = m_EventsForUser.Keys.First();

                var userEvents = m_EventsForUser[userId];

                foreach (var evt in userEvents)
                {
                    m_Ingestor.Uningest(evt);
                }
            }

            public async Task CheckUserCountIsRight()
            {
                await m_Ingestor.Commit();

                // We should end up with a data store containing a single record that counts how many unique user IDs there are
                var numUniqueUserIds    = m_EventsForUser.Count;
                var values              = m_Store.EnumerateAllValues();
                int numRecords          = 0;

                for (var val = await values.FetchNext(); val != null; val = await values.FetchNext())
                {
                    ++numRecords;
                    var count = val.Item2["value"].Value<int>();
                    Assert.AreEqual(numUniqueUserIds, count);
                }

                Assert.AreEqual(1, numRecords);
            }
        }

        [Test]
        public void CountUniqueCreepSimpleDelete()
        {
            Task.Run(async () =>
            {
                /// https://github.com/with-regard/regard-query/issues/1
                var queryBuilder = new SerializableQueryBuilder(null);
                var uniqueUsers = (SerializableQuery)queryBuilder.AllEvents().CountUniqueValues("user-id", "value");

                // Assume that the bug isn't down to the data store but the map/reduce algorithm itself, so we'll do this in-memory for now
                var resultStore = new MemoryKeyValueStore();
                var tester = new UserCreepTester(uniqueUsers, resultStore);

                // Two users
                tester.CreateNewUser();
                tester.CreateNewUser();

                // Add a few events
                tester.AddEventsForAllUsers(5);
                tester.AddEventsForAllUsers(5);
                tester.AddEventsForAllUsers(5);
                tester.AddEventsForAllUsers(5);

                // Check
                await tester.CheckUserCountIsRight();

                // Delete and check
                tester.DeleteEventsForSomeUser();
                await tester.CheckUserCountIsRight();                           // Result is 0 records??
            }).Wait();
        }

        [Test]
        public void CountUniqueCreepDeleteMixedWithAdd()
        {
            Task.Run(async () =>
            {
                /// https://github.com/with-regard/regard-query/issues/1
                var queryBuilder = new SerializableQueryBuilder(null);
                var uniqueUsers = (SerializableQuery)queryBuilder.AllEvents().CountUniqueValues("user-id", "value");

                // Assume that the bug isn't down to the data store but the map/reduce algorithm itself, so we'll do this in-memory for now
                var resultStore = new MemoryKeyValueStore();
                var tester = new UserCreepTester(uniqueUsers, resultStore);

                // Two users
                tester.CreateNewUser();
                tester.CreateNewUser();

                // Add a few events
                tester.AddEventsForAllUsers(5);
                tester.AddEventsForAllUsers(5);
                tester.AddEventsForAllUsers(5);
                tester.AddEventsForAllUsers(5);

                // Check
                await tester.CheckUserCountIsRight();

                // Delete and check
                tester.DeleteEventsForSomeUser();
                tester.CreateNewUser();
                tester.AddEventsForAllUsers(5);
                await tester.CheckUserCountIsRight();
            }).Wait();
        }

        [Test]
        [Explicit]
        public void SoakTest()
        {
            Task.Run(async () =>
            {
                const int c_Seed = 1000;
                const int c_NumOperations = 10000;

                /// https://github.com/with-regard/regard-query/issues/1
                var queryBuilder = new SerializableQueryBuilder(null);
                var uniqueUsers = (SerializableQuery)queryBuilder.AllEvents().CountUniqueValues("user-id", "value");

                // Assume that the bug isn't down to the data store but the map/reduce algorithm itself, so we'll do this in-memory for now
                var resultStore = new MemoryKeyValueStore();
                var tester = new UserCreepTester(uniqueUsers, resultStore);

                var rng = new Random(c_Seed);

                for (int x = 0; x < c_NumOperations; ++x)
                {
                    // Add 1-5 users, each with 5 events
                    // Create at least one to avoid known deletion bug detected above
                    for (int user = 0; user < rng.Next(5) + 1; ++user)
                    {
                        tester.CreateNewUser(5);
                    }

                    // Delete 0-3 users (over time, user count should grow)
                    for (int user = 0; user < rng.Next(3); ++user)
                    {
                        tester.DeleteEventsForSomeUser();
                    }

                    // Check all is well
                    await tester.CheckUserCountIsRight();
                }
            }).Wait();
        }
    }
}
