using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Data storage class representing a summarisation function
    /// </summary>
    /// <remarks>
    /// We split this up rather than hard code it as there is a need to name the part of the join used for the summarisation.
    /// </remarks>
    class SqlQuerySumFun
    {
        /// <summary>
        /// The function to apply (eg COUNT(...))
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// Whether or not to use the DISTINCT keyword (ie, COUNT(DISTINCT ...)
        /// </summary>
        public bool Distinct { get; set; }

        /// <summary>
        /// The name of the field to summarise from the event properties table
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The nae as it should appear in teh resulting <see cref="QueryResultLine"></see>
        /// </summary>
        public string ResultName { get; set; }

        /// <summary>
        /// Generates this clause as part of a query
        /// </summary>
        public string ToQuery(string propertyTableName, string propertyValueTableName)
        {
            string tableName = propertyValueTableName;
            string realFieldName = FieldName;

            if (FieldName == "PropertyName")
            {
                tableName       = propertyTableName;
                realFieldName   = "Name";
            }

            if (string.IsNullOrEmpty(Function))
            {
                return "[" + tableName + "].[" + realFieldName + "]";
            }
            else
            {
                return Function + "(" + (Distinct ? "DISTINCT " : "") + "[" + tableName + "].[" + realFieldName + "])";
            }
        }
    }
}
