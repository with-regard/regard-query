using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;
using Regard.Query.MapReduce.DataAccessor;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Product admin interface for the map/reduce query system
    /// </summary>
    internal class ProductAdmin : IProductAdmin
    {
        private readonly object m_Sync = new object();
        private readonly ProductDataStore m_DataStore;
        private readonly string m_NodeName;

        /// <summary>
        /// Cached products that we've already created objects for
        /// </summary>
        private readonly Dictionary<Tuple<string, string>, QueryableProduct> m_KnownProducts = new Dictionary<Tuple<string, string>, QueryableProduct>(); 

        public ProductAdmin(RootDataStore dataStore, string nodeName)
        {
            if (dataStore == null) throw new ArgumentNullException("dataStore");
            
            // We store the products in a separate child store, in case there's a clash somewhere along the lines
            // Product data stores aren't isolated per node, so if two nodes perform operations on the same product, the results are subject to a race condition
            m_DataStore = dataStore.ProductDataStore;
            m_NodeName = nodeName;
        }

        /// <summary>
        /// Creates a new product that can have events logged against it
        /// </summary>
        public async Task CreateProduct(string organization, string product)
        {
            // Try to retrieve an existing product
            var existingProduct = await m_DataStore.GetSettingsObjectForProduct(organization, product);

            // Nothing to do if this product is already created
            if (existingProduct != null)
            {
                return;
            }

            // Create a new product
            var productData = new JObject();
            await m_DataStore.SetSettingsObjectForProduct(organization, product, productData);
        }

        /// <summary>
        /// For a product that exists, retrieves the interface for interacting with its queries. Returns null if the product doesn't exist
        /// </summary>
        public async Task<IQueryableProduct> GetProduct(string organization, string product)
        {
            Tuple<string, string> name = new Tuple<string, string>(organization, product);
            QueryableProduct result;

            lock (m_Sync)
            {
                // Use an already known product if there is one
                if (m_KnownProducts.TryGetValue(name, out result))
                {
                    return result;
                }
            }

            // Try to retrieve an existing product
            var existingProduct = await m_DataStore.GetSettingsObjectForProduct(organization, product);

            // Can't retrieve a product that doesn't exist
            if (existingProduct == null)
            {
                return null;
            }

            // Create a new queryable store using a child store represented by the organization/product
            var newProduct = new QueryableProduct(await m_DataStore.DataStoreForIndividualProduct(organization, product), m_NodeName);

            lock (m_Sync)
            {
                // Just use an existing product if another thread created one
                if (m_KnownProducts.TryGetValue(name, out result))
                {
                    return result;
                }

                // Otherwise, remember this product and return it
                m_KnownProducts[name] = newProduct;
                return newProduct;
            }
        }
    }
}
