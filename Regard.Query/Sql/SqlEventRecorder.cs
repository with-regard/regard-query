﻿using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Records events in a SQL server database
    /// </summary>
    public class SqlEventRecorder : IEventRecorder
    {
        /// <summary>
        /// The connection to the data that this will record events to
        /// </summary>
        private readonly SqlConnection m_Connection;

        private const string c_GetShortUserId       = "SELECT [ShortUserId] FROM [OptInUser] WHERE FullUserId = @fullUserId";
        private const string c_InsertNewSession     = "INSERT INTO [Session] ([FullSessionId], [ShortUserId], [ProductId]) VALUES (@fullSessionId, @shortUserId, @productId)";
        private const string c_GetShortProductId    = "SELECT [Id] FROM [Product] WHERE [Name] = @product AND [Organization] = @organization";
        private const string c_CreateEvent          = "INSERT INTO [Event] (ShortSessionId) SELECT ShortSessionId FROM [Session] WHERE [FullSessionId] = @fullSessionId; SELECT CAST(SCOPE_IDENTITY() AS bigint)";

        private const string c_AddProperty          = "IF NOT EXISTS (SELECT [Id] FROM [EventProperty] WHERE [Name] = @propertyName)\n"
                                                    + "  INSERT INTO [EventProperty] ([Name]) VALUES (@propertyName);\n"

                                                    + "INSERT INTO [EventPropertyValues] ([EventId], [PropertyId], [Value], [NumericValue]) "
                                                    + "SELECT @eventId, [eventProp].[Id], @propertyStringValue, @propertyNumericValue "
                                                    + "FROM [EventProperty] AS [eventProp] "
                                                    + "WHERE [eventProp].[Name] = @propertyName;";

        public SqlEventRecorder(SqlConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            m_Connection = connection;
        }

        /// <summary>
        /// Creates a session ID
        /// </summary>
        private Guid GenerateSessionId()
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// Returns the short user ID given a user GUID, or null if the user is opted-out of collection (or has never opted-in)
        /// </summary>
        private async Task<long?> GetShortUserId(Guid userId, SqlTransaction transaction)
        {
            var getShortIdCommand = new SqlCommand(c_GetShortUserId, m_Connection, transaction);

            getShortIdCommand.Parameters.AddWithValue("@fullUserId", userId);

            // Fetch the short user ID
            long shortUserId;
            using (SqlDataReader shortIdReader = await getShortIdCommand.ExecuteReaderAsync())
            {
                if (!await shortIdReader.ReadAsync())
                {
                    // 0 records in result: user is not in the database
                    return null;
                }

                shortUserId = (long)shortIdReader["ShortUserId"];
            }

            return shortUserId;
        }

        /// <summary>
        /// Given the name of an organization and a product, returns the short product ID that refers to it, or null if the product is not in the database
        /// </summary>
        private async Task<long?> GetShortProductId(string organization, string product, SqlTransaction transaction)
        {
            var getShortProductIdCommand = new SqlCommand(c_GetShortProductId, m_Connection, transaction);

            getShortProductIdCommand.Parameters.AddWithValue("@organization", organization);
            getShortProductIdCommand.Parameters.AddWithValue("@product", product);

            long shortProductId;
            using (SqlDataReader shortProductIdReader = await getShortProductIdCommand.ExecuteReaderAsync())
            {
                if (!await shortProductIdReader.ReadAsync())
                {
                    // 0 records in results: product is not in the database
                    return null;
                }

                shortProductId = (long)shortProductIdReader["Id"];
            }

            return shortProductId;
        }

        /// <summary>
        /// Indicates that a new session has begun
        /// </summary>
        /// <param name="organization">The name of the organization that the session is for</param>
        /// <param name="product">The name of the product that the session is for</param>
        /// <param name="userId">A GUID that identifies the user that this session is for</param>
        /// <returns>A GUID that identifies this session, or Guid.Empty if the session can't be started (because the user is opted-out, for example)</returns>
        public async Task<Guid> StartSession(string organization, string product, Guid userId)
        {
            // Generate a session ID
            Guid newSessionId = GenerateSessionId();

            using (var sessionTransaction = m_Connection.BeginTransaction())
            { 
                // TODO: might be better to do this with a stored procedure? Would still need to detect the error cases where the product or user doesn't exist

                // Fetch the short ID for this user
                var shortUserId = await GetShortUserId(userId, sessionTransaction);
                if (!shortUserId.HasValue)
                {
                    // TODO: way to distinguish the error case 'user doesn't exist' from the case 'product doesn't exist'. Currently it's going to be a bit mysterious.
                    return Guid.Empty;
                }

                // ... and the short product ID
                // TODO: these commands could be combined into one
                var shortProductId = await GetShortProductId(organization, product, sessionTransaction);
                if (!shortProductId.HasValue)
                {
                    return Guid.Empty;
                }

                // Create an insertion command
                var insertionCommand = new SqlCommand(c_InsertNewSession, m_Connection, sessionTransaction);

                insertionCommand.Parameters.AddWithValue("@fullSessionId", newSessionId);
                insertionCommand.Parameters.AddWithValue("@shortUserId", shortUserId.Value);
                insertionCommand.Parameters.AddWithValue("@productId", shortProductId.Value);

                // Create the session
                await insertionCommand.ExecuteNonQueryAsync();

                // Done
                sessionTransaction.Commit();

                return newSessionId;
            }
        }

        /// <summary>
        /// Schedules a single event to be recorded by this object
        /// </summary>
        /// <param name="sessionId">The ID of the session (as returned by StartSession)</param>
        /// <param name="data">JSON data indicating the properties for this event</param>
        public async Task RecordEvent(Guid sessionId, JObject data)
        {
            using (var transaction = m_Connection.BeginTransaction())
            {
                // Create the event
                // TODO: could cache the short session ID to improve performance?
                // TODO: make something sensible happen if the session ID is invalid (likely scenario: user opts-out while a session is in progress)
                var createEventCmd = new SqlCommand(c_CreateEvent, m_Connection, transaction);

                createEventCmd.Parameters.AddWithValue("@fullSessionId", sessionId);

                // The creation command should return the new event ID
                long eventId = (long) await createEventCmd.ExecuteScalarAsync();

                // Store the properties
                foreach (var property in data.Properties())
                {
                    var addPropertyCmd = new SqlCommand(c_AddProperty, m_Connection, transaction);

                    // Only store property types we understand
                    var propertyType = property.Value.Type;

                    addPropertyCmd.Parameters.AddWithValue("@eventId", eventId);
                    addPropertyCmd.Parameters.AddWithValue("@propertyName", property.Name);

                    switch (propertyType)
                    {
                        case JTokenType.Boolean:
                        {
                            bool val = property.Value.Value<bool>();
                            addPropertyCmd.Parameters.AddWithValue("@propertyStringValue", val.ToString());
                            addPropertyCmd.Parameters.AddWithValue("@propertyNumericValue", val ? 1.0 : 0.0);
                            break;
                        }

                        case JTokenType.Float:
                        {
                            double val = property.Value.Value<double>();
                            addPropertyCmd.Parameters.AddWithValue("@propertyStringValue", val.ToString());
                            addPropertyCmd.Parameters.AddWithValue("@propertyNumericValue", val);
                            break;
                        }

                        case JTokenType.Integer:
                        {
                            int val = property.Value.Value<int>();
                            addPropertyCmd.Parameters.AddWithValue("@propertyStringValue", val.ToString());
                            addPropertyCmd.Parameters.AddWithValue("@propertyNumericValue", (double) val);
                            break;
                        }

                        case JTokenType.String:
                        {
                            string val = property.Value.Value<string>();
                            addPropertyCmd.Parameters.AddWithValue("@propertyStringValue", val.ToString());
                            addPropertyCmd.Parameters.AddWithValue("@propertyNumericValue", DBNull.Value);
                            break;
                        }

                        
                        case JTokenType.TimeSpan:
                        case JTokenType.Date:

                        default:
                            // Do not store this property value
                            continue;
                    }

                    // Add this property
                    await addPropertyCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
        }
    }
}