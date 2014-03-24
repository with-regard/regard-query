namespace Regard.Query.Api
{
    /// <summary>
    /// Represents a product that can be queried
    /// </summary>
    public interface IQueryableProduct
    {
        /// <summary>
        /// Creates a new query builder for this product
        /// </summary>
        IQueryBuilder CreateQueryBuilder();
    }
}
