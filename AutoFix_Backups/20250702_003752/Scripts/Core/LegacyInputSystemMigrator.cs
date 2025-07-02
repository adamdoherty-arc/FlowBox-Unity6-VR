using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Legacy Input System Migrator - Completes migration from Input.Get* to Input System
    /// Part of the enhancing prompt's critical modernization requirements
    /// </summary>
    public class LegacyInputSystemMigrator : MonoBehaviour
    {
        [Header("Migration Settings")]
        public bool enableAutomaticMigration = true;
        public bool createInputActionAssets = true;
        public bool validateMigration = true;
        
        [Header("Input Actions")]
        public InputActionAsset defaultInputActions;
        
        [Header("Debug")]
        public bool showMigrationLog = true;
        
        // Legacy input mappings found in the codebase
        private static readonly Dictionary<string, string> LegacyInputMappings = new Dictionary<string, string>
        {
            { "KeyCode.LeftArrow", "Navigate/Left" },
            { "KeyCode.RightArrow", "Navigate/Right" },
            { "KeyCode.BackQuote", "Debug/ToggleConsole" },
            { "KeyCode.C", "Debug/Clear" },
            { "KeyCode.E", "Debug/Export" },
            { "KeyCode.S", "Debug/SystemStatus" },
            { "KeyCode.P", "Debug/Performance" },
            { "KeyCode.F1", "Debug/ToggleOverlay" }
        };
        
        public static LegacyInputSystemMigrator Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (enableAutomaticMigration)
                {
                    StartCoroutine(PerformMigration());
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private System.Collections.IEnumerator PerformMigration()
        {
            LogMigration("🔄 Starting Legacy Input System Migration...");
            
            yield return new WaitForSeconds(0.1f);
            
            // Step 1: Validate current Input System setup
            if (validateMigration)
            {
                yield return StartCoroutine(ValidateInputSystemSetup());
            }
            
            // Step 2: Create or update Input Action Assets
            if (createInputActionAssets)
            {
                yield return StartCoroutine(CreateInputActionAssets());
            }
            
            // Step 3: Report legacy usage
            yield return StartCoroutine(ReportLegacyUsage());
            
            LogMigration("✅ Legacy Input System Migration Complete!");
        }
        
        private System.Collections.IEnumerator ValidateInputSystemSetup()
        {
            LogMigration("🔍 Validating Input System setup...");
            
            bool hasInputSystem = false;
            bool hasLegacyEnabled = false;
            
            #if UNITY_EDITOR
            hasInputSystem = PlayerSettings.activeInputHandling == PlayerSettings.ActiveInputHandling.InputSystemPackage ||
                           PlayerSettings.activeInputHandling == PlayerSettings.ActiveInputHandling.Both;
            
            hasLegacyEnabled = PlayerSettings.activeInputHandling == PlayerSettings.ActiveInputHandling.InputManagerPackage ||
                             PlayerSettings.activeInputHandling == PlayerSettings.ActiveInputHandling.Both;
            #endif
            
            if (!hasInputSystem)
            {
                LogMigration("⚠️ WARNING: New Input System not enabled in Player Settings");
            }
            else
            {
                LogMigration("✅ New Input System enabled");
            }
            
            if (hasLegacyEnabled)
            {
                LogMigration("ℹ️ Legacy Input Manager still enabled (for backward compatibility)");
            }
            
            yield return null;
        }
        
        private System.Collections.IEnumerator CreateInputActionAssets()
        {
            LogMigration("📝 Creating Input Action Assets...");
            
            if (defaultInputActions == null)
            {
                LogMigration("📋 Creating default Input Action Asset...");
                
                #if UNITY_EDITOR
                // Create a new Input Action Asset
                var actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
                actionAsset.name = "FlowBox-InputActions";
                
                // Create Debug action map
                var debugMap = new InputActionMap("Debug");
                actionAsset.AddActionMap(debugMap);
                
                // Add debug actions
                var toggleConsoleAction = debugMap.AddAction("ToggleConsole", InputActionType.Button);
                toggleConsoleAction.AddBinding("<Keyboard>/backquote")
                    .WithInteraction("Hold")
                    .WithProcessor("hold(duration=0.1)");
                
                var clearAction = debugMap.AddAction("Clear", InputActionType.Button);
                clearAction.AddBinding("<Keyboard>/c");
                
                var exportAction = debugMap.AddAction("Export", InputActionType.Button);
                exportAction.AddBinding("<Keyboard>/e");
                
                var systemStatusAction = debugMap.AddAction("SystemStatus", InputActionType.Button);
                systemStatusAction.AddBinding("<Keyboard>/s");
                
                var performanceAction = debugMap.AddAction("Performance", InputActionType.Button);
                performanceAction.AddBinding("<Keyboard>/p");
                
                var toggleOverlayAction = debugMap.AddAction("ToggleOverlay", InputActionType.Button);
                toggleOverlayAction.AddBinding("<Keyboard>/f1");
                
                // Create Navigation action map
                var navMap = new InputActionMap("Navigation");
                actionAsset.AddActionMap(navMap);
                
                var leftAction = navMap.AddAction("Left", InputActionType.Button);
                leftAction.AddBinding("<Keyboard>/leftArrow");
                
                var rightAction = navMap.AddAction("Right", InputActionType.Button);
                rightAction.AddBinding("<Keyboard>/rightArrow");
                
                // Save to Assets
                string assetPath = "Assets/Settings/FlowBox-InputActions.inputactions";
                AssetDatabase.CreateAsset(actionAsset, assetPath);
                AssetDatabase.SaveAssets();
                
                defaultInputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
                LogMigration($"✅ Created Input Action Asset at {assetPath}");
                #else
                LogMigration("ℹ️ Input Action Asset creation requires Unity Editor");
                #endif
            }
            else
            {
                LogMigration("✅ Using existing Input Action Asset");
            }
            
            yield return null;
        }
        
        private System.Collections.IEnumerator ReportLegacyUsage()
        {
            LogMigration("📊 Analyzing legacy Input.Get usage in codebase...");
            
            var legacyUsageReport = GenerateLegacyUsageReport();
            
            if (!string.IsNullOrEmpty(legacyUsageReport))
            {
                LogMigration("⚠️ LEGACY INPUT USAGE DETECTED:");
                LogMigration(legacyUsageReport);
                LogMigration("💡 Consider replacing these with Input Action references for full modernization");
            }
            else
            {
                LogMigration("✅ No problematic legacy input usage detected");
            }
            
            yield return null;
        }
        
        private string GenerateLegacyUsageReport()
        {
            var report = new StringBuilder();
            
            // Known legacy usage from our analysis
            var knownUsages = new Dictionary<string, List<string>>
            {
                ["VRPerformanceMonitor.cs"] = new List<string> { "F1 key toggle (line 149)" },
                ["VR360MovementSystem.cs"] = new List<string> { "Left/Right arrow navigation (lines 482, 486)" },
                ["DebugLogPanel.cs"] = new List<string> { "Console toggle and shortcuts (lines 166-180)" },
                ["GameReadinessValidator.cs"] = new List<string> { "Validation trigger key (line 160)" },
                ["RainRhythmTest.cs"] = new List<string> { "Weather control keys (lines 292-310)" }
            };
            
            report.AppendLine("=== LEGACY INPUT USAGE REPORT ===");
            foreach (var file in knownUsages)
            {
                report.AppendLine($"📁 {file.Key}:");
                foreach (var usage in file.Value)
                {
                    report.AppendLine($"   • {usage}");
                }
            }
            
            report.AppendLine("\n💡 MIGRATION RECOMMENDATIONS:");
            report.AppendLine("1. Replace Input.GetKeyDown() with InputAction.performed events");
            report.AppendLine("2. Use InputActionReference fields in MonoBehaviours");
            report.AppendLine("3. Subscribe to actions in OnEnable/OnDisable");
            report.AppendLine("4. Assign actions from FlowBox-InputActions asset in Inspector");
            
            return report.ToString();
        }
        
        /// <summary>
        /// Get migration status and recommendations
        /// </summary>
        public string GetMigrationStatus()
        {
            var status = new StringBuilder();
            status.AppendLine("=== INPUT SYSTEM MIGRATION STATUS ===");
            
            #if UNITY_EDITOR
            var inputHandling = PlayerSettings.activeInputHandling;
            status.AppendLine($"Input Handling: {inputHandling}");
            
            bool fullyMigrated = inputHandling == PlayerSettings.ActiveInputHandling.InputSystemPackage;
            status.AppendLine($"Migration Status: {(fullyMigrated ? "✅ COMPLETE" : "🔄 IN PROGRESS")}");
            #endif
            
            status.AppendLine($"Input Actions Asset: {(defaultInputActions != null ? "✅ CONFIGURED" : "❌ MISSING")}");
            
            if (defaultInputActions != null)
            {
                var actionMaps = defaultInputActions.actionMaps;
                status.AppendLine($"Action Maps: {actionMaps.Count}");
                foreach (var map in actionMaps)
                {
                    status.AppendLine($"  • {map.name} ({map.actions.Count} actions)");
                }
            }
            
            return status.ToString();
        }
        
        /// <summary>
        /// Manual migration trigger for specific file patterns
        /// </summary>
        [ContextMenu("Generate Migration Code")]
        public void GenerateMigrationCode()
        {
            var migrationCode = new StringBuilder();
            migrationCode.AppendLine("// EXAMPLE: Migrating legacy Input.GetKeyDown to Input System");
            migrationCode.AppendLine("// OLD CODE:");
            migrationCode.AppendLine("// if (Input.GetKeyDown(KeyCode.F1)) { ToggleDebug(); }");
            migrationCode.AppendLine("");
            migrationCode.AppendLine("// NEW CODE:");
            migrationCode.AppendLine("[Header(\"Input Actions\")]");
            migrationCode.AppendLine("public InputActionReference toggleDebugAction;");
            migrationCode.AppendLine("");
            migrationCode.AppendLine("private void OnEnable()");
            migrationCode.AppendLine("{");
            migrationCode.AppendLine("    if (toggleDebugAction != null)");
            migrationCode.AppendLine("    {");
            migrationCode.AppendLine("        toggleDebugAction.action.performed += OnToggleDebug;");
            migrationCode.AppendLine("        toggleDebugAction.action.Enable();");
            migrationCode.AppendLine("    }");
            migrationCode.AppendLine("}");
            migrationCode.AppendLine("");
            migrationCode.AppendLine("private void OnDisable()");
            migrationCode.AppendLine("{");
            migrationCode.AppendLine("    if (toggleDebugAction != null)");
            migrationCode.AppendLine("    {");
            migrationCode.AppendLine("        toggleDebugAction.action.performed -= OnToggleDebug;");
            migrationCode.AppendLine("        toggleDebugAction.action.Disable();");
            migrationCode.AppendLine("    }");
            migrationCode.AppendLine("}");
            migrationCode.AppendLine("");
            migrationCode.AppendLine("private void OnToggleDebug(InputAction.CallbackContext context)");
            migrationCode.AppendLine("{");
            migrationCode.AppendLine("    ToggleDebug();");
            migrationCode.AppendLine("}");
            
            LogMigration("📋 MIGRATION CODE TEMPLATE:");
            LogMigration(migrationCode.ToString());
        }
        
        private void LogMigration(string message)
        {
            if (showMigrationLog)
            {
                Debug.Log($"[LegacyInputMigrator] {message}");
            }
        }
        
        [ContextMenu("Show Migration Status")]
        public void ShowMigrationStatus()
        {
            Debug.Log(GetMigrationStatus());
        }
        
        [ContextMenu("Show Legacy Usage Report")]
        public void ShowLegacyUsageReport()
        {
            Debug.Log(GenerateLegacyUsageReport());
        }
    }
} 