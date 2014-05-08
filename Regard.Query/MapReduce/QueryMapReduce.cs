using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    // TODO: a way to add to both reduce/rereduce would be nice for the cases where the actions are actually identical

    /// <summary>
    /// A map/reduce operation that counts the number of events produced by the output
    /// </summary>
    class QueryMapReduce : IMapReduce
    {
        /// <summary>
        /// Function called during a map operation; used to compose actions together to generate the mapping operation
        /// </summary>
        public event Action<MapResult, JObject> OnMap;

        /// <summary>
        /// Function called during a reduce operation
        /// </summary>
        public event Action<JObject, IEnumerable<JObject>> OnReduce;

        /// <summary>
        /// Function called during a rereduce operation
        /// </summary>
        public event Action<JObject, IEnumerable<JObject>> OnRereduce;

        /// <summary>
        /// Maps a document onto a target
        /// </summary>
        /// <param name="target">The target where the mapped documents should be emitted</param>
        /// <param name="document">The document that is being mapped</param>
        public void Map(IMapTarget target, JObject document)
        {
            // Create a mapping result object
            MapResult result = new MapResult();

            // Perform the mapping operation
            var onMap = OnMap;
            if (onMap != null)
            {
                onMap(result, document);
            }

            // All we do is emit an empty document per document (as each document represents an event)
            result.Emit(target);
        }

        /// <summary>
        /// Reduces a set of documents with the same key into a single output document
        /// </summary>
        /// <param name="key">The name of the key assigned to these documents during the Map operation</param>
        /// <param name="mappedDocuments">The documents to reduce (emitted from the Map function)</param>
        /// <returns>An object representing the result of reducing these documents to the final value for this key</returns>
        public JObject Reduce(JArray key, IEnumerable<JObject> mappedDocuments)
        {
            // Actualise the list of documents in case they don't support multiple enumerations
            var mapList = mappedDocuments as IList<JObject> ?? mappedDocuments.ToList();

            // Merge the counts
            long count = 0;
            foreach (var doc in mappedDocuments)
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

            // Initial result is just an object containing a count
            var result = JObject.FromObject(new { Count = count });

            // Perform any extra reductions that are required
            var onReduce = OnReduce;
            if (onReduce != null)
            {
                onReduce(result, mapList);
            }

            return result;
        }

        /// <summary>
        /// Updates the result of a reduce operation (combines the result of several reductions)
        /// </summary>
        /// <param name="key">The name of the key assigned to the document being reduced</param>
        /// <param name="reductions">The reductions to combine</param>
        /// <returns>An object representing the combined reduction</returns>
        public JObject Rereduce(JArray key, IEnumerable<JObject> reductions)
        {
            // Actualise the reductions as a list
            var reductionList = reductions as IList<JObject> ?? reductions.ToList();

            // Merge the counts
            long count = 0;
            foreach (var doc in reductionList)
            {
                count += doc["Count"].Value<long>();
            }

            JObject result = JObject.FromObject(new { Count = count });

            // Perform any extra re-reduction operations necessary
            var onRereduce = OnRereduce;
            if (onRereduce != null)
            {
                onRereduce(result, reductionList);
            }

            return result;
        }

        /// <summary>
        /// Removes a set of documents from a reduction
        /// </summary>
        /// <param name="key">The key where documents are being deleted</param>
        /// <param name="reduced">The reduction with the documents included. The function is permitted to alter this object.</param>
        /// <param name="mappedDocuments">The set of mapped documents that are being removed from the result</param>
        /// <returns>The reduction with the mapped documents removed</returns>
        /// <remarks>
        /// This is needed to delete documents from the source set.
        /// </remarks>
        public JObject Unreduce(JArray key, JObject reduced, IEnumerable<JObject> mappedDocuments)
        {
            // Subtract the count to remove these documents
            reduced["Count"] = reduced["Count"].Value<long>() - mappedDocuments.Count();
            return reduced;
        }

        /// <summary>
        /// null, or the set of map/reduce operations that should be applied to the results of this operation
        /// </summary>
        public QueryMapReduce Chain { get; set; }

        /// <summary>
        /// null, or the set of map/reduce operations that should be applied to the results of this operation
        /// </summary>
        IMapReduce IMapReduce.Chain { get { return Chain; } }
    }
}
