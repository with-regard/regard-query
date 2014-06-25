using System.Linq;
using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    class IndexedBy : IComposableMapReduce, IComposableChain
    {
        private readonly string m_FieldName;
        private readonly string m_OutputName;

        public IndexedBy(string fieldName, string outputName)
        {
            m_FieldName     = fieldName;
            m_OutputName    = outputName;

            ChainWith       = new IndexChain();
        }

        public void Map(MapResult result, JObject input)
        {
            // Behaviour is the same as BrokenDownBy, except the field is an index field
            JToken keyToken;

            // Reject if no value
            if (!input.TryGetValue(m_FieldName, out keyToken))
            {
                result.Reject();
                return;
            }

            // Must be a value
            JValue keyValue = keyToken as JValue;
            if (keyValue == null)
            {
                result.Reject();
                return;
            }

            // The field value becomes part of the key and the value
            result.AddIndexKey(keyValue);
            result.SetValue(m_OutputName, keyValue);
        }

        public void Reduce(JObject result, JObject[] documents)
        {
            result[m_OutputName] = documents.First()[m_OutputName];
        }

        public void Rereduce(JObject result, JObject[] documents)
        {
            result[m_OutputName] = documents.First()[m_OutputName];
        }

        public void Unreduce(JObject result, JObject[] documents, ref bool delete)
        {
        }

        public IComposableMapReduce ChainWith { get; private set; }

        /// <summary>
        /// The index chain operation basically just removes the index key so that it generates aggregated results
        /// </summary>
        class IndexChain : IComposableMapReduce
        {
            public void Map(MapResult result, JObject document)
            {
                ChainQueryUtil.PreserveMapDocs(result, document);

                // Remove the field value from the key during the chained map
                // We need the index of the key to remove
                result.RemoveIndexKeys();

                result.SetValue("Count", (JValue)document["Count"]);
            }

            public void Reduce(JObject result, JObject[] reductions)
            {
                // Copy keys from the first document into the result, if they aren't already present
                foreach (var kvPair in reductions[0])
                {
                    // Preserve the rest
                    JToken value;
                    if (result.TryGetValue(kvPair.Key, out value))
                    {
                        continue;
                    }

                    result[kvPair.Key] = kvPair.Value;
                }
            }

            public void Rereduce(JObject result, JObject[] documents)
            {
                Reduce(result, documents);
            }

            public void Unreduce(JObject result, JObject[] documents, ref bool delete)
            {
            }
        }
    }
}
