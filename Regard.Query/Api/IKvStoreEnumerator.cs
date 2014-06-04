using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that enumerate the results from a key-value store
    /// </summary>
    public interface IKvStoreEnumerator
    {
        /// <summary>
        /// Retrieves the next object in the list
        /// </summary>
        Task<Tuple<JArray, JObject>> FetchNext();

        /// <summary>
        /// Retrieves the 'numberth' object in the list from a key-value store, or null if it doesn't exist.
        /// </summary>
        Task<Tuple<JArray, JObject>> FastForward(int number);
    }
}
