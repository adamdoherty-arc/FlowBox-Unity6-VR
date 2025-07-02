using UnityEngine;
using VRBoxingGame.Environment;
using VRBoxingGame.Boxing;
using System.Collections;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Critical System Integrator - Ties all optimized systems together
    /// Replaces legacy systems with modern optimized versions
    /// Ensures proper initialization order and dependencies
    /// </summary>
    public class CriticalSystemIntegrator : MonoBehaviour
    {
        [Header("System Integration")]
        public bool enableOptimizedSystems = true;
        public bool replaceUpdateMethods = true;
        public bool enableCachedReferences = true;
        public bool enableSceneAssetManagement = true;
        public bool enableUnity6Features = true;
        
        [Header("Performance Targets")]
        public int targetFPS = 90;
        public float maxFrameTime = 0.011f; // 11ms for 90 FPS
        
        // System references
        private OptimizedUpdateManager updateManager;
        private CachedReferenceManager referenceManager;
        private SceneAssetManager sceneAssetManager;
        private Unity6FeatureIntegrator unity6Integrator;
        private SceneGameModeIntegrator gameModeIntegrator;
        
        // Performance tracking
        private float[] frameTimeHistory = new float[300]; // 5 seconds at 60fps
        private int frameTimeIndex = 0;
        private bool systemsInitialized = false;
        
        public static CriticalSystemIntegrator Instance { get; private set; }
        
        // Events
        public System.Action OnSystemsInitialized;
        public System.Action<float> OnPerformanceWarning;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                StartCoroutine(InitializeSystemsSequentially());
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private IEnumerator InitializeSystemsSequentially()
        {
            Debug.Log("🚀 Starting Critical System Integration...");
            
            // Phase 1: Core Performance Systems
            if (enableCachedReferences)
            {
                yield return StartCoroutine(InitializeCachedReferences());
            }
            
            if (enableOptimizedSystems && replaceUpdateMethods)
            {
                yield return StartCoroutine(InitializeOptimizedUpdateManager());
            }
            
            // Phase 2: Scene Management
            if (enableSceneAssetManagement)
            {
                yield return StartCoroutine(InitializeSceneAssetManager());
            }
            
            // Phase 3: Game Mode Integration
            yield return StartCoroutine(InitializeGameModeIntegrator());
            
            // Phase 4: Unity 6 Features
            if (enableUnity6Features)
            {
                yield return StartCoroutine(InitializeUnity6Features());
            }
            
            // Phase 5: Final Integration
            yield return StartCoroutine(FinalizeSystemIntegration());
            
            systemsInitialized = true;
            OnSystemsInitialized?.Invoke();
            
            Debug.Log("✅ Critical System Integration Complete!");
            LogSystemStatus();
        }
        
        private IEnumerator InitializeCachedReferences()
        {
            Debug.Log("📝 Initializing Cached Reference Manager...");
            
            if (CachedReferenceManager.Instance == null)
            {
                GameObject cacheObj = new GameObject("Cached Reference Manager");
                referenceManager = cacheObj.AddComponent<CachedReferenceManager>();
                cacheObj.transform.SetParent(transform);
            }
            else
            {
                referenceManager = CachedReferenceManager.Instance;
            }
            
            yield return new WaitForSeconds(0.1f);
            Debug.Log("✅ Cached Reference Manager ready");
        }
        
        private IEnumerator InitializeOptimizedUpdateManager()
        {
            Debug.Log("⚡ Initializing Optimized Update Manager...");
            
            if (OptimizedUpdateManager.Instance == null)
            {
                GameObject updateObj = new GameObject("Optimized Update Manager");
                updateManager = updateObj.AddComponent<OptimizedUpdateManager>();
                updateObj.transform.SetParent(transform);
            }
            else
            {
                updateManager = OptimizedUpdateManager.Instance;
            }
            
            // Configure for VR performance
            updateManager.normalUpdateHz = targetFPS;
            updateManager.fastUpdateHz = Mathf.RoundToInt(targetFPS * 1.33f); // 120Hz for 90fps target
            updateManager.slowUpdateHz = Mathf.RoundToInt(targetFPS * 0.33f); // 30Hz for 90fps target
            
            yield return new WaitForSeconds(0.1f);
            Debug.Log("✅ Optimized Update Manager ready");
        }
        
        private IEnumerator InitializeSceneAssetManager()
        {
            Debug.Log("🏗️ Initializing Scene Asset Manager...");
            
            if (SceneAssetManager.Instance == null)
            {
                GameObject sceneObj = new GameObject("Scene Asset Manager");
                sceneAssetManager = sceneObj.AddComponent<SceneAssetManager>();
                sceneObj.transform.SetParent(transform);
            }
            else
            {
                sceneAssetManager = SceneAssetManager.Instance;
            }
            
            yield return new WaitForSeconds(0.2f);
            Debug.Log("✅ Scene Asset Manager ready");
        }
        
        private IEnumerator InitializeGameModeIntegrator()
        {
            Debug.Log("🎮 Initializing Game Mode Integrator...");
            
            if (SceneGameModeIntegrator.Instance == null)
            {
                GameObject gameModeObj = new GameObject("Scene Game Mode Integrator");
                gameModeIntegrator = gameModeObj.AddComponent<SceneGameModeIntegrator>();
                gameModeObj.transform.SetParent(transform);
            }
            else
            {
                gameModeIntegrator = SceneGameModeIntegrator.Instance;
            }
            
            yield return new WaitForSeconds(0.1f);
            Debug.Log("✅ Game Mode Integrator ready");
        }
        
        private IEnumerator InitializeUnity6Features()
        {
            Debug.Log("🆕 Initializing Unity 6 Features...");
            
            if (Unity6FeatureIntegrator.Instance == null)
            {
                GameObject unity6Obj = new GameObject("Unity 6 Feature Integrator");
                unity6Integrator = unity6Obj.AddComponent<Unity6FeatureIntegrator>();
                unity6Obj.transform.SetParent(transform);
            }
            else
            {
                unity6Integrator = Unity6FeatureIntegrator.Instance;
            }
            
            yield return new WaitForSeconds(0.3f);
            Debug.Log("✅ Unity 6 Features ready");
        }
        
        private IEnumerator FinalizeSystemIntegration()
        {
            Debug.Log("🔧 Finalizing System Integration...");
            
            // Connect systems together
            ConnectSystems();
            
            // Apply performance optimizations
            ApplyPerformanceOptimizations();
            
            // Validate system health
            yield return StartCoroutine(ValidateSystemHealth());
            
            Debug.Log("✅ System Integration Finalized");
        }
        
        private void ConnectSystems()
        {
            // Connect scene loading with game mode integration
            var sceneLoader = CachedReferenceManager.Get<SceneLoadingManager>();
            if (sceneLoader != null && gameModeIntegrator != null)
            {
                sceneLoader.OnSceneChanged += (sceneType) => 
                {
                    // Notify game mode integrator of scene changes
                    Debug.Log($"🔗 Scene changed to {sceneType}, updating game mode integration");
                };
            }
            
            // Connect performance monitoring
            if (updateManager != null)
            {
                updateManager.OnFastUpdate += TrackPerformance;
            }
        }
        
        private void ApplyPerformanceOptimizations()
        {
            // Set optimal quality settings for VR
            QualitySettings.vSyncCount = 0; // Let VR runtime handle sync
            Application.targetFrameRate = targetFPS;
            
            // Configure Unity for VR performance
            Time.fixedDeltaTime = 1f / 90f; // 90Hz physics for VR
            Physics.defaultSolverIterations = 4; // Reduce from default 6
            Physics.defaultSolverVelocityIterations = 1; // Reduce from default 4
            
            Debug.Log($"🎯 Performance optimizations applied for {targetFPS} FPS target");
        }
        
        private IEnumerator ValidateSystemHealth()
        {
            Debug.Log("🩺 Validating System Health...");
            
            // Check each system
            bool allHealthy = true;
            
            if (referenceManager == null)
            {
                Debug.LogError("❌ Cached Reference Manager missing!");
                allHealthy = false;
            }
            
            if (updateManager == null)
            {
                Debug.LogError("❌ Optimized Update Manager missing!");
                allHealthy = false;
            }
            
            if (sceneAssetManager == null)
            {
                Debug.LogError("❌ Scene Asset Manager missing!");
                allHealthy = false;
            }
            
            if (gameModeIntegrator == null)
            {
                Debug.LogError("❌ Game Mode Integrator missing!");
                allHealthy = false;
            }
            
            yield return new WaitForSeconds(0.1f);
            
            if (allHealthy)
            {
                Debug.Log("✅ All systems healthy!");
            }
            else
            {
                Debug.LogError("❌ Some systems are unhealthy!");
            }
        }
        
        private void TrackPerformance()
        {
            float frameTime = Time.unscaledDeltaTime;
            frameTimeHistory[frameTimeIndex] = frameTime;
            frameTimeIndex = (frameTimeIndex + 1) % frameTimeHistory.Length;
            
            // Check for performance warnings
            if (frameTime > maxFrameTime)
            {
                OnPerformanceWarning?.Invoke(frameTime);
                Debug.LogWarning($"⚠️ Frame time exceeded target: {frameTime * 1000:F2}ms (target: {maxFrameTime * 1000:F1}ms)");
            }
        }
        
        private void LogSystemStatus()
        {
            Debug.Log("📊 System Integration Status Report:");
            Debug.Log($"  Systems Initialized: {systemsInitialized}");
            Debug.Log($"  Target FPS: {targetFPS}");
            Debug.Log($"  Max Frame Time: {maxFrameTime * 1000:F1}ms");
            Debug.Log($"  Cached References: {enableCachedReferences}");
            Debug.Log($"  Optimized Updates: {replaceUpdateMethods}");
            Debug.Log($"  Scene Asset Management: {enableSceneAssetManagement}");
            Debug.Log($"  Unity 6 Features: {enableUnity6Features}");
            
            // Performance stats
            if (updateManager != null)
            {
                var perfReport = updateManager.GetPerformanceReport();
                Debug.Log($"  Total Managed Systems: {perfReport["TotalSystems"]}");
                Debug.Log($"  Average Update Time: {(float)perfReport["AverageUpdateTime"] * 1000:F2}ms");
            }
            
            if (referenceManager != null)
            {
                Debug.Log($"  Cached Components Available: Yes");
            }
        }
        
        /// <summary>
        /// Force system refresh - useful after scene changes
        /// </summary>
        public void RefreshAllSystems()
        {
            Debug.Log("🔄 Refreshing all systems...");
            
            if (referenceManager != null)
            {
                referenceManager.RefreshAllCaches();
            }
            
            if (updateManager != null)
            {
                // Re-register all systems
                updateManager.LogPerformanceReport();
            }
            
            Debug.Log("✅ System refresh complete");
        }
        
        /// <summary>
        /// Get current performance statistics
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> GetPerformanceStats()
        {
            var stats = new System.Collections.Generic.Dictionary<string, object>();
            
            // Calculate average frame time
            float avgFrameTime = 0f;
            for (int i = 0; i < frameTimeHistory.Length; i++)
            {
                avgFrameTime += frameTimeHistory[i];
            }
            avgFrameTime /= frameTimeHistory.Length;
            
            stats["average_frame_time"] = avgFrameTime;
            stats["average_fps"] = 1f / avgFrameTime;
            stats["target_fps"] = targetFPS;
            stats["systems_initialized"] = systemsInitialized;
            
            if (updateManager != null)
            {
                var updateStats = updateManager.GetPerformanceReport();
                foreach (var kvp in updateStats)
                {
                    stats["update_" + kvp.Key] = kvp.Value;
                }
            }
            
            return stats;
        }
        
        /// <summary>
        /// Emergency performance mode - disable non-critical systems
        /// </summary>
        public void EnableEmergencyPerformanceMode()
        {
            Debug.LogWarning("🚨 Enabling Emergency Performance Mode!");
            
            // Reduce update frequencies
            if (updateManager != null)
            {
                updateManager.SetUpdateFrequency(UpdateFrequency.Fast, 72);
                updateManager.SetUpdateFrequency(UpdateFrequency.Normal, 60);
                updateManager.SetUpdateFrequency(UpdateFrequency.Slow, 20);
            }
            
            // Reduce quality settings
            QualitySettings.SetQualityLevel(1); // Low quality
            QualitySettings.pixelLightCount = 1;
            QualitySettings.shadows = ShadowQuality.Disable;
            
            Debug.Log("⚡ Emergency performance mode active");
        }
        
        public bool AreSystemsInitialized()
        {
            return systemsInitialized;
        }
        
        private void Update()
        {
            // This is the ONLY Update method allowed in the optimized system
            // All other systems should use OptimizedUpdateManager
            if (systemsInitialized)
            {
                TrackPerformance();
            }
        }
        
        private void OnDestroy()
        {
            Debug.Log("🔚 Critical System Integrator shutting down");
        }
    }
} 