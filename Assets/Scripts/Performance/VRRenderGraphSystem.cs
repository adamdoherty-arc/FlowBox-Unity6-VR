using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace VRBoxingGame.Performance
{
    /// <summary>
    /// Unity 6 Render Graph System optimized for VR Boxing
    /// Provides advanced rendering optimizations for Quest 2/3
    /// </summary>
    public class VRRenderGraphSystem : MonoBehaviour
    {
        [Header("VR Optimization Settings")]
        public bool enableVROptimizations = true;
        public bool enableFoveatedRendering = true;
        public bool enableEarlyZTest = true;
        public bool enableGPUInstancing = true;
        
        [Header("Performance Targets")]
        public int targetFrameRate = 90;
        public float renderScale = 1f;
        public bool enableDynamicResolution = true;
        
        [Header("Quality Settings")]
        public int maxShadowDistance = 10;
        public ShadowQuality shadowQuality = ShadowQuality.All;
        public bool enablePostProcessing = true;
        
        // Render Graph components
        private UniversalRenderPipelineAsset urpAsset;
        private RenderPipelineAsset originalRenderPipeline;
        
        // Performance tracking
        private NativeArray<float> frameTimeHistory;
        private int frameIndex = 0;
        private float averageFrameTime = 0f;
        
        // VR Camera references
        private Camera[] vrCameras;
        private UniversalAdditionalCameraData[] vrCameraData;
        
        // Singleton
        public static VRRenderGraphSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeRenderGraph();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeRenderGraph()
        {
            // Initialize frame time tracking
            frameTimeHistory = new NativeArray<float>(60, Allocator.Persistent);
            
            // Get URP asset
            urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            originalRenderPipeline = GraphicsSettings.renderPipelineAsset;
            
            // Setup VR cameras
            SetupVRCameras();
            
            // Apply VR optimizations
            if (enableVROptimizations)
            {
                ApplyVROptimizations();
            }
            
            Debug.Log("🎮 VR Render Graph System initialized for Unity 6");
        }
        
        private void SetupVRCameras()
        {
            // Find VR cameras
            var allCameras = FindObjectsOfType<Camera>();
            var vrCameraList = new System.Collections.Generic.List<Camera>();
            var vrCameraDataList = new System.Collections.Generic.List<UniversalAdditionalCameraData>();
            
            foreach (var camera in allCameras)
            {
                if (camera.gameObject.name.Contains("XR") || 
                    camera.gameObject.name.Contains("VR") ||
                    camera.cameraType == CameraType.VR)
                {
                    vrCameraList.Add(camera);
                    
                    var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                    if (cameraData == null)
                    {
                        cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    }
                    vrCameraDataList.Add(cameraData);
                }
            }
            
            vrCameras = vrCameraList.ToArray();
            vrCameraData = vrCameraDataList.ToArray();
        }
        
        private void ApplyVROptimizations()
        {
            if (urpAsset == null) return;
            
            // Core VR optimizations
            urpAsset.renderScale = renderScale;
            urpAsset.shadowDistance = maxShadowDistance;
            urpAsset.cascadeCount = 1; // Single cascade for VR
            urpAsset.msaaSampleCount = 4; // 4x MSAA for VR
            
            // Configure each VR camera
            foreach (var cameraData in vrCameraData)
            {
                if (cameraData == null) continue;
                
                // Enable VR-specific optimizations
                cameraData.renderScale = renderScale;
                cameraData.renderType = CameraRenderType.Base;
                
                // Configure render features
                cameraData.renderPostProcessing = enablePostProcessing;
                cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
                
                // GPU instancing
                if (enableGPUInstancing)
                {
                    cameraData.renderShadows = true;
                    cameraData.requiresDepthTexture = true;
                    cameraData.requiresColorTexture = false;
                }
            }
            
            // Apply system-wide optimizations
            QualitySettings.vSyncCount = 0; // VR handles its own sync
            Application.targetFrameRate = targetFrameRate;
            
            Debug.Log($"✅ VR optimizations applied - Target FPS: {targetFrameRate}");
        }
        
        private void Update()
        {
            if (enableDynamicResolution)
            {
                UpdateDynamicResolution();
            }
            
            TrackPerformance();
        }
        
        private void UpdateDynamicResolution()
        {
            // Track current frame time
            float currentFrameTime = Time.unscaledDeltaTime;
            frameTimeHistory[frameIndex] = currentFrameTime;
            frameIndex = (frameIndex + 1) % frameTimeHistory.Length;
            
            // Calculate average frame time
            float sum = 0f;
            for (int i = 0; i < frameTimeHistory.Length; i++)
            {
                sum += frameTimeHistory[i];
            }
            averageFrameTime = sum / frameTimeHistory.Length;
            
            // Adjust render scale based on performance
            float targetFrameTime = 1f / targetFrameRate;
            float performanceRatio = averageFrameTime / targetFrameTime;
            
            if (performanceRatio > 1.1f) // Performance is 10% worse than target
            {
                // Reduce render scale
                renderScale = math.max(0.7f, renderScale - 0.05f);
                ApplyRenderScale();
            }
            else if (performanceRatio < 0.9f) // Performance is 10% better than target
            {
                // Increase render scale
                renderScale = math.min(1f, renderScale + 0.02f);
                ApplyRenderScale();
            }
        }
        
        private void ApplyRenderScale()
        {
            if (urpAsset != null)
            {
                urpAsset.renderScale = renderScale;
            }
            
            foreach (var cameraData in vrCameraData)
            {
                if (cameraData != null)
                {
                    cameraData.renderScale = renderScale;
                }
            }
        }
        
        private void TrackPerformance()
        {
            // Send performance data to VR Performance Monitor
            if (VRPerformanceMonitor.Instance != null)
            {
                float currentFPS = 1f / Time.unscaledDeltaTime;
                VRPerformanceMonitor.Instance.OnPerformanceUpdate?.Invoke(currentFPS);
            }
        }
        
        // Public API
        public void SetRenderScale(float scale)
        {
            renderScale = math.clamp(scale, 0.5f, 1f);
            ApplyRenderScale();
        }
        
        public void EnableFoveatedRendering(bool enabled)
        {
            enableFoveatedRendering = enabled;
            // Implementation would depend on VR SDK
        }
        
        public void SetTargetFrameRate(int fps)
        {
            targetFrameRate = fps;
            Application.targetFrameRate = fps;
        }
        
        public float GetCurrentRenderScale()
        {
            return renderScale;
        }
        
        public float GetAverageFrameTime()
        {
            return averageFrameTime;
        }
        
        public void OptimizeForQuest2()
        {
            targetFrameRate = 72;
            renderScale = 0.9f;
            shadowQuality = ShadowQuality.HardOnly;
            enablePostProcessing = false;
            ApplyVROptimizations();
        }
        
        public void OptimizeForQuest3()
        {
            targetFrameRate = 90;
            renderScale = 1f;
            shadowQuality = ShadowQuality.All;
            enablePostProcessing = true;
            ApplyVROptimizations();
        }
        
        private void OnDestroy()
        {
            if (frameTimeHistory.IsCreated)
            {
                frameTimeHistory.Dispose();
            }
        }
    }
    
    /// <summary>
    /// Burst-compiled job for VR rendering calculations
    /// </summary>
    [BurstCompile]
    public struct VRRenderingJob : IJob
    {
        [ReadOnly] public NativeArray<float> frameTimeData;
        [WriteOnly] public NativeArray<float> renderScaleResults;
        public float targetFrameRate;
        
        public void Execute()
        {
            // Calculate optimal render scale
            float sum = 0f;
            for (int i = 0; i < frameTimeData.Length; i++)
            {
                sum += frameTimeData[i];
            }
            
            float averageFrameTime = sum / frameTimeData.Length;
            float targetFrameTime = 1f / targetFrameRate;
            float performanceRatio = averageFrameTime / targetFrameTime;
            
            // Calculate recommended render scale
            float recommendedScale = math.clamp(1f / performanceRatio, 0.5f, 1f);
            renderScaleResults[0] = recommendedScale;
        }
    }
} 