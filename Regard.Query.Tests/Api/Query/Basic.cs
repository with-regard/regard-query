using System.Threading.Tasks;
using NUnit.Framework;
using Regard.Query.Api;

namespace Regard.Query.Tests.Api.Query
{
    [TestFixture("InMemory")]
    [TestFixture("LocalAzureTableStore")]
    class Basic
    {
        private string m_DataStoreType;

        public Basic(string dataStoreType)
        {
            m_DataStoreType = dataStoreType;
        }

        [Test]
        public void CanCreateAQueryBuilder()
        {
            Task.Run(async () =>
            {
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                var builder = testProduct.CreateQueryBuilder();
                Assert.IsNotNull(builder);
            }).Wait();
        }

        [Test]
        public void CanRegisterAQuery()
        {
            Task.Run(async () =>
            {
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                var builder = testProduct.CreateQueryBuilder();

                var allEventsQuery = builder.AllEvents();

                await testProduct.RegisterQuery("test", allEventsQuery);

                // There's nothing to assert here
            }).Wait();
        }

        [Test]
        public void NonExistentQueryReturnsNull()
        {
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                var queryResult = await testProduct.RunQuery("test");
                Assert.IsNull(queryResult);
            }).Wait();
        }

        [Test]
        public void QueryInitiallyHasNoResults()
        {
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                var builder = testProduct.CreateQueryBuilder();
                var allEventsQuery = builder.AllEvents();
                await testProduct.RegisterQuery("test", allEventsQuery);

                var queryResult = await testProduct.RunQuery("test");

                Assert.IsNotNull(queryResult);

                // When there are no events, the query should return an empty result set
                int resultCount = 0;
                for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
                {
                    resultCount++;
                }

                Assert.AreEqual(0, resultCount);
            }).Wait();
        }

        [Test]
        public void WeCountEventsThatOccurredBeforeTheQueryWasRegistered()
        {
            // This is a simplification for the initial version
            // The SQL query engine doesn't work this way, but the map/reduce one does.
            // We might consider it a feature of the product eventually (this means we won't ever report stats for data gathered before the user consented to a particular use)
            // For now, this is just a limitation.
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                // Run the events through before the query is registered
                await TestQueryBuilder.IngestBasic12TestDocuments(store);

                var builder = testProduct.CreateQueryBuilder();
                var allEventsQuery = builder.AllEvents();
                await testProduct.RegisterQuery("test", allEventsQuery);

                var queryResult = await testProduct.RunQuery("test");

                Assert.IsNotNull(queryResult);

                // All the results should have an event count of 0, as events preceding query registration aren't counted
                // They should also have no columns, but we don't check that here. I think it's OK if they return bonus columns, so long all the explicitly requested columns are present
                int resultCount = 0;
                for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
                {
                    resultCount++;

                    Assert.AreEqual(12, result.EventCount);
                }

                Assert.AreEqual(1, resultCount);
            }).Wait();
        }

        [Test]
        public void WeCanCountThe12TestEvents()
        {
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                var builder = testProduct.CreateQueryBuilder();
                var allEventsQuery = builder.AllEvents();
                await testProduct.RegisterQuery("test", allEventsQuery);

                // Run the events through
                await TestQueryBuilder.IngestBasic12TestDocuments(store);

                var queryResult = await testProduct.RunQuery("test");

                Assert.IsNotNull(queryResult);

                // All the results should have an event count of 12
                // They should also have no columns, but we don't check that here. I think it's OK if they return bonus columns, so long all the explicitly requested columns are present
                int resultCount = 0;
                for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
                {
                    resultCount++;

                    Assert.AreEqual(12, result.EventCount);
                }

                // And there should only be one of them
                Assert.AreEqual(1, resultCount);
            }).Wait();
        }

        [Test]
        public void CanCount12ClicksByIndexing()
        {
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                // Create a query indexed by event type
                var builder = testProduct.CreateQueryBuilder();
                var allEventsQuery = builder.AllEvents().IndexedBy("EventType");
                await testProduct.RegisterQuery("test", allEventsQuery);

                // Run the events through
                await TestQueryBuilder.IngestBasic12TestDocuments(store);

                // Request the contents of the 'Click' index
                var queryResult = await testProduct.RunIndexedQuery("test", "Click");

                Assert.IsNotNull(queryResult);

                // All the results should have an event count of 5
                int resultCount = 0;
                for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
                {
                    resultCount++;

                    Assert.AreEqual(5, result.EventCount);
                }

                // And there should only be one of them
                Assert.AreEqual(1, resultCount);
            }).Wait();
        }

        [Test]
        public void UpdatingAQueryRemovesOldResults()
        {
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                var builder = testProduct.CreateQueryBuilder();
                var allEventsQuery = builder.AllEvents();
                var sessionCountQuery = builder.AllEvents().BrokenDownBy("SessionId", "WhichSession");
                await testProduct.RegisterQuery("test", sessionCountQuery);

                // Run the events through
                await TestQueryBuilder.IngestBasic12TestDocuments(store);

                // There are three sessions that should be retrieved by this query
                // For some possible implementations the query results might not exist unless we actually read the results, so force this to occur
                // A failure here indicates a problem with something other than this specific feature
                var queryResult = await testProduct.RunQuery("test"); 
                int resultCount = 0;
                for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
                {
                    resultCount++;
                }

                Assert.AreEqual(3, resultCount);

                // Re-register the query, deleting the old results
                // We replace with the 'all events' query as this is useful for testing the behaviour that is eventually desired: that the query updates against the
                // current database contents
                await testProduct.RegisterQuery("test", allEventsQuery);

                // Re-run the test now it has been replaced
                // The old results should be gone
                queryResult = await testProduct.RunQuery("test");

                Assert.IsNotNull(queryResult);

                // All the results should have an event count of 12
                // They should also have no columns, but we don't check that here. I think it's OK if they return bonus columns, so long all the explicitly requested columns are present
                resultCount = 0;
                for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
                {
                    resultCount++;

                    // Should contain all 12 events
                    Assert.AreEqual(12, result.EventCount);
                }

                // Re-registering the query should replace the old results, so there should only be one
                Assert.AreEqual(1, resultCount);
            }).Wait();
        }

        [Test]
        public void WeCanCountTheTwoSessionsWithOneOrMoreClicks()
        {
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                var builder = testProduct.CreateQueryBuilder();
                var uniqueClicksQuery = builder.AllEvents().Only("EventType", "Click").CountUniqueValues("SessionId", "NumSessions");
                await testProduct.RegisterQuery("test", uniqueClicksQuery);

                // Run the events through
                await TestQueryBuilder.IngestBasic12TestDocuments(store);

                var queryResult = await testProduct.RunQuery("test");

                Assert.IsNotNull(queryResult);

                // All the results should have an event count of 12
                // They should also have no columns, but we don't check that here. I think it's OK if they return bonus columns, so long all the explicitly requested columns are present
                int resultCount = 0;
                for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
                {
                    resultCount++;

                    // There are 5 total click events
                    Assert.AreEqual(5, result.EventCount);

                    // There are 2 sessions with a click
                    AssertContainsField(result, "NumSessions", "2");
                }

                // And there should only be one of them
                Assert.AreEqual(1, resultCount);
            }).Wait();
        }

        [Test]
        public void CanUpdateAllQueries()
        {
            Task.Run(async () =>
            {
                // No assertions as updating queries just means they run faster later on
                // If we add instrumentation later on, we could check that it updates (but that's arguably a side-effect and not a solid guarantee of what should happen when you run a test)
                var store = await TestQueryBuilder.CreateEmptyDataStore(m_DataStoreType);
                var testProduct = await store.Products.GetProduct("WithRegard", "Test");

                var builder = testProduct.CreateQueryBuilder();
                var uniqueClicksQuery = builder.AllEvents().Only("EventType", "Click").CountUniqueValues("SessionId", "NumSessions");
                await testProduct.RegisterQuery("test", uniqueClicksQuery);

                // Run the events through
                await TestQueryBuilder.IngestBasic12TestDocuments(store);

                // Update all queries
                await testProduct.UpdateAllQueries();
            }).Wait();
        }

        /// <summary>
        /// Asserts that a query result contains a particular field
        /// </summary>
        internal static void AssertContainsField(QueryResultLine result, string fieldName, string expectedValue)
        {
            foreach (var column in result.Columns)
            {
                if (column.Name == fieldName)
                {
                    // Field exists: check value
                    Assert.AreEqual(expectedValue, column.Value);
                    return;
                }
            }

            // Field does not exist if we reach here
            Assert.Fail();
        }
    }
}
