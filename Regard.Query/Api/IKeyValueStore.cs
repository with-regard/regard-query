using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that represent a key/value store that can be used by Regard's map/reduce system
    /// </summary>
    /// <remarks>
    /// Keys are compound items stored in a JArray (stores should be able to expect an array of strings to simplify things).
    /// </remarks>
    public interface IKeyValueStore
    {
        /// <summary>
        /// Retrieves a reference to a child key/value store with a particular key
        /// </summary>
        /// <remarks>
        /// When mapping/reducing data, we need a place to store the result; rather than sharing a 'single' data store (from the point of view of the caller), we allow
        /// for multiple stores.
        /// </remarks>
        IKeyValueStore ChildStore(JArray key);

        /// <summary>
        /// Stores a value in the database, indexed by a particular key
        /// </summary>
        Task SetValue(JArray key, JObject value);

        /// <summary>
        /// Assigns a key that is unique to this child store and uses it as a key to store a value. The store guarantees that this will be unique within this process, but not 
        /// if the same child store is being accessed by multiple processes.
        /// </summary>
        /// <param name="value">The value to store</param>
        /// <returns>
        /// A long representing the key assigned to the value. The key for GetValue can be obtained by putting this result (alone) in a JArray.
        /// The result is guaranteed to be positive, and will always increase.
        /// </returns>
        Task<long> AppendValue(JObject value);

        /// <summary>
        /// Retrieves null or the value associated with a particular key
        /// </summary>
        Task<JObject> GetValue(JArray key);

        /// <summary>
        /// Enumerates all of the values in this data store
        /// </summary>
        IKvStoreEnumerator EnumerateAllValues();

        /// <summary>
        /// Erases all of the values in a particular child store
        /// </summary>
        Task DeleteChildStore(JArray key);

        /// <summary>
        /// Waits for all of the pending SetValue requests to complete (if they are cached or otherwise write-through)
        /// </summary>
        Task Commit();
    }
}
