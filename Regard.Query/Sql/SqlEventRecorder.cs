using System;
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
                // Fetch the short ID for this user
                var getShortIdCommand = new SqlCommand(c_GetShortUserId, m_Connection);

                getShortIdCommand.Parameters.AddWithValue("@fullUserId", userId);

                // Fetch the short user ID
                long shortUserId;
                using (SqlDataReader shortIdReader = await getShortIdCommand.ExecuteReaderAsync())
                {
                    if (!await shortIdReader.ReadAsync())
                    {
                        // 0 records in result: user is not in the database
                        return Guid.Empty;
                    }

                    shortUserId = (long) shortIdReader["ShortUserId"];
                }

                // ... and the short product ID
                // TODO: these commands could be combined into one
                var getShortProductIdCommand = new SqlCommand(c_GetShortProductId, m_Connection);

                getShortProductIdCommand.Parameters.AddWithValue("@organization", organization);
                getShortProductIdCommand.Parameters.AddWithValue("@product", product);

                long shortProductId;
                using (SqlDataReader shortProductIdReader = await getShortProductIdCommand.ExecuteReaderAsync())
                {
                    if (!await shortProductIdReader.ReadAsync())
                    {
                        // 0 records in results: product is not in the database
                        return Guid.Empty;
                    }

                    shortProductId = (long) shortProductIdReader["Id"];
                }

                // Create an insertion command
                var insertionCommand = new SqlCommand(c_InsertNewSession, m_Connection);

                insertionCommand.Parameters.AddWithValue("@fullSessionId", newSessionId);
                insertionCommand.Parameters.AddWithValue("@shortUserId", shortUserId);
                insertionCommand.Parameters.AddWithValue("@productId", shortProductId);

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
        /// <param name="organization">The name of the organisation that generated the event</param>
        /// <param name="product">The name of the product that generated the event</param>
        /// <param name="sessionId">The ID of the session (as returned by StartSession)</param>
        /// <param name="data">JSON data indicating the properties for this event</param>
        public async Task RecordEvent(string organization, string product, Guid sessionId, JObject data)
        {
            throw new NotImplementedException();
        }
    }
}
