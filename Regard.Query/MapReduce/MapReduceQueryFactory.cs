using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.MapReduce.Queries;
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
            var initialQuery = new CountDocuments().ToComposed();

            // Build the final query
            var finalQuery = AppendComponent(initialQuery, query);

            return finalQuery.ToMapReduce();
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
        private static IComposableMapReduce AppendComponent(IComposableMapReduce query, SerializableQuery component)
        {
            IComposableMapReduce result = query;

            // Build up the components that this query applies to
            if (component.AppliesTo != null)
            {
                result = AppendComponent(result, component.AppliesTo);
            }

            switch (component.Verb)
            {
                case QueryVerbs.AllEvents:
                    // This creates a new query
                    result = new CountDocuments().ToComposed();
                    break;

                case QueryVerbs.Only:
                    result = result.ComposeWith(new Only(component.Key, component.Value));
                    break;

                case QueryVerbs.BrokenDownBy:
                    result = result.ComposeWith(new BrokenDownBy(component.Key, component.Name));
                    break;

                case QueryVerbs.Sum:
                    result = result.ComposeWith(new SimpleMathOp(component.Key, component.Name, (a, b) => a + b, (a, b) => a-b));
                    break;
                   
                case QueryVerbs.CountUniqueValues:
                    result = result.ComposeWith(new CountUniqueValues(component.Key, component.Name));
                    break;

                case QueryVerbs.Mean:
                    result = result.ComposeWith(new Mean(component.Key, component.Name));
                    break;

                case QueryVerbs.Min:
                    // TODO: unreduce doesn't really undo the 'min' operation
                    result = result.ComposeWith(new SimpleMathOp(component.Key, component.Name, (a, b) => (a<b)?a:b, (a,b) => (a<b)?a:b));
                    break;

                case QueryVerbs.Max:
                    // TODO: unreduce doesn't really undo the 'max' operation
                    result = result.ComposeWith(new SimpleMathOp(component.Key, component.Name, (a, b) => (a>b)?a:b, (a,b) => (a>b)?a:b));
                    break;

                default:
                    // Not implemented
                    throw new NotImplementedException("Unknown query verb");
            }

            return result;
        }
    }
}
