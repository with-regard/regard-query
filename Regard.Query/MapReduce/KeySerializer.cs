using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Regard.Query.Util;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Class that serializes/deserializes keys stored in JArrays to strings
    /// </summary>
    static class KeySerializer
    {
        /// <summary>
        /// Converts a token to a string
        /// </summary>
        public static string TokenToString(JToken token)
        {
            if (token == null) return "null";

            switch (token.Type)
            {
                // In keys, most tokens should be strings, but we'll handle floats and integers explicitly too
                case JTokenType.Float:
                    return token.Value<double>().ToString(CultureInfo.InvariantCulture);

                case JTokenType.Integer:
                    return token.Value<int>().ToString(CultureInfo.InvariantCulture);

                case JTokenType.String:
                    return token.Value<string>();

                default:
                    // For unknown types, use the JSON serialization
                    // Really, the map/reduce functions shouldn't produce keys with such tokens, but we'll make this behaviour explicitly defined to avoid edge cases
                    return token.ToString(Formatting.None);
            }
        }

        /// <summary>
        /// Converts a key array to a string
        /// </summary>
        public static string KeyToString(JArray key)
        {
            if (key == null) return "null";

            StringBuilder result = new StringBuilder();

            foreach (var token in key)
            {
                // Convert the token into a string
                var tokenString     = TokenToString(token);

                // Use a sanitised version (which is safe for storage in Azure tables, but which also makes the '-' character unique)
                var sanitizedToken  = StorageUtil.SanitiseKey(tokenString);

                // Append to the result
                result.Append(sanitizedToken);
                result.Append('-');
            }

            // Return the result
            return result.ToString();
        }

        /// <summary>
        /// Creates and adds a key to an existing JObject
        /// </summary>
        public static JObject CopyAndAddKey(JObject origin, JArray key)
        {
            var copy = (JObject) origin.DeepClone();
            copy["_key"] = key;
            return copy;
        }
    }
}
