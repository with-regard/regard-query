using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Ted.Wacel;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.Serializable;

// Interface is async but some methods don't require any awaiting
#pragma warning disable 1998

namespace Regard.Query.Wacel
{
    // This assumes that we want to use one cube per query, which might not be efficient if there are several queries in existence that all use the same data.

    /// <summary>
    /// Event recorder that records events to a WACEL cube
    /// </summary>
    public class WacelCubeRecorder : IEventRecorder
    {
        /// <summary>
        /// The cube that this is for
        /// </summary>
        private readonly Cube m_WacelCube;

        /// <summary>
        /// Filtering function that decides whether or not to include a particular event in the cube
        /// </summary>
        private readonly Func<JObject, bool> m_Filter;

        public WacelCubeRecorder(SerializableQuery query)
        {
            if (query == null) throw new ArgumentNullException("query");

            m_Filter = query.CreateFilter();
        }

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
        public async Task RecordEvent(Guid sessionId, JObject data)
        {
            // Nothing to do if there's no data
            if (data == null) return;

            // Don't record the event if it's not relevant to this cube
            if (!m_Filter(data))
            {
                return;
            }

            // Create the datapoint
            DataPoint dataPoint = new DataPoint();

            // Fill with dimensions from the data
            foreach (var property in data.Properties())
            {
                
            }

            throw new NotImplementedException();
        }
    }
}
