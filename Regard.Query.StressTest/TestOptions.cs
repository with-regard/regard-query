using System.Diagnostics;

namespace Regard.Query.StressTest
{
    /// <summary>
    /// Options that describe how a stress test should be run
    /// </summary>
    /// <remarks>
    /// These options just apply to data ingestion: we could also stress test by regularly running queries. However, I think a more realistic scenario is to run
    /// the queries manually.
    /// </remarks>
    public class TestOptions
    {
        public TestOptions()
        {
            // 5 events per second, 10000 users = ~30 minutes until we generate events for all users
            RequestsPerSecond       = 5;
            EventsPerRequest        = 5;
            NumUsers                = 10000;
            MaxSimultaneousRequests = 5;
            EventActions            = new [] { "start", "click-on-something", "drag-a-thing", "horrible-crash" };
            Product                 = "StressTest";
            Organization            = "WithRegardStress";                                   // Dedicating an organization for this ensures that we can delete the data easily if (hah- when!) things go horribly horribly wrong
            EndPointUrl             = "https://api.withregard.io";
        }

        /// <summary>
        /// The number of requests to generate per second
        /// </summary>
        public int RequestsPerSecond { get; set; }

        /// <summary>
        /// The number of events to generate per request
        /// </summary>
        public int EventsPerRequest { get; set; }

        /// <summary>
        /// Possible actions that can be generated in the event payload (the events themselves are fairly simple, consisting of just a time, a user ID and 
        /// </summary>
        public string[] EventActions { get; set; }

        /// <summary>
        /// The number of seperate users to simulate
        /// </summary>
        /// <remarks>
        /// Each event is for a random user, though the initial set will cycle through the entire list
        /// </remarks>
        public int NumUsers { get; set; }

        /// <summary>
        /// The maximum number of pending requests to generate
        /// </summary>
        public int MaxSimultaneousRequests { get; set; }

        /// <summary>
        /// The URL of the endpoint where events should be sent
        /// </summary>
        public string EndPointUrl { get; set; }

        /// <summary>
        /// The name of the product that events will be generated for
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// The name of the organization that events will be generated for
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// Writes option details to the trace log
        /// </summary>
        public void WriteTrace()
        {
            Trace.WriteLine(" Requests per second:              " + RequestsPerSecond);
            Trace.WriteLine(" Events per request:               " + EventsPerRequest);
            Trace.WriteLine(" Number of unique users:           " + NumUsers);
            Trace.WriteLine(" Maximum simultaneous requests:    " + MaxSimultaneousRequests);
            Trace.WriteLine(" Organization:                     " + Organization);
            Trace.WriteLine(" Product:                          " + Product);
        }
    }
}
