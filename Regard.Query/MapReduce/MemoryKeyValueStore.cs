﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    // Interface specifies async and this is really a class for the test framework, so this warning is (probably?) not helpful
#pragma warning disable 1998

    // This uses JArray.ToString to generate string versions of the key, which is dependent on how Newtonsoft is implemented (this is likely a bad strategy for a 'real' implementation)
    
    /// <summary>
    /// Basic in-memory version of a key-value store (used for testing purposes) 
    /// </summary>
    public class MemoryKeyValueStore : IKeyValueStore
    {
        private readonly object m_Sync = new object();
        private readonly Dictionary<string, JObject> m_Objects = new Dictionary<string, JObject>();
        private readonly Dictionary<string, MemoryKeyValueStore> m_ChildStores = new Dictionary<string, MemoryKeyValueStore>(); 

        /// <summary>
        /// Retrieves a reference to a child key/value store with a particular key
        /// </summary>
        /// <remarks>
        /// When mapping/reducing data, we need a place to store the result; rather than sharing a 'single' data store (from the point of view of the caller), we allow
        /// for multiple stores.
        /// </remarks>
        public IKeyValueStore ChildStore(JArray key)
        {
            lock (m_Sync)
            {
                // Use the serialized JSON as the actual key
                string keyString = key.ToString(Formatting.None);

                MemoryKeyValueStore result;
                if (!m_ChildStores.TryGetValue(keyString, out result))
                {
                    result = m_ChildStores[keyString] = new MemoryKeyValueStore();
                }

                return result;
            }
        }

        /// <summary>
        /// Stores a value in the database, indexed by a particular key
        /// </summary>
        public async Task SetValue(JArray key, JObject value)
        {
            lock (m_Sync)
            {
                string keyString = key.ToString(Formatting.None);
                m_Objects[keyString] = value;
            }
        }

        /// <summary>
        /// Retrieves null or the value associated with a particular key
        /// </summary>
        public async Task<JObject> GetValue(JArray key)
        {
            lock (m_Sync)
            {
                string keyString = key.ToString(Formatting.None);

                JObject result;
                if (m_Objects.TryGetValue(keyString, out result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Simple implementation of the enumerator
        /// </summary>
        class AllValuesEnumerator : IKvStoreEnumerator
        {
            private readonly Func<Tuple<JArray, JObject>> m_FetchNext;

            public AllValuesEnumerator(Func<Tuple<JArray, JObject>> fetchNext)
            {
                m_FetchNext = fetchNext;
            }

            public async Task<Tuple<JArray, JObject>> FetchNext()
            {
                return m_FetchNext();
            }
        }

        /// <summary>
        /// Enumerates all of the values in this data store
        /// </summary>
        public IKvStoreEnumerator EnumerateAllValues()
        {
            List<KeyValuePair<string, JObject>> valueCopy;

            lock (m_Sync)
            {
                // Very naive algorithm...
                // Copy the values into a list
                valueCopy = new List<KeyValuePair<string, JObject>>(m_Objects); 
            }

            // Create an enumerator
            int currentIndex = -1;

            return new AllValuesEnumerator(() =>
            {
                lock (m_Sync)
                {
                    ++currentIndex;

                    // Stop once we reach the end
                    if (currentIndex >= valueCopy.Count)
                    {
                        return null;
                    }

                    var item = valueCopy[currentIndex];

                    // The key should be a serialized jarray
                    JArray key = JArray.Parse(item.Key);

                    return new Tuple<JArray, JObject>(key, item.Value);
                }
            });
        }

        /// <summary>
        /// Erases all of the values in a particular child store
        /// </summary>
        public async Task DeleteChildStore(JArray key)
        {
            lock (m_Sync)
            {
                // Use the serialized JSON as the actual key
                string keyString = key.ToString(Formatting.None);

                MemoryKeyValueStore result;
                if (m_ChildStores.TryGetValue(keyString, out result))
                {
                    m_ChildStores.Remove(keyString);
                }
            }
        }

        /// <summary>
        /// Assigns a key that is unique to this child store and uses it as a key to store a value. The store guarantees that this will be unique within this process, but not 
        /// if the same child store is being accessed by multiple processes.
        /// </summary>
        /// <param name="value">The value to store</param>
        /// <returns>
        /// A long representing the key assigned to the value. The key for GetValue can be obtained by putting this result (alone) in a JArray.
        /// The result is guaranteed to be positive, and will always increase.
        /// </returns>
        public async Task<long> AppendValue(JObject value)
        {
            lock (m_Sync)
            {
                long result = m_Objects.Count;

                var keyString = new JArray(result).ToString(Formatting.None);
                m_Objects[keyString] = value;

                return result;
            }
        }

        /// <summary>
        /// Waits for all of the pending SetValue requests to complete (if they are cached or otherwise write-through)
        /// </summary>
        public async Task Commit()
        {
            // Nothing to do
        }
    }
}
