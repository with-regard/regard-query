using System.Linq;
using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    class CountUniqueValues : IComposableMapReduce, IComposableChain
    {
        private readonly string m_FieldName;
        private readonly string m_Name;

        public CountUniqueValues(string fieldName, string name)
        {
            m_FieldName     = fieldName;
            m_Name          = name;

            ChainWith = new ChainCountUniqueValues(fieldName, name);
        }

        public void Map(MapResult result, JObject document)
        {
            // Mapping is just a matter of adding the field value to the key: this is what 'BrokenDownBy' does (except we need to remove the name and remember where in the key we are)
            // The value of the field is added to the key, and also to the result
            JToken keyToken;

            // Reject if no value
            if (!document.TryGetValue(m_FieldName, out keyToken))
            {
                result.Reject();
                return;
            }

            // Must be a value
            JValue keyValue = keyToken as JValue;
            if (keyValue == null)
            {
                result.Reject();
                return;
            }

            // The field value becomes part of the key and the value
            result.AddIndexKey(keyValue);
            result.SetValue(m_Name, keyValue);
        }

        public void Reduce(JObject result, JObject[] documents)
        {
            // If the key occurs, then it has a count of exactly one in the original
            result[m_Name] = 1;
        }

        public void Rereduce(JObject result, JObject[] documents)
        {
            Reduce(result, documents);
        }

        public void Unreduce(JObject result, JObject[] documents, ref bool delete)
        {
            // The count for this item falls to 0 if the count also falls to 0 (this assumes that this query is composed with a CountDocuments query)
            if (result["Count"].Value<long>() <= 0)
            {
                result[m_Name] = 0;
            }
            else
            {
                result[m_Name] = 1;
            }
        }

        public IComposableMapReduce ChainWith { get; private set; }

        /// <summary>
        /// Chained query for the CountUniqueValues query that produces the final results
        /// </summary>
        class ChainCountUniqueValues : IComposableMapReduce
        {
            private readonly string m_FieldName;
            private readonly string m_Name;

            public ChainCountUniqueValues(string fieldName, string name)
            {
                m_FieldName = fieldName;
                m_Name      = name;
            }

            public void Map(MapResult result, JObject document)
            {
                ChainQueryUtil.PreserveMapDocs(result, document);

                // Remove the field value from the key during the chained map
                // We need the index of the key to remove
                result.RemoveIndexKeys();

                result.SetValue("Count", (JValue)document["Count"]);
            }

            public void Reduce(JObject result, JObject[] reductions)
            {
                // Copy keys from the first document into the result, if they aren't already present
                foreach (var kvPair in reductions[0])
                {
                    // Preserve the rest
                    JToken value;
                    if (result.TryGetValue(kvPair.Key, out value))
                    {
                        continue;
                    }

                    result[kvPair.Key] = kvPair.Value;
                }

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
                    if (doc.TryGetValue(m_Name, out docCount))
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
                result[m_Name] = count;
            }

            public void Rereduce(JObject result, JObject[] documents)
            {
                Reduce(result, documents);
            }

            public void Unreduce(JObject result, JObject[] reductions, ref bool delete)
            {
                // Re-reduce with a null key for now
                // We need to un-reduce the rest of the query
                // TODO: this won't work if any un-reduce operation ever actually uses the key
                // query.Unreduce(null, result, reductions); -- won't work as it'll unreduce Count twice...

                // Subtract the values from the original query
                // (Note that the previous stage will have written '1' in here, but this shouldn't matter)
                int count = result[m_Name].Value<int>();

                foreach (var doc in reductions)
                {
                    JToken docCount;
                    if (doc.TryGetValue(m_Name, out docCount))
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
                result[m_Name] = count;
            }
        }
    }
}
