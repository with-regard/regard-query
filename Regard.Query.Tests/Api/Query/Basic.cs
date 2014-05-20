using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Regard.Query.Api;

namespace Regard.Query.Tests.Api.Query
{
    [TestFixture]
    class Basic
    {
        [Test]
        public void CanCreateAQueryBuilder()
        {
            Task.Run(async () =>
            {
                var store = await TestQueryBuilder.CreateEmptyDataStore();
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
                var store = await TestQueryBuilder.CreateEmptyDataStore();
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
                var store = await TestQueryBuilder.CreateEmptyDataStore();
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
                var store = await TestQueryBuilder.CreateEmptyDataStore();
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

                // And there should only be one of them
                Assert.AreEqual(0, resultCount);
            }).Wait();
        }

        [Test]
        public void WeDontCountEventsThatOccurBeforeTheQueryWasRegistered()
        {
            // This is a simplification for the initial version
            // The SQL query engine doesn't work this way, but the map/reduce one does.
            // We might consider it a feature of the product eventually (this means we won't ever report stats for data gathered before the user consented to a particular use)
            // For now, this is just a limitation.
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore();
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

                    Assert.AreEqual(0, result.EventCount);
                }

                // And there should only be one of them
                Assert.AreEqual(1, resultCount);
            }).Wait();
        }

        [Test]
        public void WeCanCountThe12TestEvents()
        {
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore();
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
        public void WeCanCountTheTwoSessionsWithOneOrMoreClicks()
        {
            Task.Run(async () =>
            {
                // Register the query, as before
                var store = await TestQueryBuilder.CreateEmptyDataStore();
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
