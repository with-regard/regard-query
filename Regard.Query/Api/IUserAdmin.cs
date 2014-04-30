using System;
using System.Threading.Tasks;

namespace Regard.Query.Api
{
    /// <summary>
    /// Interface used to perform actions relating to end-users
    /// </summary>
    public interface IUserAdmin
    {
        /// <summary>
        /// Marks a specific user ID as being opted in to data collection for a specific product
        /// </summary>
        Task OptIn(Guid userId);

        /// <summary>
        /// Marks a specific user ID as being opted out from data collection for a specific product
        /// </summary>
        /// <remarks>
        /// This only opts out for future data collection. Any existing data will be retained.
        /// </remarks>
        Task OptOut(Guid userId);
    }
}
