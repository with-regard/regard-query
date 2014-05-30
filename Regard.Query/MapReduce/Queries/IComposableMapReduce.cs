namespace Regard.Query.MapReduce.Queries
{
    /// <summary>
    /// Interface implemented by objects that represent a composable map/reduce operation
    /// </summary>
    /// <remarks>
    /// Traditional map/reduce functions aren't really composable, except by chaining. This modifies the map, reduce, et
    /// </remarks>
    internal interface IComposableMapReduce : IComposableMap, IComposableReduce
    {
    }
}
