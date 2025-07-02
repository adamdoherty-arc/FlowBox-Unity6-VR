using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Text;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Performance
{
    /// <summary>
    /// Unity 6 Advanced Profiler Integration System
    /// Features: Custom profiler markers, memory tracking, performance analytics
    /// </summary>
    public class AdvancedProfilerIntegration : MonoBehaviour
    {
        [Header("Profiler Configuration")]
        public bool enableCustomMarkers = true;
        public bool enableMemoryTracking = true;
        public bool enablePerformanceAnalytics = true;
        public bool enableFrameTimeAnalysis = true;
        
        [Header("Sampling Settings")]
        public int maxSamples = 1000;
        public float samplingInterval = 0.1f;
        public bool enableContinuousSampling = true;
        public bool enableVRSpecificMetrics = true;
        
        // Custom Profiler Markers
        private CustomSampler boxingFormSampler;
        private CustomSampler aiInferenceSampler;
        private CustomSampler renderingOptimizationSampler;
        private CustomSampler nativeCollectionsSampler;
        private CustomSampler addressableStreamingSampler;
        
        // Performance Data
        private NativeArray<float> frameTimeHistory;
        private NativeArray<float> memoryUsageHistory;
        private NativeArray<int> drawCallHistory;
        private NativeArray<float> vrFrameTimeHistory;
        
        // Analytics
        private PerformanceMetrics currentMetrics;
        private float lastSampleTime = 0f;
        private int sampleIndex = 0;
        
        // Singleton
        public static AdvancedProfilerIntegration Instance { get; private set; }
        
        // Properties
        public PerformanceMetrics CurrentMetrics => currentMetrics;
        public float AverageFrameTime => CalculateAverageFrameTime();
        public float AverageMemoryUsage => CalculateAverageMemoryUsage();
        
        [System.Serializable]
        public struct PerformanceMetrics
        {
            public float frameTime;
            public float memoryUsage;
            public int drawCalls;
            public float cpuTime;
            public float gpuTime;
            public float vrFrameTime;
            public int activeObjects;
            public float aiInferenceTime;
            public float renderOptimizationTime;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeProfiler();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeProfiler()
        {
            Debug.Log("📊 Initializing Advanced Profiler Integration...");
            
            // Initialize custom samplers
            if (enableCustomMarkers)
            {
                boxingFormSampler = CustomSampler.Create("VRBoxing.FormAnalysis");
                aiInferenceSampler = CustomSampler.Create("VRBoxing.AIInference");
                renderingOptimizationSampler = CustomSampler.Create("VRBoxing.RenderOptimization");
                nativeCollectionsSampler = CustomSampler.Create("VRBoxing.NativeCollections");
                addressableStreamingSampler = CustomSampler.Create("VRBoxing.AddressableStreaming");
            }
            
            // Initialize native arrays
            frameTimeHistory = new NativeArray<float>(maxSamples, Allocator.Persistent);
            memoryUsageHistory = new NativeArray<float>(maxSamples, Allocator.Persistent);
            drawCallHistory = new NativeArray<int>(maxSamples, Allocator.Persistent);
            vrFrameTimeHistory = new NativeArray<float>(maxSamples, Allocator.Persistent);
            
            Debug.Log("✅ Advanced Profiler Integration initialized!");
        }
        
        private void Update()
        {
            if (enableContinuousSampling && Time.time - lastSampleTime >= samplingInterval)
            {
                SamplePerformanceMetrics();
                lastSampleTime = Time.time;
            }
        }
        
        private void SamplePerformanceMetrics()
        {
            // Sample current performance metrics
            currentMetrics = new PerformanceMetrics
            {
                frameTime = Time.deltaTime * 1000f, // Convert to milliseconds
                memoryUsage = GetMemoryUsage(),
                drawCalls = GetDrawCalls(),
                cpuTime = GetCPUTime(),
                gpuTime = GetGPUTime(),
                vrFrameTime = GetVRFrameTime(),
                activeObjects = GetActiveObjectCount(),
                aiInferenceTime = GetAIInferenceTime(),
                renderOptimizationTime = GetRenderOptimizationTime()
            };
            
            // Store in history arrays
            int index = sampleIndex % maxSamples;
            frameTimeHistory[index] = currentMetrics.frameTime;
            memoryUsageHistory[index] = currentMetrics.memoryUsage;
            drawCallHistory[index] = currentMetrics.drawCalls;
            vrFrameTimeHistory[index] = currentMetrics.vrFrameTime;
            
            sampleIndex++;
        }
        
        private float GetMemoryUsage()
        {
            if (enableMemoryTracking)
            {
                return Profiler.GetTotalAllocatedMemory(0) / (1024f * 1024f); // MB
            }
            return 0f;
        }
        
        private int GetDrawCalls()
        {
            return UnityEngine.Rendering.DebugManager.instance != null ? 0 : 100; // Placeholder
        }
        
        private float GetCPUTime()
        {
            return Profiler.GetMonoUsedSize() / (1024f * 1024f); // Placeholder
        }
        
        private float GetGPUTime()
        {
            return 0f; // Would need GPU profiler integration
        }
        
        private float GetVRFrameTime()
        {
            if (enableVRSpecificMetrics)
            {
                return Time.deltaTime * 1000f; // VR-specific frame time
            }
            return 0f;
        }
        
        private int GetActiveObjectCount()
        {
            return FindObjectsOfType<GameObject>().Length;
        }
        
        private float GetAIInferenceTime()
        {
            if (UnitySentisAISystem.Instance != null)
            {
                return UnitySentisAISystem.Instance.AverageInferenceTime * 1000f;
            }
            return 0f;
        }
        
        private float GetRenderOptimizationTime()
        {
            if (ComputeShaderRenderingSystem.Instance != null)
            {
                return ComputeShaderRenderingSystem.Instance.CullingPerformance * 1000f;
            }
            return 0f;
        }
        
        private float CalculateAverageFrameTime()
        {
            if (sampleIndex == 0) return 0f;
            
            float total = 0f;
            int count = math.min(sampleIndex, maxSamples);
            
            for (int i = 0; i < count; i++)
            {
                total += frameTimeHistory[i];
            }
            
            return total / count;
        }
        
        private float CalculateAverageMemoryUsage()
        {
            if (sampleIndex == 0) return 0f;
            
            float total = 0f;
            int count = math.min(sampleIndex, maxSamples);
            
            for (int i = 0; i < count; i++)
            {
                total += memoryUsageHistory[i];
            }
            
            return total / count;
        }
        
        // Public API
        public void BeginSample(string sampleName)
        {
            if (enableCustomMarkers)
            {
                Profiler.BeginSample(sampleName);
            }
        }
        
        public void EndSample()
        {
            if (enableCustomMarkers)
            {
                Profiler.EndSample();
            }
        }
        
        public void BeginBoxingFormSample()
        {
            if (enableCustomMarkers && boxingFormSampler != null)
            {
                boxingFormSampler.Begin();
            }
        }
        
        public void EndBoxingFormSample()
        {
            if (enableCustomMarkers && boxingFormSampler != null)
            {
                boxingFormSampler.End();
            }
        }
        
        public void BeginAIInferenceSample()
        {
            if (enableCustomMarkers && aiInferenceSampler != null)
            {
                aiInferenceSampler.Begin();
            }
        }
        
        public void EndAIInferenceSample()
        {
            if (enableCustomMarkers && aiInferenceSampler != null)
            {
                aiInferenceSampler.End();
            }
        }
        
        public void BeginRenderOptimizationSample()
        {
            if (enableCustomMarkers && renderingOptimizationSampler != null)
            {
                renderingOptimizationSampler.Begin();
            }
        }
        
        public void EndRenderOptimizationSample()
        {
            if (enableCustomMarkers && renderingOptimizationSampler != null)
            {
                renderingOptimizationSampler.End();
            }
        }
        
        public string GeneratePerformanceReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VR Boxing Performance Report ===");
            sb.AppendLine($"Average Frame Time: {AverageFrameTime:F2}ms");
            sb.AppendLine($"Average Memory Usage: {AverageMemoryUsage:F2}MB");
            sb.AppendLine($"Current Draw Calls: {currentMetrics.drawCalls}");
            sb.AppendLine($"Active Objects: {currentMetrics.activeObjects}");
            sb.AppendLine($"AI Inference Time: {currentMetrics.aiInferenceTime:F2}ms");
            sb.AppendLine($"Render Optimization Time: {currentMetrics.renderOptimizationTime:F2}ms");
            
            if (enableVRSpecificMetrics)
            {
                sb.AppendLine($"VR Frame Time: {currentMetrics.vrFrameTime:F2}ms");
                sb.AppendLine($"VR Performance Rating: {CalculateVRPerformanceRating()}");
            }
            
            return sb.ToString();
        }
        
        private string CalculateVRPerformanceRating()
        {
            float avgFrameTime = AverageFrameTime;
            
            if (avgFrameTime < 11.1f) return "Excellent (90+ FPS)";
            if (avgFrameTime < 13.9f) return "Good (72+ FPS)";
            if (avgFrameTime < 16.7f) return "Acceptable (60+ FPS)";
            return "Poor (<60 FPS)";
        }
        
        public ProfilerAnalytics GetAnalytics()
        {
            return new ProfilerAnalytics
            {
                averageFrameTime = AverageFrameTime,
                averageMemoryUsage = AverageMemoryUsage,
                peakFrameTime = GetPeakFrameTime(),
                peakMemoryUsage = GetPeakMemoryUsage(),
                samplesCollected = math.min(sampleIndex, maxSamples),
                vrPerformanceRating = CalculateVRPerformanceRating()
            };
        }
        
        private float GetPeakFrameTime()
        {
            float peak = 0f;
            int count = math.min(sampleIndex, maxSamples);
            
            for (int i = 0; i < count; i++)
            {
                if (frameTimeHistory[i] > peak)
                {
                    peak = frameTimeHistory[i];
                }
            }
            
            return peak;
        }
        
        private float GetPeakMemoryUsage()
        {
            float peak = 0f;
            int count = math.min(sampleIndex, maxSamples);
            
            for (int i = 0; i < count; i++)
            {
                if (memoryUsageHistory[i] > peak)
                {
                    peak = memoryUsageHistory[i];
                }
            }
            
            return peak;
        }
        
        private void OnDestroy()
        {
            // Dispose native arrays
            if (frameTimeHistory.IsCreated) frameTimeHistory.Dispose();
            if (memoryUsageHistory.IsCreated) memoryUsageHistory.Dispose();
            if (drawCallHistory.IsCreated) drawCallHistory.Dispose();
            if (vrFrameTimeHistory.IsCreated) vrFrameTimeHistory.Dispose();
        }
        
        [System.Serializable]
        public struct ProfilerAnalytics
        {
            public float averageFrameTime;
            public float averageMemoryUsage;
            public float peakFrameTime;
            public float peakMemoryUsage;
            public int samplesCollected;
            public string vrPerformanceRating;
        }
    }
}
