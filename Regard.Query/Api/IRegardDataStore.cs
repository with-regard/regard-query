namespace Regard.Query.Api
{
    /// <summary>
    /// Represents an instance of the query engine that runs against a single data store. Implementations of this class are usually the
    /// entry-point into the query API.
    /// </summary>
    public interface IRegardDataStore
    {
        /// <summary>
        /// The event recorder for this engine
        /// </summary>
        /// <remarks>
        /// Note that events will be discarded if there is no project registered for them
        /// </remarks>
        IEventRecorder EventRecorder { get; }

        /// <summary>
        /// The object that can create and retrieve products in order to register or run queries against them
        /// </summary>
        IProductAdmin Products { get; }
    }
}
