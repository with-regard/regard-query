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
        /// List of recently mapped objects that are waiting to be reduced
        /// </summary>
        private readonly Queue<Tuple<JArray, JObject>> m_WaitingToReduce = new Queue<Tuple<JArray, JObject>>();

        /// <summary>
        /// List of recently mapped objects that are waiting to be unreduced (ie, deleted)
        /// </summary>
        private readonly Queue<Tuple<JArray, JObject>> m_WaitingToUnreduce = new Queue<Tuple<JArray, JObject>>();

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
        private readonly DataIngestor m_ChainIngestor;

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
        /// Causes a record to be mapped and placed on a queue to await latter processing
        /// </summary>
        private void MapToQueue(JObject record, Queue<Tuple<JArray, JObject>> targetQueue)
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

            lock (m_Sync)
            {
                // Store the emitted objects
                foreach (var obj in target.Emitted)
                {
                    targetQueue.Enqueue(obj);
                }
            }
        }

        /// <summary>
        /// Ingests a record (adding it to the results from this object)
        /// </summary>
        public void Ingest(JObject record)
        {
            // Send to the mapped objects queue
            MapToQueue(record, m_WaitingToReduce);

            // Check if we have enough objects to perform a commit
            int queueSize;
            lock (m_Sync)
            {
                queueSize = m_WaitingToReduce.Count + m_WaitingToUnreduce.Count;
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
        /// Uningests a record (removing it from the results for this object)
        /// </summary>
        public void Uningest(JObject record)
        {
            // Send to the mapped objects queue
            MapToQueue(record, m_WaitingToUnreduce);

            // Check if we have enough objects to perform a commit
            int queueSize;
            lock (m_Sync)
            {
                queueSize = m_WaitingToReduce.Count + m_WaitingToUnreduce.Count;
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
        /// Commits any unreductions that are waiting
        /// </summary>
        private async Task CommitUnreduce()
        {
            // Perform any unreductions that are necessary
            var waitingToUnreduce   = new Dictionary<string, List<JObject>>();
            var keyForKey           = new Dictionary<string, JArray>();

            for (;;)
            {
                // Get the result of the map operation on the data to unreduce
                Tuple<JArray, JObject> unreduce;
                lock (m_Sync)
                {
                    if (m_WaitingToUnreduce.Count <= 0)
                    {
                        break;
                    }
                    unreduce = m_WaitingToUnreduce.Dequeue();
                }

                // Store in the waitingToUnReduce dictionary
                string keyString = KeySerializer.KeyToString(unreduce.Item1);

                List<JObject> itemsForKey;
                if (!waitingToUnreduce.TryGetValue(keyString, out itemsForKey))
                {
                    waitingToUnreduce[keyString] = itemsForKey = new List<JObject>();
                    keyForKey[keyString] = unreduce.Item1;
                }

                itemsForKey.Add(unreduce.Item2);
            }

            // Perform the unreductions
            List<Task> storeValues = new List<Task>();
            foreach (var keyPair in waitingToUnreduce)
            {
                // Fetch the existing object from the data store
                var key             = keyForKey[keyPair.Key];
                var previousValue   = await m_Store.GetValue(key);

                if (previousValue == null)
                {
                    // We can't really unreduce a value with no previous value (it effectively means that we're trying to delete a record that never existed)
                    // Alternative possible behaviour: use an empty object here
                    continue;
                }

                // Copying the value is slower but safer
                var newValue = (JObject) previousValue.DeepClone();

                // Perform the unreduction
                var unreduced = m_MapReduce.Unreduce(key, newValue, keyPair.Value);

                // Store the resulting value
                storeValues.Add(m_Store.SetValue(key, unreduced));
            }

            await Task.WhenAll(storeValues);
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

                // Perform any unreductions that are necessary
                await CommitUnreduce();

                // Map the objects in the queue
                var waitingToReduce = new Dictionary<string, List<JObject>>();
                var keyForKey       = new Dictionary<string, JArray>();

                for (;;)                                    // .. because a while with a lock in the condition clause isn't possible
                {
                    Tuple<JArray, JObject> mapped;
                    lock (m_Sync)
                    {
                        // Stop if there are no more objects to process
                        if (m_WaitingToReduce.Count <= 0)
                        {
                            break;
                        }

                        // Get the next object in the queue
                        mapped = m_WaitingToReduce.Dequeue();
                    }

                    // Convert the string to something we can use in a dictionary
                    string keyString = KeySerializer.KeyToString(mapped.Item1);

                    // Cluster the items with the same key together
                    List<JObject> itemsForKey;
                    if (!waitingToReduce.TryGetValue(keyString, out itemsForKey))
                    {
                        itemsForKey = waitingToReduce[keyString] = new List<JObject>();
                        keyForKey[keyString] = mapped.Item1;
                    }

                    itemsForKey.Add(mapped.Item2);
                }

                // Perform a reduction on these objects
                var reducedObjects = new Dictionary<string, JObject>();

                foreach (var reducePair in waitingToReduce)
                {
                    // Reduce these items
                    var reduced = m_MapReduce.Reduce(keyForKey[reducePair.Key], reducePair.Value);
                    if (reduced != null)
                    {
                        reducedObjects[reducePair.Key] = reduced;
                    }
                }
                waitingToReduce.Clear();

                // Finally, re-reduce against the items already in the key/value store
                List<Task> storeValues = new List<Task>();
                foreach (var reducePair in reducedObjects)
                {
                    // Fetch the existing object from the data store
                    var key             = keyForKey[reducePair.Key];
                    var previousValue   = await m_Store.GetValue(key);

                    // Nothing to do if it doesn't already exist
                    if (previousValue == null)
                    {
                        storeValues.Add(m_Store.SetValue(key, reducePair.Value));

                        if (m_ChainIngestor != null)
                        {
                            // Send to the chain ingestor
                            m_ChainIngestor.Ingest(KeySerializer.CopyAndAddKey(reducePair.Value, key));
                        }
                    }
                    else
                    {
                        // Re-reduce if it does already exist
                        var rereduced = m_MapReduce.Rereduce(key, new[] {previousValue, reducePair.Value});

                        storeValues.Add(m_Store.SetValue(key, rereduced));

                        if (m_ChainIngestor != null)
                        {
                            // Send to the chain ingestor. We're effectively replacing the old value, so uningest that.
                            m_ChainIngestor.Uningest(KeySerializer.CopyAndAddKey(previousValue, key));

                            if (rereduced != null)
                            {
                                m_ChainIngestor.Ingest(KeySerializer.CopyAndAddKey(rereduced, key));
                            }
                        }
                    }
                }

                // Ensure that all the data is committed to the data store
                await Task.WhenAll(storeValues);
                var waitingTasks = new List<Task>();
                waitingTasks.Add(m_Store.Commit());

                // Also wait for any chain to complete
                if (m_ChainIngestor != null)
                {
                    waitingTasks.Add(m_ChainIngestor.Commit());
                }

                // Wait for everything to finish
                await Task.WhenAll(waitingTasks);
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
