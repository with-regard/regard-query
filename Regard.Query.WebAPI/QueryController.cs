using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.Serializable;

namespace Regard.Query.WebAPI
{
    /// <summary>
    /// ApiController that can be used to host the Regard query API - without any authentication or authorization
    /// </summary>
    [QueryAuthentication]
    public class QueryController : ApiController, IQueryController
    { 
        private readonly ControllerDataStoreFactory m_ControllerDataStore = new ControllerDataStoreFactory();
        /// <summary>
        /// The maximum length of a product or organization name
        /// </summary>
        private const int c_MaxLength = 256;
        private const int c_MaxQueryNameLength = 200;

        /// <summary>
        /// Just indicates the version of this assembly
        /// </summary>
        [HttpGet, Route("version")]
        public HttpResponseMessage Version()
        {
            return Request.CreateResponse(HttpStatusCode.OK, new { version = Assembly.GetExecutingAssembly().GetName().Version.ToString() });
        }

        /// <summary>
        /// Request to create a new product
        /// </summary>
        /// <remarks>
        /// POST to admin/v1/product/create a JSON message with the fields 'product' and 'organization' indicating the product and organization to create
        /// </remarks>
        [HttpPost, Route("admin/v1/product/create")]
        public async Task<HttpResponseMessage> CreateProduct()
        {
            try
            {
                Trace.WriteLine("Executing product/create");

                // Read the payload from the message
                var payloadString = await Request.Content.ReadAsStringAsync();

                // Convert to JSON
                JObject payload;
                try
                {
                    payload = JObject.Parse(payloadString);
                }
                catch (JsonException)
                {
                    // This is a bad request
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Could not understand payload");
                }

                // Should be a 'product' and 'organization' field to indicate what to create
                JToken productToken, organizationToken;

                if (!payload.TryGetValue("product", out productToken) ||
                    !payload.TryGetValue("organization", out organizationToken))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields missing from request");
                }

                // Sanity check what was passed in
                if (productToken.Type != JTokenType.String || organizationToken.Type != JTokenType.String)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields must be strings");
                }

                string product      = productToken.Value<string>();
                string organization = organizationToken.Value<string>();

                Trace.WriteLine("Creating new product: " + organization + "/" + product);

