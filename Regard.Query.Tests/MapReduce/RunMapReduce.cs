using System.Threading.Tasks;
using Regard.Query.Api;
using Regard.Query.MapReduce;
using Regard.Query.Serializable;

namespace Regard.Query.Tests.MapReduce
{
    static class RunMapReduce
    {
        /// <summary>
        /// Runs a map/reduce task on the basic documents
        /// </summary>
        public static async Task<IKeyValueStore> RunOnBasicDocuments(SerializableQuery query)
        {
            var mapReduce = query.GenerateMapReduce();
            var resultStore = new MemoryKeyValueStore();

            // Generate a data store and an ingestor
            var ingestor = new DataIngestor(mapReduce, resultStore);

            // Run the standard set of docs through
            // As there are no documents in the data store currently, this will reduce but not re-reduce
            await TestDataGenerator.Ingest12BasicDocuments(ingestor);

            return resultStore;
        }
    }
}
