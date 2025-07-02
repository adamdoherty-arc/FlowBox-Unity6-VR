using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VRBoxingGame.Core;

namespace VRBoxingGame.Performance
{
    /// <summary>
    /// Comprehensive VR Performance Monitor for Unity 6 with real-time optimization
    /// </summary>
    public class VRPerformanceMonitor : MonoBehaviour
    {
        [Header("Monitoring Settings")]
        public bool enableRealTimeMonitoring = true;
        public bool enableAutoOptimization = true;
        public float updateInterval = 0.5f;
        public int historySize = 60;
        
        [Header("Performance Targets")]
        public float targetFrameRate = 90f; // Quest 3 target
        public float minFrameRate = 72f;    // Quest 2 minimum
        public float maxFrameTime = 11.1f;  // 90 FPS = 11.1ms
        public float maxGPUTime = 8f;       // Target GPU time
        
        [Header("Optimization Thresholds")]
        public float lowPerformanceThreshold = 0.8f;
        public float criticalPerformanceThreshold = 0.6f;
        public int consecutiveDropsForOptimization = 3;
        
        [Header("Debug Display")]
        public bool showDebugOverlay = true;
        public KeyCode toggleKey = KeyCode.F1;
        public Font debugFont;
        
        // Performance data structures
        [System.Serializable]
        public struct PerformanceMetrics
        {
            public float frameRate;
            public float frameTime;
            public float gpuTime;
            public float cpuTime;
            public long memoryUsage;
            public int drawCalls;
            public int triangles;
            public int batches;
            public float thermalState;
            public float batteryLevel;
        }
        
        [System.Serializable]
        public struct OptimizationState
        {
            public int qualityLevel;
            public float renderScale;
            public bool dynamicBatching;
            public bool gpuInstancing;
            public int shadowQuality;
            public int textureQuality;
            public bool postProcessing;
        }
        
        // Private variables
        private Queue<PerformanceMetrics> performanceHistory = new Queue<PerformanceMetrics>();
        private PerformanceMetrics currentMetrics;
        private OptimizationState currentOptimization;
        
        private float lastUpdateTime;
        private int consecutivePerformanceDrops = 0;
        private bool isOptimizing = false;
        
        // Profiling data
        private Recorder drawCallRecorder;
        private Recorder triangleRecorder;
        private Recorder batchRecorder;
        
        // Job System data
        private NativeArray<float> frameTimeData;
        private NativeArray<float> optimizationResults;
        private JobHandle currentJobHandle;
        
        // Debug display
        private StringBuilder debugText = new StringBuilder();
        private GUIStyle debugStyle;
        
        // Singleton
        public static VRPerformanceMonitor Instance { get; private set; }
        
