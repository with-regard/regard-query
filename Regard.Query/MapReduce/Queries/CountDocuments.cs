using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    /// <summary>
    /// Counts the number of documents accepted by a reduce operation
    /// </summary>
    class CountDocuments : IComposableReduce
    {
        public void Reduce(JObject result, JObject[] documents)
        {
            long count = 0;
            foreach (var doc in documents)
            {
                JToken countVal;
                if (doc.TryGetValue("Count", out countVal))
                {
                    // A mapped document can manually specify the count if it wants
                    count += countVal.Value<long>();
                }
                else
                {
                    // If no count is specified, it counts for 1
                    count += 1;
                }
            }

            result["Count"] = count;
        }

        public void Rereduce(JObject result, JObject[] documents)
        {
            long count = 0;
            foreach (var doc in documents)
            {
                count += doc["Count"].Value<long>();
            }

            result["Count"] = count;
        }

        public void Unreduce(JObject result, JObject[] documents)
        {
            long count = result["Count"].Value<long>();
            foreach (var doc in documents)
            {
                count -= doc["Count"].Value<long>();
            }

            result["Count"] = count;
        }
    }
}
