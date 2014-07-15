using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Regard.Query.StressTest
{
    /// <summary>
    /// Class that runs a stress test
    /// </summary>
    public class StressTest
    {
        /// <summary>
        /// The user IDs used by the tests
        /// </summary>
        private static Dictionary<int, Guid> s_UserIds = new Dictionary<int, Guid>();

        private static object s_UidLock = new object();
        private static Random s_Rng = new Random();

        /// <summary>
        /// Sends a single request
        /// </summary>
        public static async Task<HttpStatusCode> SendARequest(TestOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            // Session and user ID
            Guid sessionId = Guid.NewGuid();
            int userNumber = s_Rng.Next(options.NumUsers);
            Guid userId;

            lock (s_UidLock)
            {
                if (!s_UserIds.TryGetValue(userNumber, out userId))
                {
                    // Generate a new user ID for this user number
                    userId = s_UserIds[userNumber] = Guid.NewGuid();
                }
            }

            // Generate the request payload
            JArray payload = new JArray();

            for (int evtNum = 0; evtNum < options.EventsPerRequest; ++evtNum)
            {
                // Create the contents of the 'data' field
                JObject internalEventData = new JObject();
                internalEventData["action"] = options.EventActions[s_Rng.Next(options.EventActions.Length)];

                // Create the actual event data
                JObject eventData = new JObject();

                eventData["user-id"]    = userId.ToString();
                eventData["session-id"] = sessionId.ToString();
                eventData["event-type"] = "stress-test";
                eventData["time"]       = DateTime.UtcNow.ToString("o");
                eventData["data"]       = internalEventData;

                payload.Add(eventData);
            }

            // Send the event
            var payloadBytes = Encoding.UTF8.GetBytes(payload.ToString());
            var targetUrl = options.EndPointUrl + "/track/v1/" + options.Organization + "/" + options.Product + "/event";

            // Send to the service
            var request = WebRequest.Create(new Uri(targetUrl));
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = payloadBytes.Length;

            var payloadStream = await request.GetRequestStreamAsync();
            payloadStream.Write(payloadBytes, 0, payloadBytes.Length);
            payloadStream.Close();

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                return response.StatusCode;
            }
        }

        /// <summary>
        /// Runs a stress test, optionally for only a specified period of time
        /// </summary>
        /// <remarks>
        /// Diagnostic output will be produced using Trace.WriteLine
        /// </remarks>
        public static async Task RunStressTest(TestOptions options, TimeSpan? maxDuration = null)
        {
            const int statInterval = 10000;

            if (options == null) throw new ArgumentNullException("options");

            // Write some introductory waffle
            Trace.WriteLine("=== REGARD STRESS TEST STARTING");
            options.WriteTrace();
            Trace.WriteLine("");

            // Currently active requests
            List<Task> activeRequests   = new List<Task>();

            // Remember when we started so we know when to finish
            DateTime startTime          = DateTime.Now;

            TimeSpan timeBetweenEvents  = TimeSpan.FromMilliseconds(1000.0/(double) options.RequestsPerSecond);
            DateTime nextEventTime      = DateTime.Now + timeBetweenEvents;

            // Store some stats
            DateTime nextStatTime       = DateTime.Now + TimeSpan.FromMilliseconds(statInterval);
            int totalRequests           = 0;
            int missedRequests          = 0;
            int totalErrors             = 0;

            // Lambda saves us some copy/paste work
            Action displayStats = () =>
            {
                Trace.WriteLine("Total requests:                         " + totalRequests);
                Trace.WriteLine("Missed requests:                        " + missedRequests);
                Trace.WriteLine("Requests that did not respond with 200: " + totalErrors);
                Trace.WriteLine("");
            };

            for (;;)
            {
                DateTime now = DateTime.Now;

                // Check for end of testing
                if (maxDuration != null && now >= startTime + maxDuration)
                {
                    break;
                }

                // Wait until it's time to send the next event
                TimeSpan timeUntilNextEvent = nextEventTime - now;
                if (timeUntilNextEvent.TotalMilliseconds > 0)
                {
                    await Task.Delay(timeUntilNextEvent);
                }

                // If the events are full then wait until there's a free slot
                while (activeRequests.Count >= options.MaxSimultaneousRequests)
                {
                    // Wait for a task to finish
                    await Task.WhenAny(activeRequests);

                    // Remove any finished tasks
                    for (int taskId = 0; taskId < activeRequests.Count; ++taskId)
                    {
                        if (activeRequests[taskId].IsCompleted)
                        {
                            activeRequests.RemoveAt(taskId);
                            --taskId;
                        }
                    }
                }

                // Time has moved on
                now = DateTime.Now;

                // Generate requests until we fill the queue or hit the next request time
                while (nextEventTime < now && activeRequests.Count < options.MaxSimultaneousRequests)
                {
                    // Move on to the next event
                    nextEventTime += timeBetweenEvents;

                    // Add a request
                    totalRequests++;
                    activeRequests.Add(Task.Run(async () =>
                    {
                        // Wait for result
                        var resultCode = await SendARequest(options);

                        // Mark as an error if there's a problem
                        if (resultCode != HttpStatusCode.OK)
                        {
                            Interlocked.Increment(ref totalErrors);
                        }
                    }));
                }

                // Skip events if the active set is full
                while (nextEventTime < now) {
                    missedRequests++;
                    nextEventTime += timeBetweenEvents;
                }
                
                // Display stats if it's time
                if (now >= nextStatTime)
                {
                    nextStatTime = now + TimeSpan.FromMilliseconds(statInterval);
                    displayStats();
                }
            }

            // Finish up by displaying the statistics
            Trace.WriteLine("Finishing requests");
            displayStats();
        }
    }
}
