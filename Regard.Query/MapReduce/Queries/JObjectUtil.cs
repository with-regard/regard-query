using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    internal static class JObjectUtil
    {
        /// <summary>
        /// Tries to read a field from an object, returning null if it doesn't exist
        /// </summary>
        public static JObject TryGetObject(this JToken source, string name)
        {
            if (source == null) return null;
            if (source.Type != JTokenType.Object) return null;

            JToken resultToken;
            if (!source.Value<JObject>().TryGetValue(name, out resultToken))
            {
                return null;
            }

            if (resultToken.Type != JTokenType.Object) return null;

            return resultToken.Value<JObject>();
        }
    }
}
