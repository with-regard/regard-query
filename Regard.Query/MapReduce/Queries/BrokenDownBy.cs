using System.Linq;
using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    class BrokenDownBy : IComposableMapReduce
    {
        private readonly string m_FieldName;
        private readonly string m_OutputName;

        public BrokenDownBy(string fieldName, string outputName)
        {
            m_FieldName = fieldName;
            m_OutputName = outputName;
        }

        public void Map(MapResult result, JObject input)
        {
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
            result.AddKey(keyValue);
            result.SetValue(m_OutputName, keyValue);
        }

        public void Reduce(JObject result, JObject[] documents)
        {
            result[m_OutputName] = documents.First()[m_OutputName];
        }

        public void Rereduce(JObject result, JObject[] documents)
        {
            Reduce(result, documents);
        }

        public void Unreduce(JObject result, JObject[] documents, ref bool delete)
        {
            // Nothing to do
            // (Technically we should remove the name if the document count reaches 0)
        }
    }
}
