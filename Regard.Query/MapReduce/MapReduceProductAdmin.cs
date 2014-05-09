using System.Threading.Tasks;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Product admin interface for the map/reduce query system
    /// </summary>
    internal class MapReduceProductAdmin : IProductAdmin
    {
        /// <summary>
        /// Creates a new product that can have events logged against it
        /// </summary>
        public Task CreateProduct(string organization, string product)
        {
            throw new System.NotImplementedException();
        }

        public Task<IQueryableProduct> GetProduct(string organization, string product)
        {
            throw new System.NotImplementedException();
        }
    }
}