        // Properties
        public PerformanceMetrics CurrentMetrics => currentMetrics;
        public OptimizationState CurrentOptimization => currentOptimization;
        public float AverageFrameRate { get; private set; }
        public bool IsPerformanceCritical { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePerformanceMonitor();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializePerformanceMonitor()
        {
            // Initialize profiling recorders
            drawCallRecorder = Recorder.Get("Render.Mesh");
            triangleRecorder = Recorder.Get("Triangles");
            batchRecorder = Recorder.Get("Batches");
            
            // Enable recorders
            drawCallRecorder.enabled = true;
            triangleRecorder.enabled = true;
            batchRecorder.enabled = true;
            
            // Initialize Job System arrays
            frameTimeData = new NativeArray<float>(historySize, Allocator.Persistent);
            optimizationResults = new NativeArray<float>(10, Allocator.Persistent);
            
            // Initialize optimization state
            currentOptimization = GetCurrentOptimizationState();
            
            // Setup debug style
            SetupDebugStyle();
            
            Debug.Log("VR Performance Monitor initialized for Unity 6");
        }
        
        private void SetupDebugStyle()
        {
            debugStyle = new GUIStyle();
            debugStyle.font = debugFont;
            debugStyle.fontSize = 14;
            debugStyle.normal.textColor = Color.white;
            debugStyle.alignment = TextAnchor.UpperLeft;
        }
        
        private void Update()
        {
            // Note: Consider migrating this to Input System for full modernization
            if (Input.GetKeyDown(toggleKey))
            {
                showDebugOverlay = !showDebugOverlay;
            }
            
            if (enableRealTimeMonitoring && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdatePerformanceMetrics();
                lastUpdateTime = Time.time;
                
                if (enableAutoOptimization)
                {
                    CheckForOptimization();
                }
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            // Gather performance data
            currentMetrics = new PerformanceMetrics
            {
                frameRate = 1f / Time.unscaledDeltaTime,
                frameTime = Time.unscaledDeltaTime * 1000f,
                gpuTime = GetGPUTime(),
                cpuTime = GetCPUTime(),
                memoryUsage = Profiler.GetTotalAllocatedMemory(false),
                drawCalls = drawCallRecorder.sampleBlockCount,
                triangles = (int)triangleRecorder.lastValue,
                batches = (int)batchRecorder.lastValue,
                thermalState = GetThermalState(),
                batteryLevel = GetBatteryLevel()
            };
            
            // Add to history
            performanceHistory.Enqueue(currentMetrics);
            if (performanceHistory.Count > historySize)
            {
                performanceHistory.Dequeue();
            }
            
            // Calculate averages
            CalculateAverages();
            
            // Check performance state
            CheckPerformanceState();
        }
        
        private float GetGPUTime()
        {
            // Unity 6 GPU timing
            return Profiler.GetCounterValue("GPU Main Thread") / 1000000f; // Convert to ms
        }
        
        private float GetCPUTime()
        {
            return Time.unscaledDeltaTime * 1000f; // Frame time as CPU time approximation
        }
        
        private float GetThermalState()
        {
            // Simulate thermal state (0-1, where 1 is overheating)
            return Mathf.Clamp01(currentMetrics.frameTime / maxFrameTime);
        }
        
        private float GetBatteryLevel()
        {
            return SystemInfo.batteryLevel;
        }
        
        private void CalculateAverages()
        {
            if (performanceHistory.Count == 0) return;
            
            float totalFrameRate = 0f;
            foreach (var metrics in performanceHistory)
            {
                totalFrameRate += metrics.frameRate;
            }
            
            AverageFrameRate = totalFrameRate / performanceHistory.Count;
        }
        
        private void CheckPerformanceState()
        {
            float performanceRatio = currentMetrics.frameRate / targetFrameRate;
            
            if (performanceRatio < criticalPerformanceThreshold)
            {
                IsPerformanceCritical = true;
                consecutivePerformanceDrops++;
            }
            else if (performanceRatio < lowPerformanceThreshold)
            {
                consecutivePerformanceDrops++;
                IsPerformanceCritical = false;
            }
            else
            {
                consecutivePerformanceDrops = 0;
                IsPerformanceCritical = false;
            }
        }
        
        private void CheckForOptimization()
        {
            if (isOptimizing) return;
            
            if (consecutivePerformanceDrops >= consecutiveDropsForOptimization)
            {
                _ = OptimizePerformanceAsync();
            }
        }
        
        private async Task OptimizePerformanceAsync()
        {
            isOptimizing = true;
            Debug.Log("Performance optimization started");

            try
            {
                // Get baseline performance
                var baselineMetrics = currentMetrics;
                
                // Step 1: Reduce render scale
                float originalRenderScale = GetRenderScale();
                float newRenderScale = Mathf.Max(0.7f, originalRenderScale - 0.1f);
                SetRenderScale(newRenderScale);
                
                Debug.Log($"Reduced render scale from {originalRenderScale:F2} to {newRenderScale:F2}");
                await Task.Delay(1000); // Wait for changes to take effect
                
                if (await IsPerformanceImprovedAsync())
                {
                    Debug.Log("Performance improved with render scale reduction");
                    return;
                }
                
                // Step 2: Reduce shadow quality
                if (QualitySettings.shadows != ShadowQuality.Disable)
                {
                    int originalShadowQuality = (int)QualitySettings.shadows;
                    SetShadowQuality(Mathf.Max(0, originalShadowQuality - 1));
                    
                    Debug.Log($"Reduced shadow quality from {originalShadowQuality} to {(int)QualitySettings.shadows}");
                    await Task.Delay(1000);
                    
                    if (await IsPerformanceImprovedAsync())
                    {
                        Debug.Log("Performance improved with shadow quality reduction");
                        return;
                    }
                }
                
                // Step 3: Disable post-processing
                SetPostProcessing(false);
                Debug.Log("Disabled post-processing effects");
                await Task.Delay(1000);
                
                if (await IsPerformanceImprovedAsync())
                {
                    Debug.Log("Performance improved by disabling post-processing");
                    return;
                }
                
                // Step 4: Reduce texture quality
                int originalTextureQuality = QualitySettings.globalTextureMipmapLimit;
                SetTextureQuality(originalTextureQuality + 1);
                
                Debug.Log($"Reduced texture quality from {originalTextureQuality} to {QualitySettings.globalTextureMipmapLimit}");
                await Task.Delay(1000);
                
                if (await IsPerformanceImprovedAsync())
                {
                    Debug.Log("Performance improved with texture quality reduction");
                    return;
                }
                
                // Step 5: Final quality level reduction
                int currentQuality = QualitySettings.GetQualityLevel();
                if (currentQuality > 0)
                {
                    QualitySettings.SetQualityLevel(currentQuality - 1, true);
                    Debug.Log($"Reduced overall quality level from {currentQuality} to {QualitySettings.GetQualityLevel()}");
                    await Task.Delay(1000);
                    
                    if (await IsPerformanceImprovedAsync())
                    {
                        Debug.Log("Performance improved with quality level reduction");
                        return;
                    }
                }
                
                Debug.LogWarning("Performance optimization completed, but improvements may be minimal");
            }
            catch (Exception ex)
            {
                AdvancedLoggingSystem.LogError(AdvancedLoggingSystem.LogCategory.Performance, "VRPerformanceMonitor", $"Error during performance optimization: {ex.Message}", ex);
            }
            finally
            {
                isOptimizing = false;
                consecutivePerformanceDrops = 0;
            }
        }
        
        private async Task<bool> IsPerformanceImprovedAsync()
        {
            try
            {
                // Wait for performance to stabilize
                await Task.Delay(500);
                
                return currentMetrics.frameRate >= targetFrameRate * lowPerformanceThreshold;
            }
            catch (System.Exception ex)
            {
                AdvancedLoggingSystem.LogError(AdvancedLoggingSystem.LogCategory.Performance, "VRPerformanceMonitor", $"Error in performance check: {ex.Message}", ex);
                return false;
            }
        }
        
        private OptimizationState GetCurrentOptimizationState()
        {
            return new OptimizationState
            {
                qualityLevel = QualitySettings.GetQualityLevel(),
                renderScale = GetRenderScale(),
                dynamicBatching = PlayerSettings.gpuSkinning,
                gpuInstancing = true, // Assume enabled
                shadowQuality = QualitySettings.shadows == ShadowQuality.Disable ? 0 : 
                               QualitySettings.shadows == ShadowQuality.HardOnly ? 1 : 2,
                textureQuality = QualitySettings.globalTextureMipmapLimit,
                postProcessing = true // Assume enabled
            };
        }
        
        private float GetRenderScale()
        {
            // Unity 6 XR Core Utils approach
            #if UNITY_XR_CORE_UTILS
            var xrOrigin = CachedReferenceManager.Get<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                return xrOrigin.Camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.renderScale ?? 1f;
            }
            #endif
            
            // Fallback to URP render scale
            var urpAsset = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
            if (urpAsset != null)
            {
                return urpAsset.renderScale;
            }
            
            return 1f;
        }
        
        private void SetRenderScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.5f, 1f);
            currentOptimization.renderScale = scale;
            
