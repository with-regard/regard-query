using System;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Data storage class representing a SQL substitution
    /// </summary>
    public class SqlSubstitution
    {
        public SqlSubstitution(string name, string value)
        {
            if (name == null)   throw new ArgumentNullException("name");
            if (value == null)  throw new ArgumentNullException("value");

            Name    = name;
            Value   = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }
}
