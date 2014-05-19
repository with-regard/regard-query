using System;
using Regard.Query.Api;

namespace Regard.Query.Tests
{
    /// <summary>
    /// A data store that doesn't exist, so it's possible to write tests without an implementation
    /// </summary>
    class MissingDataStore : IRegardDataStore
    {
        public IEventRecorder EventRecorder { get { throw new NotImplementedException(); } }
        public IProductAdmin Products { get { throw new NotImplementedException(); } }
    }
}
