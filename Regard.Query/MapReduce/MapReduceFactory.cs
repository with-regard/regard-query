using System;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Class that generates map/reduce queries for serialised queries
    /// </summary>
    public static class MapReduceFactory
    {
        /// <summary>
        /// Generates the map/reduce chain corresponding to the specified serializable query
        /// </summary>
        public static IMapReduce GenerateMapReduce(this SerializableQuery query)
        {
            // The default action is all events
            var result = new QueryMapReduce();

            return result;
        }

        /// <summary>
        /// Appends a query component to a query
        /// </summary>
        /// <param name="query">The query to append to</param>
        /// <param name="component">The component to append</param>
        private static void AppendComponent(QueryMapReduce query, SerializableQuery component)
        {
            switch (component.Verb)
            {
                case QueryVerbs.AllEvents:
                    // Nothing to do for this verb (it's the default action for a map/reduce query)
                    break;

                case QueryVerbs.Only:
                    // Reject queries that don't match the key
                    query.OnMap += (mapResult, document) =>
                    {
                        JToken keyValue;

                        // Reject if no value
                        if (!document.TryGetValue(component.Key, out keyValue))
                        {
                            mapResult.Reject();
                            return;
                        }

                        // Reject if wrong value
                        if (keyValue.Type != JTokenType.String)
                        {
                            mapResult.Reject();
                            return;
                        }

                        if (keyValue.Value<string>() != component.Value)
                        {
                            mapResult.Reject();
                            return;
                        }

                        // Accept this document
                    };
                    break;

                case QueryVerbs.BrokenDownBy:
                    query.OnMap += (mapResult, document) =>
                    {
                        JToken keyToken;

                        // Reject if no value
                        if (!document.TryGetValue(component.Key, out keyToken))
                        {
                            mapResult.Reject();
                            return;
                        }

                        // Must be a value
                        JValue keyValue = keyToken as JValue;
                        if (keyValue == null)
                        {
                            mapResult.Reject();
                            return;
                        }

                        // The field value becomes part of the key and the value
                        mapResult.AddKey(keyValue);
                        mapResult.SetValue(component.Key, keyValue);
                    };
                    break;

                case QueryVerbs.Sum:
                case QueryVerbs.CountUniqueValues:
                default:
                    // Not implemented
                    throw new NotImplementedException("Unknown query verb");
            }
        }
    }
}
