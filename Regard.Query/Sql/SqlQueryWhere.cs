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
        /// Generates this clause as part of a query
        /// </summary>
        public string ToQuery(string tableName)
        {
            return "[" + tableName + "].[" + FieldName + "] = ?";
        }
    }
}
