namespace Regard.Query.Serializable
{
    /// <summary>
    /// Verbs that can appear in a serializable query
    /// </summary>
    public static class QueryVerbs
    {
        public const string AllEvents           = "AllEvents";
        public const string Only                = "Only";
        public const string BrokenDownBy        = "BrokenDownBy";
        public const string Sum                 = "Sum";
        public const string Mean                = "Mean";
        public const string Min                 = "Min";
        public const string Max                 = "Max";
        public const string CountUniqueValues   = "CountUniqueValues";
        public const string IndexedBy           = "IndexedBy";
        public const string TransformDateFormat = "TransformDateFormat";
    }
}
