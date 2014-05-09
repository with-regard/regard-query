using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

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
            // The default response is 'bad request'
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.BadRequest;

            // The content will be JSON
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

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
        [HttpPost, Route("product/v1/{product}/{organization}/register-query")]
        public async Task<HttpResponseMessage> RegisterQuery()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs a query previously created by register-query
        /// </summary>
        [HttpGet, Route("product/v1/{product}/{organization}/run-query/{queryname}")]
        public async Task<HttpResponseMessage> RunQuery()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Opts in a particular user
        /// </summary>
        [HttpGet, Route("product/v1/{product}/{organization}/users/{uid}/opt-in")]
        public async Task<HttpResponseMessage> OptIn()
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
        [HttpGet, Route("product/v1/{product}/{organization}/users/{uid}/opt-out")]
        public async Task<HttpResponseMessage> OptOut()
        {
            throw new NotImplementedException();
        }
    }
}
