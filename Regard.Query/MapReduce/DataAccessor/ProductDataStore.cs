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
        private readonly IKeyValueStore m_RawProductStore;

        public ProductDataStore(IKeyValueStore rawProductStore)
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
            return await m_RawProductStore.GetValue(KeyForProduct(organization, product));
        }

        /// <summary>
        /// Updates the JSON object that defines the settings for a particular product
        /// </summary>
        public async Task SetSettingsObjectForProduct(string organization, string product, JObject productSettings)
        {
            await m_RawProductStore.SetValue(KeyForProduct(organization, product), productSettings);
        }

        /// <summary>
        /// Retrieves the data store object containing the data for a particular product
        /// </summary>
        public IndividualProductDataStore DataStoreForIndividualProduct(string organization, string product)
        {
            return new IndividualProductDataStore(m_RawProductStore.ChildStore(KeyForProduct(organization, product)));
        }
    }
}
