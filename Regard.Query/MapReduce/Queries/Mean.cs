using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    class Mean : IComposableMapReduce
    {
        private readonly string m_FieldName;
        private readonly string m_Name;

        public Mean(string fieldName, string name)
        {
            m_FieldName = fieldName;
            m_Name      = name;
        }

        public void Map(MapResult result, JObject document)
        {
            JToken keyToken;

            double val = 0;
            long count = 1;

            if (!document.TryGetValue(m_FieldName, out keyToken))
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
            result.SetIntermediateValue(m_Name, JObject.FromObject(new { Value = val, Count = count }));

            // Also store the mean value for this element, which will just be the value with only one item 
            result.SetValue(m_Name, new JValue(val));
        }

        public void Reduce(JObject result, JObject[] documents)
        {
            double sum = 0.0;
            long count = 0;

            // Add up the values in the documents
            foreach (var doc in documents)
            {
                // Get the intermediate results for this value
                var intermediateDoc = doc.TryGetObject("__intermediate__").TryGetObject(m_Name);
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
            intermediateResult[m_Name] = meanIntermediate;

            // Store in the result
            if (count == 0)
            {
                result[m_Name] = double.NaN;
            }
            else
            {
                result[m_Name] = sum / (double)count;
            }
        }

        public void Rereduce(JObject result, JObject[] documents)
        {
            Reduce(result, documents);
        }

        public void Unreduce(JObject result, JObject[] documents, ref bool delete)
        {
            double sum = result["__intermediate__"][m_Name]["Value"].Value<double>();
            long count = result["__intermediate__"][m_Name]["Count"].Value<long>();

            // Subtract the values in the documents from the result
            foreach (var doc in documents)
            {
                JToken docValue;
                if (doc.TryGetValue(m_Name, out docValue))
                {
                    var intermediateDoc = doc.TryGetObject("__intermediate__").TryGetObject(m_Name);
                    if (intermediateDoc == null)
                    {
                        continue;
                    }

                    sum -= intermediateDoc["Value"].Value<double>();
                    count -= intermediateDoc["Count"].Value<long>();
                }
            }

            result["__intermediate__"][m_Name]["Value"] = sum;
            result["__intermediate__"][m_Name]["Count"] = count;

            if (count == 0)
            {
                result[m_Name] = double.NaN;
            }
            else
            {
                result[m_Name] = sum / (double)count;
            }
        }
    }
}
