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
        /// Retrieves a page of objects from the list
        /// </summary>
        /// <param name="pageToken">null to retrieve the first page in the list, otherwise a value returned by IKeyValuePage.NextPageToken</param>
        Task<IKeyValuePage> FetchPage(string pageToken);
    }
}
