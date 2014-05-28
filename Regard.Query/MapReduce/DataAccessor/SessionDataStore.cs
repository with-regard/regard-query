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
        private IProductStoreRetrieval m_RawSessionStore;

        public SessionDataStore(IProductStoreRetrieval rawSesssionStore)
        {
            m_RawSessionStore = rawSesssionStore;
        }

        public async Task StoreSessionData(string organization, string product, Guid sessionId, JObject sessionData)
        {
            var productStore = await m_RawSessionStore.GetStoreForProduct(organization, product);
            var sessions = productStore.ChildStore(new JArray("sessions"));

            await sessions.SetValue(new JArray(sessionId.ToString()), sessionData);
        }
    }
}
