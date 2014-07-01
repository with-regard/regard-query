using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    class TransformDateFormat : IComposableMap
    {
        private readonly static DateTime s_EarlyDate = DateTime.Parse("1970-01-01T00:00:00+00:00", null, DateTimeStyles.RoundtripKind);

        private readonly string m_Key;
        private readonly string m_Name;
        private readonly string m_Format;

        public TransformDateFormat(string key, string name, string format)
        {
            m_Key       = key;
            m_Name      = name;
            m_Format    = format;
        }

        public void Map(MapResult result, JObject input)
        {
            // Get the token from the field that should contain a date
            JToken fieldValue;
            if (!input.TryGetValue(m_Key, out fieldValue) || fieldValue == null)
            {
                // Just do nothing if the field does not exist
                return;
            }

            // Parse as a date
            DateTime fieldTime;

            if (fieldValue.Type == JTokenType.Date)
            {
                // Already parsed as a date
                fieldTime = fieldValue.Value<DateTime>();
            }
            else if (fieldValue.Type == JTokenType.String)
            {
                // Try treating as an ISO8601 date
                if (!DateTime.TryParse(fieldValue.Value<string>(), null, DateTimeStyles.RoundtripKind, out fieldTime))
                {
                    // Can't use this field as a date
                    return;
                }
            }
            else
            {
                // The format of the field value si unknown
                return;
            }

            // Generate the result
            JToken newValue;
            
            switch (m_Format)
            {
                case "Days":
                    // Number of days as a number
                    newValue = new JValue((int) (fieldTime - s_EarlyDate).TotalDays);
                    break;

                default:
                    // Don't understand this format
                    newValue = new JValue("Query date format unknown");
                    break;
            }

            // Updating the input object makes the new value available to the rest of the query
            // TODO: this isn't really documented as something that you can do, but it's basically what's required in order to make this kind of operation work
            input[m_Name] = newValue;
        }
    }
}
