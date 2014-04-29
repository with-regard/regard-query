using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// A map/reduce operation that counts the number of events produced by the output
    /// </summary>
    class AllEvents : IMapReduce
    {
        public void Map(IMapTarget target, JObject document)
        {
            // All we do is emit an empty document per document (as each document represents an event)
            target.Emit("-", new JObject());
        }

        public JObject Rereduce(string key, IEnumerable<JObject> reductions)
        {
            // Merge the counts
            long count = 0;
            foreach (var doc in reductions)
            {
                count += doc["Count"].Value<long>();
            }

            return JObject.FromObject(new { Count = count });
        }

        public JObject Reduce(string key, IEnumerable<JObject> mappedDocuments)
        {
            return JObject.FromObject(new { Count = mappedDocuments.Count() });
        }

        public JObject Unreduce(string key, JObject reduced, IEnumerable<JObject> mappedDocuments)
        {
            reduced["Count"] = reduced["Count"].Value<long>() - mappedDocuments.Count();
            return reduced;
        }

        public IMapReduce Chain { get { return null; } }
    }
}
