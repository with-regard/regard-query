using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
    class QueryController : ApiController
    {
        /// <summary>
        /// The data store that this will make available
        /// </summary>
        private readonly IRegardDataStore m_DataStore;

        /// <summary>
        /// The maximum length of a product or organization name
        /// </summary>
        private const int c_MaxLength = 256;

        public QueryController(IRegardDataStore dataStore)
        {
            if (dataStore == null) throw new ArgumentNullException("dataStore");
            m_DataStore = dataStore;
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
        /// Accepts a 
        /// </remarks>
        [HttpPost, Route("product/v1/{organization}/{product}/register-query")]
        public async Task<HttpResponseMessage> RegisterQuery(string organization, string product)
        {
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
            if (string.IsNullOrEmpty(name) || name.Length > 200)
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
        public async Task<HttpResponseMessage> RunQuery(string organization, string product)
        {
            // TODO: if a query produces a very large number of results, this will be inefficient
            throw new NotImplementedException();
        }

        /// <summary>
        /// Opts in a particular user
        /// </summary>
        [HttpGet, Route("product/v1/{organization}/{product}/users/{uid}/opt-in")]
        public async Task<HttpResponseMessage> OptIn(string organization, string product)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Opts out a particular user
        /// </summary>
        /// <remarks>
        /// That is, moves the user to the state 'data is retained but not used in a query'. A future revision will have 'data is also deleted'.
        /// It's expected that the application will not send data for an opted out user. If it does, new data will be stored but won't be used in a query.
        /// </remarks>
        [HttpGet, Route("product/v1/{organization}/{product}/users/{uid}/opt-out")]
        public async Task<HttpResponseMessage> OptOut(string organization, string product)
        {
            throw new NotImplementedException();
        }
    }
}
