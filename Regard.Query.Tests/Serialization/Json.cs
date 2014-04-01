using NUnit.Framework;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.Serialization
{
    [TestFixture]
    public class Json
    {
        [Test]
        public static void AllEvents()
        {
            // Create the query
            var builder         = new SerializableQueryBuilder(null);
            var query           = builder.AllEvents();

            // Serialize and deserialize it
            var serialized      = query.ToJson();
            var deserialized    = (SerializableQuery) builder.FromJson(serialized);

            // Check that the result is the same
            Util.AssertEqual(query, deserialized, true);
        }

        [Test]
        public static void AllComponents()
        {
            // Create the query
            var builder = new SerializableQueryBuilder(null);
            var query = (SerializableQuery) builder.AllEvents().Only("Test1", "Test2").BrokenDownBy("Test3", "Test4").Sum("Test5", "Test6").CountUniqueValues("Test7", "Test8");

            // Serialize and deserialize it
            var serialized = query.ToJson();
            var deserialized = (SerializableQuery)builder.FromJson(serialized);

            // Check that the result is the same
            Util.AssertEqual(query, deserialized, true);
        }
    }
}
