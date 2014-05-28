using System.Threading.Tasks;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that retrieves Key/Value store on a per-project basis
    /// </summary>
    public interface IProductStoreRetrieval
    {
        /// <summary>
        /// Retrieves or creates a key/value store for a product
        /// </summary>
        Task<IKeyValueStore> GetStoreForProduct(string organization, string product);
    }
}
