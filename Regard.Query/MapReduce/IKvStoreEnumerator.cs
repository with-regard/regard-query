using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce
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
    }
}
