using Newtonsoft.Json.Linq;

namespace Regard.Query.Serializable
{
    /// <summary>
    /// Utility method for creating/decoding querys using JSON
    /// </summary>
    public static class JsonQuery
    {
        /// <summary>
        /// Converts a serializable query into a JObject that can be used for storage or transmission
        /// </summary>
        public static JObject ToJson(this SerializableQuery query)
        {
            // Just create an empty object for the null query
            if (query == null)
            {
                return new JObject();
            }
            
            // Recursively build up the query
            JObject appliesTo = new JObject();
            if (query.AppliesTo != null)
            {
                appliesTo = query.ToJson();
            }

            // Create the result object
            JObject result = new JObject();

            result["verb"]          = query.Verb;
            result["applies-to"]    = appliesTo;

            if (query.Key != null)      result["key"]       = query.Key;
            if (query.Value != null)    result["value"]     = query.Value;
            if (query.Name != null)     result["name"]      = query.Name;

            return result;
        }
    }
}
