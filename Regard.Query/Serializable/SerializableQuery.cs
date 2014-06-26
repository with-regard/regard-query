using System;
using System.Threading.Tasks;
using Regard.Query.Api;

namespace Regard.Query.Serializable
{
    /// <summary>
    /// Represents a query designed to be deconstructed and serialized
    /// </summary>
    public class SerializableQuery : IRegardQuery
    {
        public SerializableQuery(SerializableQueryBuilder parentBuilder)
        {
            if (parentBuilder == null) throw new ArgumentNullException("parentBuilder");

            Builder = parentBuilder;
        }

        #region IRegardQuery implementation

        /// <summary>
        /// The object that built this query (and which can be used to refine it)
        /// </summary>
        public SerializableQueryBuilder Builder { get; private set; }

        /// <summary>
        /// The object that built this query (and which can be used to refine it)
        /// </summary>
        IQueryBuilder IRegardQuery.Builder
        {
            get { return Builder; }
        }

        #endregion

        #region Query building

        /// <summary>
        /// Builds an equivalent to this query on a query builder
        /// </summary>
        public IRegardQuery Rebuild(IQueryBuilder builder)
        {
            IRegardQuery apply = null;

            // Build the initial query
            if (AppliesTo != null)
            {
                apply = AppliesTo.Rebuild(builder);
            }

            // Build this step
            switch (Verb)
            {
                case QueryVerbs.AllEvents:
                    return builder.AllEvents();

                case QueryVerbs.BrokenDownBy:
                    return builder.BrokenDownBy(apply, Key, Name);

                case QueryVerbs.CountUniqueValues:
                    return builder.CountUniqueValues(apply, Key, Name);

                case QueryVerbs.Sum:
                    return builder.Sum(apply, Key, Name);

                case QueryVerbs.Only:
                    return builder.Only(apply, Key, Value);

                case QueryVerbs.Min:
                    return builder.Min(apply, Key, Name);

                case QueryVerbs.Max:
                    return builder.Max(apply, Key, Name);

                case QueryVerbs.Mean:
                    return builder.Mean(apply, Key, Name);

                case QueryVerbs.IndexedBy:
                    return builder.IndexedBy(apply, Key);
                    
                case QueryVerbs.TransformDateFormat:
                    return builder.TransformDateFormat(apply, Key, Name, Format);

                default:
                    throw new InvalidOperationException("Unknown query verb:  " + Verb);
            }
        }

        #endregion

        #region Query model

        /// <summary>
        /// The query that this query modifies, or null if this query doesn't modify anything
        /// </summary>
        /// <remarks>
        /// Eg, if the query is AllEvents().Only('Foo'), then the 'Only' query is said to apply to the 'AllEvents' query.
        /// </remarks>
        public SerializableQuery AppliesTo { get; set; }

        /// <summary>
        /// A string representing the verb of this part of the query
        /// </summary>
        public string Verb { get; set; }

        /// <summary>
        /// The name of the key that this query should match (null if the verb doesn't match against keys)
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The value that is matched against (null if the verb doesn't match against a value)
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The name that is assigned to the result of this item (null if the verb doesn't require a name)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The format that the result should be written in
        /// </summary>
        public string Format { get; set; }

        #endregion
    }
}
