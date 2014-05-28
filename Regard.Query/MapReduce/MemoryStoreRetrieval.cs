using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Regard.Query.Api;

#pragma warning disable 1998

namespace Regard.Query.MapReduce
{
    class MemoryStoreRetrieval : IProductStoreRetrieval
    {
        private readonly object m_Sync = new object();
        private Dictionary<Tuple<string, string>, MemoryKeyValueStore> m_Stores = new Dictionary<Tuple<string, string>, MemoryKeyValueStore>();

        public async Task<IKeyValueStore> GetStoreForProduct(string organization, string product)
        {
            lock (m_Sync)
            {
                var tuple = new Tuple<string, string>(organization, product);
                MemoryKeyValueStore result;
                if (m_Stores.TryGetValue(tuple, out result))
                {
                    return result;
                }
                else
                {
                    m_Stores[tuple] = result = new MemoryKeyValueStore();
                    return result;
                }
            }
        }
    }
}
