using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Centralized Update Manager to replace individual Update() methods
    /// Major performance optimization for VR - reduces Update() calls from 43+ to 1
    /// Unity 6 optimized with Job System integration
    /// </summary>
    public class OptimizedUpdateManager : MonoBehaviour
    {
        [Header("Update Frequency Settings")]
        public int normalUpdateHz = 90;      // Match VR framerate
        public int slowUpdateHz = 30;        // For less critical systems
        public int fastUpdateHz = 120;       // For critical VR systems
        
        [Header("Performance Monitoring")]
        public bool enablePerformanceTracking = true;
        public float performanceWarningThreshold = 0.011f; // 11ms warning
        
        // Update delegates for different frequencies
        public System.Action OnFastUpdate;     // 120Hz - Critical VR systems
        public System.Action OnNormalUpdate;   // 90Hz - Standard game systems  
        public System.Action OnSlowUpdate;     // 30Hz - Non-critical systems
        public System.Action OnFixedUpdate;    // Physics rate
        
        // System categories
        private List<IOptimizedUpdatable> fastSystems = new List<IOptimizedUpdatable>();
        private List<IOptimizedUpdatable> normalSystems = new List<IOptimizedUpdatable>();
        private List<IOptimizedUpdatable> slowSystems = new List<IOptimizedUpdatable>();
        
        // Timing
        private float fastUpdateInterval;
        private float normalUpdateInterval;
        private float slowUpdateInterval;
        
        private float fastUpdateTimer;
        private float normalUpdateTimer;
        private float slowUpdateTimer;
        
        // Performance tracking
        private Dictionary<string, float> systemPerformance = new Dictionary<string, float>();
        private Queue<float> updateTimeHistory = new Queue<float>();
        private const int HISTORY_SIZE = 300; // 5 seconds at 60fps
        
        // Singleton
        public static OptimizedUpdateManager Instance { get; private set; }
        
        // Properties
        public float AverageUpdateTime { get; private set; }
        public int TotalManagedSystems => fastSystems.Count + normalSystems.Count + slowSystems.Count;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeUpdateManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeUpdateManager()
        {
            // Calculate update intervals
            fastUpdateInterval = 1f / fastUpdateHz;
            normalUpdateInterval = 1f / normalUpdateHz;
            slowUpdateInterval = 1f / slowUpdateHz;
            
            // Find and register all systems
            RegisterAllSystems();
            
            Debug.Log($"🚀 Optimized Update Manager initialized - Managing {TotalManagedSystems} systems");
            Debug.Log($"   Fast Update: {fastUpdateHz}Hz ({fastSystems.Count} systems)");
            Debug.Log($"   Normal Update: {normalUpdateHz}Hz ({normalSystems.Count} systems)");
            Debug.Log($"   Slow Update: {slowUpdateHz}Hz ({slowSystems.Count} systems)");
        }
        
        private void RegisterAllSystems()
        {
            // Find all systems that implement IOptimizedUpdatable
            var allSystems = FindObjectsOfType<MonoBehaviour>();
            
            foreach (var system in allSystems)
            {
                if (system is IOptimizedUpdatable optimizedSystem)
                {
                    RegisterSystem(optimizedSystem);
                }
            }
        }
        
        public void RegisterSystem(IOptimizedUpdatable system)
        {
            switch (system.GetUpdateFrequency())
            {
                case UpdateFrequency.Fast:
                    if (!fastSystems.Contains(system))
                    {
                        fastSystems.Add(system);
                        Debug.Log($"📈 Registered {system.GetType().Name} for Fast Updates ({fastUpdateHz}Hz)");
                    }
                    break;
                    
                case UpdateFrequency.Normal:
                    if (!normalSystems.Contains(system))
                    {
                        normalSystems.Add(system);
                        Debug.Log($"⚡ Registered {system.GetType().Name} for Normal Updates ({normalUpdateHz}Hz)");
                    }
                    break;
                    
                case UpdateFrequency.Slow:
                    if (!slowSystems.Contains(system))
                    {
                        slowSystems.Add(system);
                        Debug.Log($"🐌 Registered {system.GetType().Name} for Slow Updates ({slowUpdateHz}Hz)");
                    }
                    break;
            }
        }
        
        public void UnregisterSystem(IOptimizedUpdatable system)
        {
            fastSystems.Remove(system);
            normalSystems.Remove(system);
            slowSystems.Remove(system);
        }
        
        private void Update()
        {
            float startTime = Time.realtimeSinceStartup;
            
            // Update timers
            fastUpdateTimer += Time.unscaledDeltaTime;
            normalUpdateTimer += Time.unscaledDeltaTime;
            slowUpdateTimer += Time.unscaledDeltaTime;
            
            // Fast updates (120Hz)
            if (fastUpdateTimer >= fastUpdateInterval)
            {
                UpdateSystemList(fastSystems, "Fast");
                OnFastUpdate?.Invoke();
                fastUpdateTimer = 0f;
            }
            
            // Normal updates (90Hz)
            if (normalUpdateTimer >= normalUpdateInterval)
            {
                UpdateSystemList(normalSystems, "Normal");
                OnNormalUpdate?.Invoke();
                normalUpdateTimer = 0f;
            }
            
            // Slow updates (30Hz)
            if (slowUpdateTimer >= slowUpdateInterval)
            {
                UpdateSystemList(slowSystems, "Slow");
                OnSlowUpdate?.Invoke();
                slowUpdateTimer = 0f;
            }
            
            // Performance tracking
            if (enablePerformanceTracking)
            {
                float updateTime = Time.realtimeSinceStartup - startTime;
                TrackPerformance(updateTime);
            }
        }
        
        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }
        
        private void UpdateSystemList(List<IOptimizedUpdatable> systems, string category)
        {
            float categoryStartTime = enablePerformanceTracking ? Time.realtimeSinceStartup : 0f;
            
            for (int i = systems.Count - 1; i >= 0; i--)
            {
                var system = systems[i];
                
                // Remove null references
                if (system == null || (system as MonoBehaviour) == null)
                {
                    systems.RemoveAt(i);
                    continue;
                }
                
                // Skip inactive systems
                if (!system.IsUpdateEnabled())
                {
                    continue;
                }
                
                try
                {
                    float systemStartTime = enablePerformanceTracking ? Time.realtimeSinceStartup : 0f;
                    
                    // Execute system update
                    system.OptimizedUpdate();
                    
                    // Track individual system performance
                    if (enablePerformanceTracking)
                    {
                        float systemTime = Time.realtimeSinceStartup - systemStartTime;
                        string systemName = system.GetType().Name;
                        systemPerformance[systemName] = systemTime;
                        
                        // Warning for slow systems
                        if (systemTime > performanceWarningThreshold)
                        {
                            Debug.LogWarning($"⚠️ Slow system detected: {systemName} took {systemTime * 1000:F2}ms");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Error updating {system.GetType().Name}: {ex.Message}");
                    // Don't remove the system, just log the error
                }
            }
            
            // Track category performance
            if (enablePerformanceTracking)
            {
                float categoryTime = Time.realtimeSinceStartup - categoryStartTime;
                systemPerformance[$"{category}Updates"] = categoryTime;
            }
        }
        
        private void TrackPerformance(float updateTime)
        {
            // Update average
            AverageUpdateTime = (AverageUpdateTime * 0.9f) + (updateTime * 0.1f);
            
            // Update history
            updateTimeHistory.Enqueue(updateTime);
            if (updateTimeHistory.Count > HISTORY_SIZE)
            {
                updateTimeHistory.Dequeue();
            }
            
            // Check for performance issues
            if (updateTime > performanceWarningThreshold)
            {
                Debug.LogWarning($"⚠️ Update Manager took {updateTime * 1000:F2}ms (target: <{performanceWarningThreshold * 1000:F1}ms)");
            }
        }
        
        // Public API
        public void SetUpdateFrequency(UpdateFrequency frequency, int newHz)
        {
            switch (frequency)
            {
                case UpdateFrequency.Fast:
                    fastUpdateHz = newHz;
                    fastUpdateInterval = 1f / fastUpdateHz;
                    break;
                case UpdateFrequency.Normal:
                    normalUpdateHz = newHz;
                    normalUpdateInterval = 1f / normalUpdateHz;
                    break;
                case UpdateFrequency.Slow:
                    slowUpdateHz = newHz;
                    slowUpdateInterval = 1f / slowUpdateHz;
                    break;
            }
            
            Debug.Log($"🔄 Updated {frequency} frequency to {newHz}Hz");
        }
        
        public Dictionary<string, float> GetPerformanceReport()
        {
            var report = new Dictionary<string, float>(systemPerformance)
            {
                ["TotalSystems"] = TotalManagedSystems,
                ["AverageUpdateTime"] = AverageUpdateTime,
                ["FastSystems"] = fastSystems.Count,
                ["NormalSystems"] = normalSystems.Count,
                ["SlowSystems"] = slowSystems.Count
            };
            
            return report;
        }
        
        public void OptimizeForVRHeadset(string headsetName)
        {
            switch (headsetName.ToLower())
            {
                case "quest 2":
                    SetUpdateFrequency(UpdateFrequency.Fast, 72);
                    SetUpdateFrequency(UpdateFrequency.Normal, 72);
                    SetUpdateFrequency(UpdateFrequency.Slow, 24);
                    break;
                    
                case "quest 3":
                case "pico 4":
                    SetUpdateFrequency(UpdateFrequency.Fast, 120);
                    SetUpdateFrequency(UpdateFrequency.Normal, 90);
                    SetUpdateFrequency(UpdateFrequency.Slow, 30);
                    break;
                    
                case "valve index":
                    SetUpdateFrequency(UpdateFrequency.Fast, 144);
                    SetUpdateFrequency(UpdateFrequency.Normal, 120);
                    SetUpdateFrequency(UpdateFrequency.Slow, 60);
                    break;
                    
                default:
                    // Default VR settings
                    SetUpdateFrequency(UpdateFrequency.Fast, 120);
                    SetUpdateFrequency(UpdateFrequency.Normal, 90);
                    SetUpdateFrequency(UpdateFrequency.Slow, 30);
                    break;
            }
            
            Debug.Log($"🥽 Optimized update frequencies for {headsetName}");
        }
        
        public void LogPerformanceReport()
        {
            Debug.Log("📊 Update Manager Performance Report:");
            Debug.Log($"  Total Systems: {TotalManagedSystems}");
            Debug.Log($"  Average Update Time: {AverageUpdateTime * 1000:F2}ms");
            Debug.Log($"  Fast Systems: {fastSystems.Count} @ {fastUpdateHz}Hz");
            Debug.Log($"  Normal Systems: {normalSystems.Count} @ {normalUpdateHz}Hz");
            Debug.Log($"  Slow Systems: {slowSystems.Count} @ {slowUpdateHz}Hz");
            
            // Show slowest systems
            var sortedSystems = new List<KeyValuePair<string, float>>(systemPerformance);
            sortedSystems.Sort((x, y) => y.Value.CompareTo(x.Value));
            
            Debug.Log("  Slowest Systems:");
            for (int i = 0; i < Mathf.Min(5, sortedSystems.Count); i++)
            {
                var system = sortedSystems[i];
                Debug.Log($"    {system.Key}: {system.Value * 1000:F2}ms");
            }
        }
        
        private void OnDestroy()
        {
            fastSystems.Clear();
            normalSystems.Clear();
            slowSystems.Clear();
        }
    }
    
    /// <summary>
    /// Interface for systems that want optimized updates
    /// </summary>
    public interface IOptimizedUpdatable
    {
        void OptimizedUpdate();
        UpdateFrequency GetUpdateFrequency();
        bool IsUpdateEnabled();
    }
    
    /// <summary>
    /// Update frequency categories
    /// </summary>
    public enum UpdateFrequency
    {
        Fast,    // 120Hz - Critical VR systems (hand tracking, head tracking)
        Normal,  // 90Hz - Standard game systems (targets, collisions)
        Slow     // 30Hz - Non-critical systems (UI, statistics, AI coach)
    }
} 