using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Regard.Query
{
    /// <summary>
    /// Utilities relating to javascript
    /// </summary>
    public static class JavascriptUtil
    {
        /// <summary>
        /// Converts a .NET string value to a fully quoted Javascript string
        /// </summary>
        public static string ToJsString(this string data)
        {
            if (data == null) return "null";
            return new JValue(data).ToString(Formatting.None);
        }
    }
}
