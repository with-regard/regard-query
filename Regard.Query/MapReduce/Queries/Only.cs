using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    /// <summary>
    /// The 'Only' query
    /// </summary>
    internal class Only : IComposableMap
    {
        private readonly string m_Field;
        private readonly string m_Value;

        public Only(string field, string value)
        {
            m_Field = field;
            m_Value = value;
        }

        public void Map(MapResult result, JObject document)
        {
            JToken keyValue;

            // Reject if no value
            if (!document.TryGetValue(m_Field, out keyValue))
            {
                result.Reject();
                return;
            }

            // Reject if wrong value
            if (keyValue.Type != JTokenType.String)
            {
                result.Reject();
                return;
            }

            if (keyValue.Value<string>() != m_Value)
            {
                result.Reject();
                return;
            }

            // Accept this document
        }
    }
}
