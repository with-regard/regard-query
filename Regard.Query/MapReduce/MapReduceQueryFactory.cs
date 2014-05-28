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
    public static class MapReduceQueryFactory
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
        /// Generates the map/reduce chain corresponding to a JSON-encoded query
        /// </summary>
        public static IMapReduce GenerateMapReduce(JObject serializedQuery)
        {
            var queryBuilder = new SerializableQueryBuilder(null);
            var realQuery = (SerializableQuery) queryBuilder.FromJson(serializedQuery);

            return GenerateMapReduce(realQuery);
        }

        /// <summary>
        /// Appends a query component to a query
        /// </summary>
        /// <param name="query">The query to append to</param>
        /// <param name="component">The component to append</param>
        private static void AppendComponent(QueryMapReduce query, SerializableQuery component)
        {
            // Build up the components that this query applies to
            if (component.AppliesTo != null)
            {
                AppendComponent(query, component.AppliesTo);
            }

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

                case QueryVerbs.Mean:
                    query.Mean(component.Name, component.Key);
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
            query.ReduceAndRereduce((result, documents) =>
            {
                result[name] = documents.First()[name];
            });

            // For unreduce: we don't need to remove the name unless the count reaches 0: for the moment we'll just do nothing
        }

        /// <summary>
        /// Tries to read a field from an object, returning null if it doesn't exist
        /// </summary>
        private static JObject TryGetObject(this JToken source, string name)
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

        /// <summary>
        /// Composes a 'Mean' query with an existing map/reduce query
        /// </summary>
        internal static void Mean(this QueryMapReduce query, string name, string fieldName)
        {
            // During map/reduce, store an intermediate value in a 'hidden' key
            query.OnMap += (mapResult, document) =>
            {
                JToken keyToken;

                double val = 0;
                long count = 1;

                if (!document.TryGetValue(fieldName, out keyToken))
                {
                    // If the value doesn't exist, the value is 0
                    val = 0;
                    count = 0;
                }
                else
                {
                    // Value must evaluate to double or int (we always treat it as double in the result)
                    if (keyToken.Type == JTokenType.Integer)
                    {
                        val = keyToken.Value<long>();
                    }
                    else if (keyToken.Type == JTokenType.Float)
                    {
                        val = keyToken.Value<double>();
                    }
                    else
                    {
                        // If the value isn't numeric, treat it as 0
                        val = 0;
                        count = 0;
                    }
                }

                // Store an intermediate result with the total value and the count
                mapResult.SetIntermediateValue(name, JObject.FromObject(new { Value = val, Count = count}));

                // Also store the mean value for this element, which will just be the value with only one item 
                mapResult.SetValue(name, new JValue(val));
            };

            // Sum values on reduce and re-reduce
            query.ReduceAndRereduce((result, documents) =>
            {
                double  sum     = 0.0;
                long    count   = 0;

                // Add up the values in the documents
                foreach (var doc in documents)
                {
                    // Get the intermediate results for this value
                    var intermediateDoc = doc.TryGetObject("__intermediate__").TryGetObject(name);
                    if (intermediateDoc == null)
                    {
                        continue;
                    }

                    sum += intermediateDoc["Value"].Value<double>();
                    count += intermediateDoc["Count"].Value<long>();
                }

                // Store intermediate results
                var intermediateResult = result.TryGetObject("__intermediate__");
                if (intermediateResult == null)
                {
                    result["__intermediate__"] = intermediateResult = new JObject();
                }

                var meanIntermediate = JObject.FromObject(new { Value = sum, Count = count });
                intermediateResult[name] = meanIntermediate;

                // Store in the result
                if (count == 0)
                {
                    result[name] = double.NaN;
                }
                else
                {
                    result[name] = sum / (double) count;
                }
            });

            query.OnUnreduce += (result, documents) =>
            {
                double  sum     = result["__intermediate__"][name]["Value"].Value<double>();
                long    count   = result["__intermediate__"][name]["Count"].Value<long>();

                // Subtract the values in the documents from the result
                foreach (var doc in documents)
                {
                    JToken docValue;
                    if (doc.TryGetValue(name, out docValue))
                    {
                        var intermediateDoc = doc.TryGetObject("__intermediate__").TryGetObject(name);
                        if (intermediateDoc == null)
                        {
                            continue;
                        }

                        sum -= intermediateDoc["Value"].Value<double>();
                        count -= intermediateDoc["Count"].Value<long>();
                    }
                }

                result["__intermediate__"][name]["Value"] = sum;
                result["__intermediate__"][name]["Count"] = count;

                if (count == 0)
                {
                    result[name] = double.NaN;
                }
                else
                {
                    result[name] = sum / (double)count;
                }
            };
        }

        /// <summary>
        /// Composes a 'Sum' query with an existing map/reduce query
        /// </summary>
        internal static void Sum(this QueryMapReduce query, string name, string fieldName)
        {
            // Store the numeric value of the field in the result
            query.OnMap += (mapResult, document) =>
            {
                JToken keyToken;

                double val = 0;

                if (!document.TryGetValue(fieldName, out keyToken))
                {
                    // If the value doesn't exist, the value is 0
                    val = 0;
                }
                else
                {
                    // Value must evaluate to double or int (we always treat it as double in the result)
                    if (keyToken.Type == JTokenType.Integer)
                    {
                        val = keyToken.Value<long>();
                    }
                    else if (keyToken.Type == JTokenType.Float)
                    {
                        val = keyToken.Value<double>();
                    }
                    else
                    {
                        // If the value isn't numeric, treat it as 0
                        val = 0;
                    }
                }

                // Store in the result
                mapResult.SetValue(name, new JValue(val));
            };

            // Sum values on reduce and re-reduce
            query.ReduceAndRereduce((result, documents) =>
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

                // Store in the result
                result[name] = sum;
            });

            query.OnUnreduce += (result, documents) =>
            {
                double sum = result[name].Value<double>();

                // Subtract the values in the documents from the result
                foreach (var doc in documents)
                {
                    JToken docValue;
                    if (doc.TryGetValue(name, out docValue))
                    {
                        if (docValue.Type == JTokenType.Integer)
                        {
                            sum -= docValue.Value<int>();
                        }
                        else if (docValue.Type == JTokenType.Float)
                        {
                            sum -= docValue.Value<double>();
                        }
                    }
                }

                result[name] = sum;
            };
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
            query.ReduceAndRereduce((result, documents) =>
            {
                result[keyIndexKey] = documents.First()[keyIndexKey];
                result[name] = 1;
            });

            query.OnUnreduce += (result, documents) =>
            {
                // The count for this item falls to 0 if the count also falls to 0
                if (result["Count"].Value<long>() <= 0)
                {
                    result[name] = 0;
                }
                else
                {
                    result[name] = 1;
                }
            };

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

                result.SetValue("Count", (JValue) document["Count"]);
            };

            // Reduction operation should be a re-reduction of the first stage, except we sum the original values
            chainQuery.ReduceAndRereduce((result, documents) =>
            {
                var reductions = documents as IList<JObject> ?? documents.ToList();

                // Re-reduce with a null key for now
                // TODO: this won't work if any re-reduce operation ever actually uses the key
                // TODO: this also won't work because it doesn't update the result
                // query.Rereduce(null, reductions);

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
            });

            chainQuery.OnUnreduce += (result, documents) =>
            {
                var reductions = documents as IList<JObject> ?? documents.ToList();

                // Re-reduce with a null key for now
                // We need to un-reduce the rest of the query
                // TODO: this won't work if any un-reduce operation ever actually uses the key
                // query.Unreduce(null, result, reductions); -- won't work as it'll unreduce Count twice...

                // Subtract the values from the original query
                // (Note that the previous stage will have written '1' in here, but this shouldn't matter)
                int count = result[name].Value<int>();

                foreach (var doc in reductions)
                {
                    JToken docCount;
                    if (doc.TryGetValue(name, out docCount))
                    {
                        if (docCount.Type == JTokenType.Integer)
                        {
                            count -= docCount.Value<int>();
                        }
                        else if (docCount.Type == JTokenType.Float)
                        {
                            count -= (int)docCount.Value<double>();
                        }
                    }
                }

                // Store the result
                result[name] = count;
            };

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
