using System;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

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
                appliesTo = query.AppliesTo.ToJson();
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

        /// <summary>
        /// Retrieves a string value from an object, throwing an exception if the format is incorrect or if the value is mandatory but not present
        /// </summary>
        private static string GetString(this JObject obj, string key, bool mandatory = false)
        {
            JToken value;

            // Get the value from the object
            if (obj != null && obj.TryGetValue(key, out value) && value.Type != JTokenType.Null)
            {
                // Value exists
                if (value.Type == JTokenType.String)
                {
                    // ... and is a string, so it's correct
                    return value.Value<string>();
                }
                else
                {
                    throw new InvalidOperationException(key + " must be a string value");
                }
            }
            else
            {
                // Value doesn't exist
                if (mandatory)
                {
                    // TODO: custom exception type? We'll use IOE for format errors for now
                    throw new InvalidOperationException(key + " is a mandatory field for a JSON-formatted query");
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Converts a JObject into a query using a query builder
        /// </summary>
        public static IRegardQuery FromJson(this IQueryBuilder builder, JObject json)
        {
            if (builder == null) throw new ArgumentNullException("builder");

            // Null or empty objects produce a null result
            if (json == null || json.Count == 0)
            {
                return null;
            }

            // Fetch the 'applies-to' field and process it
            IRegardQuery    appliesTo           = null;
            JToken          appliesToObject;
            if (json.TryGetValue("applies-to", out appliesToObject))
            {
                if (appliesToObject.Type == JTokenType.Object)
                {
                    appliesTo = builder.FromJson(appliesToObject.Value<JObject>());
                }
                else
                {
                    // TODO: custom exception type for bad queries? We'll use IOE for now
                    throw new InvalidOperationException("applies-to field exists but does not contain an object");
                }
            }

            // The action depends on the verb
            // We validate the JSON here so the behaviour when fields are missing is well-defined
            string verb = GetString(json, "verb", true);

            switch (verb)
            {
                case QueryVerbs.AllEvents:
                    if (appliesTo != null)
                    {
                        throw new InvalidOperationException("AllEvents cannot be applied to an existing query");
                    }
                    return builder.AllEvents();

                case QueryVerbs.BrokenDownBy:
                    if (appliesTo == null)
                    {
                        throw new InvalidOperationException("BrokenDownBy must be applied to an existing query");
                    }
                    return builder.BrokenDownBy(appliesTo, GetString(json, "key", true), GetString(json, "name"));

                case QueryVerbs.CountUniqueValues:
                    if (appliesTo == null)
                    {
                        throw new InvalidOperationException("CountUniqueValues must be applied to an existing query");
                    }
                    return builder.CountUniqueValues(appliesTo, GetString(json, "key", true), GetString(json, "name"));

                case QueryVerbs.Sum:
                    if (appliesTo == null)
                    {
                        throw new InvalidOperationException("Sum must be applied to an existing query");
                    }
                    return builder.Sum(appliesTo, GetString(json, "key", true), GetString(json, "name"));

                case QueryVerbs.Only:
                    if (appliesTo == null)
                    {
                        throw new InvalidOperationException("Only must be applied to an existing query");
                    }
                    return builder.Only(appliesTo, GetString(json, "key", true), GetString(json, "value"));

                default:
                    throw new InvalidOperationException("Unknown JSON verb: " + verb);
            }
        }
    }
}
