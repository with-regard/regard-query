using System.Threading.Tasks;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that can iterate through the results of a query
    /// </summary>
    public interface IResultEnumerator<TResult>
    {
        /// <summary>
        /// Fetches the next result
        /// </summary>
        Task<TResult> FetchNext();
    }
}
