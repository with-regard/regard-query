using System;
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
            throw new NotImplementedException();
        }
    }
}
