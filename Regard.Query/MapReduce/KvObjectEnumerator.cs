using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Enumerator class that returns the contents of an IKvStoreEnumerator
    /// </summary>
    class KvObjectEnumerator : IResultEnumerator<JObject>
    {
        private IKvStoreEnumerator m_Enumerator;

        public KvObjectEnumerator(IKvStoreEnumerator enumerator)
        {
            if (enumerator == null) throw new ArgumentNullException("enumerator");
            m_Enumerator = enumerator;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Fetches the next result, or returns null if there are no more results
        /// </summary>
        public async Task<JObject> FetchNext()
        {
            var nextPair = await m_Enumerator.FetchNext();
            if (nextPair == null)
            {
                return null;
            }

            return nextPair.Item2;
        }
    }
}
