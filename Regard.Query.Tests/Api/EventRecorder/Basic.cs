using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Regard.Query.Tests.MapReduce;

namespace Regard.Query.Tests.Api.EventRecorder
{
    [TestFixture]
    class Basic
    {
        [Test]
        public void CanRecordThe12BasicDocuments()
        {
            Task.Run(async () =>
            {
                var store = await SetupTestProject.CreateEventRecorderTestProject();

                // Use the API form that generates a session ID
                var sessionId = await store.EventRecorder.StartSession("WithRegard", "Test", WellKnownUserIdentifier.TestUser, Guid.Empty);
                foreach (var evt in TestDataGenerator.Generate12BasicDocuments())
                {
                    await store.EventRecorder.RecordEvent(sessionId, "WithRegard", "Test", evt);
                }
            }).Wait();
        }

        [Test]
        public void AutoGenerateSessionIdIfNotExplicitlySpecified()
        {
            Task.Run(async () =>
            {
                var store = await SetupTestProject.CreateEventRecorderTestProject();

                // Specifying Guid.Empty as the session ID tells the event recorder to generate a session ID by itself
                var sessionId = await store.EventRecorder.StartSession("WithRegard", "Test", WellKnownUserIdentifier.TestUser, Guid.Empty);
                Assert.AreNotEqual(Guid.Empty, sessionId);
            }).Wait();
        }

        [Test]
        public void UseExplicitSessionIdIfSpecified()
        {
            Task.Run(async () =>
            {
                var store = await SetupTestProject.CreateEventRecorderTestProject();

                // We want to allow the endpoint or the client to generate session IDs as well
                // This is currently the way the live system generates session IDs.
                // It might not be a good idea for this to happen on the client; it seems allowing the client to choose a session ID opens us up to collisions or deliberately bad behaviour
                // We might fix this by generating IDs in the endpoint, in whcih case this is still how the event recorder will work.
                var desiredSessionId = new Guid("5FF8F956-C290-4855-AD8E-28EB79AEFB64");
                var sessionId = await store.EventRecorder.StartSession("WithRegard", "Test", WellKnownUserIdentifier.TestUser, desiredSessionId);
                Assert.AreEqual(desiredSessionId, sessionId);
            }).Wait();
        }
    }
}
