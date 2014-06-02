using System.Collections;
using System.Collections.Generic;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.Queries
{
    internal static class Compositor
    {
        /// <summary>
        /// Returns the result of combining two Ienumerables
        /// </summary>
        private static IEnumerable<T> Combine<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            foreach (var obj in a) yield return obj;
            foreach (var obj in b) yield return obj;
        }

        /// <summary>
        /// Converts an IComposableMapReduce object to an IMapReduce object
        /// </summary>
        public static IMapReduce ToMapReduce(this IComposableMapReduce obj)
        {
            return obj.ToComposed();
        }

        /// <summary>
        /// Convert a single composable object to a composed object
        /// </summary>
        public static ComposedMapReduce ToComposed(this IComposableMapReduce obj)
        {
            ComposedMapReduce alreadyComposed = obj as ComposedMapReduce;
            if (alreadyComposed != null)
            {
                return alreadyComposed;
            }

            var result = new ComposedMapReduce(new[] { obj }, new[] { obj });

            IComposableChain chain = obj as IComposableChain;
            if (chain != null)
            {
                result.Chain = chain.ChainWith.ToComposed();
            }

            return result;
        }

        /// <summary>
        /// Convert a single composable object to a composed object
        /// </summary>
        public static ComposedMapReduce ToComposed(this IComposableMap obj)
        {
            ComposedMapReduce alreadyComposed = obj as ComposedMapReduce;
            if (alreadyComposed != null)
            {
                return alreadyComposed;
            }

            var result = new ComposedMapReduce(new[] { obj }, new IComposableReduce[0]);

            IComposableChain chain = obj as IComposableChain;
            if (chain != null)
            {
                result.Chain = chain.ChainWith.ToComposed();
            }

            return result;
        }

        /// <summary>
        /// Convert a single composable object to a composed object
        /// </summary>
        public static ComposedMapReduce ToComposed(this IComposableReduce obj)
        {
            ComposedMapReduce alreadyComposed = obj as ComposedMapReduce;
            if (alreadyComposed != null)
            {
                return alreadyComposed;
            }

            var result = new ComposedMapReduce(new IComposableMap[0], new[] { obj });

            IComposableChain chain = obj as IComposableChain;
            if (chain != null)
            {
                result.Chain = chain.ChainWith.ToComposed();
            }

            return result;
        }

        /// <summary>
        /// Generates a map/reduce query that composes two existing queries
        /// </summary>
        public static ComposedMapReduce ComposeWith(this ComposedMapReduce first, ComposedMapReduce second)
        {
            var result = new ComposedMapReduce(Combine(first.Maps, second.Maps), Combine(first.Reduces, second.Reduces));

            if (first.Chain != null)
            {
                result.Chain = first.Chain;
            }

            if (second.Chain != null)
            {
                if (result.Chain != null)
                {
                    result.Chain = result.Chain.Copy();
                }

                result.AppendToChain(second.Chain);
            }

            return result;
        }

        /// <summary>
        /// Generates a map/reduce query that composes two existing queries
        /// </summary>
        public static ComposedMapReduce ComposeWith(this IComposableMapReduce first, IComposableMapReduce second)
        {
            return first.ToComposed().ComposeWith(second.ToComposed());
        }

        /// <summary>
        /// Generates a map/reduce query that composes two existing queries
        /// </summary>
        public static ComposedMapReduce ComposeWith(this IComposableMap first, IComposableMap second)
        {
            return first.ToComposed().ComposeWith(second.ToComposed());
        }

        /// <summary>
        /// Generates a map/reduce query that composes two existing queries
        /// </summary>
        public static ComposedMapReduce ComposeWith(this IComposableReduce first, IComposableReduce second)
        {
            return first.ToComposed().ComposeWith(second.ToComposed());
        }
    }
}
