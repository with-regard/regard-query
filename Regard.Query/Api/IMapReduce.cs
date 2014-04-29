using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface that can be implemented by objects that can perform a map/reduce query
    /// </summary>
    public interface IMapReduce
    {
        /// <summary>
        /// Maps a document onto a target
        /// </summary>
        /// <param name="target">The target where the mapped documents should be emitted</param>
        /// <param name="document">The document that is being mapped</param>
        void Map(IMapTarget target, JObject document);

        /// <summary>
        /// Reduces a set of documents with the same key into a single output document
        /// </summary>
        /// <param name="key">The name of the key assigned to these documents during the Map operation</param>
        /// <param name="documents">The documents to reduce</param>
        /// <returns>An object representing the result of reducing these documents to the final value for this key</returns>
        JObject Reduce(string key, IEnumerable<JObject> documents);

        /// <summary>
        /// Updates the result of a reduce operation (combines the result of several reductions)
        /// </summary>
        /// <param name="key">The name of the key assigned to the document being reduced</param>
        /// <param name="reductions">The reductions to combine</param>
        /// <returns>An object representing the combined reduction</returns>
        JObject Rereduce(string key, IEnumerable<JObject> reductions);

        /// <summary>
        /// Retrieves the map/reduce function that is next in the chain (this is applied to the output of this function), or null
        /// if there's nothing next in the chain.
        /// </summary>
        IMapReduce Chain { get; }
    }
}
