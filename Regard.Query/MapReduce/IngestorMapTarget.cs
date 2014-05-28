using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// The map target for an ing
    /// </summary>
    internal sealed class IngestorMapTarget : IMapTarget
    {
        /// <summary>
        /// The list of objects emitted by the target
        /// </summary>
        private readonly List<Tuple<JArray, JObject>> m_Objects = new List<Tuple<JArray, JObject>>();

        /// <summary>
        /// Emits a document to this target
        /// </summary>
        public void Emit(JArray key, JObject document)
        {
            m_Objects.Add(new Tuple<JArray, JObject>(key, document));
        }

        /// <summary>
        /// Resets this object so it can be used with a new map request
        /// </summary>
        public void Reset()
        {
            m_Objects.Clear();
        }

        /// <summary>
        /// Retrieves the list of emitted objects
        /// </summary>
        public IEnumerable<Tuple<JArray, JObject>> Emitted
        {
            get { return m_Objects; }
        }
    }
}
