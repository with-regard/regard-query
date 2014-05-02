using System;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Class representing the result of a map action
    /// </summary>
    /// <remarks>
    /// This allows us to compose map functions so we can build up arbitrary queries. This current version support single emit only.
    /// </remarks>
    class MapResult
    {
        /// <summary>
        /// Set to true if this result is rejected
        /// </summary>
        private bool m_Rejected;

        /// <summary>
        /// The array representing the key for this result
        /// </summary>
        private JArray m_Key = new JArray();

        /// <summary>
        /// An object representing the document that will be emitted
        /// </summary>
        private readonly JObject m_EmitDoc = new JObject();

        /// <summary>
        /// Rejects this docuument, so nothing will be emitted
        /// </summary>
        public void Reject()
        {
            m_Rejected = true;
        }

        /// <summary>
        /// Adds a value to the key for this item
        /// </summary>
        public void AddKey(JValue value)
        {
            m_Key.Add(value);
        }

        /// <summary>
        /// Sets the value of a field in the output document
        /// </summary>
        public void SetValue(string field, JValue value)
        {
            m_EmitDoc[field] = value;
        }

        /// <summary>
        /// Sets a new key value, replacing the old one
        /// </summary>
        public void SetKey(JArray newKey)
        {
            m_Key = (JArray) newKey.DeepClone();
        }

        /// <summary>
        /// The document that this will emit (callers can update this)
        /// </summary>
        public JObject Document { get { return m_EmitDoc; } }

        /// <summary>
        /// Emits the result of this operation to a target
        /// </summary>
        public void Emit(IMapTarget target)
        {
            if (target == null) throw new ArgumentNullException("target");

            // Nothing to emit if we rejected the document
            if (m_Rejected)
            {
                return;
            }

            // Send to the target
            target.Emit(m_Key, m_EmitDoc);
        }
    }
}
