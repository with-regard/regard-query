using NUnit.Framework;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.Serialization
{
    [TestFixture]
    internal static class Util
    {
        public static void AssertEqual(SerializableQuery expected, SerializableQuery actual, bool recurse)
        {
            if (expected == null && actual == null) return;
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);

            Assert.AreEqual(expected.Verb, actual.Verb);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Format, actual.Format);

            if (recurse)
            {
                AssertEqual(expected.AppliesTo, actual.AppliesTo, true);
            }
            else
            {
                if (expected.AppliesTo != null)
                {
                    Assert.IsNotNull(actual.AppliesTo);
                }
                else
                {
                    Assert.IsNull(actual.AppliesTo);
                }
            }
        }

        [Test]
        public static void EqualsAreEqual()
        {
            var builder = new SerializableQueryBuilder(null);
            AssertEqual(builder.AllEvents(), builder.AllEvents(), true);
            AssertEqual(builder.Only(builder.AllEvents(), "Test1", "Test2"), builder.Only(builder.AllEvents(), "Test1", "Test2"), true);
        }

        [Test]
        public static void NotEqualAreNotEqual()
        {
            var builder = new SerializableQueryBuilder(null);

            // These assertions are *supposed* to fail here. If they don't fail then any tests that depend on the utility class is invalid
            try
            {
                AssertEqual(builder.Only(builder.AllEvents(), "Test1", "Test2"), builder.AllEvents(), true);
                AssertEqual(builder.AllEvents(), builder.Only(builder.AllEvents(), "Test1", "Test2"), true);
            }
            catch (AssertionException)
            {
                // OK
                return;
            }

            Assert.Fail("Assertions passed");
        }
    }
}
