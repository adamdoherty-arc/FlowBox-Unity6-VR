using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using VRBoxingGame.Performance;
using System.Text;
using System.Collections.Generic;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Runtime Validation Summary - Comprehensive status check for all enhancing prompt optimizations
    /// </summary>
    public class RuntimeValidationSummary : MonoBehaviour
    {
        [Header("Validation Settings")]
        public bool enableContinuousValidation = true;
        public float validationInterval = 5f;
        public bool showGUIPanel = true;
        public KeyCode toggleKey = KeyCode.F12;
        
        [Header("Performance Thresholds")]
        public float targetFrameRate = 90f;
        public float criticalFrameRate = 72f;
        public float maxFrameTime = 11.1f; // 90fps = 11.1ms
        
        private ValidationReport currentReport;
        private bool showValidationGUI = true;
        private float lastValidationTime;
        private GUIStyle headerStyle;
        private GUIStyle normalStyle;
        private GUIStyle errorStyle;
        private GUIStyle successStyle;
        
        public static RuntimeValidationSummary Instance { get; private set; }
        
        [System.Serializable]
        public class ValidationReport
        {
            public bool projectSettingsValid = false;
            public bool urpConfigurationValid = false;
            public bool xrSystemValid = false;
            public bool performanceOptimal = false;
            public bool inputSystemModernized = false;
            public bool addressablesConfigured = false;
            public bool objectPoolingActive = false;
            public bool optimizedUpdateActive = false;
            
            public float currentFPS = 0f;
            public float averageFrameTime = 0f;
            public string overallStatus = "Unknown";
            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();
            public List<string> optimizations = new List<string>();
            
            public float GetOverallScore()
            {
                int totalChecks = 8;
                int passedChecks = 0;
                
                if (projectSettingsValid) passedChecks++;
                if (urpConfigurationValid) passedChecks++;
                if (xrSystemValid) passedChecks++;
                if (performanceOptimal) passedChecks++;
                if (inputSystemModernized) passedChecks++;
                if (addressablesConfigured) passedChecks++;
                if (objectPoolingActive) passedChecks++;
                if (optimizedUpdateActive) passedChecks++;
                
                return (float)passedChecks / totalChecks * 100f;
            }
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeValidation();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeValidation()
        {
            currentReport = new ValidationReport();
            lastValidationTime = Time.time;
            
            Debug.Log("🔍 Runtime Validation Summary initialized - Press F12 to toggle GUI");
        }
        
        private void Update()
        {
            // Toggle GUI
            if (Input.GetKeyDown(toggleKey))
            {
                showValidationGUI = !showValidationGUI;
            }
            
            // Continuous validation
            if (enableContinuousValidation && Time.time - lastValidationTime >= validationInterval)
            {
                ValidateAllSystems();
                lastValidationTime = Time.time;
            }
        }
        
        public void ValidateAllSystems()
        {
            currentReport.warnings.Clear();
            currentReport.errors.Clear();
            currentReport.optimizations.Clear();
            
            // 1. Project Settings Validation
            ValidateProjectSettings();
            
            // 2. URP Configuration
            ValidateURPConfiguration();
            
            // 3. XR System Status
            ValidateXRSystem();
            
            // 4. Performance Analysis
            ValidatePerformance();
            
            // 5. Input System Modernization
            ValidateInputSystem();
            
            // 6. Addressables Configuration
            ValidateAddressables();
            
            // 7. Object Pooling Status
            ValidateObjectPooling();
            
            // 8. Optimized Update System
            ValidateOptimizedUpdates();
            
            // Calculate overall status
            float score = currentReport.GetOverallScore();
            if (score >= 90f)
                currentReport.overallStatus = "✅ EXCELLENT";
            else if (score >= 75f)
                currentReport.overallStatus = "✅ GOOD";
            else if (score >= 60f)
                currentReport.overallStatus = "⚠️ NEEDS IMPROVEMENT";
            else
                currentReport.overallStatus = "❌ CRITICAL ISSUES";
            
            Debug.Log($"[ValidationSummary] Overall Status: {currentReport.overallStatus} (Score: {score:F1}%)");
        }
        
        private void ValidateProjectSettings()
        {
            bool allValid = true;
            
            // VSync Check
            if (QualitySettings.vSyncCount != 0)
            {
                currentReport.errors.Add("VSync should be disabled for VR");
                allValid = false;
            }
            
            // Target Frame Rate
            if (Application.targetFrameRate != targetFrameRate && Application.targetFrameRate != -1)
            {
                currentReport.warnings.Add($"Target frame rate is {Application.targetFrameRate}, recommend {targetFrameRate}");
            }
            
            // MSAA Check
            if (QualitySettings.antiAliasing < 4)
            {
                currentReport.warnings.Add($"MSAA is {QualitySettings.antiAliasing}x, recommend 4x for VR");
            }
            
            // Shadow Settings
            if (QualitySettings.shadowCascades > 1)
            {
                currentReport.warnings.Add($"Shadow cascades is {QualitySettings.shadowCascades}, recommend 1 for VR");
            }
            
            currentReport.projectSettingsValid = allValid;
            if (allValid) currentReport.optimizations.Add("Project settings optimized for VR");
        }
        
        private void ValidateURPConfiguration()
        {
            var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            
            if (urpAsset == null)
            {
                currentReport.errors.Add("No URP asset assigned");
                currentReport.urpConfigurationValid = false;
                return;
            }
            
            bool validConfig = true;
            
            // Check shadow distance
            if (urpAsset.shadowDistance > 50f)
            {
                currentReport.warnings.Add($"URP shadow distance is {urpAsset.shadowDistance}m, recommend < 50m for VR");
            }
            
            // Check MSAA
            if (urpAsset.msaaSampleCount < 4)
            {
                currentReport.warnings.Add($"URP MSAA is {urpAsset.msaaSampleCount}x, recommend 4x for VR");
            }
            
            // Check SRP Batcher
            if (urpAsset.useSRPBatcher)
            {
                currentReport.optimizations.Add("SRP Batcher enabled for better performance");
            }
            else
            {
                currentReport.warnings.Add("SRP Batcher should be enabled");
                validConfig = false;
            }
            
            currentReport.urpConfigurationValid = validConfig;
            if (validConfig) currentReport.optimizations.Add("URP configured optimally for VR");
        }
        
        private void ValidateXRSystem()
        {
            var xrSettings = XRGeneralSettings.Instance;
            bool xrValid = xrSettings?.Manager?.activeLoader != null;
            
            if (!xrValid)
            {
                currentReport.errors.Add("No XR loader active");
                currentReport.xrSystemValid = false;
                return;
            }
            
            currentReport.optimizations.Add("XR system active and configured");
            currentReport.xrSystemValid = true;
        }
        
        private void ValidatePerformance()
        {
            if (VRPerformanceMonitor.Instance != null)
            {
                var metrics = VRPerformanceMonitor.Instance.CurrentMetrics;
                currentReport.currentFPS = metrics.frameRate;
                currentReport.averageFrameTime = metrics.frameTime;
                
                if (metrics.frameRate >= targetFrameRate * 0.9f)
                {
                    currentReport.optimizations.Add($"Performance optimal: {metrics.frameRate:F1} FPS");
                    currentReport.performanceOptimal = true;
                }
                else if (metrics.frameRate >= criticalFrameRate)
                {
                    currentReport.warnings.Add($"Performance acceptable: {metrics.frameRate:F1} FPS");
                    currentReport.performanceOptimal = false;
                }
                else
                {
                    currentReport.errors.Add($"Performance critical: {metrics.frameRate:F1} FPS");
                    currentReport.performanceOptimal = false;
                }
            }
            else
            {
                currentReport.warnings.Add("VR Performance Monitor not found");
                currentReport.performanceOptimal = false;
            }
        }
        
        private void ValidateInputSystem()
        {
            // Check if modern Input System is being used
            bool modernInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>() != null;
            
            if (modernInput)
            {
                currentReport.optimizations.Add("Modern Input System detected");
                currentReport.inputSystemModernized = true;
            }
            else
            {
                currentReport.warnings.Add("Legacy Input detected - consider migrating to Input System");
                currentReport.inputSystemModernized = false;
            }
        }
        
        private void ValidateAddressables()
        {
            if (VRBoxingGame.Streaming.AddressableStreamingSystem.Instance != null)
            {
                currentReport.optimizations.Add("Addressable Streaming System active");
                currentReport.addressablesConfigured = true;
            }
            else
            {
                currentReport.warnings.Add("Addressable Streaming System not found");
                currentReport.addressablesConfigured = false;
            }
        }
        
        private void ValidateObjectPooling()
        {
            if (ObjectPoolManager.Instance != null)
            {
                currentReport.optimizations.Add("Object Pooling System active");
                currentReport.objectPoolingActive = true;
            }
            else
            {
                currentReport.warnings.Add("Object Pooling System not found");
                currentReport.objectPoolingActive = false;
            }
        }
        
        private void ValidateOptimizedUpdates()
        {
            if (OptimizedUpdateManager.Instance != null)
            {
                currentReport.optimizations.Add("Optimized Update Manager active");
                currentReport.optimizedUpdateActive = true;
            }
            else
            {
                currentReport.warnings.Add("Optimized Update Manager not found");
                currentReport.optimizedUpdateActive = false;
            }
        }
        
        private void OnGUI()
        {
            if (!showGUIPanel || !showValidationGUI) return;
            
            // Initialize styles
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                normalStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
                errorStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
                errorStyle.normal.textColor = Color.red;
                successStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
                successStyle.normal.textColor = Color.green;
            }
            
            // Main panel
            GUI.Box(new Rect(10, 10, 400, 600), "");
            GUILayout.BeginArea(new Rect(20, 20, 380, 580));
            
            GUILayout.Label("🔍 ENHANCING PROMPT VALIDATION", headerStyle);
            GUILayout.Space(10);
            
            // Overall Status
            GUIStyle statusStyle = currentReport.GetOverallScore() >= 75f ? successStyle : errorStyle;
            GUILayout.Label($"Overall Status: {currentReport.overallStatus}", statusStyle);
            GUILayout.Label($"Score: {currentReport.GetOverallScore():F1}%", normalStyle);
            
            if (currentReport.currentFPS > 0)
            {
                GUIStyle fpsStyle = currentReport.currentFPS >= targetFrameRate * 0.9f ? successStyle : errorStyle;
                GUILayout.Label($"FPS: {currentReport.currentFPS:F1} | Frame: {currentReport.averageFrameTime:F1}ms", fpsStyle);
            }
            
            GUILayout.Space(10);
            
            // System Status
            GUILayout.Label("SYSTEM STATUS:", headerStyle);
            DrawStatusItem("Project Settings", currentReport.projectSettingsValid);
            DrawStatusItem("URP Configuration", currentReport.urpConfigurationValid);
            DrawStatusItem("XR System", currentReport.xrSystemValid);
            DrawStatusItem("Performance", currentReport.performanceOptimal);
            DrawStatusItem("Input System", currentReport.inputSystemModernized);
            DrawStatusItem("Addressables", currentReport.addressablesConfigured);
            DrawStatusItem("Object Pooling", currentReport.objectPoolingActive);
            DrawStatusItem("Optimized Updates", currentReport.optimizedUpdateActive);
            
            GUILayout.Space(10);
            
            // Errors
            if (currentReport.errors.Count > 0)
            {
                GUILayout.Label("❌ ERRORS:", errorStyle);
                foreach (string error in currentReport.errors)
                {
                    GUILayout.Label($"• {error}", errorStyle);
                }
                GUILayout.Space(5);
            }
            
            // Warnings
            if (currentReport.warnings.Count > 0)
            {
                GUILayout.Label("⚠️ WARNINGS:", normalStyle);
                foreach (string warning in currentReport.warnings)
                {
                    GUILayout.Label($"• {warning}", normalStyle);
                }
                GUILayout.Space(5);
            }
            
            // Optimizations
            if (currentReport.optimizations.Count > 0)
            {
                GUILayout.Label("✅ OPTIMIZATIONS:", successStyle);
                foreach (string optimization in currentReport.optimizations)
                {
                    GUILayout.Label($"• {optimization}", successStyle);
                }
            }
            
            GUILayout.Space(10);
            
            // Controls
            if (GUILayout.Button("Refresh Validation"))
            {
                ValidateAllSystems();
            }
            
            if (GUILayout.Button("Apply Critical Fixes"))
            {
                ApplyCriticalFixes();
            }
            
            GUILayout.Label($"Press {toggleKey} to toggle this panel", normalStyle);
            
            GUILayout.EndArea();
        }
        
        private void DrawStatusItem(string name, bool status)
        {
            GUIStyle style = status ? successStyle : errorStyle;
            string indicator = status ? "✅" : "❌";
            GUILayout.Label($"{indicator} {name}", style);
        }
        
        private void ApplyCriticalFixes()
        {
            Debug.Log("🔧 Applying critical fixes...");
            
            // Fix VSync
            if (QualitySettings.vSyncCount != 0)
            {
                QualitySettings.vSyncCount = 0;
                Debug.Log("Fixed: VSync disabled");
            }
            
            // Fix target frame rate
            if (Application.targetFrameRate != targetFrameRate)
            {
                Application.targetFrameRate = (int)targetFrameRate;
                Debug.Log($"Fixed: Target frame rate set to {targetFrameRate}");
            }
            
            // Trigger critical optimizer if available
            if (CriticalVROptimizer.Instance != null)
            {
                CriticalVROptimizer.Instance.ReapplyOptimizations();
                Debug.Log("Applied: Critical VR optimizations");
            }
            
            // Revalidate after fixes
            ValidateAllSystems();
        }
        
        // Public API
        public ValidationReport GetCurrentReport()
        {
            return currentReport;
        }
        
        public void ForceValidation()
        {
            ValidateAllSystems();
        }
        
        [ContextMenu("Show Validation Report")]
        public void ShowValidationReport()
        {
            ValidateAllSystems();
            Debug.Log(GenerateTextReport());
        }
        
        private string GenerateTextReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ENHANCING PROMPT VALIDATION REPORT ===");
            sb.AppendLine($"Overall Status: {currentReport.overallStatus}");
            sb.AppendLine($"Score: {currentReport.GetOverallScore():F1}%");
            sb.AppendLine($"Performance: {currentReport.currentFPS:F1} FPS | {currentReport.averageFrameTime:F1}ms");
            sb.AppendLine();
            
            sb.AppendLine("SYSTEM STATUS:");
            sb.AppendLine($"Project Settings: {(currentReport.projectSettingsValid ? "✅" : "❌")}");
            sb.AppendLine($"URP Configuration: {(currentReport.urpConfigurationValid ? "✅" : "❌")}");
            sb.AppendLine($"XR System: {(currentReport.xrSystemValid ? "✅" : "❌")}");
            sb.AppendLine($"Performance: {(currentReport.performanceOptimal ? "✅" : "❌")}");
            sb.AppendLine($"Input System: {(currentReport.inputSystemModernized ? "✅" : "❌")}");
            sb.AppendLine($"Addressables: {(currentReport.addressablesConfigured ? "✅" : "❌")}");
            sb.AppendLine($"Object Pooling: {(currentReport.objectPoolingActive ? "✅" : "❌")}");
            sb.AppendLine($"Optimized Updates: {(currentReport.optimizedUpdateActive ? "✅" : "❌")}");
            
            if (currentReport.errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("ERRORS:");
                foreach (string error in currentReport.errors)
                    sb.AppendLine($"• {error}");
            }
            
            if (currentReport.warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("WARNINGS:");
                foreach (string warning in currentReport.warnings)
                    sb.AppendLine($"• {warning}");
            }
            
            if (currentReport.optimizations.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("OPTIMIZATIONS ACTIVE:");
                foreach (string optimization in currentReport.optimizations)
                    sb.AppendLine($"• {optimization}");
            }
            
            return sb.ToString();
        }
    }
} 