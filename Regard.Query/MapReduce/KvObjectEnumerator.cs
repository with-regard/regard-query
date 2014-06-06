using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Enumerator class that returns the contents of an IKvStoreEnumerator
    /// </summary>
    class KvObjectEnumerator : IPagedResultEnumerator<JObject>
    {
        private readonly IKeyValuePage m_CurrentPage;
        private IEnumerator<Tuple<JArray, JObject>> m_Enumerator;

        public KvObjectEnumerator(IKeyValuePage page)
        {
            if (page == null) throw new ArgumentNullException("page");

            m_CurrentPage = page;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Fetches the next result, or returns null if there are no more results
        /// </summary>
        public async Task<JObject> FetchNext()
        {
            if (m_Enumerator == null)
            {
                var enumerable = await m_CurrentPage.GetObjects();
                m_Enumerator = enumerable.GetEnumerator();
            }

            if (!m_Enumerator.MoveNext())
            {
                return null;
            }

            return m_Enumerator.Current.Item2;
        }

        /// <summary>
        /// The token to pass to the generator call for the next page, or null if this is the last page
        /// </summary>
        public async Task<string> GetNextPageToken()
        {
            return await m_CurrentPage.GetNextPageToken();
        }
    }
}
