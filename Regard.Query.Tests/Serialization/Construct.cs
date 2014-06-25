using NUnit.Framework;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.Serialization
{
    [TestFixture]
    class Construct
    {
        [Test]
        public void AllEvents()
        {
            var builder     = new SerializableQueryBuilder(null);
            var allEvents   = builder.AllEvents();

            Assert.AreEqual(QueryVerbs.AllEvents, allEvents.Verb);
            Assert.IsNull(allEvents.Key);
            Assert.IsNull(allEvents.Value);
            Assert.IsNull(allEvents.Name);
            Assert.IsNull(allEvents.AppliesTo);
        }

        [Test]
        public void Only()
        {
            var builder = new SerializableQueryBuilder(null);
            var only = builder.Only(builder.AllEvents(), "Test1", "Test2");

            Assert.AreEqual(QueryVerbs.Only, only.Verb);
            Assert.AreEqual("Test1", only.Key);
            Assert.AreEqual("Test2", only.Value);
            Assert.IsNull(only.Name);
            Assert.IsNotNull(only.AppliesTo);
        }

        [Test]
        public void BrokenDownBy()
        {
            var builder = new SerializableQueryBuilder(null);
            var brokenDownBy = builder.BrokenDownBy(builder.AllEvents(), "Test1", "Test2");

            Assert.AreEqual(QueryVerbs.BrokenDownBy, brokenDownBy.Verb);
            Assert.AreEqual("Test1", brokenDownBy.Key);
            Assert.AreEqual("Test2", brokenDownBy.Name);
            Assert.IsNull(brokenDownBy.Value);
            Assert.IsNotNull(brokenDownBy.AppliesTo);
        }

        [Test]
        public void CountUniqueValues()
        {
            var builder = new SerializableQueryBuilder(null);
            var countUnique = builder.CountUniqueValues(builder.AllEvents(), "Test1", "Test2");

            Assert.AreEqual(QueryVerbs.CountUniqueValues, countUnique.Verb);
            Assert.AreEqual("Test1", countUnique.Key);
            Assert.AreEqual("Test2", countUnique.Name);
            Assert.IsNull(countUnique.Value);
            Assert.IsNotNull(countUnique.AppliesTo);
        }

        [Test]
        public void IndexedBy()
        {
            var builder = new SerializableQueryBuilder(null);
            var countUnique = builder.IndexedBy(builder.AllEvents(), "Test1");

            Assert.AreEqual(QueryVerbs.IndexedBy, countUnique.Verb);
            Assert.AreEqual("Test1", countUnique.Key);
            Assert.AreEqual("Test2", countUnique.Name);
            Assert.IsNull(countUnique.Value);
            Assert.IsNotNull(countUnique.AppliesTo);
        }

        [Test]
        public void Sum()
        {
            var builder = new SerializableQueryBuilder(null);
            var sum = builder.Sum(builder.AllEvents(), "Test1", "Test2");

            Assert.AreEqual(QueryVerbs.Sum, sum.Verb);
            Assert.AreEqual("Test1", sum.Key);
            Assert.AreEqual("Test2", sum.Name);
            Assert.IsNull(sum.Value);
            Assert.IsNotNull(sum.AppliesTo);
        }

        [Test]
        public void Min()
        {
            var builder = new SerializableQueryBuilder(null);
            var sum = builder.Min(builder.AllEvents(), "Test1", "Test2");

            Assert.AreEqual(QueryVerbs.Min, sum.Verb);
            Assert.AreEqual("Test1", sum.Key);
            Assert.AreEqual("Test2", sum.Name);
            Assert.IsNull(sum.Value);
            Assert.IsNotNull(sum.AppliesTo);
        }

        [Test]
        public void Max()
        {
            var builder = new SerializableQueryBuilder(null);
            var sum = builder.Max(builder.AllEvents(), "Test1", "Test2");

            Assert.AreEqual(QueryVerbs.Max, sum.Verb);
            Assert.AreEqual("Test1", sum.Key);
            Assert.AreEqual("Test2", sum.Name);
            Assert.IsNull(sum.Value);
            Assert.IsNotNull(sum.AppliesTo);
        }

        [Test]
        public void Mean()
        {
            var builder = new SerializableQueryBuilder(null);
            var sum = builder.Mean(builder.AllEvents(), "Test1", "Test2");

            Assert.AreEqual(QueryVerbs.Mean, sum.Verb);
            Assert.AreEqual("Test1", sum.Key);
            Assert.AreEqual("Test2", sum.Name);
            Assert.IsNull(sum.Value);
            Assert.IsNotNull(sum.AppliesTo);
        }
    }
}
