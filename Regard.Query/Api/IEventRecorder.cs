using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that can record events in a data store that can be queried with the query API
    /// </summary>
    public interface IEventRecorder
    {
        /// <summary>
        /// Indicates that a new session has begun
        /// </summary>
        /// <param name="organization">The name of the organization that the session is for</param>
        /// <param name="product">The name of the product that the session is for</param>
        /// <param name="userId">A GUID that identifies the user that this session is for</param>
        /// <returns>A GUID that identifies this session, or Guid.Empty if the session can't be started (because the user is opted-out, for example)</returns>
        Task<Guid> StartSession(string organization, string product, Guid userId);

        /// <summary>
        /// Schedules a single event to be recorded by this object
        /// </summary>
        /// <param name="organization">The name of the organisation that generated the event</param>
        /// <param name="product">The name of the product that generated the event</param>
        /// <param name="sessionId">The ID of the session (as returned by StartSession)</param>
        /// <param name="data">JSON data indicating the properties for this event</param>
        Task RecordEvent(string organization, string product, Guid sessionId, JObject data);
    }
}
