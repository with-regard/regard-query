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

            var result = new ComposedMapReduce(new[] { obj }, new[] { obj }, new IComposableReduce[0]);

            IComposableChain chain = obj as IComposableChain;
            if (chain != null)
            {
                result.Chain = chain.ChainWith.ToComposed();
                SetChainedRereduces(result.Chain, result, chain);
            }

            return result;
        }

        /// <summary>
        /// Sets the list of re-reduces to run on a chain for a composition
        /// </summary>
        private static void SetChainedRereduces(ComposedMapReduce chained, ComposedMapReduce source, IComposableChain original)
        {
            if (chained == null) return;
            if (source == null) return;

            // Everything that reduces - *except* the item that is causing the chain - should be copied into the result
            List<IComposableReduce> rereduces = new List<IComposableReduce>();

            foreach (var reduce in source.Reduces)
            {
                // Ignore the item in the chain
                if (ReferenceEquals(original, reduce))
                {
                    continue;
                }

                // This reduce should be re-done in the chain
                rereduces.Add(reduce);
            }

            // Store in the chained item
            chained.SetRereduces(rereduces);
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

            var alreadyMapReduce = obj as IComposableMapReduce;
            if (alreadyMapReduce != null)
            {
                return alreadyMapReduce.ToComposed();
            }

            var result = new ComposedMapReduce(new[] { obj }, new IComposableReduce[0], new IComposableReduce[0]);

            IComposableChain chain = obj as IComposableChain;
            if (chain != null)
            {
                ComposedMapReduce newChain;
                result.Chain = newChain = chain.ChainWith.ToComposed();
                SetChainedRereduces(newChain, result, chain);
            }

            return result;
        }

        /// <summary>
        /// Convert a single composable object to a composed object
        /// </summary>
        public static ComposedMapReduce ToComposed(this IComposableReduce obj)
        {
            var alreadyComposed = obj as ComposedMapReduce;
            if (alreadyComposed != null)
            {
                return alreadyComposed;
            }

            var alreadyMapReduce = obj as IComposableMapReduce;
            if (alreadyMapReduce != null)
            {
                return alreadyMapReduce.ToComposed();
            }

            var result = new ComposedMapReduce(new IComposableMap[0], new[] { obj }, new IComposableReduce[0]);

            IComposableChain chain = obj as IComposableChain;
            if (chain != null)
            {
                ComposedMapReduce newChain;
                result.Chain = newChain = chain.ChainWith.ToComposed();
                SetChainedRereduces(newChain, result, chain);
            }

            return result;
        }

        /// <summary>
        /// Generates a map/reduce query that composes two existing queries
        /// </summary>
        public static ComposedMapReduce ComposeWith(this ComposedMapReduce first, ComposedMapReduce second)
        {
            var result = new ComposedMapReduce(Combine(first.Maps, second.Maps), Combine(first.Reduces, second.Reduces), new IComposableReduce[0]);

            // TODO: this chain composition is only really tested for the simplest cases
            if (first.Chain != null)
            {
                result.Chain = first.Chain.Copy();

                // When chaining, make sure that this reduce is re-run
                result.Chain.AppendRereduce(second);
            }

            if (second.Chain != null)
            {
                var secondChain = second.Chain.Copy();

                // When chaining, make sure that this reduce is re-run
                secondChain.AppendRereduce(first);
                result.AppendToChain(secondChain);
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
