using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace VRBoxingGame.Performance
{
    /// <summary>
    /// Advanced Object Pool Manager optimized for Unity 6 with Job System integration
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        public int initialPoolSize = 50;
        public int maxPoolSize = 200;
        public bool enableDynamicExpansion = true;
        public bool enableJobSystemOptimization = true;
        
        [Header("Prefab Pools")]
        public PooledObjectConfig[] poolConfigs;
        
        // Pool storage
        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, Transform> poolParents = new Dictionary<string, Transform>();
        private Dictionary<GameObject, string> activeObjects = new Dictionary<GameObject, string>();
        
        // Performance tracking
        private int totalObjectsCreated = 0;
        private int totalObjectsReused = 0;
        private float poolEfficiency = 0f;
        
        // Job System data
        private NativeArray<float> positionData;
        private NativeArray<float> rotationData;
        private JobHandle currentJobHandle;
        
        // Singleton
        public static ObjectPoolManager Instance { get; private set; }
        
        // Properties
        public float PoolEfficiency => poolEfficiency;
        public int TotalObjectsCreated => totalObjectsCreated;
        public int TotalObjectsReused => totalObjectsReused;
        
        [System.Serializable]
        public class PooledObjectConfig
        {
            public string poolName;
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 50;
            public bool preWarm = true;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializePools()
        {
            // Create pool parent objects for organization
            Transform poolContainer = new GameObject("Object Pools").transform;
            poolContainer.SetParent(transform);
            
            foreach (var config in poolConfigs)
            {
                CreatePool(config, poolContainer);
            }
            
            // Initialize Job System arrays if enabled
            if (enableJobSystemOptimization)
            {
                positionData = new NativeArray<float>(maxPoolSize * 3, Allocator.Persistent);
                rotationData = new NativeArray<float>(maxPoolSize * 4, Allocator.Persistent);
            }
            
            Debug.Log($"Object Pool Manager initialized with {poolConfigs.Length} pools");
        }
        
        private void CreatePool(PooledObjectConfig config, Transform parent)
        {
            if (pools.ContainsKey(config.poolName))
            {
                Debug.LogWarning($"Pool {config.poolName} already exists!");
                return;
            }
            
            // Create pool parent
            GameObject poolParent = new GameObject($"Pool_{config.poolName}");
            poolParent.transform.SetParent(parent);
            poolParents[config.poolName] = poolParent.transform;
            
            // Initialize pool queue
            Queue<GameObject> pool = new Queue<GameObject>();
            pools[config.poolName] = pool;
            
            // Pre-warm pool if enabled
            if (config.preWarm)
            {
                for (int i = 0; i < config.initialSize; i++)
                {
                    GameObject obj = CreateNewObject(config.prefab, poolParent.transform);
                    obj.SetActive(false);
                    pool.Enqueue(obj);
                }
            }
            
            Debug.Log($"Created pool '{config.poolName}' with {config.initialSize} pre-warmed objects");
        }
        
        public GameObject SpawnObject(string poolName, Vector3 position, Quaternion rotation)
        {
            if (!pools.ContainsKey(poolName))
            {
                AdvancedLoggingSystem.LogError(AdvancedLoggingSystem.LogCategory.Performance, "ObjectPoolManager", $"Pool '{poolName}' does not exist!");
                return null;
            }
            
            GameObject obj = GetPooledObject(poolName);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
                
                activeObjects[obj] = poolName;
                totalObjectsReused++;
                
                // Notify pooled object of spawn
                IPooledObject pooledComponent = obj.GetComponent<IPooledObject>();
                pooledComponent?.OnObjectSpawn();
                
                UpdatePoolEfficiency();
                return obj;
            }
            
            return null;
        }
        
        public void ReturnObject(GameObject obj)
        {
            if (!activeObjects.ContainsKey(obj))
            {
                Debug.LogWarning("Trying to return object that wasn't spawned from pool!");
                return;
            }
            
            string poolName = activeObjects[obj];
            activeObjects.Remove(obj);
            
            // Notify pooled object of return
            IPooledObject pooledComponent = obj.GetComponent<IPooledObject>();
            pooledComponent?.OnObjectReturn();
            
            // Reset object state
            obj.SetActive(false);
            obj.transform.SetParent(poolParents[poolName]);
            
            // Return to pool
            pools[poolName].Enqueue(obj);
        }
        
        private GameObject GetPooledObject(string poolName)
        {
            Queue<GameObject> pool = pools[poolName];
            
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            
            // Pool is empty, create new object if expansion is enabled
            if (enableDynamicExpansion)
            {
                PooledObjectConfig config = GetPoolConfig(poolName);
                if (config != null && GetActiveObjectCount(poolName) < config.maxSize)
                {
                    GameObject newObj = CreateNewObject(config.prefab, poolParents[poolName]);
                    totalObjectsCreated++;
                    return newObj;
                }
            }
            
            Debug.LogWarning($"Pool '{poolName}' is exhausted and cannot expand!");
            return null;
        }
        
        private GameObject CreateNewObject(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            
            // Add pooled object component if it doesn't exist
            if (obj.GetComponent<IPooledObject>() == null)
            {
                obj.AddComponent<PooledObjectBehaviour>();
            }
            
            return obj;
        }
        
        private PooledObjectConfig GetPoolConfig(string poolName)
        {
            foreach (var config in poolConfigs)
            {
                if (config.poolName == poolName)
                    return config;
            }
            return null;
        }
        
        private int GetActiveObjectCount(string poolName)
        {
            int count = 0;
            foreach (var kvp in activeObjects)
            {
                if (kvp.Value == poolName)
                    count++;
            }
            return count;
        }
        
        private void UpdatePoolEfficiency()
        {
            int totalOperations = totalObjectsCreated + totalObjectsReused;
            if (totalOperations > 0)
            {
                poolEfficiency = (float)totalObjectsReused / totalOperations;
            }
        }
        
        // Unity 6 Job System optimization for batch operations
        [BurstCompile]
        public struct BatchPositionUpdateJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> positions;
            [ReadOnly] public NativeArray<float> rotations;
            public NativeArray<float> results;
            
            public void Execute(int index)
            {
                // Batch update positions and rotations
                int posIndex = index * 3;
                int rotIndex = index * 4;
                
                // Perform optimized calculations
                results[index] = positions[posIndex] + positions[posIndex + 1] + positions[posIndex + 2];
            }
        }
        
        public void BatchUpdateObjects(List<GameObject> objects)
        {
            if (!enableJobSystemOptimization || objects.Count == 0)
                return;
            
            // Complete previous job
            currentJobHandle.Complete();
            
            // Prepare data for job
            for (int i = 0; i < objects.Count && i < maxPoolSize; i++)
            {
                Vector3 pos = objects[i].transform.position;
                Quaternion rot = objects[i].transform.rotation;
                
                int posIndex = i * 3;
                int rotIndex = i * 4;
                
                positionData[posIndex] = pos.x;
                positionData[posIndex + 1] = pos.y;
                positionData[posIndex + 2] = pos.z;
                
                rotationData[rotIndex] = rot.x;
                rotationData[rotIndex + 1] = rot.y;
                rotationData[rotIndex + 2] = rot.z;
                rotationData[rotIndex + 3] = rot.w;
            }
            
            // Schedule job
            var job = new BatchPositionUpdateJob
            {
                positions = positionData,
                rotations = rotationData,
                results = new NativeArray<float>(objects.Count, Allocator.TempJob)
            };
            
            currentJobHandle = job.Schedule(objects.Count, 32);
        }
        
        public void ClearPool(string poolName)
        {
            if (!pools.ContainsKey(poolName))
                return;
            
            // Return all active objects
            List<GameObject> toReturn = new List<GameObject>();
            foreach (var kvp in activeObjects)
            {
                if (kvp.Value == poolName)
                    toReturn.Add(kvp.Key);
            }
            
            foreach (var obj in toReturn)
            {
                ReturnObject(obj);
            }
            
            // Clear pool
            Queue<GameObject> pool = pools[poolName];
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                    DestroyImmediate(obj);
            }
        }
        
        public void ClearAllPools()
        {
            foreach (string poolName in pools.Keys)
            {
                ClearPool(poolName);
            }
        }
        
        // Performance monitoring
        public PoolStats GetPoolStats(string poolName)
        {
            if (!pools.ContainsKey(poolName))
                return null;
            
            return new PoolStats
            {
                poolName = poolName,
                totalObjects = pools[poolName].Count + GetActiveObjectCount(poolName),
                activeObjects = GetActiveObjectCount(poolName),
                availableObjects = pools[poolName].Count,
                efficiency = poolEfficiency
            };
        }
        
        private void OnDestroy()
        {
            // Complete any running jobs
            if (enableJobSystemOptimization)
            {
                currentJobHandle.Complete();
                
                if (positionData.IsCreated)
                    positionData.Dispose();
                if (rotationData.IsCreated)
                    rotationData.Dispose();
            }
        }
        
        [System.Serializable]
        public class PoolStats
        {
            public string poolName;
            public int totalObjects;
            public int activeObjects;
            public int availableObjects;
            public float efficiency;
        }
    }
    
    // Interface for pooled objects
    public interface IPooledObject
    {
        void OnObjectSpawn();
        void OnObjectReturn();
    }
    
    // Default pooled object behaviour
    public class PooledObjectBehaviour : MonoBehaviour, IPooledObject
    {
        public virtual void OnObjectSpawn()
        {
            // Override in derived classes
        }
        
        public virtual void OnObjectReturn()
        {
            // Reset object state
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}

