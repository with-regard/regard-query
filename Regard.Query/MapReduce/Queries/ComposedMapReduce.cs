using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.Queries
{
    /// <summary>
    /// Represents a map/reduce operation built up from smaller operations
    /// </summary>
    internal sealed class ComposedMapReduce : IComposableMapReduce, IMapReduce
    {
        private readonly List<IComposableMap> m_Maps; 
        private readonly List<IComposableReduce> m_Reduces;

        public ComposedMapReduce(IEnumerable<IComposableMap> maps, IEnumerable<IComposableReduce> reduces)
        {
            if (maps == null)       maps = new IComposableMap[0];
            if (reduces == null)    reduces = new IComposableReduce[0];

            m_Maps      = new List<IComposableMap>(maps);
            m_Reduces   = new List<IComposableReduce>(reduces);
        }

        public IEnumerable<IComposableMap> Maps
        {
            get { return m_Maps; }
        }

        public IEnumerable<IComposableReduce> Reduces
        {
            get { return m_Reduces; }
        }

        /// <summary>
        /// Updates the MapResult on the basis of the data stored in the input object
        /// </summary>
        public void Map(MapResult result, JObject input)
        {
            foreach (var map in m_Maps)
            {
                map.Map(result, input);
            }
        }

        /// <summary>
        /// Updates the supplied result object by reducing the documents, which may contain the results from other reduction operations
        /// </summary>
        /// <remarks>
        /// There is always at least one document to reduce
        /// </remarks>
        public void Reduce(JObject result, JObject[] documents)
        {
            foreach (var reduce in m_Reduces)
            {
                reduce.Reduce(result, documents);
            }
        }

        /// <summary>
        /// Re-reduces a set of documents that were previously processed by Reduce
        /// </summary>
        /// <remarks>
        /// For many type of reduce implementations, this can just call Reduce()
        /// </remarks>
        public void Rereduce(JObject result, JObject[] documents)
        {
            foreach (var reduce in m_Reduces)
            {
                reduce.Rereduce(result, documents);
            }
        }

        /// <summary>
        /// Unreduces a series of documents from the result
        /// </summary>
        /// <remarks>
        /// This is an extension to the traditional map/reduce scheme, required for supporting document deletion and query chaining
        /// </remarks>
        public void Unreduce(JObject result, JObject[] documents)
        {
            foreach (var reduce in m_Reduces)
            {
                reduce.Unreduce(result, documents);
            }
        }

        /// <summary>
        /// Maps a document onto a target
        /// </summary>
        /// <param name="target">The target where the mapped documents should be emitted</param>
        /// <param name="document">The document that is being mapped</param>
        public void Map(IMapTarget target, JObject document)
        {
            MapResult result = new MapResult();
            Map(result, document);
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
            var mapList = mappedDocuments.ToArray();
            JObject result = new JObject();

            Reduce(result, mapList);

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
            var mapList = reductions.ToArray();
            JObject result = new JObject();

            Rereduce(result, mapList);

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
            var mapList = mappedDocuments.ToArray();
            JObject result = new JObject();

            Unreduce(result, mapList);

            return result;
        }

        /// <summary>
        /// Retrieves the map/reduce function that is next in the chain (this is applied to the output of this function), or null
        /// if there's nothing next in the chain.
        /// </summary>
        /// <remarks>
        /// The documents passed to the chain will be the reduced result of this operation. Each document will have a '_key' field
        /// indicating the key that was reduced.
        /// </remarks>
        public ComposedMapReduce Chain { get; set; }

        IMapReduce IMapReduce.Chain { get { return Chain; } }

        /// <summary>
        /// Creates a copy of this object
        /// </summary>
        public ComposedMapReduce Copy()
        {
            var result = new ComposedMapReduce(m_Maps, m_Reduces);
            if (Chain != null)
            {
                result.Chain = Chain.Copy();
            }

            return result;
        }

        /// <summary>
        /// Appends an object to the end of the chained queries for this object
        /// </summary>
        public void AppendToChain(ComposedMapReduce newChain)
        {
            if (Chain == null)
            {
                Chain = newChain;
                return;
            }
            else
            {
                Chain.AppendToChain(newChain);
            }
        }
    }
}
