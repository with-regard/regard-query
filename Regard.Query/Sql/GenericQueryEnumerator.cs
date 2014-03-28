using System;
using System.Threading.Tasks;
using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Generic implementation of the query enumerator that uses a lambda to 
    /// </summary>
    internal class GenericQueryEnumerator : IResultEnumerator<QueryResultLine>
    {
        /// <summary>
        /// Function that acquires the next value
        /// </summary>
        private readonly Func<Task<QueryResultLine>> m_NextFunc;

        /// <summary>
        /// Function called when this object is disposed
        /// </summary>
        private readonly Action m_Dispose;

        public GenericQueryEnumerator(Func<Task<QueryResultLine>> nextFunc, Action dispose)
        {
            if (nextFunc == null)
            {
                throw new ArgumentNullException("nextFunc");
            }

            if (dispose == null)
            {
                dispose = () => { };
            }

            m_NextFunc  = nextFunc;
            m_Dispose   = dispose;
        }

        public void Dispose()
        {
            m_Dispose();
        }

        /// <summary>
        /// Fetches the next result, or returns null if there are no more results
        /// </summary>
        public Task<QueryResultLine> FetchNext()
        {
            return m_NextFunc();
        }
    }
}
