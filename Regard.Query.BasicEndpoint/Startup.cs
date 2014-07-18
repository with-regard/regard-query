using System.Threading;
using System.Web.Http;
using Owin;
using Regard.Query.Services.QueryRefresh;

namespace Regard.Query.BasicEndpoint
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // We need some completion threads for finishing data writes
            ThreadPool.SetMaxThreads(40, 100);

            // Monitor for query update requests
            //var queryService = new QueryRefreshService();
            //queryService.Start();

            // Configure for attribute routes
            var httpConfiguration = new HttpConfiguration();
            httpConfiguration.MapHttpAttributeRoutes();

            app.UseWebApi(httpConfiguration);

            app.Run(async context =>
            {
                // Default behaviour is is to just 404
                context.Response.ContentType    = "text/plain";
                context.Response.StatusCode     = 404;
                await context.Response.WriteAsync("");
            });
        }
    }
}