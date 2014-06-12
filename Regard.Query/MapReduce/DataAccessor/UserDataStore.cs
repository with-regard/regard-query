using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.DataAccessor
{
    /// <summary>
    /// Represents the actions that can be performed against a data store containing information about users for an individual product
    /// </summary>
    class UserDataStore
    {
        private readonly IKeyValueStore m_RawDataStore;

        public UserDataStore(IKeyValueStore rawDataStore)
        {
            m_RawDataStore = rawDataStore;
        }

        public async Task SetUserData(Guid userId, JObject userData)
        {
            await m_RawDataStore.SetValue(new JArray(userId.ToString()), userData);
            await m_RawDataStore.Commit();
        }
    }
}
