using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.DataAccessor
{
    /// <summary>
    /// Methods used for accessing data relating to sessions
    /// </summary>
    class SessionDataStore
    {
        /// <summary>
        /// The underlying data store
        /// </summary>
        private IKeyValueStore m_RawSessionStore;

        public SessionDataStore(IKeyValueStore rawSesssionStore)
        {
            m_RawSessionStore = rawSesssionStore;
        }

        public Task StoreSessionData(string organization, string product, Guid sessionId, JObject sessionData)
        {
            return m_RawSessionStore.ChildStore(new JArray(organization, product)).SetValue(new JArray(sessionId.ToString()), sessionData);
        }
    }
}
