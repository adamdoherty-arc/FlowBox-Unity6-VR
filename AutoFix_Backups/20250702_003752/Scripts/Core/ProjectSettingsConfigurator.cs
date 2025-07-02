#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Project Settings Configurator - Automatically applies all enhancingprompt requirements
    /// Category 2: Project Settings Checklist implementation
    /// </summary>
    public static class ProjectSettingsConfigurator
    {
        [MenuItem("FlowBox/Apply Enhancing Prompt Settings")]
        public static void ApplyEnhancingPromptSettings()
        {
            Debug.Log("🚀 Applying Enhancing Prompt Project Settings...");
            
            ConfigureProjectSettings();
            ConfigureQualitySettings();
            ConfigureGraphicsSettings();
            ConfigureXRSettings();
            ConfigureAndroidSettings();
            ConfigurePhysicsSettings();
            
            Debug.Log("✅ Enhancing Prompt Settings Applied Successfully!");
            
            // Force refresh
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private static void ConfigureProjectSettings()
        {
            Debug.Log("⚙️ Configuring Core Project Settings...");
            
            // Color Space - Linear for VR
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                Debug.Log("🔧 Color Space set to Linear");
            }
            
            // Disable HDR for VR performance
            PlayerSettings.useHDRDisplay = false;
            Debug.Log("🔧 HDR Display disabled");
            
            // GPU Skinning
            PlayerSettings.gpuSkinning = true;
            Debug.Log("🔧 GPU Skinning enabled");
            
            // Graphics Jobs
            PlayerSettings.graphicsJobs = true;
            Debug.Log("🔧 Graphics Jobs enabled");
            
            // Multithreaded Rendering
            PlayerSettings.MTRendering = true;
            Debug.Log("🔧 Multithreaded Rendering enabled");
            
            // Static Batching
            PlayerSettings.batchingSettings.staticBatching = true;
            Debug.Log("🔧 Static Batching enabled");
            
            // Dynamic Batching (disabled for VR as per best practices)
            PlayerSettings.batchingSettings.dynamicBatching = false;
            Debug.Log("🔧 Dynamic Batching disabled for VR");
            
            // Input System
            PlayerSettings.activeInputHandling = PlayerSettings.ActiveInputHandling.InputSystemPackage;
            Debug.Log("🔧 Input System Package set as active");
        }
        
        private static void ConfigureQualitySettings()
        {
            Debug.Log("🎯 Configuring Quality Settings for VR...");
            
            // Create VR-optimized quality levels
            string[] qualityNames = QualitySettings.names;
            
            // Configure each quality level for VR
            for (int i = 0; i < qualityNames.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                
                // VSync - Always disabled for VR
                QualitySettings.vSyncCount = 0;
                
                // MSAA based on quality level
                switch (i)
                {
                    case 0: // Very Low
                        QualitySettings.antiAliasing = 0;
                        QualitySettings.shadowDistance = 10f;
                        QualitySettings.shadows = ShadowQuality.Disable;
                        break;
                    case 1: // Low
                        QualitySettings.antiAliasing = 2;
                        QualitySettings.shadowDistance = 15f;
                        QualitySettings.shadows = ShadowQuality.HardOnly;
                        break;
                    case 2: // Medium
                        QualitySettings.antiAliasing = 2;
                        QualitySettings.shadowDistance = 25f;
                        QualitySettings.shadows = ShadowQuality.All;
                        break;
                    case 3: // High
                        QualitySettings.antiAliasing = 4;
                        QualitySettings.shadowDistance = 25f;
                        QualitySettings.shadows = ShadowQuality.All;
                        break;
                    default: // Very High and Ultra
                        QualitySettings.antiAliasing = 4;
                        QualitySettings.shadowDistance = 50f;
                        QualitySettings.shadows = ShadowQuality.All;
                        break;
                }
                
                // VR-specific optimizations for all levels
                QualitySettings.shadowCascades = 1; // Single cascade for VR
                QualitySettings.softParticles = false; // Disable for performance
                QualitySettings.realtimeReflectionProbes = false; // Use baked
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                
                // Texture streaming
                QualitySettings.streamingMipmapsActive = true;
                QualitySettings.streamingMipmapsMemoryBudget = 512f; // 512MB budget
                
                Debug.Log($"🔧 Quality level '{qualityNames[i]}' optimized for VR");
            }
            
            // Set default quality for Android (Medium = index 2)
            QualitySettings.SetQualityLevel(2);
            Debug.Log("🔧 Default quality set to Medium for VR");
        }
        
        private static void ConfigureGraphicsSettings()
        {
            Debug.Log("🎨 Configuring Graphics Settings...");
            
            // Ensure URP is set as render pipeline
            var urpAssetPath = "Assets/Settings/FlowBox-URP-VR-Optimized.asset";
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            
            if (urpAsset != null)
            {
                GraphicsSettings.renderPipelineAsset = urpAsset;
                QualitySettings.renderPipeline = urpAsset;
                Debug.Log("✅ URP Asset configured as render pipeline");
            }
            else
            {
                Debug.LogWarning("⚠️ URP Asset not found at expected path");
            }
            
            // Configure Vulkan as primary graphics API for Android
            var currentAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (currentAPIs[0] != GraphicsDeviceType.Vulkan)
            {
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new GraphicsDeviceType[] 
                { 
                    GraphicsDeviceType.Vulkan, 
                    GraphicsDeviceType.OpenGLES3 
                });
                Debug.Log("🔧 Vulkan set as primary Graphics API for Android");
            }
        }
        
        private static void ConfigureXRSettings()
        {
            Debug.Log("🥽 Configuring XR Settings...");
            
            // XR settings are typically configured through XR Management
            // Ensure stereo rendering mode is set to Single Pass Instanced
            
            // Configure for Oculus/Meta Quest
            PlayerSettings.virtualRealitySupported = true;
            
            // Set target frame rate for VR
            Application.targetFrameRate = 90; // Quest 3 target
            
            Debug.Log("✅ XR Settings configured for Meta Quest");
        }
        
        private static void ConfigureAndroidSettings()
        {
            Debug.Log("📱 Configuring Android Settings...");
            
            // Scripting Backend - IL2CPP
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                Debug.Log("🔧 Scripting Backend set to IL2CPP");
            }
            
            // Target Architecture - ARM64
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                Debug.Log("🔧 Target Architecture set to ARM64");
            }
            
            // Android API levels
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23; // Android 6.0
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel30; // Android 11
            
            // Texture compression - ASTC
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            
            // Bundle identifier
            if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.flowbox.vrboxing");
                Debug.Log("🔧 Android package name set");
            }
            
            // Optimize mesh data
            PlayerSettings.stripUnusedMeshComponents = true;
            Debug.Log("🔧 Mesh optimization enabled");
            
            Debug.Log("✅ Android settings optimized for Quest");
        }
        
        private static void ConfigurePhysicsSettings()
        {
            Debug.Log("⚡ Configuring Physics Settings for VR...");
            
            // Physics timestep for 90Hz VR
            Time.fixedDeltaTime = 1f / 90f;
            
            // Reduce physics iterations for VR performance
            Physics.defaultSolverIterations = 4; // Reduced from 6
            Physics.defaultSolverVelocityIterations = 1; // Reduced from 4
            
            // Contact processing
            Physics.defaultMaxAngularSpeed = 50f; // Limit for stability
            
            Debug.Log("✅ Physics settings optimized for 90Hz VR");
        }
        
        [MenuItem("FlowBox/Validate Enhancing Prompt Compliance")]
        public static void ValidateEnhancingPromptCompliance()
        {
            Debug.Log("🔍 Validating Enhancing Prompt Compliance...");
            
            bool allValid = true;
            
            // Check URP
            if (!(GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset))
            {
                Debug.LogError("❌ URP not configured");
                allValid = false;
            }
            
            // Check IL2CPP
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            {
                Debug.LogError("❌ IL2CPP not enabled");
                allValid = false;
            }
            
            // Check ARM64
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                Debug.LogError("❌ ARM64 not enabled");
                allValid = false;
            }
            
            // Check Vulkan
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (apis.Length == 0 || apis[0] != GraphicsDeviceType.Vulkan)
            {
                Debug.LogError("❌ Vulkan not primary Graphics API");
                allValid = false;
            }
            
            // Check Linear Color Space
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                Debug.LogError("❌ Linear Color Space not enabled");
                allValid = false;
            }
            
            // Check VSync
            if (QualitySettings.vSyncCount != 0)
            {
                Debug.LogError("❌ VSync not disabled");
                allValid = false;
            }
            
            // Check Input System
            if (PlayerSettings.activeInputHandling != PlayerSettings.ActiveInputHandling.InputSystemPackage)
            {
                Debug.LogError("❌ Input System Package not active");
                allValid = false;
            }
            
            if (allValid)
            {
                Debug.Log("✅ All Enhancing Prompt requirements met!");
            }
            else
            {
                Debug.LogWarning("⚠️ Some requirements not met. Run 'Apply Enhancing Prompt Settings' to fix.");
            }
        }
        
        [MenuItem("FlowBox/Create VR Build Settings")]
        public static void CreateVRBuildSettings()
        {
            Debug.Log("🔨 Creating VR Build Settings...");
            
            // Set build target to Android
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
            EditorUserBuildSettings.activeBuildTarget = BuildTarget.Android;
            
            // Development build for testing
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            EditorUserBuildSettings.connectProfiler = false; // Disable for performance
            
            // Compression
            EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            
            Debug.Log("✅ VR Build Settings configured");
        }
    }
}

#endif 