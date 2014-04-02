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

        /// <summary>
        /// User identifier for a test user (who gets automatically opted-in to everything but cannot be used for production data)
        /// </summary>
        public readonly static Guid TestUser = new Guid("F16CB994-00FF-4326-B0DB-F316F7EC2942");
    }
}
