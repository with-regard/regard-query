using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Interface implemented by objects that represent a key/value store that can be used by Regard's map/reduce system
    /// </summary>
    public interface IKeyValueStore
    {
        /// <summary>
        /// Retrieves a reference to a child key/value store with a particular key
        /// </summary>
        /// <remarks>
        /// When mapping/reducing data, we need a place to store the result; rather than sharing a 'single' data store (from the point of view of the caller), we allow
        /// for multiple stores.
        /// </remarks>
        Task<IKeyValueStore> ChildStore(JArray key);

        /// <summary>
        /// Stores a value in the database, indexed by a particular key
        /// </summary>
        Task SetValue(JArray key, JObject value);

        /// <summary>
        /// Retrieves null or the value associated with a particular key
        /// </summary>
        Task<JObject> GetValue(JArray key);
    }
}
