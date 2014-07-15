using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Regard.Query.StressTest
{
    /// <summary>
    /// Class that runs a stress test
    /// </summary>
    public class StressTest
    {
        /// <summary>
        /// Sends a single request
        /// </summary>
        public static async Task SendARequest(TestOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            // TODO: implement me
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

            // Lambda saves us some copy/paste work
            Action displayStats = () =>
            {
                Trace.WriteLine("Total requests:  " + totalRequests);
                Trace.WriteLine("Missed requests: " + missedRequests);
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
                while (activeRequests.Count > options.MaxSimultaneousRequests)
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
                    activeRequests.Add(SendARequest(options));
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
