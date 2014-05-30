using System.Collections;
using System.Collections.Generic;

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
        /// Convert a single composable object to a composed object
        /// </summary>
        public static ComposedMapReduce ToComposed(this IComposableMapReduce obj)
        {
            ComposedMapReduce alreadyComposed = obj as ComposedMapReduce;
            if (alreadyComposed != null)
            {
                return alreadyComposed;
            }

            return new ComposedMapReduce(new[] { obj }, new[] { obj });
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

            return new ComposedMapReduce(new[] { obj }, new IComposableReduce[0]);
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

            return new ComposedMapReduce(new IComposableMap[0], new[] { obj });
        }

        /// <summary>
        /// Generates a map/reduce query that composes two existing queries
        /// </summary>
        public static ComposedMapReduce ComposeWith(this ComposedMapReduce first, ComposedMapReduce second)
        {
            return new ComposedMapReduce(Combine(first.Maps, second.Maps), Combine(first.Reduces, second.Reduces));
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
