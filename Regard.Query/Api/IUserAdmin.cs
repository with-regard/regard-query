using System;

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
        void OptIn(Guid userId, string organization, string product);

        /// <summary>
        /// Marks a specific user ID as being opted out from data collection for a specific product
        /// </summary>
        /// <remarks>
        /// This only opts out for future data collection. Any existing data will be retained.
        /// </remarks>
        void OptOut(Guid userId, string organization, string product);
    }
}
