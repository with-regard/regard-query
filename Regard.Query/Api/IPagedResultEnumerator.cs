namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that iterate through a partial set of results
    /// </summary>
    public interface IPagedResultEnumerator<TResult> : IResultEnumerator<TResult> where TResult : class
    {
        /// <summary>
        /// The token to pass to the generator call for the next page, or null if this is the last page
        /// </summary>
        string NextPageToken { get; }
    }
}