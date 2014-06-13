using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Regard.Query.WebAPI
{
    public interface IQueryController
    {
        /// <summary>
        /// Just indicates the version of this assembly
        /// </summary>
        [HttpGet, Route("version")]
        Task<HttpResponseMessage> Version();

        /// <summary>
        /// Request to create a new product
        /// </summary>
        /// <remarks>
        /// POST to admin/v1/product/create a JSON message with the fields 'product' and 'organization' indicating the product and organization to create
        /// </remarks>
        //[HttpPost, Route("admin/v1/product/create")]
        //Task<HttpResponseMessage> CreateProduct();

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
        //[HttpPost, Route("product/v1/{organization}/{product}/register-query")]
        //Task<HttpResponseMessage> RegisterQuery(string organization, string product);

        /// <summary>
        /// Runs a query previously created by register-query and returns all of the results
        /// </summary>
        [HttpGet, Route("product/v1/{organization}/{product}/run-query/{queryname}")]
        Task<HttpResponseMessage> RunQuery(string organization, string product, string queryname);

        /// <summary>
        /// Opts in a particular user
        /// </summary>
        //[HttpPost, Route("product/v1/{organization}/{product}/users/{uid}/opt-in")]
        //Task<HttpResponseMessage> OptIn(string organization, string product, string uid);

        /// <summary>
        /// Opts out a particular user
        /// </summary>
        /// <remarks>
        /// That is, moves the user to the state 'data is retained but not used in a query'. A future revision will have 'data is also deleted'.
        /// It's expected that the application will not send data for an opted out user. If it does, new data will be stored but won't be used in a query.
        /// </remarks>
        //[HttpPost, Route("product/v1/{organization}/{product}/users/{uid}/opt-out")]
        //Task<HttpResponseMessage> OptOut(string organization, string product, string uid);
    }
}