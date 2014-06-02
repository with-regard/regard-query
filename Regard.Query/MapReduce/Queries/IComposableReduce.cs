using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    /// <summary>
    /// Represents a composable reduce/rereduce/unreduce operation
    /// </summary>
    internal interface IComposableReduce
    {
        /// <summary>
        /// Updates the supplied result object by reducing the documents, which may contain the results from other reduction operations
        /// </summary>
        /// <remarks>
        /// There is always at least one document to reduce
        /// </remarks>
        void Reduce(JObject result, JObject[] documents);

        /// <summary>
        /// Re-reduces a set of documents that were previously processed by Reduce
        /// </summary>
        /// <remarks>
        /// For many type of reduce implementations, this can just call Reduce()
        /// </remarks>
        void Rereduce(JObject result, JObject[] documents);

        /// <summary>
        /// Unreduces a series of documents from the result
        /// </summary>
        /// <param name="result">Initially this is a copy of the document as it was. This call should modify this to reverse the effects of reducing the documents
        /// array.</param>
        /// <param name="documents">The documents to remove from the results (the output of the map function)</param>
        /// <param name="delete">Initially set to false, can be set to true to indicate the result should be removed</param>
        /// <remarks>
        /// This is an extension to the traditional map/reduce scheme, required for supporting document deletion and query chaining
        /// </remarks>
        void Unreduce(JObject result, JObject[] documents, ref bool delete);
    }
}