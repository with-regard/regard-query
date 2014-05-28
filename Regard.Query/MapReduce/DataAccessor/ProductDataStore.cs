using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce.DataAccessor
{
    /// <summary>
    /// Class that manages the data related to individual products
    /// </summary>
    class ProductDataStore
    {
        private readonly IProductStoreRetrieval m_RawProductStore;

        public ProductDataStore(IProductStoreRetrieval rawProductStore)
        {
            if (rawProductStore == null) throw new ArgumentNullException("rawProductStore");
            m_RawProductStore = rawProductStore;
        }

        public static JArray KeyForProduct(string organization, string product)
        {
            return JArray.FromObject(new[] { organization, product });
        }

        /// <summary>
        /// Retrieves the JSON object that defines the settings for a particular product
        /// </summary>
        public async Task<JObject> GetSettingsObjectForProduct(string organization, string product)
        {
            var productStore = await m_RawProductStore.GetStoreForProduct(organization, product);

            return await productStore.ChildStore(new JArray("settings")).GetValue(KeyForProduct(organization, product));
        }

        /// <summary>
        /// Updates the JSON object that defines the settings for a particular product
        /// </summary>
        public async Task SetSettingsObjectForProduct(string organization, string product, JObject productSettings)
        {
            var productStore = await m_RawProductStore.GetStoreForProduct(organization, product);
            await productStore.ChildStore(new JArray("settings")).SetValue(KeyForProduct(organization, product), productSettings);
        }

        /// <summary>
        /// Retrieves the data store object containing the data for a particular product
        /// </summary>
        public async Task<IndividualProductDataStore> DataStoreForIndividualProduct(string organization, string product)
        {
            var productStore = await m_RawProductStore.GetStoreForProduct(organization, product);
            return new IndividualProductDataStore(productStore.ChildStore(new JArray("data")));
        }
    }
}
