using System;
using System.Collections.Generic;
using Microsoft.Ted.Wacel;
using Newtonsoft.Json.Linq;
using Regard.Query.Serializable;

namespace Regard.Query.Wacel
{
    /// <summary>
    /// Class that produces cube dimensions from a serialized query
    /// </summary>
    public class CubeDimensionHelper
    {
        /// <summary>
        /// The query that this will help out with
        /// </summary>
        private readonly SerializableQuery m_Query;

        /// <summary>
        /// Dictionary mapping property names to cube dimensions
        /// </summary>
        private readonly Dictionary<string, Dimension> m_Dimensions = new Dictionary<string, Dimension>();

        /// <summary>
        /// Finds the cube dimensions relevant to a particular query
        /// </summary>
        private void FillDimensions(SerializableQuery query)
        {
            // Recursively fill in the dimensions for the query that this applies to
            if (query == null)
            {
                return;
            }
            FillDimensions(query.AppliesTo);

            // Work out the dimensions introduced by this query element
            switch (query.Verb)
            {
                case QueryVerbs.AllEvents:
                case QueryVerbs.Only:
                    // Doesn't introduce a dimension (is used to filter events instead, for example)
                    break;

                case QueryVerbs.BrokenDownBy:
                case QueryVerbs.CountUniqueValues:
                    {
                        var dimension = new Dimension(query.Key);
                        dimension.DimensionType = DimensionType.Free;
                        m_Dimensions[query.Key] = dimension;
                    }
                    break;

                case QueryVerbs.Sum:
                    // Not implemented yet
                    break;
            }
        }

        public CubeDimensionHelper(SerializableQuery query)
        {
            // Nothing to do if the query isn't present
            if (query == null) throw new ArgumentNullException("query");

            m_Query = query;

            FillDimensions(m_Query);
        }

        /// <summary>
        /// Creates the data for an event (returns null if the event is missing required data)
        /// </summary>
        public DataPoint CreateData(JObject evt)
        {
            // Sanity check
            if (evt == null)
            {
                return null;
            }

            // Generate the data point
            DataPoint dataPoint     = new DataPoint();
            dataPoint.Coordinates   = new List<DimensionNode>();

            foreach (var dim in m_Dimensions)
            {
                // Get the value of the field for this dimension
                JToken token;
                if (!evt.TryGetValue(dim.Key, out token))
                {
                    return null;
                }

                JValue asValue = token as JValue;
                if (asValue == null || asValue.Value == null)
                {
                    return null;
                }

                var stringValue = asValue.Value.ToString();

                dataPoint.Coordinates.Add(new DimensionNode(stringValue, dim.Value, DimensionNodeType.Independant));
            }

            return dataPoint;
        }
    }
}
