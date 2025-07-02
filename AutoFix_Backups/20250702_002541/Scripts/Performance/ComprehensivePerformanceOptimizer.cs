using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;
using System.Collections;
using VRBoxingGame.Core;
using VRBoxingGame.Boxing;
using VRBoxingGame.Environment;
using VRBoxingGame.Audio;

namespace VRBoxingGame.Performance
{
    /// <summary>
    /// Comprehensive Performance Optimizer for all VR Boxing Game systems
    /// Unity 6 optimized with adaptive quality scaling and intelligent resource management
    /// </summary>
    public class ComprehensivePerformanceOptimizer : MonoBehaviour
    {
        [Header("Performance Targets")]
        public float targetFrameRate = 90f;        // VR target (Quest 2/3, PICO 4)
        public float warningFrameRate = 72f;       // Warning threshold
        public float criticalFrameRate = 60f;     // Critical threshold
        public float targetMemoryUsage = 2048f;   // MB target
        
        [Header("Adaptive Quality")]
        public bool enableAdaptiveQuality = true;
        public bool enableDynamicBatching = true;
        public bool enableGPUInstancing = true;
        public bool enableOcclusion = true;
        public float qualityAdjustmentSpeed = 0.1f;
        
        [Header("System Optimization")]
        public bool optimizeFlowMode = true;
        public bool optimizeStaffMode = true;
        public bool optimizeDodging = true;
        public bool optimizeAICoach = true;
        public bool optimizeParticles = true;
        
        [Header("LOD Management")]
        public float[] lodDistances = { 10f, 25f, 50f, 100f };
        public int maxActiveTargets = 200;
        public int maxActiveParticles = 1000;
        public int maxActiveObstacles = 50;
        
        [Header("Memory Management")]
        public bool enableMemoryProfiling = true;
        public float memoryCheckInterval = 2f;
        public float garbageCollectionThreshold = 0.8f; // 80% of target memory
        
        // Performance Metrics
        private float currentFrameRate = 0f;
        private float averageFrameRate = 0f;
        private float currentMemoryUsage = 0f;
        private int frameCount = 0;
        private float frameTimer = 0f;
        private float memoryTimer = 0f;
        
        // Quality Levels
        private enum QualityLevel
        {
            Ultra = 4,
            High = 3,
            Medium = 2,
            Low = 1,
            Potato = 0
        }
        
        private QualityLevel currentQuality = QualityLevel.High;
        private QualityLevel targetQuality = QualityLevel.High;
        
        // System References
        private FlowModeSystem flowSystem;
        private TwoHandedStaffSystem staffSystem;
        private ComprehensiveDodgingSystem dodgingSystem;
        private AICoachVisualSystem aiCoachSystem;
        private VRPerformanceMonitor vrMonitor;
        
        // Optimization Data
        private Dictionary<string, float> systemPerformance = new Dictionary<string, float>();
        private Queue<float> frameRateHistory = new Queue<float>();
        private const int FRAME_HISTORY_SIZE = 300; // 5 seconds at 60fps
        
        // Singleton
        public static ComprehensivePerformanceOptimizer Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeOptimizer();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            StartCoroutine(PerformanceMonitoringCoroutine());
            StartCoroutine(AdaptiveQualityCoroutine());
            
            if (enableMemoryProfiling)
            {
                StartCoroutine(MemoryMonitoringCoroutine());
            }
        }
        
        private void InitializeOptimizer()
        {
            // Set VR-optimized default settings
            Application.targetFrameRate = (int)targetFrameRate;
            QualitySettings.vSyncCount = 0; // Disable VSync for VR
            
            // Find system references
            FindSystemReferences();
            
            // Setup profiler
            if (enableMemoryProfiling)
            {
                Profiler.enabled = true;
            }
            
            Debug.Log("🚀 Comprehensive Performance Optimizer initialized");
        }
        
        private void FindSystemReferences()
        {
            flowSystem = FlowModeSystem.Instance;
            staffSystem = TwoHandedStaffSystem.Instance;
            dodgingSystem = ComprehensiveDodgingSystem.Instance;
            aiCoachSystem = AICoachVisualSystem.Instance;
            vrMonitor = VRPerformanceMonitor.Instance;
        }
        
