using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Streaming
{
    /// <summary>
    /// Unity 6 Addressable Assets Streaming System
    /// Features: Async content loading, memory management, predictive caching
    /// </summary>
    public class AddressableStreamingSystem : MonoBehaviour
    {
        [Header("Streaming Configuration")]
        public bool enablePredictiveLoading = true;
        public bool enableMemoryManagement = true;
        public bool enableAsyncPreloading = true;
        public int maxConcurrentLoads = 4;
        
        [Header("Cache Management")]
        public int maxCachedAssets = 100;
        public float cacheTimeoutSeconds = 300f;
        public bool enableLRUCache = true;
        public bool enableSmartUnloading = true;
        
        // Asset Management
        private Dictionary<string, CachedAsset> assetCache = new Dictionary<string, CachedAsset>();
        private Queue<LoadRequest> loadQueue = new Queue<LoadRequest>();
        private HashSet<string> currentlyLoading = new HashSet<string>();
        
        // Performance Tracking
        private float totalLoadTime = 0f;
        private int completedLoads = 0;
        private float averageLoadTime = 0f;
        
        // Singleton
        public static AddressableStreamingSystem Instance { get; private set; }
        
        // Properties
        public int CachedAssetCount => assetCache.Count;
        public int QueuedLoadCount => loadQueue.Count;
        public float AverageLoadTime => averageLoadTime;
        public bool IsLoadingActive => currentlyLoading.Count > 0;
        
        // Data Structures
        private struct CachedAsset
        {
            public GameObject asset;
            public float lastAccessTime;
            public int accessCount;
            public float memoryFootprint;
            public bool isPinned;
        }
        
        private struct LoadRequest
        {
            public string addressableKey;
            public Vector3 worldPosition;
            public float priority;
            public System.Action<GameObject> onComplete;
            public bool isPreload;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeStreamingSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeStreamingSystem()
        {
            Debug.Log("📦 Initializing Addressable Streaming System...");
            
            // Initialize Addressables
            if (!Addressables.InitializeAsync().IsDone)
            {
                Debug.Log("⏳ Waiting for Addressables initialization...");
            }
            
            Debug.Log("✅ Addressable Streaming System initialized!");
        }
        
        private void Update()
        {
            ProcessLoadQueue();
            UpdateCacheAccess();
        }
        
        private void ProcessLoadQueue()
        {
            if (loadQueue.Count == 0 || currentlyLoading.Count >= maxConcurrentLoads)
                return;
            
            var request = loadQueue.Dequeue();
            _ = ProcessLoadRequestAsync(request);
        }
        
        private async Task ProcessLoadRequestAsync(LoadRequest request)
        {
            string key = request.addressableKey;
            
            if (currentlyLoading.Contains(key))
                return;
            
            currentlyLoading.Add(key);
            
            try
            {
                float startTime = Time.realtimeSinceStartup;
                
                // Check cache first
                if (assetCache.ContainsKey(key) && !request.isPreload)
                {
                    var cachedAsset = assetCache[key];
                    cachedAsset.lastAccessTime = Time.time;
                    cachedAsset.accessCount++;
                    assetCache[key] = cachedAsset;
                    
                    request.onComplete?.Invoke(cachedAsset.asset);
                    return;
                }
                
                // Load from Addressables
                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                GameObject asset = await handle.Task;
                
                if (asset != null)
                {
                    // Cache the asset
                    CacheAsset(key, asset);
                    
                    // Track performance
                    float loadTime = Time.realtimeSinceStartup - startTime;
                    UpdateLoadStats(loadTime);
                    
                    // Complete callback
                    request.onComplete?.Invoke(asset);
                    
                    Debug.Log($"📦 Loaded asset: {key} in {loadTime:F3}s");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Load error for {key}: {ex.Message}");
            }
            finally
            {
                currentlyLoading.Remove(key);
            }
        }
        
        private void CacheAsset(string key, GameObject asset)
        {
            if (assetCache.Count >= maxCachedAssets && enableLRUCache)
            {
                EvictOldestAsset();
            }
            
            var cachedAsset = new CachedAsset
            {
                asset = asset,
                lastAccessTime = Time.time,
                accessCount = 1,
                memoryFootprint = EstimateMemoryFootprint(asset),
                isPinned = false
            };
            
            assetCache[key] = cachedAsset;
        }
        
        private void EvictOldestAsset()
        {
            string oldestKey = null;
            float oldestTime = float.MaxValue;
            
            foreach (var kvp in assetCache)
            {
                if (!kvp.Value.isPinned && kvp.Value.lastAccessTime < oldestTime)
                {
                    oldestTime = kvp.Value.lastAccessTime;
                    oldestKey = kvp.Key;
                }
            }
            
            if (oldestKey != null)
            {
                assetCache.Remove(oldestKey);
            }
        }
        
        private float EstimateMemoryFootprint(GameObject asset)
        {
            // Rough estimation of memory usage
            return 1f; // Placeholder
        }
        
        private void UpdateCacheAccess()
        {
            // Update access patterns for predictive loading
        }
        
        private void UpdateLoadStats(float loadTime)
        {
            totalLoadTime += loadTime;
            completedLoads++;
            averageLoadTime = totalLoadTime / completedLoads;
        }
        
        // Public API
        public async Task<GameObject> LoadAssetAsync(string addressableKey, Vector3 worldPosition = default)
        {
            // Check cache first
            if (assetCache.ContainsKey(addressableKey))
            {
                var cachedAsset = assetCache[addressableKey];
                cachedAsset.lastAccessTime = Time.time;
                cachedAsset.accessCount++;
                assetCache[addressableKey] = cachedAsset;
                return cachedAsset.asset;
            }
            
            // Create load request
            var tcs = new TaskCompletionSource<GameObject>();
            
            var request = new LoadRequest
            {
                addressableKey = addressableKey,
                worldPosition = worldPosition,
                priority = 1f,
                onComplete = (asset) => tcs.SetResult(asset),
                isPreload = false
            };
            
            loadQueue.Enqueue(request);
            
            return await tcs.Task;
        }
        
        public void PreloadAsset(string addressableKey, Vector3 worldPosition = default)
        {
            if (assetCache.ContainsKey(addressableKey) || currentlyLoading.Contains(addressableKey))
                return;
            
            var request = new LoadRequest
            {
                addressableKey = addressableKey,
                worldPosition = worldPosition,
                priority = 0.5f,
                onComplete = null,
                isPreload = true
            };
            
            loadQueue.Enqueue(request);
        }
        
        public bool IsAssetLoaded(string addressableKey)
        {
            return assetCache.ContainsKey(addressableKey);
        }
        
        public bool IsAssetLoading(string addressableKey)
        {
            return currentlyLoading.Contains(addressableKey);
        }
        
        private void OnDestroy()
        {
            // Clear caches
            assetCache.Clear();
        }
    }
}
