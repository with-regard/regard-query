using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

            // Build the final query
            AppendComponent(result, query);

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
                    query.Only(component.Key, component.Value);
                    break;

                case QueryVerbs.BrokenDownBy:
                    query.BrokenDownBy(component.Name, component.Key);
                    break;

                case QueryVerbs.Sum:
                    query.Sum(component.Name, component.Key);
                    break;
                   
                case QueryVerbs.CountUniqueValues:
                    query.CountUniqueValues(component.Name, component.Key);
                    break;

                default:
                    // Not implemented
                    throw new NotImplementedException("Unknown query verb");
            }
        }

        /// <summary>
        /// Composes an 'Only' operation to a QueryMapReduce object
        /// </summary>
        internal static void Only(this QueryMapReduce query, string key, string value)
        {
            // Reject queries that don't match the key
            query.OnMap += (mapResult, document) =>
            {
                JToken keyValue;

                // Reject if no value
                if (!document.TryGetValue(key, out keyValue))
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

                if (keyValue.Value<string>() != value)
                {
                    mapResult.Reject();
                    return;
                }

                // Accept this document
            };
        }

        /// <summary>
        /// Composes a 'BrokenDownBy' query with an existing map/reduce query
        /// </summary>
        internal static void BrokenDownBy(this QueryMapReduce query, string name, string fieldName)
        {
            // The value of the field is added to the key, and also to the result
            query.OnMap += (mapResult, document) =>
            {
                JToken keyToken;

                // Reject if no value
                if (!document.TryGetValue(fieldName, out keyToken))
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
                mapResult.SetValue(name, keyValue);
            };

            // Add the value of the field during the reduction
            Action<JObject, IEnumerable<JObject>> reduce = (result, documents) =>
            {
                result[name] = documents.First()[name];
            };

            query.OnReduce += reduce;

            // Don't need to do this for re-reduce as the value will already be present
        }

        /// <summary>
        /// Composes a 'Sum' query with an existing map/reduce query
        /// </summary>
        internal static void Sum(this QueryMapReduce query, string name, string fieldName)
        {
            // Store the numeric value of the field in the result
            // Reject items that don't contain this item
            query.OnMap += (mapResult, document) =>
            {
                JToken keyToken;

                // Reject if no value
                if (!document.TryGetValue(fieldName, out keyToken))
                {
                    mapResult.Reject();
                    return;
                }

                // Value must evaluate to double or int (we always treat it as double in the result)
                double val = 0;

                if (keyToken.Type == JTokenType.Integer)
                {
                    val = keyToken.Value<int>();
                }
                else if (keyToken.Type == JTokenType.Float)
                {
                    val = keyToken.Value<double>();
                }
                else
                {
                    mapResult.Reject();
                }

                // Store in the result
                mapResult.SetValue(name, new JValue(val));
            };

            // Sum values on reduce and re-reduce
            Action<JObject, IEnumerable<JObject>> reduce = (result, documents) =>
            {
                double sum = 0.0;

                // Add up the values in the documents
                foreach (var doc in documents)
                {
                    JToken docValue;
                    if (doc.TryGetValue(name, out docValue))
                    {
                        if (docValue.Type == JTokenType.Integer)
                        {
                            sum += docValue.Value<int>();
                        }
                        else if (docValue.Type == JTokenType.Float)
                        {
                            sum += docValue.Value<double>();
                        }
                    }
                }
            };

            query.OnReduce += reduce;
            query.OnRereduce += reduce;
        }

        /// <summary>
        /// Composes a map/reduce query that counts the number of unique values of a particular field
        /// </summary>
        internal static void CountUniqueValues(this QueryMapReduce query, string name, string fieldName)
        {
            string keyIndexKey = "_keyIndex_" + name;

            // Mapping is just a matter of adding the field value to the key: this is what 'BrokenDownBy' does (except we need to remove the name and remember where in the key we are)
            // The value of the field is added to the key, and also to the result
            query.OnMap += (mapResult, document) =>
            {
                JToken keyToken;

                // Reject if no value
                if (!document.TryGetValue(fieldName, out keyToken))
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
                int keyIndex = mapResult.AddKey(keyValue);
                mapResult.SetValue(name, keyValue);

                // Store the key index so we can remove from the key later on
                mapResult.SetValue(keyIndexKey, new JValue(keyIndex));
            };

            // If the key occurs, then it has a count of exactly one in the original
            Action<JObject, IEnumerable<JObject>> reduce = (result, documents) =>
            {
                result[keyIndexKey] = documents.First()[keyIndexKey];
                result[name] = 1;
            };

            query.OnReduce      += reduce;
            query.OnRereduce    += reduce;

            // Chain another map/reduce operation to count the results
            QueryMapReduce chainQuery = new QueryMapReduce();

            // Pass through the values from the original document
            chainQuery.PreserveMapDocs();

            // Remove the field value from the key during the chained map
            chainQuery.OnMap += (result, document) =>
            {
                // We need the index of the key to remove
                JToken keyIndexToken;
                int realKeyIndex = -1;

                if (!document.TryGetValue(keyIndexKey, out keyIndexToken))
                {
                    keyIndexToken = null;
                }

                if (keyIndexToken != null)
                {
                    if (keyIndexToken.Type == JTokenType.Integer)
                    {
                        realKeyIndex = keyIndexToken.Value<int>();
                    }
                    else if (keyIndexToken.Type == JTokenType.Float)
                    {
                        realKeyIndex = (int) keyIndexToken.Value<double>();
                    }
                }

                // Set the key to null if it exists
                if (realKeyIndex >= 0)
                {
                    result.RemoveKeyAtIndex(realKeyIndex);
                }
            };

            // Reduction operation should be a re-reduction of the first stage, except we sum the original values
            Action<JObject, IEnumerable<JObject>> chainReduce = (result, documents) =>
            {
                var reductions = documents as IList<JObject> ?? documents.ToList();

                // Re-reduce with a null key for now
                // TODO: this won't work if any re-reduce operation ever actually uses the key
                query.Rereduce(null, reductions);

                // Sum the values from the original query
                // (Note that the previous stage will have written '1' in here, but this shouldn't matter)
                int count = 0;

                foreach (var doc in reductions)
                {
                    JToken docCount;
                    if (doc.TryGetValue(name, out docCount))
                    {
                        if (docCount.Type == JTokenType.Integer)
                        {
                            count += docCount.Value<int>();
                        }
                        else if (docCount.Type == JTokenType.Float)
                        {
                            count += (int)docCount.Value<double>();
                        }
                    }
                }

                // Store the result
                result[name] = count;
            };

            chainQuery.OnReduce     += chainReduce;
            chainQuery.OnRereduce   += chainReduce;

            // Apply the chain
            chainQuery.Chain = query.Chain;
            query.Chain = chainQuery;
        }

        /// <summary>
        /// Adds map operations to a query that preserves the original input (using the _key field to generate the key for the result)
        /// </summary>
        /// <remarks>
        /// This leaves the reduce operation as the default (so we end up with a count of the number of documents)
        /// </remarks>
        internal static void PreserveMapDocs(this QueryMapReduce query)
        {
            query.OnMap += (result, document) =>
            {
                // The _key field should contain the key we want to use (as an array)
                JArray key = null;
                JToken keyToken;
                if (document.TryGetValue("_key", out keyToken))
                {
                    key = keyToken as JArray;
                }

                // Ignore documents with the wrong key
                if (key == null)
                {
                    result.Reject();
                    return;
                }

                // Update the key
                result.SetKey(key);

                // Copy the rest of the values from the document
                foreach (var keyValue in document)
                {
                    if (keyValue.Key == "_key") continue;
                    result.Document[keyValue.Key] = keyValue.Value.DeepClone();
                }
            };
        }
    }
}
