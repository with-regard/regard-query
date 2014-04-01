using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.Serialization
{
    [TestFixture]
    class Replay
    {
        private static void Try(SerializableQuery query)
        {
            Assert.IsNotNull(query);

            // If we can replay to another serializable builder and get the same query, then it works
            var builder = new SerializableQueryBuilder(null);
            var result = (SerializableQuery) query.Rebuild(builder);

            Assert.IsNotNull(result);
            Assert.IsFalse(ReferenceEquals(result, query));                 // Hm, doesn't technically have to be true
            Util.AssertEqual(query, result, true);
        }

        [Test]
        public static void AllEvents()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try(builder.AllEvents());
        }

        [Test]
        public static void Only()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().Only("Test1", "Test2"));
        }

        [Test]
        public static void BrokenDownBy()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().BrokenDownBy("Test1", "Test2"));
        }

        [Test]
        public static void CountUnique()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().CountUniqueValues("Test1", "Test2"));
        }


        [Test]
        public static void Sum()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().Sum("Test1", "Test2"));
        }

        [Test]
        public static void AllComponents()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().Only("Test1", "Test2").BrokenDownBy("Test3", "Test4").Sum("Test5", "Test6").CountUniqueValues("Test7", "Test8"));
        }
    }
}
