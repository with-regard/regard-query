using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    /// <summary>
    /// Represents a composable map operation
    /// </summary>
    /// <remarks>
    /// Map and reduce are seperated out as operations as some query types only need to perform one of the two operations
    /// </remarks>
    internal interface IComposableMap
    {
        /// <summary>
        /// Updates the MapResult on the basis of the data stored in the input object
        /// </summary>
        /// <remarks>
        /// Updating the input object is also allowed: this makes it possible to provide alternative views of the input to other 
        /// composed map operations. This is useful for things like reformatting a date and then using the reformatted version to
        /// break down the results.
        /// </remarks>
        void Map(MapResult result, JObject input);
    }
}
