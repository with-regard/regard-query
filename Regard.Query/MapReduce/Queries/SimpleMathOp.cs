using System;
using Newtonsoft.Json.Linq;

namespace Regard.Query.MapReduce.Queries
{
    class SimpleMathOp : IComposableMapReduce
    {
        private readonly string m_FieldName;
        private readonly string m_OutputName;
        private readonly Func<double, double, double> m_Do;
        private readonly Func<double, double, double> m_Undo;

        /// <summary>
        /// Creates a new map/reduce operation that combines values in a particular field through a mathematical operation
        /// </summary>
        /// <param name="fieldName">The field name in the input</param>
        /// <param name="outputName">The name of the field where the output should be stored</param>
        /// <param name="do">A function that performs the operation</param>
        /// <param name="undo">A function that undoes the operation (returning the result of removing the second result from the first)</param>
        public SimpleMathOp(string fieldName, string outputName, Func<double, double, double> @do, Func<double, double, double> undo)
        {
            m_FieldName     = fieldName;
            m_OutputName    = outputName;
            m_Do            = @do;
            m_Undo          = undo;
        }

        public void Map(MapResult result, JObject input)
        {
            // Map the field from the input to the output if it can be interpreted as a double
            JToken value;
            if (input.TryGetValue(m_FieldName, out value))
            {
                if (value.Type == JTokenType.Integer || value.Type == JTokenType.Float)
                {
                    result.SetValue(m_OutputName, new JValue(value.Value<double>()));
                }
                else if (value.Type == JTokenType.String)
                {
                    double doubleValue;
                    if (double.TryParse(value.Value<string>(), out doubleValue))
                    {
                        result.SetValue(m_OutputName, new JValue(doubleValue));
                    }
                }
            }
        }

        public void Reduce(JObject result, JObject[] documents)
        {
            double total = 0;
            bool firstResult = true;

            foreach (var doc in documents)
            {
                // Ignore documents that don't contain a valid value
                JToken thisValToken;
                if (!doc.TryGetValue(m_OutputName, out thisValToken))
                {
                    continue;
                }

                if (thisValToken.Type != JTokenType.Integer && thisValToken.Type != JTokenType.Float)
                {
                    continue;
                }

                if (firstResult)
                {
                    // If there's only one result, then there's no operation to perform
                    total = thisValToken.Value<double>();
                    firstResult = false;
                }
                else
                {
                    // Otherwise, combine to create the final result
                    total = m_Do(total, thisValToken.Value<double>());
                }
            }

            // Store the total
            if (!firstResult)
            {
                result[m_OutputName] = total;
            }
        }

        public void Rereduce(JObject result, JObject[] documents)
        {
            // Same behaviour as reduce
            Reduce(result, documents);
        }

        public void Unreduce(JObject result, JObject[] documents)
        {
            double total = 0;

            JToken resultToken;
            if (!result.TryGetValue(m_OutputName, out resultToken))
            {
                // There are no values in the result
                return;
            }

            if (resultToken.Type != JTokenType.Integer || resultToken.Type != JTokenType.Float)
            {
                // Result value is not a double
                return;
            }

            total = resultToken.Value<double>();

            foreach (var doc in documents)
            {
                // Ignore documents that don't contain a valid value
                JToken thisValToken;
                if (!doc.TryGetValue(m_OutputName, out thisValToken))
                {
                    continue;
                }

                if (thisValToken.Type != JTokenType.Integer && thisValToken.Type != JTokenType.Float)
                {
                    continue;
                }

                    total = m_Undo(total, thisValToken.Value<double>());
            }

            // Store the total
            result[m_OutputName] = total;
        }
    }
}
