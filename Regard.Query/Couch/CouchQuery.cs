using System.Text;

namespace Regard.Query.Couch
{
    /// <summary>
    /// Class that represents a query that can be run on a CouchDB database
    /// </summary>
    /// <remarks>
    /// CouchDB queries need to be actualised as views before they can be run
    /// </remarks>
    class CouchQuery
    {
        /// <summary>
        /// JS functions that exclude docs during the map function
        /// </summary>
        /// <remarks>
        /// The document will be in the 'doc' variable. This should be a series of if statements that 
        /// </remarks>
        private readonly StringBuilder m_Exclusions;

        /// <summary>
        /// JS functions that build the key that a particular document maps onto
        /// </summary>
        /// <remarks>
        /// The key is stored in the docKey variable, which starts as the empty string
        /// </remarks>
        private readonly StringBuilder m_KeyBuilder;

        /// <summary>
        /// The reduce function
        /// </summary>
        private readonly StringBuilder m_Reduce;

        public CouchQuery()
        {
            // Functions are initially empty
            m_Exclusions    = new StringBuilder();
            m_KeyBuilder    = new StringBuilder();
            m_Reduce        = new StringBuilder();
        }

        /// <summary>
        /// Creates the map function for this query
        /// </summary>
        public string GenerateMapFunction()
        {
            return "function(doc) {\n"
                + m_Exclusions
                + "var docKey = [];\n"
                + m_KeyBuilder
                + "\n}\n";
        }

        /// <summary>
        /// Creates the reduce function for this query
        /// </summary>
        public string GenerateReduceFunction()
        {
            return "function(keys, values, rereduce) {\n" 
                + m_Reduce 
                + "\n}\n";
        }

        /// <summary>
        /// Restricts the results to only those with a field with a particular value
        /// </summary>
        public void Only(string field, string value)
        {
            // Exclude anything using the map function that doesn't match the string
            m_Exclusions.Append("if (doc[" + field.ToJsString() + "] !== " + value.ToJsString() + "{ return; }\n");
        }

        /// <summary>
        /// Partitions the results by a particular field
        /// </summary>
        public void BrokenDownBy(string key, string name)
        {
            // This builds up the key for this document
            m_KeyBuilder.Append("docKey.push(doc[" + key.ToJsString() + "]);\n");

            // TODO: This also introduces a field in the emitted document indicating the value of this item
        }
    }
}