                if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(organization))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot be empty");
                }

                if (product.Length > c_MaxLength || organization.Length > c_MaxLength)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "Fields cannot contain too many characters");
                }

                // Actually create the product
                var regardDataStore = await m_ControllerDataStore.EnsureDataStore();

                await regardDataStore.Products.CreateProduct(organization, product);

                return Request.CreateResponse(HttpStatusCode.Created, new {});
            }
            catch (Exception e)
            {
                Trace.TraceError("Error during product/create: {0}", e);
                throw;
            }
        }

        /// <summary>
        /// Registers a query for a particular product
        /// </summary>
        /// <remarks>
        /// Queries must be registered before they can be used; this supports map/reduce style databases where ad-hoc queries are
        /// very slow, as well as allowing for a future update to support real-time updating.
        /// <para/>
        /// Post to product/v1/fooCorp/llamatron/register query a request with a query-name and query-definition field. The query-definition should use the same
        /// serialization format that we use internally (see <see cref="Regard.Query.Serializable.JsonQuery"/> for details)
        /// </remarks>
        [HttpPost, Route("product/v1/{organization}/{product}/register-query")]
        public async Task<HttpResponseMessage> RegisterQuery(string organization, string product)
        {
            // Validate the URL
            if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(organization))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot be empty");
            }
            if (product.Length > c_MaxLength || organization.Length > c_MaxLength)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot contain too many characters");
            }

            // Check that the org/product exsits
            var dataStore = await m_ControllerDataStore.EnsureDataStore();
            var queryableProduct = await dataStore.Products.GetProduct(organization, product);
            if (queryableProduct == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Could not find product");
            }

            // Read the payload from the message
            var payloadString = await Request.Content.ReadAsStringAsync();

            // Convert to JSON
            JObject payload;
            try
            {
                payload = JObject.Parse(payloadString);
            }
            catch (JsonException)
            {
                // This is a bad request
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Could not understand payload");
            }

            // Should be a name and a query
            JToken nameToken;
            JToken queryToken;
            JObject query;

            if (!payload.TryGetValue("query-name", out nameToken))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing query name");
            }

            if (!payload.TryGetValue("query-definition", out queryToken))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing query definition");
            }

            query = queryToken as JObject;
            if (query == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid query definition");
            }

            // Validate the name
            string name;
            if (nameToken.Type != JTokenType.String)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Name must be a string");
            }

            name = nameToken.Value<string>();
            if (string.IsNullOrEmpty(name) || name.Length > c_MaxQueryNameLength)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Name has a bad length");
            }

            // Try to deserialize the query
            var queryBuilder = queryableProduct.CreateQueryBuilder();
            if (queryBuilder == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Query builder missing");
            }

            IRegardQuery builtQuery;

            // Will throw an InvalidOperationException if the query is bad
            try
            {
                builtQuery = queryBuilder.FromJson(query);
                if (builtQuery == null)
                {
                    // Shouldn't happen, I think
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Query failed to build");
                }
            }
            catch (InvalidOperationException e)
            {
                Trace.WriteLine("QueryController: could not build query: " + e);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid query definition");
            }

            // Register the query
            await queryableProduct.RegisterQuery(name, builtQuery);

            // OK
            return Request.CreateResponse(HttpStatusCode.Created, new {});
        }

        /// <summary>
        /// Runs a query previously created by register-query and returns all of the results
        /// </summary>
        [HttpGet, Route("product/v1/{organization}/{product}/run-query/{queryname}")]
        public Task<HttpResponseMessage> RunQuery(string organization, string product, string queryname)
        {
            return RunQuery(organization, product, queryname, null);
        }

        /// <summary>
        /// Runs a query previously created by register-query and returns all of the results
        /// </summary>
        [HttpGet, Route("product/v1/{organization}/{product}/run-query/{queryname}/{index}")]
        public async Task<HttpResponseMessage> RunQuery(string organization, string product, string queryname, string index)
        {
            // Validate the URL
            if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(organization))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot be empty");
            }
            if (product.Length > c_MaxLength || organization.Length > c_MaxLength)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot contain too many characters");
            }

            // TODO: return the first 'n' query results instead
            // Check that the org/product exsits
            var dataStore = await m_ControllerDataStore.EnsureDataStore();

            var queryableProduct = await dataStore.Products.GetProduct(organization, product);

            if (queryableProduct == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Could not find product");
            }

            // Validate the name
            if (string.IsNullOrEmpty(queryname) || queryname.Length > c_MaxQueryNameLength)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Name has a bad length");
            }

            // Attempt to run the query
            IResultEnumerator<QueryResultLine> queryResult;

            try
            {
                if (index != null)
                {
                    queryResult = await queryableProduct.RunIndexedQuery(queryname, index);
                }
                else
                {
                    queryResult = await queryableProduct.RunQuery(queryname);
                }
            }
            catch (InvalidOperationException e)
            {
                Trace.WriteLine("Could not run query: " + e);

                // This could happen for other reasons - however, the most common will be that the query was not found, so say that was what happened to the user
                // Check the trace for the real reason if it's different
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Query is not registered");
            }

            if (queryResult == null)
            {
                // If the result is null, then the query could not be found
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Query is not registered");
            }

            // Read the response lines
            // TODO: make it possible to read partial results in case there are a lot of queries
            var lines = new JArray();
            for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
            {
                JObject line = new JObject {{"EventCount", result.EventCount}};
                foreach (var column in result.Columns)
                {
                    line[column.Name] = column.Value;
                }

                // TODO: maybe exclude result lines with an event count of 1 to prevent identification of individual users
                lines.Add(line);
            }

            // Build the final model
            var model = new JObject { { "Results", lines } };

            // Generate the result object
            return Request.CreateResponse(HttpStatusCode.OK, model);
        }

        /// <summary>
        /// Retrieves the list of events for a particular user
        /// </summary>
        /// <remarks>
        /// Offset indicates the event to begin at
        /// </remarks>
        [HttpGet, Route("product/v1/{organization}/{product}/get-events-for-user/{uid}/{offset}")]
        public async Task<HttpResponseMessage> GetEventsForUser(string organization, string product, string uid, int offset)
        {
            // Verify that the UID is a GUID
            Guid parsedUid;
            if (!Guid.TryParse(uid, out parsedUid))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "UID is not a GUID");
            }

            var dataStore = await m_ControllerDataStore.EnsureDataStore();

            // Get the product, and ensure that it exists
            var queryableProduct = await dataStore.Products.GetProduct(organization, product);
            if (queryableProduct == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Could not find product");
            }

            // Retrieve the data for this UID
            string nextPage = null;
            JArray results = new JArray();

            // TODO: return only one page at a time
            do
            {
                var userEvents = await queryableProduct.RetrieveEventsForUser(parsedUid, nextPage);

                // Copy to a JArray for later returning
                for (var nextEvent = await userEvents.FetchNext(); nextEvent != null; nextEvent = await userEvents.FetchNext())
                {
                    results.Add(nextEvent);
                }

                nextPage = await userEvents.GetNextPageToken();
            } while (nextPage != null);

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        /// <summary>
        /// Retrieves the list of events for a particular user
        /// </summary>
        [HttpGet, Route("product/v1/{organization}/{product}/get-events-for-user/{uid}")]
        public async Task<HttpResponseMessage> GetEventsForUser(string organization, string product, string uid)
        {
            return await GetEventsForUser(organization, product, uid, 0);
        }

        /// <summary>
        /// Opts in a particular user
        /// </summary>
        [HttpPost, Route("product/v1/{organization}/{product}/users/{uid}/opt-in")]
        public async Task<HttpResponseMessage> OptIn(string organization, string product, string uid)
        {
            // Validate the URL
            if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(uid))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot be empty");
            }
            if (product.Length > c_MaxLength || organization.Length > c_MaxLength || uid.Length > c_MaxLength)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot contain too many characters");
            }

            // Check that the org/product exsits
            var dataStore = await m_ControllerDataStore.EnsureDataStore();
            var queryableProduct = await dataStore.Products.GetProduct(organization, product);
            if (queryableProduct == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Could not find product");
            }

            // UID should be a GUID
            Guid uidGuid;

            if (!Guid.TryParse(uid, out uidGuid))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "UID must be a GUID");
            }

            // Opt in
            await queryableProduct.Users.OptIn(uidGuid);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// Opts out a particular user
        /// </summary>
        /// <remarks>
        /// That is, moves the user to the state 'data is retained but not used in a query'. A future revision will have 'data is also deleted'.
        /// It's expected that the application will not send data for an opted out user. If it does, new data will be stored but won't be used in a query.
        /// </remarks>
        [HttpPost, Route("product/v1/{organization}/{product}/users/{uid}/opt-out")]
        public async Task<HttpResponseMessage> OptOut(string organization, string product, string uid)
        {
            // Validate the URL
            if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(uid))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot be empty");
            }
            if (product.Length > c_MaxLength || organization.Length > c_MaxLength || uid.Length > c_MaxLength)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot contain too many characters");
            }

            // Check that the org/product exsits
            var dataStore = await m_ControllerDataStore.EnsureDataStore();
            var queryableProduct = await dataStore.Products.GetProduct(organization, product);
            if (queryableProduct == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Could not find product");
            }

            // UID should be a GUID
            Guid uidGuid;

            if (!Guid.TryParse(uid, out uidGuid))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "UID must be a GUID");
            }

            // Opt out
            await queryableProduct.Users.OptOut(uidGuid);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// Request to delete the data associated with a particualr user ID
        /// </summary>
        [HttpPost, Route("product/v1/{organization}/{product}/users/{uid}/delete-data")]
        public async Task<HttpResponseMessage> Delete(string organization, string product, string uid)
        {
            // Validate the URL
            if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(uid))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot be empty");
            }
            if (product.Length > c_MaxLength || organization.Length > c_MaxLength || uid.Length > c_MaxLength)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot contain too many characters");
            }

            // Check that the org/product exsits
            var dataStore = await m_ControllerDataStore.EnsureDataStore();
            var queryableProduct = await dataStore.Products.GetProduct(organization, product);
            if (queryableProduct == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Could not find product");
            }

            // UID should be a GUID
            Guid uidGuid;

            if (!Guid.TryParse(uid, out uidGuid))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "UID must be a GUID");
            }

            // Delete the data for this user; also opt-out in case there's an error
            await queryableProduct.Users.OptOut(uidGuid);
            await queryableProduct.Users.DeleteData(uidGuid);

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
