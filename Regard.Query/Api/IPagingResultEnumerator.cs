namespace Regard.Query.Api
{
    /// <summary>
    /// Result enumerator that can be
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPagingResultEnumerator<T> : IResultEnumerator<T> where T : class
    {
        /// <summary>
        /// Fast-forwards this enumerator so that the next call to FetchNext will retrieve the element at the specified index (a positive offset from the current index)
        /// </summary>
        void FastForward(int index);
    }
}
