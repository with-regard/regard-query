using System.Threading.Tasks;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that retrieves Key/Value store on a per-project basis
    /// </summary>
    public interface IProjectStoreRetrieval
    {
        /// <summary>
        /// Retrieves or creates a key/value store for a project
        /// </summary>
        Task<IKeyValueStore> GetStoreForProject(string organization, string project);
    }
}
