namespace Regard.Query.MapReduce.Queries
{
    /// <summary>
    /// Interface implemented by objects that chain together composable queries. Chains are eventually 'unrolled' in the order that they occur (and can't
    /// themselves chain other queries)
    /// </summary>
    internal interface IComposableChain
    {
        IComposableMapReduce ChainWith { get; }
    }
}
