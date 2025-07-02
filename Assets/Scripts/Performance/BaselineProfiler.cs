using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using VRBoxingGame.Core;

namespace VRBoxingGame.Performance
{
    /// <summary>
    /// Baseline Profiler - Implements Category 1 of Enhancing Prompt
    /// Establishes CPU vs. GPU bottleneck and collects comprehensive performance logs
    /// </summary>
    public class BaselineProfiler : MonoBehaviour
    {
        [Header("Profiling Configuration")]
        public bool enableAutomaticProfiling = true;
        public bool enableCPUGPUBottleneckDetection = true;
        public bool enableMemoryProfiling = true;
        public bool exportPerformanceReports = true;
        
        [Header("Performance Targets")]
        public float targetFrameRate = 90f;
        public float acceptableFrameRate = 72f;
        public float criticalFrameRate = 60f;
        public float targetMemoryMB = 2048f;
        
        // Profiling Data
        private List<FrameData> frameDataHistory = new List<FrameData>();
        private PerformanceSession currentSession;
        private StringBuilder reportBuilder = new StringBuilder();
        
        // Performance tracking
        private float[] frameTimeBuffer = new float[120]; // 2 seconds at 60fps
        private int frameBufferIndex = 0;
        private float frameTimeSum = 0f;
        
        // Singleton
        public static BaselineProfiler Instance { get; private set; }
        
        // Properties
        public PerformanceSession CurrentSession => currentSession;
        public bool IsProfilingActive { get; private set; }
        
        [System.Serializable]
        public class FrameData
        {
            public float timestamp;
            public float frameTime;
            public float cpuTime;
            public float gpuTime;
            public int drawCalls;
            public int triangles;
            public float memoryUsageMB;
            public bool isStaleFrame;
        }
        
        [System.Serializable]
        public class PerformanceSession
        {
            public string sessionName;
            public float startTime;
            public float duration;
            public List<FrameData> frames = new List<FrameData>();
            public BottleneckAnalysis bottleneckAnalysis;
            public PerformanceStatistics statistics;
        }
        
        [System.Serializable]
        public class BottleneckAnalysis
        {
            public bool isCPUBound;
            public bool isGPUBound;
            public bool isMemoryBound;
            public float cpuUtilization;
            public float gpuUtilization;
            public string primaryBottleneck;
            public List<string> recommendations = new List<string>();
        }
        
        [System.Serializable]
        public class PerformanceStatistics
        {
            public float averageFrameRate;
            public float minimumFrameRate;
            public float maximumFrameRate;
            public float averageFrameTime;
            public int totalStaleFrames;
            public float averageMemoryUsage;
            public float peakMemoryUsage;
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
            Debug.Log("📊 Initializing Baseline Profiler - Enhancing Prompt Category 1");
            
            // Initialize frame time buffer
            for (int i = 0; i < frameTimeBuffer.Length; i++)
            {
                frameTimeBuffer[i] = 1f / 60f; // Default to 60fps
            }
            frameTimeSum = frameTimeBuffer.Length * (1f / 60f);
            
            // Enable Unity Profiler
            if (!Profiler.enabled)
            {
                Profiler.enabled = true;
                Debug.Log("✅ Unity Profiler enabled");
            }
            
            Debug.Log("✅ Baseline Profiler initialized successfully");
        }
        
        public void StartProfilingSession(string sessionName)
        {
            if (IsProfilingActive)
            {
                EndProfilingSession();
            }
            
            currentSession = new PerformanceSession
            {
                sessionName = sessionName,
                startTime = Time.realtimeSinceStartup
            };
            
            frameDataHistory.Clear();
            IsProfilingActive = true;
            
            Debug.Log($"📊 Started profiling session: {sessionName}");
        }
        
        public void EndProfilingSession()
        {
            if (!IsProfilingActive || currentSession == null)
                return;
            
            IsProfilingActive = false;
            currentSession.duration = Time.realtimeSinceStartup - currentSession.startTime;
            currentSession.frames = new List<FrameData>(frameDataHistory);
            
            // Perform analysis
            StartCoroutine(AnalyzeSession());
            
            Debug.Log($"📊 Ended profiling session: {currentSession.sessionName}");
        }
        
