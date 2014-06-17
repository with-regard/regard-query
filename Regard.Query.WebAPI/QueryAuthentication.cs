using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using Microsoft.WindowsAzure;

namespace Regard.Query.WebAPI
{
    class QueryAuthentication : Attribute, IAuthenticationFilter
    {
        /// <summary>
        /// Gets or sets a value indicating whether more than one instance of the indicated attribute can be specified for a single program element.
        /// </summary>
        /// <returns>
        /// true if more than one instance is allowed to be specified; otherwise, false. The default is false.
        /// </returns>
        public bool AllowMultiple { get; private set; }

        /// <summary>
        /// Authenticates the request.
        /// </summary>
        /// <returns>
        /// A Task that will perform authentication.
        /// </returns>
        /// <param name="context">The authentication context.</param><param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            bool authenticationFailed = true;

            // The request must specify basic authentication
            if (request.Headers.Authorization != null &&
                request.Headers.Authorization.Scheme.Equals("basic", StringComparison.InvariantCultureIgnoreCase))
            {
                // Decode the credentials
                var latin1      = Encoding.GetEncoding("iso-8859-1");
                var credentials = latin1.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter));

                // Split into a userID/password pair
                // ':' is an invalid character in the password using this technique
                var parts       = credentials.Split(':');
                if (parts.Length < 2)
                {
                    // Malformed request
                    parts = new[] { "", "" };
                }

                // Check the password
                var username = parts[0];
                var password = parts[1];

                // Use the values stored in cloud configuration to check against the password
                // (This is pretty rubbish as a way to store the password but should be adequate at the moment)
                // A HMAC scheme would be much better, particularly if we eventually want to support multiple clients
                // Could also restrict IP ranges to those we know belong to Azure
                var targetUsername = CloudConfigurationManager.GetSetting("Regard.JsonAPI.UserId");
                var targetPassword = CloudConfigurationManager.GetSetting("Regard.JsonAPI.Password");

                // To test, check that the username and password are identical to see if authentication is actually working
                if (targetUsername == username && targetPassword == password)
                {
                    // Password is valid
                    authenticationFailed = false;
                }
            }

            if (authenticationFailed)
            {
                // Return an unauthorized result
                context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
            }

            return Task.FromResult(0);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}
