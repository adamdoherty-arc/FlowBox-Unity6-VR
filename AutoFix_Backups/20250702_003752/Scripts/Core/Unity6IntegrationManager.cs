using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Boxing;
using VRBoxingGame.Boxing.ECS;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Unity 6 Integration Manager - Coordinates all advanced systems
    /// Provides unified interface for enhanced Unity programming capabilities
    /// </summary>
    public class Unity6IntegrationManager : MonoBehaviour
    {
        [Header("System Integration")]
        public bool enableAdvancedFormProcessor = true;
        public bool enableECSTargetSystem = true;
        public bool enableVRRenderGraph = true;
        public bool enablePerformanceMonitoring = true;
        
        [Header("Performance Optimization")]
        public bool enableJobSystemOptimization = true;
        public bool enableBurstCompilation = true;
        public bool enableNativeCollections = true;
        public bool enableAsyncProcessing = true;
        
        [Header("VR Quality Targets")]
        public int quest3TargetFPS = 90;
        public int quest2TargetFPS = 72;
        public float maxFrameTime = 11.1f; // 90 FPS = 11.1ms
        
        // System references
        private AdvancedBoxingFormProcessor formProcessor;
        private ECSTargetSystem ecsTargetSystem;
        private VRRenderGraphSystem renderGraphSystem;
        private VRPerformanceMonitor performanceMonitor;
        
        // Integration status
        private bool systemsInitialized = false;
        private Dictionary<string, bool> systemStatus = new Dictionary<string, bool>();
        
        // Performance data
        private NativeArray<float> systemPerformanceData;
        private JobHandle integrationJobHandle;
        
        // Singleton
        public static Unity6IntegrationManager Instance { get; private set; }
        
        // Public properties
        public bool SystemsInitialized => systemsInitialized;
        public Dictionary<string, bool> SystemStatus => systemStatus;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _ = InitializeAdvancedSystemsAsync();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private async Task InitializeAdvancedSystemsAsync()
        {
            Debug.Log("🚀 Initializing Unity 6 Advanced Systems...");
            
            try
            {
                // Initialize performance tracking
                systemPerformanceData = new NativeArray<float>(10, Allocator.Persistent);
                
                // Initialize systems in optimal order
                await InitializePerformanceMonitoring();
                await InitializeVRRenderGraph();
                await InitializeAdvancedFormProcessor();
                await InitializeECSTargetSystem();
                
                // Verify system integration
                await ValidateSystemIntegration();
                
                // Apply device-specific optimizations
                ApplyDeviceOptimizations();
                
                systemsInitialized = true;
                Debug.Log("✅ Unity 6 Advanced Systems initialized successfully!");
                
                // Start integration monitoring
                _ = MonitorSystemIntegrationAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize Unity 6 systems: {ex.Message}");
            }
        }
        
        private async Task InitializePerformanceMonitoring()
        {
            if (!enablePerformanceMonitoring) return;
            
            Debug.Log("📊 Initializing Performance Monitor...");
            
            // Find or create performance monitor
            performanceMonitor = VRPerformanceMonitor.Instance;
            if (performanceMonitor == null)
            {
                var perfMonitorGO = new GameObject("VR Performance Monitor");
                performanceMonitor = perfMonitorGO.AddComponent<VRPerformanceMonitor>();
            }
            
            // Configure for Unity 6
            performanceMonitor.enableRealTimeMonitoring = true;
            performanceMonitor.enableAutoOptimization = true;
            performanceMonitor.targetFrameRate = quest3TargetFPS;
            
            systemStatus["PerformanceMonitor"] = true;
            await Task.Yield();
        }
        
        private async Task InitializeVRRenderGraph()
        {
            if (!enableVRRenderGraph) return;
            
            Debug.Log("🎮 Initializing VR Render Graph...");
            
            // Create render graph system
            var renderGraphGO = new GameObject("VR Render Graph System");
            renderGraphSystem = renderGraphGO.AddComponent<VRRenderGraphSystem>();
            
            // Configure for Unity 6
            renderGraphSystem.enableVROptimizations = true;
            renderGraphSystem.enableDynamicResolution = true;
            renderGraphSystem.targetFrameRate = quest3TargetFPS;
            
            systemStatus["VRRenderGraph"] = true;
            await Task.Yield();
        }
        
        private async Task InitializeAdvancedFormProcessor()
        {
            if (!enableAdvancedFormProcessor) return;
            
            Debug.Log("🥊 Initializing Advanced Form Processor...");
            
            // Create form processor
            var formProcessorGO = new GameObject("Advanced Boxing Form Processor");
            formProcessor = formProcessorGO.AddComponent<AdvancedBoxingFormProcessor>();
            
            // Configure Job System
            formProcessor.enableBurstCompilation = enableBurstCompilation;
            formProcessor.updateFrequency = 30f; // 30 FPS analysis
            
            systemStatus["FormProcessor"] = true;
            await Task.Yield();
        }
        
        private async Task InitializeECSTargetSystem()
        {
            if (!enableECSTargetSystem) return;
            
            Debug.Log("🎯 Initializing ECS Target System...");
            
            // Create ECS system
            var ecsSystemGO = new GameObject("ECS Target System");
            ecsTargetSystem = ecsSystemGO.AddComponent<ECSTargetSystem>();
            
            // Configure ECS
            ecsTargetSystem.maxTargets = 1000;
            ecsTargetSystem.enableBurstCompilation = enableBurstCompilation;
            
            systemStatus["ECSTargetSystem"] = true;
            await Task.Yield();
        }
        
        private async Task ValidateSystemIntegration()
        {
            Debug.Log("🔍 Validating System Integration...");
            
            bool allSystemsValid = true;
            
            // Validate each system
            foreach (var system in systemStatus)
            {
                if (!system.Value)
                {
                    Debug.LogWarning($"⚠️ System {system.Key} failed to initialize");
                    allSystemsValid = false;
                }
            }
            
            // Test inter-system communication
            if (formProcessor != null && ecsTargetSystem != null)
            {
                // Test form processor -> target system integration
                var formData = formProcessor.GetCurrentFormData();
                Debug.Log($"📋 Form-Target integration test: Stance = {formData.stance}");
            }
            
            if (performanceMonitor != null && renderGraphSystem != null)
            {
                // Test performance monitor -> render graph integration
                float currentFPS = 1f / Time.unscaledDeltaTime;
                Debug.Log($"📈 Performance-Render integration test: FPS = {currentFPS:F1}");
            }
            
            if (allSystemsValid)
            {
                Debug.Log("✅ All systems validated successfully");
            }
            else
            {
                Debug.LogError("❌ System validation failed");
            }
            
            await Task.Yield();
        }
        
        private void ApplyDeviceOptimizations()
        {
            string deviceModel = SystemInfo.deviceModel.ToLower();
            
            if (deviceModel.Contains("quest 3"))
            {
                Debug.Log("🥽 Applying Quest 3 optimizations...");
                
                // Quest 3 - High performance settings
                if (renderGraphSystem != null)
                {
                    renderGraphSystem.OptimizeForQuest3();
                }
                
                if (performanceMonitor != null)
                {
                    performanceMonitor.targetFrameRate = quest3TargetFPS;
                }
            }
            else if (deviceModel.Contains("quest 2"))
            {
                Debug.Log("🥽 Applying Quest 2 optimizations...");
                
                // Quest 2 - Balanced performance settings
                if (renderGraphSystem != null)
                {
                    renderGraphSystem.OptimizeForQuest2();
                }
                
                if (performanceMonitor != null)
                {
                    performanceMonitor.targetFrameRate = quest2TargetFPS;
                }
            }
            else
            {
                Debug.Log("🖥️ Applying PC VR optimizations...");
                
                // PC VR - Maximum quality settings
                if (renderGraphSystem != null)
                {
                    renderGraphSystem.SetTargetFrameRate(90);
                    renderGraphSystem.SetRenderScale(1f);
                }
            }
        }
        
        private async Task MonitorSystemIntegrationAsync()
        {
            while (systemsInitialized && this != null)
            {
                // Monitor system performance
                MonitorSystemPerformance();
                
                // Check for system failures
                CheckSystemHealth();
                
                // Optimize inter-system communication
                OptimizeSystemIntegration();
                
                // Wait before next check
                await Task.Delay(1000); // Check every second
            }
        }
        
        private void MonitorSystemPerformance()
        {
            if (!enablePerformanceMonitoring) return;
            
            // Collect performance metrics from each system
            float formProcessorPerf = formProcessor != null ? formProcessor.GetFormScore() : 0f;
            float ecsSystemPerf = ecsTargetSystem != null ? ecsTargetSystem.ActiveTargetCount / 1000f : 0f;
            float renderGraphPerf = renderGraphSystem != null ? renderGraphSystem.GetCurrentRenderScale() : 1f;
            
            // Update performance data
            if (systemPerformanceData.IsCreated && systemPerformanceData.Length >= 3)
            {
                systemPerformanceData[0] = formProcessorPerf;
                systemPerformanceData[1] = ecsSystemPerf;
                systemPerformanceData[2] = renderGraphPerf;
            }
        }
        
        private void CheckSystemHealth()
        {
            // Check if any systems have failed
            bool formProcessorHealthy = formProcessor != null && formProcessor.enabled;
            bool ecsSystemHealthy = ecsTargetSystem != null && ecsTargetSystem.enabled;
            bool renderGraphHealthy = renderGraphSystem != null && renderGraphSystem.enabled;
            bool performanceMonitorHealthy = performanceMonitor != null && performanceMonitor.enabled;
            
            // Update system status
            systemStatus["FormProcessor"] = formProcessorHealthy;
            systemStatus["ECSTargetSystem"] = ecsSystemHealthy;
            systemStatus["VRRenderGraph"] = renderGraphHealthy;
            systemStatus["PerformanceMonitor"] = performanceMonitorHealthy;
            
            // Log any system failures
            foreach (var system in systemStatus)
            {
                if (!system.Value)
                {
                    Debug.LogError($"🚨 System failure detected: {system.Key}");
                }
            }
        }
        
        private void OptimizeSystemIntegration()
        {
            if (!enableJobSystemOptimization) return;
            
            // Schedule integration optimization job
            var optimizationJob = new SystemIntegrationJob
            {
                performanceData = systemPerformanceData,
                targetFrameTime = maxFrameTime,
                optimizationResults = new NativeArray<float>(3, Allocator.TempJob)
            };
            
            integrationJobHandle = optimizationJob.Schedule();
            
            // Complete job and apply results
            integrationJobHandle.Complete();
            
            if (optimizationJob.optimizationResults.IsCreated)
            {
                // Apply optimization results
                float recommendedFormFreq = optimizationJob.optimizationResults[0];
                float recommendedTargetLimit = optimizationJob.optimizationResults[1];
                float recommendedRenderScale = optimizationJob.optimizationResults[2];
                
                // Apply optimizations
                if (formProcessor != null && recommendedFormFreq > 0)
                {
                    formProcessor.updateFrequency = recommendedFormFreq;
                }
                
                if (ecsTargetSystem != null && recommendedTargetLimit > 0)
                {
                    ecsTargetSystem.maxTargets = Mathf.RoundToInt(recommendedTargetLimit);
                }
                
                if (renderGraphSystem != null && recommendedRenderScale > 0)
                {
                    renderGraphSystem.SetRenderScale(recommendedRenderScale);
                }
                
                optimizationJob.optimizationResults.Dispose();
            }
        }
        
        // Public API
        public void ForceSystemOptimization()
        {
            OptimizeSystemIntegration();
        }
        
        public string GetSystemStatusReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Unity 6 System Status ===");
            
            foreach (var system in systemStatus)
            {
                string status = system.Value ? "✅ ACTIVE" : "❌ FAILED";
                report.AppendLine($"{system.Key}: {status}");
            }
            
            if (systemPerformanceData.IsCreated)
            {
                report.AppendLine("\n=== Performance Metrics ===");
                report.AppendLine($"Form Processor: {systemPerformanceData[0]:F2}");
                report.AppendLine($"ECS Targets: {systemPerformanceData[1]:F2}");
                report.AppendLine($"Render Scale: {systemPerformanceData[2]:F2}");
            }
            
            return report.ToString();
        }
        
        public async Task RestartFailedSystemsAsync()
        {
            Debug.Log("🔄 Restarting failed systems...");
            
            foreach (var system in systemStatus)
            {
                if (!system.Value)
                {
                    switch (system.Key)
                    {
                        case "FormProcessor":
                            await InitializeAdvancedFormProcessor();
                            break;
                        case "ECSTargetSystem":
                            await InitializeECSTargetSystem();
                            break;
                        case "VRRenderGraph":
                            await InitializeVRRenderGraph();
                            break;
                        case "PerformanceMonitor":
                            await InitializePerformanceMonitoring();
                            break;
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            // Complete any running jobs
            if (integrationJobHandle.IsCreated)
            {
                integrationJobHandle.Complete();
            }
            
            // Dispose native arrays
            if (systemPerformanceData.IsCreated)
            {
                systemPerformanceData.Dispose();
            }
        }
    }
    
    /// <summary>
    /// Burst-compiled job for system integration optimization
    /// </summary>
    [BurstCompile]
    public struct SystemIntegrationJob : IJob
    {
        [ReadOnly] public NativeArray<float> performanceData;
        [ReadOnly] public float targetFrameTime;
        [WriteOnly] public NativeArray<float> optimizationResults;
        
        public void Execute()
        {
            if (performanceData.Length < 3 || optimizationResults.Length < 3) return;
            
            float formPerf = performanceData[0];
            float ecsPerf = performanceData[1];
            float renderPerf = performanceData[2];
            
            // Calculate optimization recommendations
            float recommendedFormFreq = math.lerp(20f, 40f, 1f - formPerf);
            float recommendedTargetLimit = math.lerp(500f, 1500f, 1f - ecsPerf);
            float recommendedRenderScale = math.clamp(renderPerf * 1.1f, 0.7f, 1f);
            
            optimizationResults[0] = recommendedFormFreq;
            optimizationResults[1] = recommendedTargetLimit;
            optimizationResults[2] = recommendedRenderScale;
        }
    }
} 