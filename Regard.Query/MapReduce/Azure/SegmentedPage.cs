using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.Azure
{
    /// <summary>
    /// A page from an Azure data store
    /// </summary>
    class SegmentedPage : IKeyValuePage
    {
        /// <summary>
        /// Retrieves the objects in this page
        /// </summary>
        public Task<IEnumerable<Tuple<JArray, JObject>>> GetObjects()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A token that can be used to retrieve the page that follows this one, or null if this is the last page.
        /// </summary>
        /// <remarks>
        /// Page tokens are persistent, so you can re-run the same enumeration and use the old page token to resume it from where the
        /// previous one left off.
        /// </remarks>
        public Task<string> GetNextPageToken()
        {
            throw new NotImplementedException();
        }
    }
}
