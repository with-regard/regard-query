using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Ted.Wacel;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

// Interface is async but some methods don't require any awaiting
#pragma warning disable 1998

namespace Regard.Query.Wacel
{
    /// <summary>
    /// Event recorder that records events to a WACEL cube
    /// </summary>
    public class WacelCubeRecorder : IEventRecorder
    {
        private readonly Cube m_WacelCube;

        /// <summary>
        /// Indicates that a new session has begun
        /// </summary>
        /// <param name="organization">The name of the organization that the session is for</param>
        /// <param name="product">The name of the product that the session is for</param>
        /// <param name="userId">A GUID that identifies the user that this session is for</param>
        /// <returns>A GUID that identifies this session, or Guid.Empty if the session can't be started (because the user is opted-out, for example)</returns>
        public async Task<Guid> StartSession(string organization, string product, Guid userId)
        {
            // TODO: proper implementation
            return Guid.NewGuid();
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
