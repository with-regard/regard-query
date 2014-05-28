using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.DataAccessor
{
    /// <summary>
    /// Represents a data store that contains data for an individual product
    /// </summary>
    class IndividualProductDataStore
    {
        private readonly IKeyValueStore m_RawDataStore;

        public IndividualProductDataStore(IKeyValueStore rawDataStore)
        {
            if (rawDataStore == null) throw new ArgumentNullException("rawDataStore");
            m_RawDataStore = rawDataStore;

            Users = new UserDataStore(m_RawDataStore.ChildStore(new JArray("users")));
            Queries = new QueryDataStore(m_RawDataStore.ChildStore(new JArray("queries")));
        }

        public UserDataStore Users { get; private set; }
        public QueryDataStore Queries { get; private set; }

        public async Task DeleteQueryResults(string queryName, string nodeName)
        {
            await m_RawDataStore.DeleteChildStore(new JArray("query-results", queryName));
            var queryStatusStore = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));               // TODO: erase in other nodes too, I think
            await queryStatusStore.SetValue(new JArray(queryName), null);
        }

        public DataIngestor CreateIngestorForQuery(string queryName, string nodeName, IMapReduce mapReduceAlgorithm)
        {
            var thisQueryStore = m_RawDataStore.ChildStore(new JArray("query-results", queryName)).ChildStore(new JArray(nodeName));
            var ingestor = new DataIngestor(mapReduceAlgorithm, thisQueryStore);

            return ingestor;
        }

        public async Task<JObject> GetQueryStatus(string queryName, string nodeName)
        {
            var queryStatusStore    = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));
            var status              = await queryStatusStore.GetValue(new JArray(queryName));

            return status;
        }

        public async Task SetQueryStatus(string queryName, string nodeName, JObject newStatus)
        {
            var queryStatusStore = m_RawDataStore.ChildStore(new JArray("query-status", nodeName));
            await queryStatusStore.SetValue(new JArray(queryName), newStatus);
        }

        public IKeyValueStore GetRawEventStore(string nodeName)
        {
            return m_RawDataStore.ChildStore(new JArray("raw-events", nodeName));            
        }

        public IKeyValueStore GetQueryResults(string queryName, string nodeName)
        {
            var results = m_RawDataStore.ChildStore(new JArray("query-results", queryName)).ChildStore(new JArray(nodeName));
            return results;
        }

        public async Task Commit(string nodeName)
        {
            await GetRawEventStore(nodeName).Commit();
        }
    }
}
