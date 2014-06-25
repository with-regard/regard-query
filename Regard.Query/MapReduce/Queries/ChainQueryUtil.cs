using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    /// <summary>
    /// Utility methods to help with chaining queries
    /// </summary>
    internal class ChainQueryUtil
    {

        /// <summary>
        /// Adds map operations to a query that preserves the original input (using the _key field to generate the key for the result)
        /// </summary>
        /// <remarks>
        /// This leaves the reduce operation as the default (so we end up with a count of the number of documents)
        /// </remarks>
        public static void PreserveMapDocs(MapResult result, JObject document)
        {
            // The _key field should contain the key we want to use (as an array)
            JArray key = null;
            JToken keyToken;
            if (document.TryGetValue("_key", out keyToken))
            {
                key = keyToken as JArray;
            }

            // Ignore documents with the wrong key
            if (key == null)
            {
                result.Reject();
                return;
            }

            // Update the key
            result.SetKey(key, true);

            // Copy the rest of the values from the document
            foreach (var keyValue in document)
            {
                if (keyValue.Key == "_key") continue;
                result.Document[keyValue.Key] = keyValue.Value.DeepClone();
            }
        }
    }
}
