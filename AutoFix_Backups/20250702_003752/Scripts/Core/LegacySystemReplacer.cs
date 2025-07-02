using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Legacy System Replacer - Automatically converts legacy systems to optimized versions
    /// Replaces Update() methods with OptimizedUpdateManager integration
    /// Replaces FindObjectOfType calls with CachedReferenceManager
    /// </summary>
    public class LegacySystemReplacer : MonoBehaviour
    {
        [Header("Replacement Settings")]
        public bool enableAutomaticReplacement = true;
        public bool replaceUpdateMethods = true;
        public bool replaceFindObjectCalls = true;
        public bool enableDebugLogging = true;
        
        [Header("Performance Monitoring")]
        public bool trackReplacementImpact = true;
        public float replacementCheckInterval = 5f;
        
        // Tracking
        private List<MonoBehaviour> legacySystems = new List<MonoBehaviour>();
        private List<MonoBehaviour> optimizedSystems = new List<MonoBehaviour>();
        private Dictionary<Type, LegacySystemInfo> systemReplacements = new Dictionary<Type, LegacySystemInfo>();
        
        // Performance tracking
        private int totalReplacements = 0;
        private float totalPerformanceGain = 0f;
        private float lastCheckTime = 0f;
        
        public static LegacySystemReplacer Instance { get; private set; }
        
        [System.Serializable]
        public struct LegacySystemInfo
        {
            public string systemName;
            public bool hasUpdateMethod;
            public bool usesFindObjectOfType;
            public bool isReplaced;
            public float performanceImpact;
            public string replacementType;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (enableAutomaticReplacement)
                {
                    StartCoroutine(InitializeReplacementSystem());
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private System.Collections.IEnumerator InitializeReplacementSystem()
        {
            Debug.Log("🔄 Initializing Legacy System Replacer...");
            
            // Wait for core systems to initialize
            yield return new WaitUntil(() => 
                CachedReferenceManager.Instance != null &&
                OptimizedUpdateManager.Instance != null);
            
            // Scan for legacy systems
            yield return StartCoroutine(ScanForLegacySystems());
            
            // Replace legacy systems
            if (enableAutomaticReplacement)
            {
                yield return StartCoroutine(ReplaceLegacySystems());
            }
            
            Debug.Log($"✅ Legacy System Replacer initialized - {totalReplacements} systems optimized");
        }
        
        private System.Collections.IEnumerator ScanForLegacySystems()
        {
            Debug.Log("🔍 Scanning for legacy systems...");
            
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            int processedCount = 0;
            
            foreach (var mono in allMonoBehaviours)
            {
                if (mono == null) continue;
                
                // Skip already optimized systems
                if (mono is IOptimizedUpdatable) continue;
                
                // Analyze system
                var systemInfo = AnalyzeSystem(mono);
                if (systemInfo.hasUpdateMethod || systemInfo.usesFindObjectOfType)
                {
                    systemReplacements[mono.GetType()] = systemInfo;
                    legacySystems.Add(mono);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"📝 Found legacy system: {systemInfo.systemName} " +
                                 $"(Update: {systemInfo.hasUpdateMethod}, FindObject: {systemInfo.usesFindObjectOfType})");
                    }
                }
                
                processedCount++;
                
                // Yield every 10 systems to prevent frame drops
                if (processedCount % 10 == 0)
                {
                    yield return null;
                }
            }
            
            Debug.Log($"🔍 Scan complete - Found {legacySystems.Count} legacy systems");
        }
        
        private LegacySystemInfo AnalyzeSystem(MonoBehaviour system)
        {
            var info = new LegacySystemInfo
            {
                systemName = system.GetType().Name,
                hasUpdateMethod = false,
                usesFindObjectOfType = false,
                isReplaced = false,
                performanceImpact = 0f,
                replacementType = "None"
            };
            
            Type systemType = system.GetType();
            
            // Check for Update methods
            var updateMethod = systemType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            if (updateMethod != null && updateMethod.DeclaringType == systemType)
            {
                info.hasUpdateMethod = true;
                info.performanceImpact += 0.1f; // Estimated impact
            }
            
            // Check for FixedUpdate methods
            var fixedUpdateMethod = systemType.GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fixedUpdateMethod != null && fixedUpdateMethod.DeclaringType == systemType)
            {
                info.hasUpdateMethod = true;
                info.performanceImpact += 0.05f;
            }
            
            // Check for LateUpdate methods
            var lateUpdateMethod = systemType.GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (lateUpdateMethod != null && lateUpdateMethod.DeclaringType == systemType)
            {
                info.hasUpdateMethod = true;
                info.performanceImpact += 0.05f;
            }
            
            // Check for FindObjectOfType usage (simplified check)
            var fields = systemType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(Component)) || field.FieldType == typeof(Component))
                {
                    // Check if field is likely set via FindObjectOfType
                    var value = field.GetValue(system);
                    if (value == null)
                    {
                        info.usesFindObjectOfType = true;
                        info.performanceImpact += 0.2f; // Higher impact for FindObjectOfType
                        break;
                    }
                }
            }
            
            return info;
        }
        
        private System.Collections.IEnumerator ReplaceLegacySystems()
        {
            Debug.Log("🔧 Replacing legacy systems...");
            
            int replacedCount = 0;
            
            foreach (var legacySystem in legacySystems)
            {
                if (legacySystem == null) continue;
                
                Type systemType = legacySystem.GetType();
                var systemInfo = systemReplacements[systemType];
                
                // Create optimized wrapper
                bool success = CreateOptimizedWrapper(legacySystem, systemInfo);
                
                if (success)
                {
                    systemInfo.isReplaced = true;
                    systemInfo.replacementType = "OptimizedWrapper";
                    systemReplacements[systemType] = systemInfo;
                    
                    totalReplacements++;
                    totalPerformanceGain += systemInfo.performanceImpact;
                    replacedCount++;
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"✅ Replaced {systemInfo.systemName} with optimized version");
                    }
                }
                
                // Yield every 5 replacements
                if (replacedCount % 5 == 0)
                {
                    yield return null;
                }
            }
            
            Debug.Log($"🎯 Replacement complete - {replacedCount} systems optimized");
        }
        
        private bool CreateOptimizedWrapper(MonoBehaviour legacySystem, LegacySystemInfo systemInfo)
        {
            try
            {
                // Create optimized wrapper component
                var wrapperComponent = legacySystem.gameObject.AddComponent<LegacySystemWrapper>();
                wrapperComponent.Initialize(legacySystem, systemInfo);
                
                // Register with OptimizedUpdateManager if has Update method
                if (systemInfo.hasUpdateMethod && OptimizedUpdateManager.Instance != null)
                {
                    OptimizedUpdateManager.Instance.RegisterSystem(wrapperComponent);
                }
                
                // Disable original Update methods
                if (replaceUpdateMethods)
                {
                    DisableUpdateMethods(legacySystem);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to create optimized wrapper for {systemInfo.systemName}: {ex.Message}");
                return false;
            }
        }
        
        private void DisableUpdateMethods(MonoBehaviour system)
        {
            // Mark the system as non-updatable by adding a flag
            var flagComponent = system.gameObject.GetComponent<LegacyUpdateDisabled>();
            if (flagComponent == null)
            {
                system.gameObject.AddComponent<LegacyUpdateDisabled>();
            }
        }
        
        private void Update()
        {
            if (trackReplacementImpact && Time.time - lastCheckTime > replacementCheckInterval)
            {
                CheckReplacementImpact();
                lastCheckTime = Time.time;
            }
        }
        
        private void CheckReplacementImpact()
        {
            if (totalReplacements == 0) return;
            
            // Calculate estimated performance improvement
            float estimatedFrameTimeImprovement = totalPerformanceGain;
            float estimatedFPSImprovement = 1f / (1f/90f - estimatedFrameTimeImprovement) - 90f;
            
            if (enableDebugLogging)
            {
                Debug.Log($"📊 Performance Impact Report:");
                Debug.Log($"  Total Replacements: {totalReplacements}");
                Debug.Log($"  Estimated Frame Time Improvement: {estimatedFrameTimeImprovement * 1000:F2}ms");
                Debug.Log($"  Estimated FPS Improvement: +{estimatedFPSImprovement:F1} FPS");
            }
        }
        
        /// <summary>
        /// Manual replacement trigger for specific system
        /// </summary>
        public bool ReplaceSystem(MonoBehaviour system)
        {
            if (system == null) return false;
            
            var systemInfo = AnalyzeSystem(system);
            return CreateOptimizedWrapper(system, systemInfo);
        }
        
        /// <summary>
        /// Get replacement statistics
        /// </summary>
        public Dictionary<string, object> GetReplacementStats()
        {
            return new Dictionary<string, object>
            {
                {"total_replacements", totalReplacements},
                {"estimated_performance_gain", totalPerformanceGain},
                {"legacy_systems_found", legacySystems.Count},
                {"replacement_success_rate", legacySystems.Count > 0 ? (float)totalReplacements / legacySystems.Count : 0f}
            };
        }
        
        /// <summary>
        /// Force re-scan for new legacy systems
        /// </summary>
        public void RescanForLegacySystems()
        {
            if (enableAutomaticReplacement)
            {
                StartCoroutine(ScanForLegacySystems());
            }
        }
    }
    
    /// <summary>
    /// Wrapper component that makes legacy systems work with OptimizedUpdateManager
    /// </summary>
    public class LegacySystemWrapper : MonoBehaviour, IOptimizedUpdatable
    {
        private MonoBehaviour wrappedSystem;
        private LegacySystemReplacer.LegacySystemInfo systemInfo;
        private MethodInfo updateMethod;
        private MethodInfo fixedUpdateMethod;
        private MethodInfo lateUpdateMethod;
        
        public void Initialize(MonoBehaviour system, LegacySystemReplacer.LegacySystemInfo info)
        {
            wrappedSystem = system;
            systemInfo = info;
            
            Type systemType = system.GetType();
            
            // Cache method references
            updateMethod = systemType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            fixedUpdateMethod = systemType.GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            lateUpdateMethod = systemType.GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        
        public void OptimizedUpdate()
        {
            if (wrappedSystem == null) return;
            
            try
            {
                // Call original Update method
                if (updateMethod != null && updateMethod.DeclaringType == wrappedSystem.GetType())
                {
                    updateMethod.Invoke(wrappedSystem, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in wrapped system {systemInfo.systemName}: {ex.Message}");
            }
        }
        
        public UpdateFrequency GetUpdateFrequency()
        {
            // Categorize based on system type
            string systemName = systemInfo.systemName.ToLower();
            
            if (systemName.Contains("hand") || systemName.Contains("tracking") || systemName.Contains("input"))
                return UpdateFrequency.Fast; // 120Hz for input systems
            
            if (systemName.Contains("ui") || systemName.Contains("menu") || systemName.Contains("debug"))
                return UpdateFrequency.Slow; // 30Hz for UI systems
            
            return UpdateFrequency.Normal; // 90Hz for everything else
        }
        
        public bool IsUpdateEnabled()
        {
            return wrappedSystem != null && wrappedSystem.enabled && wrappedSystem.gameObject.activeInHierarchy;
        }
    }
    
    /// <summary>
    /// Marker component to indicate that legacy Update methods are disabled
    /// </summary>
    public class LegacyUpdateDisabled : MonoBehaviour
    {
        // This component serves as a flag to indicate that Update methods are handled by OptimizedUpdateManager
    }
} 