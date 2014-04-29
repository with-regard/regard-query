using Newtonsoft.Json.Linq;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface implemented by objects that represent output targets for map operations
    /// </summary>
    public interface IMapTarget
    {
        /// <summary>
        /// Emits a document to this target
        /// </summary>
        void Emit(string key, JObject document);
    }
}
