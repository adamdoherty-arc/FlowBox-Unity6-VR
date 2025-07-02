using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VRBoxingGame.Environment;
using VRBoxingGame.Boxing;
using VRBoxingGame.UI;
using VRBoxingGame.Core;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// System Integration Validator - Ensures all systems work together properly
    /// Checks for conflicts, missing dependencies, and performance issues
    /// </summary>
    public class SystemIntegrationValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        public bool validateOnStart = true;
        public bool enableContinuousValidation = true;
        public float validationInterval = 30f;
        
        [Header("Performance Thresholds")]
        public float maxFrameTime = 0.011f; // 11ms for 90 FPS
        public int maxActiveUpdate = 5; // Maximum allowed individual Update() methods
        public int maxFindObjectCalls = 3; // Maximum allowed FindObjectOfType calls per frame
        
        // Validation results
        private ValidationReport currentReport;
        private List<ValidationIssue> criticalIssues = new List<ValidationIssue>();
        private List<ValidationIssue> warnings = new List<ValidationIssue>();
        
        public static SystemIntegrationValidator Instance { get; private set; }
        
        [System.Serializable]
        public struct ValidationReport
        {
            public bool isValid;
            public int criticalIssues;
            public int warnings;
            public float overallScore;
            public string timestamp;
            public SystemStatus[] systemStatuses;
        }
        
        [System.Serializable]
        public struct SystemStatus
        {
            public string systemName;
            public bool isActive;
            public bool isOptimized;
            public float performanceImpact;
            public string[] dependencies;
            public string status;
        }
        
        [System.Serializable]
        public struct ValidationIssue
        {
            public string category;
            public string description;
            public string systemName;
            public IssueSeverity severity;
            public string solution;
        }
        
        public enum IssueSeverity
        {
            Critical,
            Warning,
            Info
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (validateOnStart)
            {
                StartCoroutine(PerformComprehensiveValidation());
            }
            
            if (enableContinuousValidation)
            {
                InvokeRepeating(nameof(QuickValidationCheck), validationInterval, validationInterval);
            }
        }
        
        private System.Collections.IEnumerator PerformComprehensiveValidation()
        {
            Debug.Log("🔍 Starting comprehensive system validation...");
            
            criticalIssues.Clear();
            warnings.Clear();
            
            // Wait for systems to initialize
            yield return new WaitForSeconds(2f);
            
            // Validate core systems
            yield return StartCoroutine(ValidateCoreSystems());
            
            // Validate menu systems
            yield return StartCoroutine(ValidateMenuSystems());
            
            // Validate performance systems
            yield return StartCoroutine(ValidatePerformanceSystems());
            
            // Validate integration
            yield return StartCoroutine(ValidateSystemIntegration());
            
            // Generate report
            GenerateValidationReport();
            
            Debug.Log($"✅ Validation complete - Score: {currentReport.overallScore:F1}/100");
        }
        
        private System.Collections.IEnumerator ValidateCoreSystems()
        {
            Debug.Log("🔍 Validating core systems...");
            
            // OptimizedUpdateManager
            ValidateOptimizedUpdateManager();
            yield return null;
            
            // CachedReferenceManager
            ValidateCachedReferenceManager();
            yield return null;
            
            // SceneAssetManager
            ValidateSceneAssetManager();
            yield return null;
            
            // Unity6FeatureIntegrator
            ValidateUnity6FeatureIntegrator();
            yield return null;
            
            // CriticalSystemIntegrator
            ValidateCriticalSystemIntegrator();
            yield return null;
        }
        
        private void ValidateOptimizedUpdateManager()
        {
            var updateManager = OptimizedUpdateManager.Instance;
            if (updateManager == null)
            {
                AddCriticalIssue("OptimizedUpdateManager", "OptimizedUpdateManager not found", 
                    "Create OptimizedUpdateManager instance");
                return;
            }
            
            // Check if systems are registered
            int managedSystems = updateManager.TotalManagedSystems;
            if (managedSystems == 0)
            {
                AddWarning("OptimizedUpdateManager", "No systems registered with OptimizedUpdateManager", 
                    "Register systems with OptimizedUpdateManager");
            }
            
            // Check for remaining individual Update() methods
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            int individualUpdates = 0;
            
            foreach (var mono in allMonoBehaviours)
            {
                if (mono is IOptimizedUpdatable) continue;
                if (mono.GetComponent<LegacyUpdateDisabled>() != null) continue;
                
                var updateMethod = mono.GetType().GetMethod("Update", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (updateMethod != null && updateMethod.DeclaringType == mono.GetType())
                {
                    individualUpdates++;
                    if (individualUpdates <= 5) // Log first 5 for debugging
                    {
                        AddWarning("Performance", $"Individual Update() method in {mono.GetType().Name}", 
                            "Convert to IOptimizedUpdatable");
                    }
                }
            }
            
            if (individualUpdates > maxActiveUpdate)
            {
                AddCriticalIssue("Performance", $"{individualUpdates} individual Update() methods found (max: {maxActiveUpdate})", 
                    "Convert systems to use OptimizedUpdateManager");
            }
        }
        
        private void ValidateCachedReferenceManager()
        {
            var cacheManager = CachedReferenceManager.Instance;
            if (cacheManager == null)
            {
                AddCriticalIssue("CachedReferenceManager", "CachedReferenceManager not found", 
                    "Create CachedReferenceManager instance");
                return;
            }
            
            // Check cache utilization
            int cacheSize = cacheManager.GetCacheSize();
            if (cacheSize == 0)
            {
                AddWarning("CachedReferenceManager", "No cached references found", 
                    "Cache frequently accessed components");
            }
        }
        
        private void ValidateSceneAssetManager()
        {
            var sceneManager = SceneAssetManager.Instance;
            if (sceneManager == null)
            {
                AddCriticalIssue("SceneAssetManager", "SceneAssetManager not found", 
                    "Create SceneAssetManager instance");
                return;
            }
            
            // Check if scenes are properly configured
            if (!sceneManager.AreSceneAssetsConfigured())
            {
                AddWarning("SceneAssetManager", "Scene assets not properly configured", 
                    "Configure scene asset references");
            }
        }
        
        private void ValidateUnity6FeatureIntegrator()
        {
            var unity6Integrator = Unity6FeatureIntegrator.Instance;
            if (unity6Integrator == null)
            {
                AddWarning("Unity6FeatureIntegrator", "Unity6FeatureIntegrator not found", 
                    "Create Unity6FeatureIntegrator for modern Unity features");
            }
        }
        
        private void ValidateCriticalSystemIntegrator()
        {
            var systemIntegrator = CriticalSystemIntegrator.Instance;
            if (systemIntegrator == null)
            {
                AddCriticalIssue("CriticalSystemIntegrator", "CriticalSystemIntegrator not found", 
                    "Create CriticalSystemIntegrator instance");
                return;
            }
            
            if (!systemIntegrator.AreSystemsInitialized())
            {
                AddWarning("CriticalSystemIntegrator", "Systems not fully initialized", 
                    "Wait for system initialization to complete");
            }
        }
        
        private System.Collections.IEnumerator ValidateMenuSystems()
        {
            Debug.Log("🔍 Validating menu systems...");
            
            // Check for menu conflicts
            var enhancedMenu = CachedReferenceManager.Get<EnhancedMainMenuSystem>();
            var optimizedMenu = CachedReferenceManager.Get<EnhancedMainMenuSystemOptimized>();
            var basicMenu = CachedReferenceManager.Get<MainMenuSystem>();
            
            int menuCount = 0;
            if (enhancedMenu != null) menuCount++;
            if (optimizedMenu != null) menuCount++;
            if (basicMenu != null) menuCount++;
            
            if (menuCount > 1)
            {
                AddWarning("MenuSystems", $"Multiple menu systems found ({menuCount})", 
                    "Use only one menu system to avoid conflicts");
            }
            
            if (menuCount == 0)
            {
                AddCriticalIssue("MenuSystems", "No menu system found", 
                    "Add a menu system to the scene");
            }
            
            yield return null;
        }
        
        private System.Collections.IEnumerator ValidatePerformanceSystems()
        {
            Debug.Log("🔍 Validating performance systems...");
            
            // Check VR Performance Monitor
            var perfMonitor = VRPerformanceMonitor.Instance;
            if (perfMonitor == null)
            {
                AddWarning("Performance", "VRPerformanceMonitor not found", 
                    "Add VRPerformanceMonitor for performance tracking");
            }
            
            // Check Object Pool Manager
            var poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                AddWarning("Performance", "ObjectPoolManager not found", 
                    "Add ObjectPoolManager for object pooling");
            }
            
            // Check Material Pool
            var materialPool = MaterialPool.Instance;
            if (materialPool == null)
            {
                AddWarning("Performance", "MaterialPool not found", 
                    "Add MaterialPool for material management");
            }
            
            yield return null;
        }
        
        private System.Collections.IEnumerator ValidateSystemIntegration()
        {
            Debug.Log("🔍 Validating system integration...");
            
            // Check if systems are properly connected
            ValidateGameManagerIntegration();
            yield return null;
            
            ValidateSceneSystemIntegration();
            yield return null;
            
            ValidateAudioSystemIntegration();
            yield return null;
            
            ValidateInputSystemIntegration();
            yield return null;
        }
        
        private void ValidateGameManagerIntegration()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                AddCriticalIssue("GameManager", "GameManager not found", 
                    "Create GameManager instance");
                return;
            }
            
            // Check event system
            if (gameManager.OnGameStateChanged == null)
            {
                AddWarning("GameManager", "OnGameStateChanged event not initialized", 
                    "Initialize event system in GameManager");
            }
        }
        
        private void ValidateSceneSystemIntegration()
        {
            var sceneLoader = SceneLoadingManager.Instance;
            var sceneTransform = SceneTransformationSystem.Instance;
            var sceneSense = EnhancedSceneSenseSystem.Instance;
            
            if (sceneLoader == null)
            {
                AddWarning("SceneSystem", "SceneLoadingManager not found", 
                    "Add SceneLoadingManager for scene management");
            }
            
            if (sceneTransform == null)
            {
                AddWarning("SceneSystem", "SceneTransformationSystem not found", 
                    "Add SceneTransformationSystem for scene effects");
            }
            
            if (sceneSense == null)
            {
                AddWarning("SceneSystem", "EnhancedSceneSenseSystem not found", 
                    "Add EnhancedSceneSenseSystem for immersive environments");
            }
        }
        
        private void ValidateAudioSystemIntegration()
        {
            var audioManager = AdvancedAudioManager.Instance;
            if (audioManager == null)
            {
                AddWarning("AudioSystem", "AdvancedAudioManager not found", 
                    "Add AdvancedAudioManager for audio management");
            }
        }
        
        private void ValidateInputSystemIntegration()
        {
            var handTracking = HandTrackingManager.Instance;
            var hapticManager = HapticFeedbackManager.Instance;
            
            if (handTracking == null)
            {
                AddWarning("InputSystem", "HandTrackingManager not found", 
                    "Add HandTrackingManager for hand tracking");
            }
            
            if (hapticManager == null)
            {
                AddWarning("InputSystem", "HapticFeedbackManager not found", 
                    "Add HapticFeedbackManager for haptic feedback");
            }
        }
        
        private void QuickValidationCheck()
        {
            // Quick performance check
            float currentFrameTime = Time.unscaledDeltaTime;
            if (currentFrameTime > maxFrameTime)
            {
                Debug.LogWarning($"⚠️ Frame time exceeded threshold: {currentFrameTime * 1000:F2}ms (max: {maxFrameTime * 1000:F1}ms)");
            }
        }
        
        private void AddCriticalIssue(string category, string description, string solution)
        {
            criticalIssues.Add(new ValidationIssue
            {
                category = category,
                description = description,
                systemName = category,
                severity = IssueSeverity.Critical,
                solution = solution
            });
        }
        
        private void AddWarning(string category, string description, string solution)
        {
            warnings.Add(new ValidationIssue
            {
                category = category,
                description = description,
                systemName = category,
                severity = IssueSeverity.Warning,
                solution = solution
            });
        }
        
        private void GenerateValidationReport()
        {
            // Calculate overall score
            float score = 100f;
            score -= criticalIssues.Count * 20f; // -20 points per critical issue
            score -= warnings.Count * 5f; // -5 points per warning
            score = Mathf.Max(0f, score);
            
            currentReport = new ValidationReport
            {
                isValid = criticalIssues.Count == 0,
                criticalIssues = criticalIssues.Count,
                warnings = warnings.Count,
                overallScore = score,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                systemStatuses = GetSystemStatuses()
            };
            
            // Log results
            LogValidationResults();
        }
        
        private SystemStatus[] GetSystemStatuses()
        {
            var statuses = new List<SystemStatus>();
            
            // Core systems
            AddSystemStatus(statuses, "OptimizedUpdateManager", OptimizedUpdateManager.Instance != null);
            AddSystemStatus(statuses, "CachedReferenceManager", CachedReferenceManager.Instance != null);
            AddSystemStatus(statuses, "SceneAssetManager", SceneAssetManager.Instance != null);
            AddSystemStatus(statuses, "CriticalSystemIntegrator", CriticalSystemIntegrator.Instance != null);
            
            // Game systems
            AddSystemStatus(statuses, "GameManager", GameManager.Instance != null);
            AddSystemStatus(statuses, "AdvancedAudioManager", AdvancedAudioManager.Instance != null);
            AddSystemStatus(statuses, "HandTrackingManager", HandTrackingManager.Instance != null);
            
            return statuses.ToArray();
        }
        
        private void AddSystemStatus(List<SystemStatus> statuses, string systemName, bool isActive)
        {
            statuses.Add(new SystemStatus
            {
                systemName = systemName,
                isActive = isActive,
                isOptimized = true, // Assume optimized for now
                performanceImpact = 0f,
                dependencies = new string[0],
                status = isActive ? "Active" : "Missing"
            });
        }
        
        private void LogValidationResults()
        {
            Debug.Log("📊 SYSTEM VALIDATION REPORT");
            Debug.Log($"Overall Score: {currentReport.overallScore:F1}/100");
            Debug.Log($"Critical Issues: {criticalIssues.Count}");
            Debug.Log($"Warnings: {warnings.Count}");
            
            if (criticalIssues.Count > 0)
            {
                Debug.LogError("❌ CRITICAL ISSUES:");
                foreach (var issue in criticalIssues)
                {
                    Debug.LogError($"  - {issue.category}: {issue.description}");
                    Debug.LogError($"    Solution: {issue.solution}");
                }
            }
            
            if (warnings.Count > 0)
            {
                Debug.LogWarning("⚠️ WARNINGS:");
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"  - {warning.category}: {warning.description}");
                    Debug.LogWarning($"    Solution: {warning.solution}");
                }
            }
            
            if (criticalIssues.Count == 0 && warnings.Count == 0)
            {
                Debug.Log("✅ All systems validated successfully!");
            }
        }
        
        // Public API
        public ValidationReport GetLastValidationReport()
        {
            return currentReport;
        }
        
        public void ForceValidation()
        {
            StartCoroutine(PerformComprehensiveValidation());
        }
        
        public bool HasCriticalIssues()
        {
            return criticalIssues.Count > 0;
        }
        
        public List<ValidationIssue> GetCriticalIssues()
        {
            return new List<ValidationIssue>(criticalIssues);
        }
        
        public List<ValidationIssue> GetWarnings()
        {
            return new List<ValidationIssue>(warnings);
        }
    }
} 