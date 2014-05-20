using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Product admin interface for the map/reduce query system
    /// </summary>
    internal class ProductAdmin : IProductAdmin
    {
        private readonly IKeyValueStore m_DataStore;
        private readonly string m_NodeName;

        public ProductAdmin(IKeyValueStore dataStore, string nodeName)
        {
            if (dataStore == null) throw new ArgumentNullException("dataStore");
            
            // We store the products in a separate child store, in case there's a clash somewhere along the lines
            // Product data stores aren't isolated per node, so if two nodes perform operations on the same product, the results are subject to a race condition
            m_DataStore = dataStore.ChildStore(JArray.FromObject(new[] { "products" }));
            m_NodeName = nodeName;
        }

        /// <summary>
        /// Creates the data store key for a particular product 
        /// </summary>
        internal static JArray KeyForProduct(string organization, string product)
        {
            return JArray.FromObject(new[] {organization, product});
        }

        /// <summary>
        /// Retrieves the JObject representing the configuration for a particular product (null if it does not exist)
        /// </summary>
        private async Task<JObject> ObjectForProduct(string organization, string product)
        {
            return await m_DataStore.GetValue(KeyForProduct(organization, product));
        }

        /// <summary>
        /// Updates the JObject for a particular product
        /// </summary>
        private async Task SetObjectForProduct(string organization, string product, JObject value)
        {
            await m_DataStore.SetValue(KeyForProduct(organization, product), value);
        }

        /// <summary>
        /// Creates a new product that can have events logged against it
        /// </summary>
        public async Task CreateProduct(string organization, string product)
        {
            // Try to retrieve an existing product
            var existingProduct = await ObjectForProduct(organization, product);

            // Nothing to do if this product is already created
            if (existingProduct != null)
            {
                return;
            }

            // Create a new product
            var productData = new JObject();
            await SetObjectForProduct(organization, product, productData);
        }

        /// <summary>
        /// For a product that exists, retrieves the interface for interacting with its queries. Returns null if the product doesn't exist
        /// </summary>
        public async Task<IQueryableProduct> GetProduct(string organization, string product)
        {
            // Try to retrieve an existing product
            var existingProduct = await ObjectForProduct(organization, product);

            // Can't retrieve a product that doesn't exist
            if (existingProduct == null)
            {
                return null;
            }

            // Create a new queryable store using a child store represented by the organization/product
            return new QueryableProduct(m_DataStore.ChildStore(KeyForProduct(organization, product)), m_NodeName);
        }
    }
}
