using System;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.Queries
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
        /// The current index position
        /// </summary>
        private int m_IndexPos = 0;

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
            // Normal keys go on the end of the key
            m_Key.Add(value);
        }

        /// <summary>
        /// Adds an indexing key to the key for this item
        /// </summary>
        public void AddIndexKey(JValue value)
        {
            // Indexes form the start of the key
            m_Key.Insert(m_IndexPos, value);
            ++m_IndexPos;
        }

        /// <summary>
        /// Sets the key at the specified index to null (so it won't be used in the result). The key length is not changed (this is so indexes can be reliably stored without needing to be updated)
        /// </summary>
        public void RemoveKeyAtIndex(int index)
        {
            // Invalid index is a no-op
            if (index < 0)              return;
            if (index >= m_Key.Count)   return;
            m_Key[index] = null;
        }

        /// <summary>
        /// Sets the value of a field in the output document
        /// </summary>
        public void SetValue(string field, JValue value)
        {
            m_EmitDoc[field] = value;
        }

        /// <summary>
        /// Sets the value of an intermediate field (stored in the __intermediate__ field of the resulting doc)
        /// </summary>
        public void SetIntermediateValue(string field, JObject value)
        {
            JToken intermediateToken;
            if (!m_EmitDoc.TryGetValue("__intermediate__", out intermediateToken))
            {
                m_EmitDoc["__intermediate__"] = new JObject();
                intermediateToken = m_EmitDoc["__intermediate__"];
            }

            intermediateToken[field] = value;
        }

        /// <summary>
        /// Removes a field from the result
        /// </summary>
        public void RemoveValue(string field)
        {
            m_EmitDoc.Remove(field);
        }

        /// <summary>
        /// Removes the index keys from this value
        /// </summary>
        public void RemoveIndexKeys()
        {
            if (m_IndexPos <= 0)
            {
                return;
            }

            // Create a new key without the index
            JArray newKey = new JArray();
            for (int pos = m_IndexPos; pos < m_Key.Count; ++pos)
            {
                newKey.Add(m_Key[pos]);
            }

            // Replace the key with the non-indexed version
            m_Key = newKey;
            m_IndexPos = 0;

        }

        /// <summary>
        /// Sets a new key value, replacing the old one
        /// </summary>
        /// <param name="newKey">The new key value</param>
        /// <param name="preserveIndex"></param>
        public void SetKey(JArray newKey, bool preserveIndex)
        {
            // Record a copy of the key
            m_Key = (JArray) newKey.DeepClone();

            // If the key is supposed to contain an index, restore it
            if (preserveIndex)
            {
                var indexValue = m_Key.Last.Value<int>();
                m_IndexPos = indexValue;
                m_Key.RemoveAt(m_Key.Count-1);
            }
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

            var key = m_Key;

            // If there's an index, then encode it at the end of the key
            if (m_IndexPos > 0)
            {
                key = (JArray) key.DeepClone();
                key.Add(new JValue(m_IndexPos));
            }


            // Send to the target
            target.Emit(key, m_EmitDoc);
        }
    }
}
