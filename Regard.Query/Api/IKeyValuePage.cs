﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Regard.Query.Api
{
    /// <summary>
    /// Represents a page of data from a Key-Value enumerator
    /// </summary>
    /// <remarks>
    /// Paging is stateless and its precise behaviour is defined by the underlying implementation. The design chosen here happens
    /// to be easy to implement with Azure tables.
    /// </remarks>
    public interface IKeyValuePage
    {
        /// <summary>
        /// Retrieves the objects in this page
        /// </summary>
        Task<IEnumerable<Tuple<JArray, JObject>>> GetObjects();

        /// <summary>
        /// A token that can be used to retrieve the page that follows this one, or null if this is the last page.
        /// </summary>
        /// <remarks>
        /// Page tokens are persistent, so you can re-run the same enumeration and use the old page token to resume it from where the
        /// previous one left off.
        /// </remarks>
        Task<string> GetNextPageToken();
    }
}