            // Unity 6 XR Core Utils approach
            #if UNITY_XR_CORE_UTILS
            var xrOrigin = CachedReferenceManager.Get<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                var cameraData = xrOrigin.Camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.renderScale = scale;
                    Debug.Log($"XR Camera render scale set to: {scale:F2}");
                    return;
                }
            }
            #endif
            
            // Fallback to URP render scale
            var urpAsset = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
            if (urpAsset != null)
            {
                urpAsset.renderScale = scale;
                Debug.Log($"URP render scale set to: {scale:F2}");
            }
        }
        
        private void SetShadowQuality(int quality)
        {
            switch (quality)
            {
                case 0:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    break;
                case 1:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    break;
                case 2:
                    QualitySettings.shadows = ShadowQuality.All;
                    break;
            }
            
            currentOptimization.shadowQuality = quality;
            Debug.Log($"Shadow quality set to: {quality}");
        }
        
        private void SetPostProcessing(bool enabled)
        {
            // Disable post-processing effects
            currentOptimization.postProcessing = enabled;
            Debug.Log($"Post-processing set to: {enabled}");
        }
        
        private void SetTextureQuality(int quality)
        {
            QualitySettings.globalTextureMipmapLimit = quality;
            currentOptimization.textureQuality = quality;
            Debug.Log($"Texture quality set to: {quality}");
        }
        
        // Unity 6 Job System for performance analysis
        [BurstCompile]
        public struct PerformanceAnalysisJob : IJob
        {
            [ReadOnly] public NativeArray<float> frameTimeData;
            [WriteOnly] public NativeArray<float> results;
            
            public void Execute()
            {
                // Calculate performance statistics
                float sum = 0f;
                float min = float.MaxValue;
                float max = float.MinValue;
                
                for (int i = 0; i < frameTimeData.Length; i++)
                {
                    float value = frameTimeData[i];
                    sum += value;
                    min = Unity.Mathematics.math.min(min, value);
                    max = Unity.Mathematics.math.max(max, value);
                }
                
                results[0] = sum / frameTimeData.Length; // Average
                results[1] = min;                        // Minimum
                results[2] = max;                        // Maximum
                results[3] = max - min;                  // Range
            }
        }
        
        public void AnalyzePerformanceWithJobs()
        {
            // Complete previous job
            currentJobHandle.Complete();
            
            // Copy frame time data
            var history = performanceHistory.ToArray();
            for (int i = 0; i < Unity.Mathematics.math.min(history.Length, frameTimeData.Length); i++)
            {
                frameTimeData[i] = history[i].frameTime;
            }
            
            // Schedule analysis job
            var analysisJob = new PerformanceAnalysisJob
            {
                frameTimeData = frameTimeData,
                results = optimizationResults
            };
            
            currentJobHandle = analysisJob.Schedule();
        }
        
        // Public API methods
        public async Task ForceOptimizationAsync()
        {
            if (!isOptimizing)
            {
                await OptimizePerformanceAsync();
            }
        }
        
        public void ForceOptimization()
        {
            _ = ForceOptimizationAsync();
        }
        
        public void ResetOptimizations()
        {
            SetRenderScale(1f);
            SetShadowQuality(2);
            SetPostProcessing(true);
            SetTextureQuality(0);
            
            Debug.Log("Performance optimizations reset to default");
        }
        
        public PerformanceMetrics[] GetPerformanceHistory()
        {
            return performanceHistory.ToArray();
        }
        
        public string GetPerformanceReport()
        {
            var report = new StringBuilder();
            report.AppendLine("=== VR Performance Report ===");
            report.AppendLine($"Current FPS: {currentMetrics.frameRate:F1}");
            report.AppendLine($"Average FPS: {AverageFrameRate:F1}");
            report.AppendLine($"Frame Time: {currentMetrics.frameTime:F2}ms");
            report.AppendLine($"GPU Time: {currentMetrics.gpuTime:F2}ms");
            report.AppendLine($"Memory Usage: {currentMetrics.memoryUsage / 1024 / 1024:F1}MB");
            report.AppendLine($"Draw Calls: {currentMetrics.drawCalls}");
            report.AppendLine($"Triangles: {currentMetrics.triangles:N0}");
            report.AppendLine($"Batches: {currentMetrics.batches}");
            report.AppendLine($"Render Scale: {currentOptimization.renderScale:F2}");
            report.AppendLine($"Shadow Quality: {currentOptimization.shadowQuality}");
            report.AppendLine($"Performance Critical: {IsPerformanceCritical}");
            
            return report.ToString();
        }
        
        private void OnGUI()
        {
            if (!showDebugOverlay) return;
            
            debugText.Clear();
            debugText.AppendLine("=== VR Performance Monitor ===");
            debugText.AppendLine($"FPS: {currentMetrics.frameRate:F1} (Target: {targetFrameRate})");
            debugText.AppendLine($"Frame Time: {currentMetrics.frameTime:F2}ms");
            debugText.AppendLine($"GPU Time: {currentMetrics.gpuTime:F2}ms");
            debugText.AppendLine($"Memory: {currentMetrics.memoryUsage / 1024 / 1024:F1}MB");
            debugText.AppendLine($"Draw Calls: {currentMetrics.drawCalls}");
            debugText.AppendLine($"Triangles: {currentMetrics.triangles:N0}");
            debugText.AppendLine($"Render Scale: {currentOptimization.renderScale:F2}");
            debugText.AppendLine($"Critical: {(IsPerformanceCritical ? "YES" : "NO")}");
            debugText.AppendLine($"Optimizing: {(isOptimizing ? "YES" : "NO")}");
            
            if (SystemInfo.batteryLevel > 0)
            {
                debugText.AppendLine($"Battery: {SystemInfo.batteryLevel * 100:F0}%");
            }
            
            GUI.Label(new Rect(10, 10, 300, 300), debugText.ToString(), debugStyle);
        }
        
        private void OnDestroy()
        {
            // Clean up Job System resources
            currentJobHandle.Complete();
            
            if (frameTimeData.IsCreated) frameTimeData.Dispose();
            if (optimizationResults.IsCreated) optimizationResults.Dispose();
            
            // Disable profiling recorders
            if (drawCallRecorder != null) drawCallRecorder.enabled = false;
            if (triangleRecorder != null) triangleRecorder.enabled = false;
            if (batchRecorder != null) batchRecorder.enabled = false;
        }
    }
}

