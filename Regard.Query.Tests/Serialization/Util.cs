using NUnit.Framework;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.Serialization
{
    internal static class Util
    {
        public static void AssertEqual(SerializableQuery expected, SerializableQuery actual, bool recurse = false)
        {
            if (expected == null && actual == null) return;
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);

            Assert.AreEqual(expected.Verb, actual.Verb);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Value, actual.Value);

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
    }
}
