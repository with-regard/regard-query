namespace Regard.Query.Api
{
    /// <summary>
    /// Retrieves and creates products that can have events recorded against them
    /// </summary>
    public interface IProductAdmin
    {
        /// <summary>
        /// Creates a new product that can have events logged against it
        /// </summary>
        void CreateProduct(string organization, string product);

        /// <summary>
        /// For a product that exists, retrieves the interface for interacting with its queries
        /// </summary>
        IQueryableProduct GetProduct(string organization, string product);
    }
}
