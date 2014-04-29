using System.Threading.Tasks;

namespace Regard.Query.Api
{
    /// <summary>
    /// Class representing a query for Regard data
    /// </summary>
    public interface IRegardQuery
    {
        /// <summary>
        /// The object that built this query (and which can be used to refine it)
        /// </summary>
        IQueryBuilder Builder { get; }
    }
}
