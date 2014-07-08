using System.Threading.Tasks;
using Regard.Query.Api;

namespace Regard.Query.WebAPI
{
    internal class ControllerDataStoreFactory
    {
        /// <summary>
        /// Synchronisation object (protects m_CreatingDataStore in particular)
        /// </summary>
        private readonly object m_Sync = new object();

        /// <summary>
        /// The data store that this will make available
        /// </summary>
        private IRegardDataStore m_DataStore;

        /// <summary>
        /// Task that executes if we're trying to create the default data store
        /// </summary>
        private Task m_CreatingDataStore;


        /// <summary>
        /// Ensures that the m_DataStore parameter is populated
        /// </summary>
        internal async Task<IRegardDataStore> EnsureDataStore()
        {
            Task alreadyRunning = null;

            lock (m_Sync)
            {
                // Nothing to do if the data store is already created
                if (m_DataStore != null)
                {
                    return m_DataStore;
                }

                // Check if something else is already retrieving the data store
                alreadyRunning = m_CreatingDataStore;

                // If nothing is running, then this is the first thing to try to create a data store, so create a new one
                if (alreadyRunning == null)
                {
                    m_CreatingDataStore = alreadyRunning = ActuallyCreateDataStore();
                }
            }

            // Wait for the data store to finish creating
            try
            {
                await alreadyRunning;

                return m_DataStore;
            }
            finally
            {
                // The data store should exist and 
                lock (m_Sync)
                {
                    m_CreatingDataStore = null;
                }
            }
        }

        /// <summary>
        /// Task that actually creates a data store
        /// </summary>
        private async Task ActuallyCreateDataStore()
        {
            // Generate a data store
            var newDataStore = await DataStoreFactory.CreateDefaultDataStore();

            // Store it in the data store variable
            lock (m_Sync)
            {
                m_DataStore = newDataStore;
            }
        }
    }
}