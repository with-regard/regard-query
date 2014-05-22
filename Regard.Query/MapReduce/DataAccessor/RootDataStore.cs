using System;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.DataAccessor
{
    /// <summary>
    /// Class that provides actions relating to the base data store used by the map-reduce system
    /// </summary>
    class RootDataStore
    {
        private readonly IKeyValueStore m_RawRootStore;

        public RootDataStore(IKeyValueStore rawRootStore)
        {
            if (rawRootStore == null) throw new ArgumentNullException("rawRootStore");
            m_RawRootStore = rawRootStore;

            ProductDataStore = new ProductDataStore(m_RawRootStore.ChildStore(new JArray("products")));
            SessionDataStore = new SessionDataStore(m_RawRootStore.ChildStore(new JArray("sessions")));
        }

        public ProductDataStore ProductDataStore { get; private set; }
        public SessionDataStore SessionDataStore { get; private set; }
    }
}
