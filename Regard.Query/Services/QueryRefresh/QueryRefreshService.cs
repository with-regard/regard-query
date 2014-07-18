using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.Services.QueryRefresh
{
    /// <summary>
    /// In order to deliver query results in a timely manner for products with a high volume of events, we need to periodically
    /// update their results. This service listens to a service bus for notifications about products that have received a
    /// large number of events, and updates the data associated with them.
    /// </summary>
    /// <remarks>
    /// Performing an update every 5000 events or so will ensure that the response time for any given query is under about
    /// 5 seconds.
    /// </remarks>
    public class QueryRefreshService
    {
        private object m_Sync = new object();
        
        /// <summary>
        /// Flag set when the service should shut down
        /// </summary>
        private bool m_Stop;

        /// <summary>
        /// True if the service is running
        /// </summary>
        private bool m_Running;

        /// <summary>
        /// Starts the refresh service running
        /// </summary>
        public void Start()
        {
            lock (m_Sync)
            {
                // Only start once
                if (m_Running)
                {
                    return;
                }

                // Stop the service when the role stops
                RoleEnvironment.Stopping += RoleStopping;

                // Flag as running
                m_Running = true;

                // Start the service thread
                Thread serviceThread        = new Thread(RunService);

                serviceThread.Start();
            }
        }

        /// <summary>
        /// Callback from the role environment when it gets a message that 
        /// </summary>
        private void RoleStopping(object sender, RoleEnvironmentStoppingEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// Stops this refresh service from running
        /// </summary>
        public void Stop()
        {
            lock (m_Sync)
            {
                if (!m_Running)
                {
                    return;
                }

                // Request that the thread stop
                m_Stop = true;
                Monitor.PulseAll(m_Sync);
            }
        }

        /// <summary>
        /// Runs the update service
        /// </summary>
        private void RunService()
        {
            // Read service bus configuration
            string serviceBusConnectionString   = CloudConfigurationManager.GetSetting("Regard.ServiceBus.QueryUpdate.ConnectionString");
            string topic                        = CloudConfigurationManager.GetSetting("Regard.ServiceBus.QueryUpdate.EventTopic");
            string subscription                 = CloudConfigurationManager.GetSetting("Regard.ServiceBus.QueryUpdate.Subscription");

            // Create the data store we're going to use
            var dataStoreTask = DataStoreFactory.CreateDefaultDataStore();
            dataStoreTask.Wait();
            var dataStore = dataStoreTask.Result;

            if (string.IsNullOrEmpty(serviceBusConnectionString) || string.IsNullOrEmpty(topic) ||
                string.IsNullOrEmpty(subscription))
            {
                Trace.WriteLine("Query update service not running: settings are not available");
                RunNoService();
            }

            // Open the service bus
            var regardNamespace = NamespaceManager.CreateFromConnectionString(serviceBusConnectionString);

            // Create the topic and subscription
            if (!regardNamespace.TopicExists(topic))
            {
                regardNamespace.CreateTopic(topic);
            }
            if (!regardNamespace.SubscriptionExists(topic, subscription))
            {
                regardNamespace.CreateSubscription(topic, subscription);
            }

            // Create the client
            var serviceBus = SubscriptionClient.CreateFromConnectionString(serviceBusConnectionString, topic, subscription);

            // Number of messages to process at a time
            const int batchSize = 5;

            // Run until something tells us to stop
            try
            {
                Trace.WriteLine("Listening for request to update query results");

                while (!m_Stop)
                {
                    // Receive any messages on the service bus
                    var messages = serviceBus.ReceiveBatch(batchSize, TimeSpan.FromSeconds(30));

                    // Each message will generate a task: we'll ignore further requests until all of these complete
                    // (This limits the maximum query update load)
                    var updateTasks = new List<Task>();
                    var completedMessageLockTokens = new List<Guid>();
                    foreach (var receivedMessage in messages)
                    {
                        try
                        {
                            // Decode the message payload
                            JObject messagePayload;

                            // We'll mark this message as completed, even if it doesn't process OK
                            completedMessageLockTokens.Add(receivedMessage.LockToken);

                            using (var rawMessage = receivedMessage.GetBody<Stream>())
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    rawMessage.CopyTo(memoryStream);
                                    var messageBody = Encoding.UTF8.GetString(memoryStream.ToArray());
                                    messagePayload = JObject.Parse(messageBody);
                                }
                            }

                            // Process this message
                            updateTasks.Add(ProcessUpdateQuery(dataStore, messagePayload));
                        }
                        catch (Exception e)
                        {
                            // Log exceptions and continue processing messages
                            Trace.TraceError("Exception while processing update requests: {0}", e.Message);
                        }
                    }

                    // Mark the messages as completed
                    if (completedMessageLockTokens.Count > 0)
                    {
                        serviceBus.CompleteBatch(completedMessageLockTokens);
                    }

                    // Wait for the update tasks to complete before continuing
                    Task.WhenAll(updateTasks).Wait();
                }
            }
            finally
            {
                // Mark this service as no longer running
                m_Running = false;

            }
        }

        /// <summary>
        /// Processes an update query request
        /// </summary>
        private async Task ProcessUpdateQuery(IRegardDataStore dataStore, JObject messagePayload)
        {
            try
            {
                // The payload specifies an organization/product to update
                var organization    = messagePayload["Organization"].Value<string>();
                var product         = messagePayload["Product"].Value<string>();

                // Fetch this product
                var queryable       = await dataStore.Products.GetProduct(organization, product);

                // Update its queries
                if (queryable != null)
                {
                    await queryable.UpdateAllQueries();
                }
                else
                {
                    Trace.TraceWarning("Unable to process update for missing product: " + organization + "/" + product);                    
                }
            }
            catch (Exception e)
            {
                // Note any errors that might occur
                Trace.TraceError("Error while updating a query: {0}", e.Message);
            }
        }

        /// <summary>
        /// If we're not configured to run the refresh service, then just run a dummy thread that waits until it's time to stop
        /// </summary>
        private void RunNoService()
        {
            lock (m_Sync)
            {
                while (!m_Stop)
                {
                    Monitor.Wait(m_Sync);
                }
            }
        }
    }
}
