using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Regard.Query.Api;

namespace Regard.Query.MapReduce
{
    /// <summary>
    /// Class that applies a map/reduce algorithm to a series of records and writes them to a key/value store
    /// </summary>
    public class DataIngestor
    {
        /// <summary>
        /// Synchronisation object
        /// </summary>
        private readonly object m_Sync = new object();

        /// <summary>
        /// The map/reduce algorithm that is used for the data store this is referring to
        /// </summary>
        private readonly IMapReduce m_MapReduce;

        /// <summary>
        /// The store where the reduced results are written
        /// </summary>
        private readonly IKeyValueStore m_Store;

        /// <summary>
        /// List of recently mapped objects
        /// </summary>
        private readonly Queue<Tuple<JArray, JObject>> m_MappedObjects = new Queue<Tuple<JArray, JObject>>();

        /// <summary>
        /// Maximum number of objects to keep in the queue
        /// </summary>
        private const int c_MaxQueueSize = 200;

        /// <summary>
        /// The map target being used for the current thread
        /// </summary>
        [ThreadStatic] private static IngestorMapTarget s_MapTarget;

        /// <summary>
        /// Task set to non-null when at least one thread is committing
        /// </summary>
        /// <remarks>
        /// The parameter is just there because TaskCompletionSource isn't smart enough to create void tasks
        /// </remarks>
        private Task m_Committing;

        /// <summary>
        /// The data ingestor that handles any chained queries
        /// </summary>
        private DataIngestor m_ChainIngestor;

        public DataIngestor(IMapReduce mapReduce, IKeyValueStore store)
        {
            if (mapReduce == null)  throw new ArgumentNullException("mapReduce");
            if (store == null)      throw new ArgumentNullException("store");

            m_MapReduce = mapReduce;
            m_Store     = store;

            // If there's a chain, the input store is the output of the map/reduce function as a whole (ie, the end of the chain)
            if (m_MapReduce.Chain != null)
            {
                // Provide another data ingestor to reduce the chain results
                m_ChainIngestor = new DataIngestor(m_MapReduce.Chain, m_Store);

                // The results for this map/reduce are intermediate: we store them in a child store (called 'chain')
                m_Store = m_Store.ChildStore(new JArray("chain"));
            }
        }

        /// <summary>
        /// Causes a record to be ingested by this class
        /// </summary>
        public void Ingest(JObject record)
        {
            // Fetch the target for the current thread (we re-use it to reduce stress on the garbage collector)
            var target = s_MapTarget;
            if (target == null)
            {
                target = s_MapTarget = new IngestorMapTarget();
            }

            // Reset the target and perform the map operation
            target.Reset();
            m_MapReduce.Map(target, record);

            int queueSize;
            lock (m_Sync)
            {
                // Store the emitted objects
                foreach (var obj in target.Emitted)
                {
                    m_MappedObjects.Enqueue(obj);
                }

                queueSize = m_MappedObjects.Count;
            }

            // Commit the objects if the queue gets too large
            if (queueSize >= c_MaxQueueSize)
            {
                // OK to ignore the task (we can await m_Committing if we care)
#pragma warning disable 4014
                Commit();
#pragma warning restore 4014
            }
        }

        /// <summary>
        /// Commits any records that have not been processed
        /// </summary>
        public async Task Commit()
        {
            // We use a task to guarantee that only one commit operation can operate at a time
            // (We don't use ContinueWith here as it's not clear how the first task would start or the last task would clean up the value, at least not without code at least as complex as this)
            TaskCompletionSource<int> ourTask = new TaskCompletionSource<int>();

            try
            {
                // Acquire the m_Committing task
                for (;;)                                    // .. because we have a complex exit condition
                {
                    // If nothing if comitting then we become the 'main' commit source
                    Task currentCommit;
                    lock (m_Sync)
                    {
                        currentCommit = m_Committing;
                        if (currentCommit == null)
                        {
                            // When there's no task running, we become the foreground task
                            m_Committing = ourTask.Task;
                            break;
                        }
                    }

                    // Wait for the task that is/was committing to complete
                    await currentCommit;
                }

                // Reduce the items in the queue
                for (;;)                                    // .. because a while with a lock in the condition clause isn't possible
                {
                    Tuple<JArray, JObject> mapped;
                    lock (m_Sync)
                    {
                        // Stop if there are no more objects to process
                        if (m_MappedObjects.Count <= 0)
                        {
                            break;
                        }

                        // Get the next object in the queue
                        mapped = m_MappedObjects.Dequeue();
                    }
                }
            }
            finally
            {
                // Mark our task as completed and unset m_Committing (which should always be our task)
                lock (m_Sync)
                {
                    ourTask.SetResult(0);
                    m_Committing = null;
                }
            }
        }
    }
}
