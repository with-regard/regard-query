using System;
using Newtonsoft.Json.Linq;

namespace Regard.Query.Serializable
{
    /// <summary>
    /// Utilities for creating filters from serializable queries
    /// </summary>
    public static class SerialFilter
    {
        /// <summary>
        /// Creates a filter that returns true if an event (specified by JSON) will be used as part of a query.
        /// </summary>
        public static Func<JObject, bool> CreateFilter(this SerializableQuery query)
        {
            var filter = CreateFilterOrNull(query);

            if (filter == null)
            {
                filter = obj => true;
            }

            return filter;
        }

        /// <summary>
        /// Creates a filter that returns true if an event (specified by JSON) will be used as part of a query. Returns null if all events are allowed through
        /// </summary>
        private static Func<JObject, bool> CreateFilterOrNull(SerializableQuery query)
        {
            // The null query matches nothing
            if (query == null) return obj => false;

            // Recursively create the filter from the remainder of the query
            Func<JObject, bool> matchNext;
            if (query.AppliesTo != null)
            {
                matchNext = CreateFilterOrNull(query.AppliesTo);
            }
            else
            {
                matchNext = null;
            }

            // Create a new query based on the verb
            Func<JObject, bool> matchThis = null;

            switch (query.Verb)
            {
                case QueryVerbs.Only:
                    // The key must exist and have the specified value
                    matchThis = obj =>
                        {
                            JToken token;
                            if (obj.TryGetValue(query.Key, out token))
                            {
                                JValue value = token as JValue;

                                // If this is not a value, then this doesn't match
                                if (value == null)
                                {
                                    return false;
                                }

                                // If the value is null then this doesn't match
                                if (value.Value == null)
                                {
                                    return false;
                                }

                                // Otherwise perform a string match
                                if (value.Value.ToString() == query.Value)
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        };
                    break;
            }

            // Compose the function so that all the conditions are applied to the event
            if (matchThis == null)
            {
                return matchNext;
            }
            else
            {
                if (matchNext == null)
                {
                    return matchThis;
                }
                else
                {
                    return obj => matchNext(obj) && matchThis(obj);
                }
            }
        }
    }
}
