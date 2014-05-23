using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Key/value store that targets Azure data stores
    /// </summary>
    public class AzureKeyValueStore : IKeyValueStore
    {
        /// <summary>
        /// Creates a new key value store using an Azure table
        /// </summary>
        public AzureKeyValueStore(string connectionString, string tableName)
        {
            
        }

        public IKeyValueStore ChildStore(JArray key)
        {
            throw new System.NotImplementedException();
        }

        public Task SetValue(JArray key, JObject value)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> AppendValue(JObject value)
        {
            throw new System.NotImplementedException();
        }

        public Task<JObject> GetValue(JArray key)
        {
            throw new System.NotImplementedException();
        }

        public IKvStoreEnumerator EnumerateAllValues()
        {
            throw new System.NotImplementedException();
        }

        public IKvStoreEnumerator EnumerateValuesAppendedSince(long appendKey)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteChildStore(JArray key)
        {
            throw new System.NotImplementedException();
        }

        public Task Commit()
        {
            throw new System.NotImplementedException();
        }
    }
}
