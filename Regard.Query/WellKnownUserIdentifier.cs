using System;

namespace Regard.Query
{
    /// <summary>
    /// Identifiers for 'well-known' users (that is, user IDs with particular meanings)
    /// </summary>
    public static class WellKnownUserIdentifier
    {
        /// <summary>
        /// User identifier that means 'the developer of the current product' 
        /// </summary>
        public readonly static Guid ProductDeveloper = new Guid("5C96A4B1-CB43-48C0-9EEC-53A7E0CFDBCA");
    }
}
