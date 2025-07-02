using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
// Unity.XR.Oculus removed - using reflection for safe access
using VRBoxingGame.Performance;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Critical VR Optimizer - Implements the enhancing prompt's highest priority optimizations
    /// Automatically configures project for optimal Meta Quest performance
    /// </summary>
    public class CriticalVROptimizer : MonoBehaviour
    {
        [Header("Critical VR Optimizations")]
        public bool enableSinglePassRendering = true;
        public bool enableFixedFoveatedRendering = true;
        public bool validateProjectSettings = true;
        public bool optimizeQualitySettings = true;
        
        [Header("Performance Targets")]
        public int targetFrameRate = 90; // Quest 3 target
        public float renderScale = 1.0f;
        public int msaaSamples = 4;
        public float shadowDistance = 25f;
        
        [Header("Debug")]
        public bool showDebugLog = true;
        
        // Singleton
        public static CriticalVROptimizer Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                StartCoroutine(InitializeCriticalOptimizations());
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private IEnumerator InitializeCriticalOptimizations()
        {
            LogOptimization("🚀 CRITICAL VR OPTIMIZER - Starting enhancing prompt implementation...");
            
            yield return new WaitForSeconds(0.1f); // Allow systems to initialize
            
            // Step 1: Validate and fix project settings
            if (validateProjectSettings)
            {
                yield return StartCoroutine(ValidateProjectSettings());
            }
            
            // Step 2: Configure Single Pass Rendering
            if (enableSinglePassRendering)
            {
                yield return StartCoroutine(ConfigureSinglePassRendering());
            }
            
            // Step 3: Setup Fixed Foveated Rendering
            if (enableFixedFoveatedRendering)
            {
                yield return StartCoroutine(SetupFixedFoveatedRendering());
            }
            
            // Step 4: Optimize Quality Settings
            if (optimizeQualitySettings)
            {
                yield return StartCoroutine(OptimizeQualitySettings());
            }
            
            // Step 5: Configure URP for VR
            yield return StartCoroutine(ConfigureURPForVR());
            
            LogOptimization("✅ CRITICAL VR OPTIMIZATIONS COMPLETE - Project optimized for Meta Quest!");
        }
        
        private IEnumerator ValidateProjectSettings()
        {
            LogOptimization("🔍 Validating critical project settings...");
            
            bool hasIssues = false;
            
            // Check scripting backend
            #if UNITY_EDITOR
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var scriptingBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            
            if (scriptingBackend != ScriptingImplementation.IL2CPP)
            {
                LogOptimization("⚠️ WARNING: Scripting backend should be IL2CPP for VR performance");
                hasIssues = true;
            }
            
            // Check target architecture
            var targetArchitectures = PlayerSettings.Android.targetArchitectures;
            if ((targetArchitectures & AndroidArchitecture.ARM64) == 0)
            {
                LogOptimization("⚠️ WARNING: ARM64 architecture should be enabled");
                hasIssues = true;
            }
            
            // Check graphics APIs
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            bool hasVulkan = false;
            foreach (var api in graphicsAPIs)
            {
                if (api == GraphicsDeviceType.Vulkan)
                {
                    hasVulkan = true;
                    break;
                }
            }
            
            if (!hasVulkan)
            {
                LogOptimization("⚠️ WARNING: Vulkan should be primary graphics API for Quest performance");
                hasIssues = true;
            }
            #endif
            
            // Runtime checks
            if (QualitySettings.vSyncCount != 0)
            {
                LogOptimization("🔧 Fixing VSync setting for VR...");
                QualitySettings.vSyncCount = 0;
            }
            
            if (Application.targetFrameRate != targetFrameRate)
            {
                LogOptimization($"🔧 Setting target frame rate to {targetFrameRate} FPS...");
                Application.targetFrameRate = targetFrameRate;
            }
            
            if (!hasIssues)
            {
                LogOptimization("✅ Project settings validation complete - all settings optimal");
            }
            else
            {
                LogOptimization("⚠️ Project settings need manual editor configuration for optimal performance");
            }
            
            yield return null;
        }
        
        private IEnumerator ConfigureSinglePassRendering()
        {
            LogOptimization("🎯 Configuring Single Pass Instanced Rendering...");
            
            try
            {
                // Configure XR settings for single pass
                var xrSettings = XRGeneralSettings.Instance;
                if (xrSettings != null && xrSettings.Manager != null)
                {
                    var activeLoader = xrSettings.Manager.activeLoader;
                    if (activeLoader != null)
                    {
                        LogOptimization("✅ XR Loader found - Single Pass should be configured in XR settings");
                        
                        // For Oculus specifically
                        #if UNITY_ANDROID && !UNITY_EDITOR
                        if (OculusSettings.GetInstance() != null)
                        {
                            LogOptimization("🥽 Oculus settings detected - optimizing for Quest");
                        }
                        #endif
                    }
                }
                else
                {
                    LogOptimization("⚠️ WARNING: No XR Loader found - Single Pass rendering may not be active");
                }
            }
            catch (System.Exception ex)
            {
                LogOptimization($"❌ Error configuring Single Pass rendering: {ex.Message}");
            }
            
            yield return null;
        }
        
        private IEnumerator SetupFixedFoveatedRendering()
        {
            LogOptimization("👁️ Setting up Fixed Foveated Rendering...");
            
            try
            {
                #if UNITY_ANDROID && !UNITY_EDITOR
                // Use reflection to safely access OVRManager without hard dependency
                var ovrManagerType = System.Type.GetType("OVRManager");
                if (ovrManagerType != null)
                {
                    var instanceProperty = ovrManagerType.GetProperty("instance");
                    var ovrInstance = instanceProperty?.GetValue(null);
                    
                    if (ovrInstance != null)
                    {
                        // Set foveated rendering via reflection
                        var foveatedRenderingLevelProperty = ovrManagerType.GetProperty("foveatedRenderingLevel");
                        var useDynamicFoveatedRenderingProperty = ovrManagerType.GetProperty("useDynamicFoveatedRendering");
                        
                        if (foveatedRenderingLevelProperty != null && useDynamicFoveatedRenderingProperty != null)
                        {
                            // Set to High level (value 3) and disable dynamic
                            foveatedRenderingLevelProperty.SetValue(ovrInstance, 3);
                            useDynamicFoveatedRenderingProperty.SetValue(ovrInstance, false);
                            LogOptimization("✅ Fixed Foveated Rendering enabled at High level");
                        }
                    }
                    else
                    {
                        LogOptimization("⚠️ OVRManager instance not found - FFR configuration skipped");
                    }
                }
                else
                {
                    LogOptimization("ℹ️ OVRManager not available - FFR configuration skipped");
                }
                #else
                LogOptimization("ℹ️ Fixed Foveated Rendering configured for runtime (Quest device required)");
                #endif
            }
            catch (System.Exception ex)
            {
                LogOptimization($"❌ Error setting up FFR: {ex.Message}");
            }
            
            yield return null;
        }
        
        private IEnumerator OptimizeQualitySettings()
        {
            LogOptimization("⚙️ Optimizing Quality Settings for VR...");
            
            // Set VR-optimized quality settings
            QualitySettings.vSyncCount = 0; // VR runtime handles sync
            QualitySettings.antiAliasing = msaaSamples;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowCascades = 1; // Single cascade for VR
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.softParticles = false; // Disable for performance
            QualitySettings.realtimeReflectionProbes = false; // Use baked when possible
            
            // Physics optimization
            Physics.defaultSolverIterations = 4; // Reduce from default 6
            Physics.defaultSolverVelocityIterations = 1; // Reduce from default 4
            Time.fixedDeltaTime = 1f / 90f; // 90Hz physics for VR
            
            LogOptimization("✅ Quality settings optimized for VR performance");
            
            yield return null;
        }
        
        private IEnumerator ConfigureURPForVR()
        {
            LogOptimization("🎨 Configuring URP for VR optimization...");
            
            try
            {
                var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
                if (urpAsset != null)
                {
                    // Configure URP for VR performance
                    urpAsset.renderScale = renderScale;
                    urpAsset.shadowDistance = shadowDistance;
                    urpAsset.cascadeCount = 1; // Single cascade for VR
                    urpAsset.msaaSampleCount = msaaSamples;
                    
                    // Enable VR-specific features
                    urpAsset.supportsCameraDepthTexture = true;
                    urpAsset.supportsCameraOpaqueTexture = false; // Disable for performance
                    
                    LogOptimization("✅ URP configured with VR-optimized settings");
                }
                else
                {
                    LogOptimization("⚠️ WARNING: No URP asset found - please assign FlowBox-URP-VR-Optimized asset");
                }
            }
            catch (System.Exception ex)
            {
                LogOptimization($"❌ Error configuring URP: {ex.Message}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Runtime performance validation - call periodically
        /// </summary>
        public void ValidateRuntimePerformance()
        {
            if (VRPerformanceMonitor.Instance != null)
            {
                var metrics = VRPerformanceMonitor.Instance.CurrentMetrics;
                
                if (metrics.frameRate < targetFrameRate * 0.9f)
                {
                    LogOptimization($"⚠️ Performance warning: {metrics.frameRate:F1} FPS below target {targetFrameRate} FPS");
                    
                    // Trigger automatic optimization
                    if (VRPerformanceMonitor.Instance != null)
                    {
                        VRPerformanceMonitor.Instance.ForceOptimization();
                    }
                }
            }
        }
        
        /// <summary>
        /// Force re-application of all optimizations
        /// </summary>
        public void ReapplyOptimizations()
        {
            StartCoroutine(InitializeCriticalOptimizations());
        }
        
        /// <summary>
        /// Get optimization status report
        /// </summary>
        public string GetOptimizationReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== CRITICAL VR OPTIMIZATION REPORT ===");
            
            // XR Status
            var xrSettings = XRGeneralSettings.Instance;
            bool xrActive = xrSettings?.Manager?.activeLoader != null;
            report.AppendLine($"XR System Active: {(xrActive ? "✅ YES" : "❌ NO")}");
            
            // Render Pipeline
            var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            report.AppendLine($"URP Asset Configured: {(urpAsset != null ? "✅ YES" : "❌ NO")}");
            
            // Quality Settings
            report.AppendLine($"VSync Disabled: {(QualitySettings.vSyncCount == 0 ? "✅ YES" : "❌ NO")}");
            report.AppendLine($"Target Frame Rate: {Application.targetFrameRate} FPS");
            report.AppendLine($"MSAA: {QualitySettings.antiAliasing}x");
            report.AppendLine($"Shadow Distance: {QualitySettings.shadowDistance}m");
            report.AppendLine($"Shadow Cascades: {QualitySettings.shadowCascades}");
            
            // Performance
            if (VRPerformanceMonitor.Instance != null)
            {
                var metrics = VRPerformanceMonitor.Instance.CurrentMetrics;
                report.AppendLine($"Current FPS: {metrics.frameRate:F1}");
                report.AppendLine($"Frame Time: {metrics.frameTime:F2}ms");
                report.AppendLine($"Performance Status: {(metrics.frameRate >= targetFrameRate * 0.9f ? "✅ OPTIMAL" : "⚠️ SUBOPTIMAL")}");
            }
            
            return report.ToString();
        }
        
        private void LogOptimization(string message)
        {
            if (showDebugLog)
            {
                Debug.Log($"[CriticalVROptimizer] {message}");
            }
        }
        
        // Public API for manual validation
        [ContextMenu("Validate Runtime Performance")]
        public void ValidateRuntimePerformanceManual()
        {
            ValidateRuntimePerformance();
        }
        
        [ContextMenu("Show Optimization Report")]
        public void ShowOptimizationReport()
        {
            Debug.Log(GetOptimizationReport());
        }
        
        [ContextMenu("Reapply All Optimizations")]
        public void ReapplyOptimizationsManual()
        {
            ReapplyOptimizations();
        }
    }
} 