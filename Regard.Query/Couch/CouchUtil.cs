using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Regard.Query.Couch
{
    /// <summary>
    /// Utility classes for helping out with Couch
    /// </summary>
    public static class CouchUtil
    {
        private const string c_InvalidChars = @"([\/\\\#\?\-]|[\x00-\x1f]|[\x7f-\x9f])";

        /// <summary>
        /// Converts a string to one that contains characters that can be encoded within a single URI path component
        /// </summary>
        public static string SanitiseAsUriComponent(string value)
        {
            if (value == null) return null;

            return Regex.Replace(value, c_InvalidChars, match =>
            {
                StringBuilder result = new StringBuilder();

                result.Append('-');
                foreach (var matchChar in match.Value)
                {
                    result.Append(((int)matchChar).ToString("x"));
                }
                result.Append('-');

                return result.ToString();
            });
        }

        /// <summary>
        /// Retrieves the name of the database to use for a specific organization/product
        /// </summary>
        public static string GetDatabaseName(string organization, string product)
        {
            return SanitiseAsUriComponent(organization + '/' + product);
        }

        /// <summary>
        /// Encodes a JObject as UTF-8 bytes
        /// </summary>
        private static byte[] ToBytesUTF8(JObject obj)
        {
            return Encoding.UTF8.GetBytes(obj.ToString(Formatting.None));
        }

        /// <summary>
        /// Inserts a set of documents into a CouchDB database
        /// </summary>
        public static async Task<WebResponse> PutDocuments(Uri couchDbUri, string database, IEnumerable<KeyValuePair<string, JObject>> documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException("documents");
            }

            // Work out the location of the bulk docs URI for this database
            // TODO: new Uri(x, y) strips off any extra path information so this is wrong in general
            var bulkUri     = new Uri(couchDbUri, database + "/_bulk_docs");

            // Generate the request object
            JObject requestObject   = new JObject();
            JArray  docs            = new JArray();

            foreach (var doc in documents)
            {
                JObject newDoc  = new JObject(doc.Value);
                newDoc["_id"]   = doc.Key;

                docs.Add(newDoc);
            }

            requestObject["docs"] = docs;

            // Convert to a byte array
            var requestBytes = ToBytesUTF8(requestObject);
            
            // Start putting together a request
            const int maxRetryCount = 60;

            for (int retry = 0; retry < maxRetryCount; ++retry)
            {
                try
                {
                    var request = WebRequest.Create(bulkUri);

                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.ContentLength = requestBytes.Length;

                    using (var stream = await request.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                        stream.Close();
                    }

                    // Perform the update
                    return await request.GetResponseAsync();
                }
                catch (IOException)
                {
                    // Might happen if couch is restarting, for example
                    if (retry + 1 == maxRetryCount)
                    {
                        // Re-throw if we hit the retry count
                        throw;
                    }
                }

                // Sleep a while before retrying
                await Task.Delay(100);
            }

            throw new InvalidOperationException("Failed to write data: ran out of retries");
        }
    }
}
