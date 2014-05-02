using System;
using System.Data.SqlClient;
using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Regard data store implemented using SQL server
    /// </summary>
    public class SqlDataStore : IRegardDataStore
    {
        public SqlDataStore(SqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            EventRecorder   = new SqlEventRecorder(connection);
            Products        = new SqlProductAdmin(connection);
        }

        public IEventRecorder EventRecorder { get; private set; }
        public IProductAdmin Products { get; private set; }
    }
}
