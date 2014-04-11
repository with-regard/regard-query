using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.Couch
{
    /// <summary>
    /// Class that records events in an Apache CouchDB database
    /// </summary>
    public class CouchEventRecorder : IEventRecorder
    {
        /// <summary>
        /// The URI of the CouchDB database that this should write events to
        /// </summary>
        private readonly Uri m_CouchDbUri;

        public CouchEventRecorder(Uri couchDbUri)
        {
            if (couchDbUri == null)
            {
                throw new ArgumentNullException("couchDbUri");
            }

            m_CouchDbUri = couchDbUri;
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
            // Create a GUID for this session
            var sessionGuid = Guid.NewGuid();

            // TODO: check user opt-in status and don't record for users who are not opted in
            // TODO: check product/organization registration status and don't record for products that don't exist

            // Create a new session document
            JObject sessionDocument = new JObject();

            sessionDocument["session-id"]   = sessionGuid.ToString();
            sessionDocument["organization"] = organization;
            sessionDocument["product"]      = product;

            await CouchUtil.PutDocuments(m_CouchDbUri, "sessions", new[] { new KeyValuePair<string, JObject>("session/" + sessionGuid, sessionDocument) });

            return sessionGuid;
        }

        /// <summary>
        /// Schedules a single event to be recorded by this object
        /// </summary>
        /// <param name="sessionId">The ID of the session (as returned by StartSession)</param>
        /// <param name="data">JSON data indicating the properties for this event</param>
        public async Task RecordEvent(Guid sessionId, JObject data)
        {
            // TODO: retrieve the database that corresponds to this session ID
            string database = "temp-testdb";

            // Store data for this event
            Guid    eventGuid   = Guid.NewGuid();
            JObject eventObject = new JObject();

            eventObject["session-id"]   = sessionId.ToString();
            eventObject["user-data"]    = data;

            await CouchUtil.PutDocuments(m_CouchDbUri, database, new[] { new KeyValuePair<string, JObject>("events/" + sessionId + "/" + eventGuid, eventObject) });
        }
    }
}
