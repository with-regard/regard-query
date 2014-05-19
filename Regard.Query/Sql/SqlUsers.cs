using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Class for performing actions on users for SQL server
    /// </summary>
    public class SqlUsers : IUserAdmin
    {
        /// <summary>
        /// The database connection to use
        /// </summary>
        private readonly SqlConnection m_Connection;

        /// <summary>
        /// The product ID that we are administering the user list for
        /// </summary>
        private readonly long m_ProductId;

        /// <summary>
        /// State name representing 'opted in to share data with the developer'
        /// </summary>
        private const string c_StateOptIn = "ShareWithDeveloper";

        /// <summary>
        /// State name representing 'only recording data for the user'
        /// </summary>
        private const string c_StateOptOut = "ShareWithDeveloper";

        private const string c_UpdateUser 
                                    = "DECLARE @stateId int;\n"
                                    + "SET @stateId = (SELECT [StateID] FROM [OptInState] WHERE name = @newStateName);\n"

                                    + "IF EXISTS (SELECT [ShortUserId] FROM [OptInUser] WHERE [FullUserId] = @userId AND [ProductId] = @productId)\n"
                                    + "BEGIN\n"
                                    + "  UPDATE [OptInUser] SET OptInStateId = @stateId WHERE [FullUserId] = @userId;\n"
                                    + "END\n"
                                    + "ELSE\n"
                                    + "BEGIN\n"
                                    + "  INSERT INTO [OptInUser] ([FullUserId], [ProductId], [OptInStateId]) VALUES (@userId, @productId, @stateId);\n"
                                    + "END\n"
                                    ;

        public SqlUsers(SqlConnection connection, long productId)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            m_Connection = connection;
            m_ProductId = productId;
        }

        /// <summary>
        /// Marks a specific user ID as being opted in to data collection for a specific product
        /// </summary>
        public async Task OptIn(Guid userId)
        {
            //using (var transaction = m_Connection.BeginTransaction())
            {
                var updateUser = new SqlCommand(c_UpdateUser, m_Connection, null);

                updateUser.Parameters.AddWithValue("@userId", userId);
                updateUser.Parameters.AddWithValue("@newStateName", c_StateOptIn);
                updateUser.Parameters.AddWithValue("@productId", m_ProductId);

                await updateUser.ExecuteNonQueryAsync();

                //transaction.Commit();
            }
        }

        /// <summary>
        /// Marks a specific user ID as being opted out from data collection for a specific product
        /// </summary>
        /// <remarks>
        /// This only opts out for future data collection. Any existing data will be retained.
        /// </remarks>
        public async Task OptOut(Guid userId)
        {
            //using (var transaction = m_Connection.BeginTransaction())
            {
                // TODO: this will actually start collecting data for the user if they weren't in the database to begin with!
                // (Though it won't include it in any results)

                var updateUser = new SqlCommand(c_UpdateUser, m_Connection, null);

                updateUser.Parameters.AddWithValue("@userId", userId);
                updateUser.Parameters.AddWithValue("@newStateName", c_StateOptOut);
                updateUser.Parameters.AddWithValue("@productId", m_ProductId);

                await updateUser.ExecuteNonQueryAsync();

                // transaction.Commit();
            }
        }
    }
}