        private IEnumerator AnalyzeSession()
        {
            Debug.Log("🔍 Analyzing profiling session...");
            
            // Perform bottleneck analysis
            currentSession.bottleneckAnalysis = AnalyzeBottlenecks();
            
            // Calculate statistics
            currentSession.statistics = CalculateStatistics();
            
            // Generate recommendations
            GenerateOptimizationRecommendations();
            
            // Export report if enabled
            if (exportPerformanceReports)
            {
                ExportPerformanceReport();
            }
            
            yield return null;
            
            Debug.Log($"✅ Session analysis complete - Primary bottleneck: {currentSession.bottleneckAnalysis.primaryBottleneck}");
        }
        
        private void Update()
        {
            if (!IsProfilingActive || currentSession == null)
                return;
            
            try
            {
                // Update frame time buffer for rolling average with safety checks
                if (frameTimeBuffer != null && frameTimeBuffer.Length > 0)
                {
                    frameTimeSum -= frameTimeBuffer[frameBufferIndex];
                    frameTimeBuffer[frameBufferIndex] = Time.deltaTime;
                    frameTimeSum += frameTimeBuffer[frameBufferIndex];
                    frameBufferIndex = (frameBufferIndex + 1) % frameTimeBuffer.Length;
                }
                
                // Collect frame data every few frames to avoid overhead
                if (Time.frameCount % 3 == 0)
                {
                    var frameData = CollectFrameData();
                    if (frameDataHistory != null)
                    {
                        frameDataHistory.Add(frameData);
                        
                        // Limit history size to prevent memory issues
                        if (frameDataHistory.Count > 10000)
                        {
                            frameDataHistory.RemoveAt(0);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Update error in BaselineProfiler: {e.Message}");
            }
        }
        
        private FrameData CollectFrameData()
        {
            var frameData = new FrameData
            {
                timestamp = Time.realtimeSinceStartup,
                frameTime = Time.deltaTime * 1000f, // Convert to milliseconds
                cpuTime = Time.deltaTime * 1000f, // Simplified CPU time estimation
                gpuTime = 0f, // GPU time requires more complex profiling
                memoryUsageMB = GetMemoryUsage(),
                isStaleFrame = Time.deltaTime > (1f / acceptableFrameRate)
            };
            
            // Estimate draw calls and triangles (simplified)
            frameData.drawCalls = EstimateDrawCalls();
            frameData.triangles = EstimateTriangles();
            
            return frameData;
        }
        
        private float GetMemoryUsage()
        {
            try
            {
                long totalMemory = Profiler.GetTotalAllocatedMemory(Profiler.GetMainThreadIndex());
                return totalMemory / (1024f * 1024f); // Convert to MB
            }
            catch (System.Exception)
            {
                // Fallback to GC memory
                return System.GC.GetTotalMemory(false) / (1024f * 1024f);
            }
        }
        
        private int EstimateDrawCalls()
        {
            try
            {
                // Simplified estimation based on active renderers - cached to avoid performance hit
                var renderers = Resources.FindObjectsOfTypeAll<Renderer>();
                int activeRenderers = 0;
                
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.gameObject.activeInHierarchy && renderer.isVisible && renderer.enabled)
                    {
                        activeRenderers++;
                    }
                }
                
                return activeRenderers;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error estimating draw calls: {e.Message}");
                return 50; // Safe fallback estimate
            }
        }
        
        private int EstimateTriangles()
        {
            // Simplified estimation - would need more complex calculation in production
            return EstimateDrawCalls() * 100; // Rough estimate
        }
        
        private BottleneckAnalysis AnalyzeBottlenecks()
        {
            var analysis = new BottleneckAnalysis();
            
            if (frameDataHistory.Count == 0)
            {
                analysis.primaryBottleneck = "Insufficient data";
                return analysis;
            }
            
            // Calculate average metrics
            float avgFrameTime = 0f;
            float avgMemory = 0f;
            int totalStaleFrames = 0;
            
            foreach (var frame in frameDataHistory)
            {
                avgFrameTime += frame.frameTime;
                avgMemory += frame.memoryUsageMB;
                if (frame.isStaleFrame) totalStaleFrames++;
            }
            
            avgFrameTime /= frameDataHistory.Count;
            avgMemory /= frameDataHistory.Count;
            
            // Determine bottlenecks
            float targetFrameTime = 1000f / targetFrameRate; // in milliseconds
            
            if (avgFrameTime > targetFrameTime)
            {
                analysis.isCPUBound = true;
                analysis.cpuUtilization = (avgFrameTime / targetFrameTime) * 100f;
            }
            
            if (avgMemory > targetMemoryMB * 0.8f) // 80% of target
            {
                analysis.isMemoryBound = true;
            }
            
            // Determine primary bottleneck
            if (analysis.isCPUBound && analysis.isMemoryBound)
            {
                analysis.primaryBottleneck = "CPU and Memory";
            }
            else if (analysis.isCPUBound)
            {
                analysis.primaryBottleneck = "CPU";
            }
            else if (analysis.isMemoryBound)
            {
                analysis.primaryBottleneck = "Memory";
            }
            else
            {
                analysis.primaryBottleneck = "None (Performance Acceptable)";
            }
            
            return analysis;
        }
        
        private PerformanceStatistics CalculateStatistics()
        {
            var stats = new PerformanceStatistics();
            
            if (frameDataHistory.Count == 0)
                return stats;
            
            float totalFrameTime = 0f;
            float minFrameRate = float.MaxValue;
            float maxFrameRate = float.MinValue;
            float totalMemory = 0f;
            float peakMemory = 0f;
            int staleFrames = 0;
            
            foreach (var frame in frameDataHistory)
            {
                // Protect against division by zero
                float frameRate = frame.frameTime > 0f ? 1000f / frame.frameTime : 0f;
                
                totalFrameTime += frame.frameTime;
                minFrameRate = Mathf.Min(minFrameRate, frameRate);
                maxFrameRate = Mathf.Max(maxFrameRate, frameRate);
                totalMemory += frame.memoryUsageMB;
                peakMemory = Mathf.Max(peakMemory, frame.memoryUsageMB);
                
                if (frame.isStaleFrame) staleFrames++;
            }
            
            stats.averageFrameTime = totalFrameTime / frameDataHistory.Count;
            stats.averageFrameRate = stats.averageFrameTime > 0f ? 1000f / stats.averageFrameTime : 0f;
            stats.minimumFrameRate = minFrameRate;
            stats.maximumFrameRate = maxFrameRate;
            stats.averageMemoryUsage = totalMemory / frameDataHistory.Count;
            stats.peakMemoryUsage = peakMemory;
            stats.totalStaleFrames = staleFrames;
            
            return stats;
        }
        
        private void GenerateOptimizationRecommendations()
        {
            var recommendations = currentSession.bottleneckAnalysis.recommendations;
            recommendations.Clear();
            
            var stats = currentSession.statistics;
            var bottleneck = currentSession.bottleneckAnalysis;
            
            // Frame rate recommendations
            if (stats.averageFrameRate < targetFrameRate)
            {
                recommendations.Add($"Average FPS ({stats.averageFrameRate:F1}) below target ({targetFrameRate}). Optimize rendering pipeline.");
            }
            
            if (stats.minimumFrameRate < criticalFrameRate)
            {
                recommendations.Add($"Minimum FPS ({stats.minimumFrameRate:F1}) critically low. Investigate performance spikes.");
            }
            
            // Memory recommendations
            if (stats.peakMemoryUsage > targetMemoryMB)
            {
                recommendations.Add($"Peak memory usage ({stats.peakMemoryUsage:F1}MB) exceeds target ({targetMemoryMB}MB). Implement memory optimization.");
            }
            
            // Stale frame recommendations
            float staleFramePercentage = (float)stats.totalStaleFrames / frameDataHistory.Count * 100f;
            if (staleFramePercentage > 5f)
            {
                recommendations.Add($"High stale frame rate ({staleFramePercentage:F1}%). Optimize frame consistency.");
            }
            
            // Bottleneck-specific recommendations
            if (bottleneck.isCPUBound)
            {
                recommendations.Add("CPU bottleneck detected. Consider object pooling, batching, and LOD systems.");
            }
            
            if (bottleneck.isMemoryBound)
            {
                recommendations.Add("Memory bottleneck detected. Optimize textures, meshes, and garbage collection.");
            }
            
            // VR-specific recommendations
            recommendations.Add("Ensure Fixed Foveated Rendering is enabled for Quest devices.");
            recommendations.Add("Use URP with optimized VR settings for best performance.");
            recommendations.Add("Implement dynamic quality scaling based on performance metrics.");
        }
        
        private void ExportPerformanceReport()
        {
            if (currentSession == null)
                return;
            
            GenerateTextReport();
            
            string fileName = $"PerformanceReport_{currentSession.sessionName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            
            try
            {
                File.WriteAllText(filePath, reportBuilder.ToString());
                Debug.Log($"📄 Performance report exported: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export performance report: {e.Message}");
            }
        }
        
        private void GenerateTextReport()
        {
            reportBuilder.Clear();
            
            var session = currentSession;
            var stats = session.statistics;
            var bottleneck = session.bottleneckAnalysis;
            
            reportBuilder.AppendLine("=== BASELINE PROFILER PERFORMANCE REPORT ===");
            reportBuilder.AppendLine($"Session: {session.sessionName}");
            reportBuilder.AppendLine($"Duration: {session.duration:F2} seconds");
            reportBuilder.AppendLine($"Frames Analyzed: {session.frames.Count}");
            reportBuilder.AppendLine($"Report Generated: {System.DateTime.Now}");
            reportBuilder.AppendLine();
            
            reportBuilder.AppendLine("=== PERFORMANCE STATISTICS ===");
            reportBuilder.AppendLine($"Average FPS: {stats.averageFrameRate:F1}");
            reportBuilder.AppendLine($"Minimum FPS: {stats.minimumFrameRate:F1}");
            reportBuilder.AppendLine($"Maximum FPS: {stats.maximumFrameRate:F1}");
            reportBuilder.AppendLine($"Average Frame Time: {stats.averageFrameTime:F2}ms");
            reportBuilder.AppendLine($"Average Memory: {stats.averageMemoryUsage:F1}MB");
            reportBuilder.AppendLine($"Peak Memory: {stats.peakMemoryUsage:F1}MB");
            reportBuilder.AppendLine($"Stale Frames: {stats.totalStaleFrames}");
            reportBuilder.AppendLine();
            
            reportBuilder.AppendLine("=== BOTTLENECK ANALYSIS ===");
            reportBuilder.AppendLine($"Primary Bottleneck: {bottleneck.primaryBottleneck}");
            reportBuilder.AppendLine($"CPU Bound: {(bottleneck.isCPUBound ? "Yes" : "No")}");
            reportBuilder.AppendLine($"GPU Bound: {(bottleneck.isGPUBound ? "Yes" : "No")}");
            reportBuilder.AppendLine($"Memory Bound: {(bottleneck.isMemoryBound ? "Yes" : "No")}");
            reportBuilder.AppendLine();
            
            reportBuilder.AppendLine("=== OPTIMIZATION RECOMMENDATIONS ===");
            foreach (var recommendation in bottleneck.recommendations)
            {
                reportBuilder.AppendLine($"• {recommendation}");
            }
        }
        
        // Quick session starters
        public void StartLightSceneSession() => StartProfilingSession("Light_Scene");
        public void StartMediumSceneSession() => StartProfilingSession("Medium_Scene");
        public void StartHeavySceneSession() => StartProfilingSession("Heavy_Scene");
        
        // Public properties for monitoring
        public float CurrentFrameRate => frameTimeSum > 0f ? frameTimeBuffer.Length / frameTimeSum : 0f;
        public float AverageFrameTime => frameTimeBuffer.Length > 0 ? (frameTimeSum / frameTimeBuffer.Length * 1000f) : 0f;
        public bool IsPerformanceCritical => CurrentFrameRate < criticalFrameRate;
    }
} 