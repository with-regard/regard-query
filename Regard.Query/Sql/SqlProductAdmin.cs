using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Regard.Query.Api;

namespace Regard.Query.Sql
{
    /// <summary>
    /// Sql Server product administration interface
    /// </summary>
    class SqlProductAdmin : IProductAdmin
    {
        /// <summary>
        /// The database connection where the product data is stored
        /// </summary>
        private readonly SqlConnection m_Connection;

        private const string c_CreateProduct = "INSERT INTO [Product] ([Name], [Organization]) VALUES (@productName, @productOrganization)";

        private const string c_GetProductId = "SELECT [Id] FROM [Product] WHERE [Name] = @productName AND [Organization] = @productOrganization";

        public SqlProductAdmin(SqlConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            m_Connection = connection;
        }

        /// <summary>
        /// Creates a new product that can have events logged against it
        /// </summary>
        public async Task CreateProduct(string organization, string product)
        {
            // using (var transaction = m_Connection.BeginTransaction())
            {
                // Insert a new product into the database
                var createANewProduct = new SqlCommand(c_CreateProduct, m_Connection, null);

                createANewProduct.Parameters.AddWithValue("@productName", product);
                createANewProduct.Parameters.AddWithValue("@productOrganization", organization);

                await createANewProduct.ExecuteNonQueryAsync();
                //transaction.Commit();
            }
        }

        /// <summary>
        /// For a product that exists, retrieves the interface for interacting with its queries
        /// </summary>
        public async Task<IQueryableProduct> GetProduct(string organization, string product)
        {
            // Try to retrieve the ID for this product
            var getTheProductId = new SqlCommand(c_GetProductId, m_Connection);

            getTheProductId.Parameters.AddWithValue("@productName", product);
            getTheProductId.Parameters.AddWithValue("@productOrganization", organization);

            long productId;
            using (var reader = await getTheProductId.ExecuteReaderAsync())
            {
                // Result is null if there is no matching product
                if (!await reader.ReadAsync())
                {
                    return null;
                }

                // Get the product ID
                productId = reader.GetFieldValue<long>(0);
            }

            // Create the product interface
            return new SqlQueryableProduct(m_Connection, productId);
        }
    }
}
