using System;
using System.Collections.Generic;
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
    // TODO: this class should be better factored

    /// <summary>
    /// ApiController that can be used to host the Regard query API - without any authentication or authorization
    /// </summary>
    public class QueryController : ApiController
    {
        /// <summary>
        /// Synchronisation object (protects m_CreatingDataStore in particular)
        /// </summary>
        private readonly object m_Sync = new object();

        /// <summary>
        /// The data store that this will make available
        /// </summary>
        private IRegardDataStore m_DataStore;

        /// <summary>
        /// The maximum length of a product or organization name
        /// </summary>
        private const int c_MaxLength = 256;
        private const int c_MaxQueryNameLength = 200;

        /// <summary>
        /// Task that executes if we're trying to create the default data store
        /// </summary>
        private Task m_CreatingDataStore;

        /// <summary>
        /// Creates a new query controller with the default data store
        /// </summary>
        public QueryController()
        {
            // TODO: we could also support DI here
        }

        /// <summary>
        /// Creates a new query controller with a specific data store
        /// </summary>
        /// <param name="dataStore">The data store to use, or null if the controller should create the default data store using <see cref="DataStoreFactory.CreateDefaultDataStore"></see></param>
        public QueryController(IRegardDataStore dataStore)
        {
            m_DataStore = dataStore;
        }

        /// <summary>
        /// Task that actually creates a data store
        /// </summary>
        private async Task ActuallyCreateDataStore()
        {
            // Generate a data store
            var newDataStore = await DataStoreFactory.CreateDefaultDataStore();

            // Store it in the data store variable
            lock (m_Sync)
            {
                m_DataStore = newDataStore;
            }
        }

        /// <summary>
        /// Ensures that the m_DataStore parameter is populated
        /// </summary>
        private async Task EnsureDataStore()
        {
            Task alreadyRunning = null;

            lock (m_Sync)
            {
                // Nothing to do if the data store is already created
                if (m_DataStore != null)
                {
                    return;
                }

                // Check if something else is already retrieving the data store
                alreadyRunning = m_CreatingDataStore;

                // If nothing is running, then this is the first thing to try to create a data store, so create a new one
                if (alreadyRunning == null)
                {
                    m_CreatingDataStore = alreadyRunning = ActuallyCreateDataStore();
                }
            }

            // Wait for the data store to finish creating
            try
            {
                await alreadyRunning;
            }
            finally
            {
                // The data store should exist and 
                lock (m_Sync)
                {
                    m_CreatingDataStore = null;
                }
            }
        }

        /// <summary>
        /// Just indicates the version of this assembly
        /// </summary>
        [HttpGet, Route("version")]
        public async Task<HttpResponseMessage> Version()
        {
            await EnsureDataStore();

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
            await EnsureDataStore();

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

            if (!payload.TryGetValue("product", out productToken) || !payload.TryGetValue("organization", out organizationToken))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields missing from request");
            }

            // Sanity check what was passed in
            if (productToken.Type != JTokenType.String || organizationToken.Type != JTokenType.String)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields must be strings");
            }

            string product = productToken.Value<string>();
            string organization = organizationToken.Value<string>();

            if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(organization))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot be empty");
            }

            if (product.Length > c_MaxLength || organization.Length > c_MaxLength)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Fields cannot contain too many characters");
            }

            // Actually create the product
            await m_DataStore.Products.CreateProduct(organization, product);
            return Request.CreateResponse(HttpStatusCode.Created, new {});
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
            await EnsureDataStore();

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
            var queryableProduct = await m_DataStore.Products.GetProduct(organization, product);
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
        public async Task<HttpResponseMessage> RunQuery(string organization, string product, string queryname)
        {
            await EnsureDataStore();

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
            var queryableProduct = await m_DataStore.Products.GetProduct(organization, product);
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
                queryResult = await queryableProduct.RunQuery(queryname);
            }
            catch (InvalidOperationException e)
            {
                Trace.WriteLine("Could not run query: " + e);

                // This could happen for other reasons - however, the most common will be that the query was not found, so say that was what happened to the user
                // Check the trace for the real reason if it's different
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Query is not registered");
            }

            // Read the response lines
            // TODO: make it possible to read partial results in case there are a lot of queries
            var lines = new List<QueryResultLine>();
            for (var result = await queryResult.FetchNext(); result != null; result = await queryResult.FetchNext())
            {
                // TODO: maybe exclude result lines with a count of 1 to prevent identification of individual users
                lines.Add(result);
            }

            // Build the final model
            var model = new QueryResponseModel {Results = lines};

            // Generate the result object
            return Request.CreateResponse(HttpStatusCode.OK, model);
        }

        /// <summary>
        /// Opts in a particular user
        /// </summary>
        [HttpGet, Route("product/v1/{organization}/{product}/users/{uid}/opt-in")]
        public async Task<HttpResponseMessage> OptIn(string organization, string product, string uid)
        {
            await EnsureDataStore();

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
            var queryableProduct = await m_DataStore.Products.GetProduct(organization, product);
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
        [HttpGet, Route("product/v1/{organization}/{product}/users/{uid}/opt-out")]
        public async Task<HttpResponseMessage> OptOut(string organization, string product, string uid)
        {
            await EnsureDataStore();

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
            var queryableProduct = await m_DataStore.Products.GetProduct(organization, product);
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
    }
}
