using UnityEngine;
using System.Collections.Generic;
using VRBoxingGame.Audio;
using VRBoxingGame.Boxing;
using VRBoxingGame.Environment;
using VRBoxingGame.HandTracking;
using VRBoxingGame.Performance;
using VRBoxingGame.UI;
using Unity.XR.CoreUtils;
using VRBoxingGame.Core;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// System Registry - Centralized system reference management
    /// Eliminates expensive FindObjectOfType calls by caching system references
    /// </summary>
    public class SystemRegistry : MonoBehaviour
    {
        private static SystemRegistry instance;
        private Dictionary<System.Type, Component> systemCache = new Dictionary<System.Type, Component>();
        private bool isInitialized = false;
        
        public static SystemRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CachedReferenceManager.Get<SystemRegistry>();
                    if (instance == null)
                    {
                        GameObject registryGO = new GameObject("System Registry");
                        instance = registryGO.AddComponent<SystemRegistry>();
                        DontDestroyOnLoad(registryGO);
                    }
                    instance.InitializeRegistry();
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeRegistry();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeRegistry()
        {
            if (isInitialized) return;
            
            // Cache all system references
            CacheSystemReferences();
            isInitialized = true;
            
            Debug.Log($"🏗️ System Registry initialized with {systemCache.Count} cached systems");
        }
        
        private void CacheSystemReferences()
        {
            // Core Systems
            CacheSystem<GameManager>();
            CacheSystem<VRCameraHelper>();
            CacheSystem<Unity6IntegrationManager>();
            CacheSystem<AdvancedGameStateManager>();
            
            // Audio Systems
            CacheSystem<AdvancedAudioManager>();
            CacheSystem<TestTrack>();
            CacheSystem<MusicReactiveSystem>();
            
            // Boxing Systems
            CacheSystem<RhythmTargetSystem>();
            CacheSystem<EnhancedPunchDetector>();
            CacheSystem<BoxingFormTracker>();
            CacheSystem<AdvancedTargetSystem>();
            CacheSystem<AdvancedBoxingFormProcessor>();
            CacheSystem<ECSTargetSystem>();
            CacheSystem<PredictiveTargetingSystem>();
            
            // Environment Systems
            CacheSystem<DynamicBackgroundSystem>();
            CacheSystem<SceneLoadingManager>();
            CacheSystem<RainSceneCreator>();
            CacheSystem<UnderwaterFishSystem>();
            CacheSystem<AdvancedImmersiveEnvironmentSystem>();
            CacheSystem<SceneTransformationSystem>();
            
            // Hand Tracking Systems
            CacheSystem<HandTrackingManager>();
            CacheSystem<AdvancedGestureRecognition>();
            CacheSystem<HapticFeedbackManager>();
            
            // Performance Systems
            CacheSystem<VRPerformanceMonitor>();
            CacheSystem<ObjectPoolManager>();
            CacheSystem<VRRenderGraphSystem>();
            CacheSystem<MaterialPool>();
            
            // UI Systems
            CacheSystem<GameUI>();
            CacheSystem<MainMenuSystem>();
            
            // XR Systems
            CacheSystem<XROrigin>();
        }
        
        private void CacheSystem<T>() where T : Component
        {
            T system = CachedReferenceManager.Get<T>();
            if (system != null)
            {
                systemCache[typeof(T)] = system;
            }
        }
        
        /// <summary>
        /// Get cached system reference (replaces FindObjectOfType)
        /// </summary>
        public static T GetSystem<T>() where T : Component
        {
            if (Instance.systemCache.TryGetValue(typeof(T), out Component system))
            {
                return system as T;
            }
            
            // If not cached, try to find and cache it
            T foundSystem = CachedReferenceManager.Get<T>();
            if (foundSystem != null)
            {
                Instance.systemCache[typeof(T)] = foundSystem;
            }
            
            return foundSystem;
        }
        
        /// <summary>
        /// Register a system manually (useful for dynamically created systems)
        /// </summary>
        public static void RegisterSystem<T>(T system) where T : Component
        {
            if (system != null)
            {
                Instance.systemCache[typeof(T)] = system;
            }
        }
        
        /// <summary>
        /// Unregister a system (useful when systems are destroyed)
        /// </summary>
        public static void UnregisterSystem<T>() where T : Component
        {
            Instance.systemCache.Remove(typeof(T));
        }
        
        /// <summary>
        /// Refresh all system caches (call after scene changes)
        /// </summary>
        public static void RefreshSystemCache()
        {
            if (Instance != null)
            {
                Instance.systemCache.Clear();
                Instance.CacheSystemReferences();
            }
        }
        
        /// <summary>
        /// Check if a system is available
        /// </summary>
        public static bool HasSystem<T>() where T : Component
        {
            return GetSystem<T>() != null;
        }
        
        /// <summary>
        /// Safe system access with null checking
        /// </summary>
        public static bool TryGetSystem<T>(out T system) where T : Component
        {
            system = GetSystem<T>();
            return system != null;
        }
        
        /// <summary>
        /// Get system status report
        /// </summary>
        public static string GetSystemStatusReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== System Registry Status ===");
            report.AppendLine($"Cached Systems: {Instance.systemCache.Count}");
            
            foreach (var kvp in Instance.systemCache)
            {
                string status = kvp.Value != null ? "✅ Active" : "❌ Null";
                report.AppendLine($"{kvp.Key.Name}: {status}");
            }
            
            return report.ToString();
        }
        
        private void OnDestroy()
        {
            systemCache.Clear();
        }
    }
} 