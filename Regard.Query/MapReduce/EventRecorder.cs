using System;
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
        /// Indicates that a new session has begun
        /// </summary>
        /// <param name="organization">The name of the organization that the session is for</param>
        /// <param name="product">The name of the product that the session is for</param>
        /// <param name="userId">A GUID that identifies the user that this session is for</param>
        /// <param name="sessionId">Should be Guid.Empty to indicate that the call should generate a session ID, otherwise it should be a session ID that has not been used before</param>
        /// <returns>A GUID that identifies this session, or Guid.Empty if the session can't be started (because the user is opted-out, for example)
        /// If sessionId is not Guid.Empty, then it will be the return value</returns>
        public Task<Guid> StartSession(string organization, string product, Guid userId, Guid sessionId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Schedules a single event to be recorded by this object
        /// </summary>
        /// <param name="sessionId">The ID of the session (as returned by StartSession)</param>
        /// <param name="data">JSON data indicating the properties for this event</param>
        public Task RecordEvent(Guid sessionId, JObject data)
        {
            throw new NotImplementedException();
        }
    }
}
