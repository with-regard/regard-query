using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.Serialization
{
    [TestFixture]
    public class Json
    {
        /// <summary>
        /// Sees if the specified serializable query serializes and deserializes to the same thing
        /// </summary>
        /// <param name="query"></param>
        private static void Try(SerializableQuery query)
        {
            Assert.IsNotNull(query);

            var builder = new SerializableQueryBuilder(null);

            // Serialize and deserialize it
            var serialized = query.ToJson();
            var deserialized = (SerializableQuery)builder.FromJson(serialized);

            // Check that the result is the same
            Util.AssertEqual(query, deserialized, true);
        }

        [Test]
        public static void AllEvents()
        {
            // Create the query
            var builder         = new SerializableQueryBuilder(null);
            Try(builder.AllEvents());
        }

        [Test]
        public static void Only()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery) builder.AllEvents().Only("Test1", "Test2"));
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
        public static void IndexedBy()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().IndexedBy("Test1"));
        }

        [Test]
        public static void TranformDateFormat()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().TransformDateFormat("Test1", "Test2", "Test3"));
        }


        [Test]
        public static void Sum()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().Sum("Test1", "Test2"));
        }

        [Test]
        public static void Mean()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().Mean("Test1", "Test2"));
        }


        [Test]
        public static void Min()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().Min("Test1", "Test2"));
        }

        [Test]
        public static void Max()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery)builder.AllEvents().Max("Test1", "Test2"));
        }

        [Test]
        public static void AllComponents()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            Try((SerializableQuery) builder.AllEvents().Only("Test1", "Test2").BrokenDownBy("Test3", "Test4").Sum("Test5", "Test6").CountUniqueValues("Test7", "Test8"));
        }
    }
}