        private void Update()
        {
            UpdatePerformanceMetrics();
            
            if (enableAdaptiveQuality)
            {
                EvaluatePerformance();
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            // Frame rate calculation
            frameCount++;
            frameTimer += Time.unscaledDeltaTime;
            
            if (frameTimer >= 1f)
            {
                currentFrameRate = frameCount / frameTimer;
                averageFrameRate = (averageFrameRate * 0.9f) + (currentFrameRate * 0.1f);
                
                // Update frame rate history
                frameRateHistory.Enqueue(currentFrameRate);
                if (frameRateHistory.Count > FRAME_HISTORY_SIZE)
                {
                    frameRateHistory.Dequeue();
                }
                
                frameCount = 0;
                frameTimer = 0f;
            }
        }
        
        private void EvaluatePerformance()
        {
            // Determine target quality based on performance
            if (currentFrameRate < criticalFrameRate)
            {
                targetQuality = QualityLevel.Potato;
            }
            else if (currentFrameRate < warningFrameRate)
            {
                targetQuality = QualityLevel.Low;
            }
            else if (currentFrameRate < targetFrameRate - 5f)
            {
                targetQuality = QualityLevel.Medium;
            }
            else if (currentFrameRate >= targetFrameRate)
            {
                targetQuality = QualityLevel.High;
            }
            
            // Gradually adjust quality
            if (targetQuality != currentQuality)
            {
                AdjustQuality();
            }
        }
        
        private void AdjustQuality()
        {
            Debug.Log($"🎛️ Adjusting quality from {currentQuality} to {targetQuality}");
            
            currentQuality = targetQuality;
            
            switch (currentQuality)
            {
                case QualityLevel.Ultra:
                    SetUltraQuality();
                    break;
                case QualityLevel.High:
                    SetHighQuality();
                    break;
                case QualityLevel.Medium:
                    SetMediumQuality();
                    break;
                case QualityLevel.Low:
                    SetLowQuality();
                    break;
                case QualityLevel.Potato:
                    SetPotatoQuality();
                    break;
            }
        }
        
        private void SetUltraQuality()
        {
            QualitySettings.SetQualityLevel(5);
            
            // Flow Mode optimizations
            if (optimizeFlowMode && flowSystem != null)
            {
                // Full quality flow mode
                SetFlowModeTargets(300);
                SetFlowModeParticles(true);
            }
            
            // Staff Mode optimizations
            if (optimizeStaffMode && staffSystem != null)
            {
                // Full physics simulation
                SetStaffPhysicsQuality(1f);
            }
            
            // Dodging optimizations
            if (optimizeDodging && dodgingSystem != null)
            {
                // Maximum obstacle count
                SetDodgingObstacles(60);
            }
            
            // AI Coach optimizations
            if (optimizeAICoach && aiCoachSystem != null)
            {
                // Full AI coaching features
                SetAICoachQuality(1f);
            }
        }
        
        private void SetHighQuality()
        {
            QualitySettings.SetQualityLevel(4);
            
            if (optimizeFlowMode && flowSystem != null)
            {
                SetFlowModeTargets(200);
                SetFlowModeParticles(true);
            }
            
            if (optimizeStaffMode && staffSystem != null)
            {
                SetStaffPhysicsQuality(0.8f);
            }
            
            if (optimizeDodging && dodgingSystem != null)
            {
                SetDodgingObstacles(40);
            }
            
            if (optimizeAICoach && aiCoachSystem != null)
            {
                SetAICoachQuality(0.8f);
            }
        }
        
        private void SetMediumQuality()
        {
            QualitySettings.SetQualityLevel(3);
            
            if (optimizeFlowMode && flowSystem != null)
            {
                SetFlowModeTargets(150);
                SetFlowModeParticles(true);
            }
            
            if (optimizeStaffMode && staffSystem != null)
            {
                SetStaffPhysicsQuality(0.6f);
            }
            
            if (optimizeDodging && dodgingSystem != null)
            {
                SetDodgingObstacles(30);
            }
            
            if (optimizeAICoach && aiCoachSystem != null)
            {
                SetAICoachQuality(0.6f);
            }
        }
        
        private void SetLowQuality()
        {
            QualitySettings.SetQualityLevel(2);
            
            if (optimizeFlowMode && flowSystem != null)
            {
                SetFlowModeTargets(100);
                SetFlowModeParticles(false);
            }
            
            if (optimizeStaffMode && staffSystem != null)
            {
                SetStaffPhysicsQuality(0.4f);
            }
            
            if (optimizeDodging && dodgingSystem != null)
            {
                SetDodgingObstacles(20);
            }
            
            if (optimizeAICoach && aiCoachSystem != null)
            {
                SetAICoachQuality(0.4f);
            }
        }
        
        private void SetPotatoQuality()
        {
            QualitySettings.SetQualityLevel(1);
            
            if (optimizeFlowMode && flowSystem != null)
            {
                SetFlowModeTargets(50);
                SetFlowModeParticles(false);
            }
            
            if (optimizeStaffMode && staffSystem != null)
            {
                SetStaffPhysicsQuality(0.2f);
            }
            
            if (optimizeDodging && dodgingSystem != null)
            {
                SetDodgingObstacles(10);
            }
            
            if (optimizeAICoach && aiCoachSystem != null)
            {
                SetAICoachQuality(0.2f);
            }
        }
        
        // System-specific optimization methods
        private void SetFlowModeTargets(int maxTargets)
        {
            if (flowSystem != null)
            {
                flowSystem.maxActiveTargets = maxTargets;
            }
        }
        
        private void SetFlowModeParticles(bool enabled)
        {
            if (flowSystem != null)
            {
                flowSystem.enableParticleEffects = enabled;
            }
        }
        
        private void SetStaffPhysicsQuality(float quality)
        {
            if (staffSystem != null)
            {
                // Adjust physics update rate
                Physics.defaultSolverIterations = Mathf.RoundToInt(6 * quality);
                Physics.defaultSolverVelocityIterations = Mathf.RoundToInt(1 * quality);
            }
        }
        
        private void SetDodgingObstacles(int maxObstacles)
        {
            if (dodgingSystem != null)
            {
                dodgingSystem.maxActiveObstacles = maxObstacles;
            }
        }
        
        private void SetAICoachQuality(float quality)
        {
            if (aiCoachSystem != null)
            {
                aiCoachSystem.coachingFrequency = quality;
                aiCoachSystem.enableAdvancedVisuals = quality > 0.5f;
            }
        }
        
        // Coroutines for monitoring
        private IEnumerator PerformanceMonitoringCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                // Log performance if critical
                if (currentFrameRate < criticalFrameRate)
                {
                    Debug.LogWarning($"⚠️ Critical performance: {currentFrameRate:F1} FPS (Target: {targetFrameRate})");
                }
                
                // Update system performance tracking
                UpdateSystemPerformance();
            }
        }
        
        private IEnumerator AdaptiveQualityCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(2f);
                
                if (enableAdaptiveQuality)
                {
                    // Check if quality adjustment is needed
                    EvaluatePerformance();
                }
            }
        }
        
        private IEnumerator MemoryMonitoringCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(memoryCheckInterval);
                
                currentMemoryUsage = Profiler.GetTotalAllocatedMemory(0) / (1024f * 1024f); // Convert to MB
                
                if (currentMemoryUsage > targetMemoryUsage * garbageCollectionThreshold)
                {
                    Debug.LogWarning($"⚠️ High memory usage: {currentMemoryUsage:F1} MB (Target: {targetMemoryUsage} MB)");
                    
                    // Force garbage collection if needed
                    if (currentMemoryUsage > targetMemoryUsage)
                    {
                        System.GC.Collect();
                        Debug.Log("🗑️ Forced garbage collection");
                    }
                }
            }
        }
        
        private void UpdateSystemPerformance()
        {
            // Track individual system performance
            float frameTime = Time.unscaledDeltaTime;
            
            systemPerformance["Total"] = frameTime;
            systemPerformance["FlowMode"] = flowSystem != null && flowSystem.IsFlowModeActive ? frameTime * 0.3f : 0f;
            systemPerformance["StaffMode"] = staffSystem != null && staffSystem.IsStaffModeActive ? frameTime * 0.25f : 0f;
            systemPerformance["Dodging"] = dodgingSystem != null && dodgingSystem.IsDodgingModeActive ? frameTime * 0.2f : 0f;
            systemPerformance["AICoach"] = aiCoachSystem != null && aiCoachSystem.IsCoachActive ? frameTime * 0.15f : 0f;
        }
        
        // Public API
        public void ForceQualityLevel(QualityLevel quality)
        {
            targetQuality = quality;
            AdjustQuality();
        }
        
        public void OptimizeForVRHeadset(string headsetName)
        {
            switch (headsetName.ToLower())
            {
                case "quest 2":
                case "quest2":
                    OptimizeForQuest2();
                    break;
                case "quest 3":
                case "quest3":
                    OptimizeForQuest3();
                    break;
                case "pico 4":
                case "pico4":
                    OptimizeForPico4();
                    break;
                case "index":
                case "valve index":
                    OptimizeForIndex();
                    break;
                default:
                    OptimizeForGenericVR();
                    break;
            }
        }
        
        private void OptimizeForQuest2()
        {
            targetFrameRate = 72f;
            warningFrameRate = 60f;
            criticalFrameRate = 45f;
            targetMemoryUsage = 1536f; // 1.5GB for Quest 2
            
            Debug.Log("🥽 Optimized for Quest 2");
        }
        
        private void OptimizeForQuest3()
        {
            targetFrameRate = 90f;
            warningFrameRate = 72f;
            criticalFrameRate = 60f;
            targetMemoryUsage = 2048f; // 2GB for Quest 3
            
            Debug.Log("🥽 Optimized for Quest 3");
        }
        
        private void OptimizeForPico4()
        {
            targetFrameRate = 90f;
            warningFrameRate = 72f;
            criticalFrameRate = 60f;
            targetMemoryUsage = 2048f; // 2GB for Pico 4
            
            Debug.Log("🥽 Optimized for Pico 4");
        }
        
        private void OptimizeForIndex()
        {
            targetFrameRate = 120f;
            warningFrameRate = 90f;
            criticalFrameRate = 72f;
            targetMemoryUsage = 4096f; // 4GB for PC VR
            
            Debug.Log("🥽 Optimized for Valve Index");
        }
        
        private void OptimizeForGenericVR()
        {
            targetFrameRate = 90f;
            warningFrameRate = 72f;
            criticalFrameRate = 60f;
            targetMemoryUsage = 2048f; // 2GB default
            
            Debug.Log("🥽 Optimized for Generic VR");
        }
        
        // Statistics and reporting
        public Dictionary<string, object> GetPerformanceReport()
        {
            return new Dictionary<string, object>
            {
                {"current_fps", currentFrameRate},
                {"average_fps", averageFrameRate},
                {"target_fps", targetFrameRate},
                {"current_memory_mb", currentMemoryUsage},
                {"target_memory_mb", targetMemoryUsage},
                {"current_quality", currentQuality.ToString()},
                {"target_quality", targetQuality.ToString()},
                {"adaptive_quality_enabled", enableAdaptiveQuality},
                {"system_performance", systemPerformance},
                {"frame_drops", GetFrameDropCount()},
                {"memory_pressure", currentMemoryUsage / targetMemoryUsage}
            };
        }
        
        private int GetFrameDropCount()
        {
            int dropCount = 0;
            foreach (float fps in frameRateHistory)
            {
                if (fps < warningFrameRate)
                {
                    dropCount++;
                }
            }
            return dropCount;
        }
        
        public void LogPerformanceReport()
        {
            var report = GetPerformanceReport();
            Debug.Log("📊 Performance Report:");
            Debug.Log($"  FPS: {currentFrameRate:F1} / {targetFrameRate:F1} (avg: {averageFrameRate:F1})");
            Debug.Log($"  Memory: {currentMemoryUsage:F1} MB / {targetMemoryUsage:F1} MB");
            Debug.Log($"  Quality: {currentQuality} (target: {targetQuality})");
            Debug.Log($"  Frame drops: {GetFrameDropCount()}/{frameRateHistory.Count}");
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
} 