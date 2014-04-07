namespace Regard.Query.Sql
{
    /// <summary>
    /// Data storage class representing part of a SQL where statement
    /// </summary>
    class SqlQueryWhere
    {
        /// <summary>
        /// The field name to compare against (in the event properties table). Caution, no checking is done to make sure the name is valid.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The value that the field must have
        /// </summary>
        public string FieldValue { get; set; }

        /// <summary>
        /// The name of the parameter that this should be compared to (or null if this is not assigned yet)
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// Generates this clause as part of a query
        /// </summary>
        /// <remarks>
        /// Note that all the fields must be assigned before this can be legitimately called
        /// </remarks>
        public string ToQuery(string propertyTableName, string propertyValueTableName)
        {
            string tableName = propertyValueTableName;
            string realFieldName = FieldName;

            if (FieldName == "PropertyName")
            {
                tableName = propertyTableName;
                realFieldName = "Name";
            }

            return "[" + tableName + "].[" + realFieldName + "] = " + ParameterName;
        }
    }
}
