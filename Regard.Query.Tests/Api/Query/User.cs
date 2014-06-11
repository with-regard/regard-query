using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Regard.Query.Api;

namespace Regard.Query.Tests.Api.Query
{
    [TestFixture("InMemory")]
    [TestFixture("LocalAzureTableStore")]
    class User
    {
        private string m_DataStoreType;

        private static readonly Guid[] c_UserIds = new[] {new Guid("675E1191-936E-44E4-BA13-2A3FA3D97F8D"), new Guid("34585225-9935-4889-B05F-FB8CF44EB12F"), new Guid("631ED963-6A6E-471E-B392-11BCCCA2460E")};

        public User(string dataStoreType)
        {
            m_DataStoreType = dataStoreType;
        }

        /// <summary>
        /// Creates a data store containing 12 events for each of the user IDs
        /// </summary>
        private async Task<IRegardDataStore> GenerateUserDataStore()
        {
            var dataStore = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
            await dataStore.Products.CreateProduct("WithRegard", "Test");

            var product = await dataStore.Products.GetProduct("WithRegard", "Test");

            // Opt-in the users
            foreach (var uid in c_UserIds)
            {
                await product.Users.OptIn(uid);
            }

            // Ingest 12 events for each of these users
            foreach (var uid in c_UserIds)
            {
                await TestQueryBuilder.IngestBasic12TestDocumentsForUser(dataStore, uid);
            }

            return dataStore;
        }

        [Test]
        public void CanRetrieveEventsForAUser()
        {
            Task.Run(async () =>
            {
                // Create the data store
                var dataStore = await GenerateUserDataStore();
                var product = await dataStore.Products.GetProduct("WithRegard", "Test");

                // Should be 12 events for each user
                foreach (var uid in c_UserIds)
                {
                    string nextPageToken = null;
                    int count = 0;

                    do
                    {
                        var userEvents = await product.RetrieveEventsForUser(uid, nextPageToken);

                        for (var nextEvent = await userEvents.FetchNext(); nextEvent != null; nextEvent = await userEvents.FetchNext())
                        {
                            ++count;
                        }

                        nextPageToken = await userEvents.GetNextPageToken();
                    }
                    while (nextPageToken != null);

                    Assert.AreEqual(12, count);
                }
            }).Wait();
        }

        [Test]
        public void EventContentsLookRight()
        {
            Task.Run(async () =>
            {
                // Create the data store
                var dataStore = await GenerateUserDataStore();
                var product = await dataStore.Products.GetProduct("WithRegard", "Test");

                // Should be 12 events for each user
                foreach (var uid in c_UserIds)
                {
                    string nextPageToken = null;
                    int count = 0;

                    do
                    {
                        var userEvents = await product.RetrieveEventsForUser(uid, nextPageToken);

                        for (var nextEvent = await userEvents.FetchNext(); nextEvent != null; nextEvent = await userEvents.FetchNext())
                        {
                            // Should contain an event type with certain values
                            switch (nextEvent["EventType"].Value<string>())
                            {
                                case "Start":
                                case "Stop":
                                case "Click":
                                case "NotClick":
                                    // OK
                                    break;

                                default:
                                    Assert.Fail();
                                    break;
                            }

                            ++count;
                        }

                        nextPageToken = await userEvents.GetNextPageToken();
                    } while (nextPageToken != null);

                    Assert.AreEqual(12, count);
                }
            }).Wait();
        }

        [Test]
        public void CanDeleteEventsForAUser()
        {
            Task.Run(async () =>
            {
                // Create the data store
                var dataStore = await GenerateUserDataStore();
                var product = await dataStore.Products.GetProduct("WithRegard", "Test");

                // Delete the first user ID
                var deleteUserId = c_UserIds[0];
                await product.Users.DeleteData(deleteUserId);

                // Should be 12 events for each user, except the one that has been deleted
                foreach (var uid in c_UserIds)
                {
                    string nextPageToken = null;
                    int count = 0;

                    do
                    {
                        var userEvents = await product.RetrieveEventsForUser(uid, nextPageToken);
                        if (userEvents == null) break;

                        for (var nextEvent = await userEvents.FetchNext(); nextEvent != null; nextEvent = await userEvents.FetchNext())
                        {
                            ++count;
                        }

                        nextPageToken = await userEvents.GetNextPageToken();
                    }
                    while (nextPageToken != null);

                    if (Equals(deleteUserId, uid))
                    {
                        Assert.AreEqual(0, count);
                    }
                    else
                    {
                        Assert.AreEqual(12, count);
                    }
                }
            }).Wait();
        }

        [Test]
        public void DeletingUserEventAlsoDeletesQueryData()
        {
            Task.Run(async () =>
            {
                // Create the data store
                var dataStore = await GenerateUserDataStore();
                var product = await dataStore.Products.GetProduct("WithRegard", "Test");

                // Use an all events query to test this
                var query = product.CreateQueryBuilder().AllEvents();
                await product.RegisterQuery("Test", query);

                // Should initially be 12 * number of UIDs events
                var queryResult = await product.RunQuery("Test");
                var numProducts = await queryResult.FetchNext();

                Assert.AreEqual(12 * c_UserIds.Length, numProducts.EventCount);

                // Delete one user ID
                await product.Users.DeleteData(c_UserIds[0]);

                // Should be 12 less events in the query
                queryResult = await product.RunQuery("Test");
                numProducts = await queryResult.FetchNext();

                Assert.AreEqual(12 * (c_UserIds.Length - 1), numProducts.EventCount);
            }).Wait();
        }

        [Test]
        public void DeletingUserEventAlsoDeletesQueryDataIfQueryIsNotRunFirst()
        {
            // This can make a difference: if events aren't processed immediately, then any events that are still waiting to be processed
            // should be deleted before the query is executed

            Task.Run(async () =>
            {
                // Create the data store
                var dataStore = await GenerateUserDataStore();
                var product = await dataStore.Products.GetProduct("WithRegard", "Test");

                // Use an all events query to test this
                var query = product.CreateQueryBuilder().AllEvents();
                await product.RegisterQuery("Test", query);

                // Delete one user ID
                await product.Users.DeleteData(c_UserIds[0]);

                // Should be 12 less events in the query
                var queryResult = await product.RunQuery("Test");
                var numProducts = await queryResult.FetchNext();

                Assert.AreEqual(12 * (c_UserIds.Length - 1), numProducts.EventCount);
            }).Wait();
        }
    }
}
