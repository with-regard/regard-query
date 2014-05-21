using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Event recorder for the map/reduce query store
    /// </summary>
    internal class EventRecorder : IEventRecorder
    {
        /// <summary>
        /// The root data store where this will store its events
        /// </summary>
        private readonly IKeyValueStore m_RootDataStore;

        /// <summary>
        /// The name of this node
        /// </summary>
        private readonly string m_NodeName;

        /// <summary>
        /// Data store used to store information about active sessions
        /// </summary>
        /// <remarks>
        /// Session stores are shared across all instances: we assume that any given session will just be redefined the same way
        /// </remarks>
        private readonly IKeyValueStore m_SessionDataStore;

        public EventRecorder(IKeyValueStore rootDataStore, string nodeName)
        {
            m_RootDataStore = rootDataStore;
            m_NodeName      = nodeName;

            // Sessions are stored in a data store shared across all nodes
            m_SessionDataStore = m_RootDataStore.ChildStore(new JArray("sessions"));
        }

        /// <summary>
        /// Indicates that a new session has begun
        /// </summary>
        /// <param name="organization">The name of the organization that the session is for</param>
        /// <param name="product">The name of the product that the session is for</param>
        /// <param name="userId">A GUID that identifies the user that this session is for</param>
        /// <param name="sessionId">Should be Guid.Empty to indicate that the call should generate a session ID, otherwise it should be a session ID that has not been used before</param>
        /// <returns>A GUID that identifies this session, or Guid.Empty if the session can't be started (because the user is opted-out, for example)
        /// If sessionId is not Guid.Empty, then it will be the return value</returns>
        public async Task<Guid> StartSession(string organization, string product, Guid userId, Guid sessionId)
        {
            // TODO: do not start sessions for products that don't exist
            // TODO: do not start sessions for unknown user IDs
            // TODO: do not start sessions for opted-out user IDs

            // Generate a new session ID if the one passed in was empty
            if (sessionId == Guid.Empty)
            {
                sessionId = Guid.NewGuid();
            }

            // Add this session to the data store
            JObject sessionData = JObject.FromObject(new
            {
                Organization = organization,
                Product = product,
                UserId = userId,
            });

            // Create the session in the store
            // Overwrite it if it already exists: this should be OK provided that session IDs are never re-used
            await m_SessionDataStore.ChildStore(new JArray(organization, product)).SetValue(new JArray(sessionId.ToString()), sessionData);

            return sessionId;
        }

        /// <summary>
        /// Schedules a single event to be recorded by this object
        /// </summary>
        /// <param name="sessionId">The ID of the session (as returned by StartSession)</param>
        /// <param name="organization">The name of the organization that the session is for</param>
        /// <param name="product">The name of the product that the session is for</param>
        /// <param name="data">JSON data indicating the properties for this event</param>
        public async Task RecordEvent(Guid sessionId, string organization, string product, JObject data)
        {
            // TODO: do not record events for sessions that don't exist
            // TODO: in particular, do not record events for users who are not opted in

            // Store in the raw events store for this product/organization
            var productStore    = m_RootDataStore.ChildStore(ProductAdmin.KeyForProduct(organization, product));
            var eventStore      = productStore.ChildStore(new JArray("raw-events", m_NodeName));

            await eventStore.ChildStore(new JArray(organization, product, m_NodeName)).AppendValue(data);
        }
    }
}
