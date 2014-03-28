using System;
using System.Threading.Tasks;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that can iterate through the results of a query (effectively just a task-based version of IEnumerable)
    /// </summary>
    public interface IResultEnumerator<TResult> : IDisposable where TResult : class
    {
        /// <summary>
        /// Fetches the next result, or returns null if there are no more results
        /// </summary>
        Task<TResult> FetchNext();
    }
}
