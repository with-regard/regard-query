﻿namespace Regard.Query.Api
{
    /// <summary>
    /// Class that creates a fluent query build API with extension methods
    /// </summary>
    public static class Fluent
    {
        public static IRegardQuery Only(this IRegardQuery query, string key, string value)
        {
            return query.Builder.Only(query, key, value);
        }

        public static IRegardQuery BrokenDownBy(this IRegardQuery query, string key)
        {
            return query.Builder.BrokenDownBy(query, key);
        }

        public static IRegardQuery Sum(this IRegardQuery query, string key)
        {
            return query.Builder.Sum(query, key);
        }

        public static IRegardQuery CountUniqueValues(this IRegardQuery query, string key)
        {
            return query.Builder.CountUniqueValues(query, key);
        }
    }
}
