using System;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Represents a map/reduce data store
    /// </summary>
    class DataStore : IRegardDataStore
    {
        /// <summary>
        /// The root key/value store
        /// </summary>
        private readonly IKeyValueStore m_Store;

        /// <summary>
        /// The name of the current node
        /// </summary>
        private readonly string m_NodeName;

        /// <summary>
        /// The event recorder for this data store
        /// </summary>
        private readonly EventRecorder m_EventRecorder;

        /// <summary>
        /// The product admin interface
        /// </summary>
        private readonly ProductAdmin m_ProductAdmin;

        /// <summary>
        /// Creates a new map/reduce data store
        /// </summary>
        /// <param name="store">The root data store</param>
        /// <param name="nodeName">The name of a node that's exclusive to this process</param>
        /// <remarks>
        /// To allow for multiple nodes, we follow Microsoft's advice and use separate partitions for each data consumer. No two running consumers should use the same
        /// node name: events will be lost if this occurs
        /// </remarks>
        public DataStore(IKeyValueStore store, string nodeName)
        {
            if (store == null) throw new ArgumentNullException("store");

            m_Store     = store;
            m_NodeName  = nodeName;

            m_EventRecorder = new EventRecorder();
            m_ProductAdmin  = new ProductAdmin(m_Store);
        }

        /// <summary>
        /// The event recorder for this engine
        /// </summary>
        /// <remarks>
        /// Note that events will be discarded if there is no project registered for them
        /// </remarks>
        public IEventRecorder EventRecorder { get { return m_EventRecorder; } }

        /// <summary>
        /// The object that can create and retrieve products in order to register or run queries against them
        /// </summary>
        public IProductAdmin Products { get { return m_ProductAdmin; } }
    }
}
