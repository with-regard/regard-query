namespace Regard.Query.Api
{
    /// <summary>
    /// Class that creates a fluent query build API with extension methods
    /// </summary>
    public static class FluentQuery
    {
        public static IRegardQuery Only(this IRegardQuery query, string key, string value)
        {
            return query.Builder.Only(query, key, value);
        }

        public static IRegardQuery BrokenDownBy(this IRegardQuery query, string key, string name)
        {
            return query.Builder.BrokenDownBy(query, key, name);
        }

        public static IRegardQuery Sum(this IRegardQuery query, string key, string name)
        {
            return query.Builder.Sum(query, key, name);
        }

        public static IRegardQuery Mean(this IRegardQuery query, string key, string name)
        {
            return query.Builder.Mean(query, key, name);
        }

        public static IRegardQuery Min(this IRegardQuery query, string key, string name)
        {
            return query.Builder.Min(query, key, name);
        }

        public static IRegardQuery Max(this IRegardQuery query, string key, string name)
        {
            return query.Builder.Max(query, key, name);
        }

        public static IRegardQuery CountUniqueValues(this IRegardQuery query, string key, string name)
        {
            return query.Builder.CountUniqueValues(query, key, name);
        }

        public static IRegardQuery IndexedBy(this IRegardQuery query, string key)
        {
            return query.Builder.IndexedBy(query, key);
        }

        public static IRegardQuery TransformDateFormat(this IRegardQuery query, string key, string name, string format)
        {
            return query.Builder.TransformDateFormat(query, key, name, format);
        }
    }
}
