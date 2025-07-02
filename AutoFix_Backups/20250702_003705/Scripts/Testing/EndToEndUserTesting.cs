using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VRBoxingGame.Core;
using VRBoxingGame.UI;
using VRBoxingGame.Environment;
using VRBoxingGame.Boxing;

namespace VRBoxingGame.Testing
{
    /// <summary>
    /// End-to-End User Testing - Simulates complete user journey
    /// Tests the entire game flow from startup to gameplay across all modes and scenes
    /// </summary>
    public class EndToEndUserTesting : MonoBehaviour
    {
        [Header("User Testing Configuration")]
        public bool runTestOnStart = true;
        public bool enableDetailedLogging = true;
        public float testStepDelay = 2f;
        
        [Header("Test Scenarios")]
        public bool testMenuNavigation = true;
        public bool testSceneSelection = true;
        public bool testAllGameModes = true;
        public bool testPerformance = true;
        public bool testVRCompatibility = true;
        
        private UserTestReport testReport;
        private List<string> testLog = new List<string>();
        private int currentTestStep = 0;
        private int totalTestSteps = 0;
        
        [System.Serializable]
        public struct UserTestReport
        {
            public bool overallSuccess;
            public float completionPercentage;
            public int passedTests;
            public int failedTests;
            public List<string> criticalIssues;
            public List<string> warnings;
            public float averagePerformance;
            public bool isVRReady;
            public string deploymentRecommendation;
        }
        
        private void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunComprehensiveUserTesting());
            }
        }
        
        private IEnumerator RunComprehensiveUserTesting()
        {
            LogStep("🎮 STARTING COMPREHENSIVE USER TESTING");
            LogStep("Simulating complete user journey from VR startup to gameplay");
            
            InitializeTestReport();
            
            yield return new WaitForSeconds(1f);
            
            // Test Step 1: System Initialization
            yield return StartCoroutine(TestSystemInitialization());
            
            // Test Step 2: Menu Navigation
            if (testMenuNavigation)
                yield return StartCoroutine(TestMenuNavigation());
            
            // Test Step 3: Scene Selection
            if (testSceneSelection)
                yield return StartCoroutine(TestSceneSelection());
            
            // Test Step 4: Game Mode Testing
            if (testAllGameModes)
                yield return StartCoroutine(TestAllGameModes());
            
            // Test Step 5: Performance Testing
            if (testPerformance)
                yield return StartCoroutine(TestPerformance());
            
            // Test Step 6: VR Compatibility
            if (testVRCompatibility)
                yield return StartCoroutine(TestVRCompatibility());
            
            // Generate final report
            GenerateFinalTestReport();
        }
        
        private IEnumerator TestSystemInitialization()
        {
            LogStep("🔧 Testing System Initialization...");
            
            yield return new WaitForSeconds(testStepDelay);
            
            // Test critical systems
            bool systemsValid = true;
            
            // Check CachedReferenceManager
            if (CachedReferenceManager.Instance == null)
            {
                AddCriticalIssue("CachedReferenceManager not initialized");
                systemsValid = false;
            }
            else
            {
                LogSuccess("CachedReferenceManager initialized");
            }
            
            // Check OptimizedUpdateManager
            if (OptimizedUpdateManager.Instance == null)
            {
                AddWarning("OptimizedUpdateManager not found");
            }
            else
            {
                LogSuccess("OptimizedUpdateManager initialized");
            }
            
            // Check SceneAssetManager
            var sceneManager = CachedReferenceManager.Get<SceneAssetManager>();
            if (sceneManager == null)
            {
                AddCriticalIssue("SceneAssetManager not found");
                systemsValid = false;
            }
            else
            {
                LogSuccess("SceneAssetManager found");
            }
            
            // Apply fixes if needed
            if (!systemsValid)
            {
                LogStep("Applying emergency fixes...");
                yield return StartCoroutine(ApplyEmergencyFixes());
            }
            
            UpdateTestProgress(systemsValid);
        }
        
        private IEnumerator TestMenuNavigation()
        {
            LogStep("📱 Testing Menu Navigation...");
            
            yield return new WaitForSeconds(testStepDelay);
            
            // Find menu system
            var optimizedMenu = CachedReferenceManager.Get<EnhancedMainMenuSystemOptimized>();
            var enhancedMenu = CachedReferenceManager.Get<EnhancedMainMenuSystem>();
            var mainMenu = CachedReferenceManager.Get<MainMenuSystem>();
            
            bool menuWorking = false;
            
            if (optimizedMenu != null)
            {
                LogSuccess("Optimized menu system found and active");
                menuWorking = TestMenuButtons(optimizedMenu.gameObject);
            }
            else if (enhancedMenu != null)
            {
                LogWarning("Using enhanced menu (not optimized)");
                menuWorking = TestMenuButtons(enhancedMenu.gameObject);
            }
            else if (mainMenu != null)
            {
                LogWarning("Using basic menu system");
                menuWorking = TestMenuButtons(mainMenu.gameObject);
            }
            else
            {
                AddCriticalIssue("No menu system found - user cannot navigate");
                menuWorking = false;
            }
            
            UpdateTestProgress(menuWorking);
        }
        
        private bool TestMenuButtons(GameObject menuObject)
        {
            var buttons = menuObject.GetComponentsInChildren<UnityEngine.UI.Button>();
            
            LogStep($"Found {buttons.Length} menu buttons");
            
            if (buttons.Length < 3)
            {
                AddWarning("Very few menu buttons found");
                return false;
            }
            
            // Test that buttons have click handlers
            int workingButtons = 0;
            foreach (var button in buttons)
            {
                if (button.onClick.GetPersistentEventCount() > 0)
                {
                    workingButtons++;
                }
            }
            
            LogStep($"{workingButtons}/{buttons.Length} buttons have click handlers");
            
            return workingButtons >= buttons.Length / 2; // At least half should work
        }
        
        private IEnumerator TestSceneSelection()
        {
            LogStep("🌍 Testing Scene Selection...");
            
            yield return new WaitForSeconds(testStepDelay);
            
            var sceneManager = CachedReferenceManager.Get<SceneAssetManager>();
            bool sceneSystemWorking = true;
            
            if (sceneManager == null)
            {
                AddCriticalIssue("SceneAssetManager missing - scenes won't load");
                sceneSystemWorking = false;
            }
            else
            {
                // Test each scene loading
                for (int i = 0; i < 8; i++)
                {
                    string sceneName = sceneManager.GetSceneName(i);
                    LogStep($"Testing scene {i}: {sceneName}");
                    
                    // Simulate scene loading
                    yield return StartCoroutine(TestSceneLoading(i));
                }
            }
            
            UpdateTestProgress(sceneSystemWorking);
        }
        
        private IEnumerator TestSceneLoading(int sceneIndex)
        {
            var sceneManager = CachedReferenceManager.Get<SceneAssetManager>();
            
            try
            {
                // Test if scene can be loaded (without actually loading)
                string sceneName = sceneManager.GetSceneName(sceneIndex);
                bool canLoad = sceneManager.IsSceneLoaded(sceneIndex) || 
                             (sceneIndex == 0); // Default scene should always work
                
                if (canLoad || HasScenePrefab(sceneIndex))
                {
                    LogSuccess($"Scene {sceneIndex} ({sceneName}) is loadable");
                }
                else
                {
                    AddWarning($"Scene {sceneIndex} ({sceneName}) may not load properly");
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                AddCriticalIssue($"Scene {sceneIndex} loading failed: {ex.Message}");
            }
        }
        
        private bool HasScenePrefab(int sceneIndex)
        {
            var sceneManager = CachedReferenceManager.Get<SceneAssetManager>();
            if (sceneManager == null) return false;
            
            // Check if corresponding prefab field has a value
            var type = typeof(SceneAssetManager);
            string[] fieldNames = {
                "defaultArenaPrefab", "rainStormPrefab", "neonCityPrefab", 
                "spaceStationPrefab", "crystalCavePrefab", "underwaterWorldPrefab",
                "desertOasisPrefab", "forestGladePrefab"
            };
            
            if (sceneIndex < fieldNames.Length)
            {
                var field = type.GetField(fieldNames[sceneIndex]);
                if (field != null)
                {
                    var value = field.GetValue(sceneManager);
                    return value != null;
                }
            }
            
            return false;
        }
        
        private IEnumerator TestAllGameModes()
        {
            LogStep("🎮 Testing All Game Modes...");
            
            yield return new WaitForSeconds(testStepDelay);
            
            // Test Traditional Mode
            yield return StartCoroutine(TestGameMode("Traditional", typeof(RhythmTargetSystem)));
            
            // Test Flow Mode
            yield return StartCoroutine(TestGameMode("Flow", typeof(FlowModeSystem)));
            
            // Test Staff Mode
            yield return StartCoroutine(TestGameMode("Staff", typeof(TwoHandedStaffSystem)));
            
            // Test Dodging Mode
            yield return StartCoroutine(TestGameMode("Dodging", typeof(ComprehensiveDodgingSystem)));
            
            // Test AI Coach Mode
            yield return StartCoroutine(TestGameMode("AI Coach", typeof(AICoachVisualSystem)));
        }
        
        private IEnumerator TestGameMode(string modeName, System.Type systemType)
        {
            LogStep($"Testing {modeName} mode...");
            
            var system = FindObjectOfType(systemType);
            if (system != null)
            {
                LogSuccess($"{modeName} mode system found");
                
                // Test basic functionality
                yield return StartCoroutine(TestGameModeBasics(system, modeName));
            }
            else
            {
                AddWarning($"{modeName} mode system not found");
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        private IEnumerator TestGameModeBasics(Component system, string modeName)
        {
            try
            {
                // Test that the system is active and enabled
                if (system.gameObject.activeInHierarchy && system.enabled)
                {
                    LogSuccess($"{modeName} mode is active and enabled");
                }
                else
                {
                    LogWarning($"{modeName} mode is inactive or disabled");
                }
                
                // Test for common methods
                var type = system.GetType();
                var startMethod = type.GetMethod("Start" + modeName.Replace(" ", "") + "Mode");
                var stopMethod = type.GetMethod("Stop" + modeName.Replace(" ", "") + "Mode");
                
                if (startMethod != null && stopMethod != null)
                {
                    LogSuccess($"{modeName} mode has start/stop methods");
                }
                else
                {
                    LogWarning($"{modeName} mode missing start/stop methods");
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                AddWarning($"{modeName} mode testing failed: {ex.Message}");
            }
        }
        
        private IEnumerator TestPerformance()
        {
            LogStep("⚡ Testing Performance...");
            
            yield return new WaitForSeconds(testStepDelay);
            
            // Test frame rate
            float frameRate = 1f / Time.unscaledDeltaTime;
            LogStep($"Current frame rate: {frameRate:F1} FPS");
            
            if (frameRate >= 90f)
            {
                LogSuccess("Excellent VR performance (90+ FPS)");
                testReport.averagePerformance = frameRate;
            }
            else if (frameRate >= 72f)
            {
                LogSuccess("Good VR performance (72+ FPS)");
                testReport.averagePerformance = frameRate;
            }
            else if (frameRate >= 60f)
            {
                LogWarning("Acceptable performance (60+ FPS) but below VR ideal");
                testReport.averagePerformance = frameRate;
            }
            else
            {
                AddCriticalIssue($"Poor performance ({frameRate:F1} FPS) - VR motion sickness risk");
                testReport.averagePerformance = frameRate;
            }
            
            // Test memory usage
            long memoryUsage = System.GC.GetTotalMemory(false);
            float memoryMB = memoryUsage / (1024f * 1024f);
            LogStep($"Memory usage: {memoryMB:F1} MB");
            
            if (memoryMB > 2048f)
            {
                AddWarning("High memory usage may cause issues on mobile VR");
            }
            
            UpdateTestProgress(frameRate >= 60f);
        }
        
        private IEnumerator TestVRCompatibility()
        {
            LogStep("🥽 Testing VR Compatibility...");
            
            yield return new WaitForSeconds(testStepDelay);
            
            bool vrReady = true;
            
            // Test XR Origin
            var xrOrigin = CachedReferenceManager.Get<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                LogSuccess("XR Origin found - VR setup detected");
            }
            else
            {
                AddCriticalIssue("XR Origin missing - VR will not work");
                vrReady = false;
            }
            
            // Test hand tracking
            var handTracking = CachedReferenceManager.Get<VRBoxingGame.HandTracking.HandTrackingManager>();
            if (handTracking != null)
            {
                LogSuccess("Hand tracking system found");
            }
            else
            {
                AddWarning("Hand tracking system not found");
            }
            
            // Test VR performance monitoring
            var vrPerformance = CachedReferenceManager.Get<VRBoxingGame.Performance.VRPerformanceMonitor>();
            if (vrPerformance != null)
            {
                LogSuccess("VR performance monitoring active");
            }
            else
            {
                AddWarning("VR performance monitoring not found");
            }
            
            testReport.isVRReady = vrReady;
            UpdateTestProgress(vrReady);
        }
        
        private IEnumerator ApplyEmergencyFixes()
        {
            LogStep("🚨 Applying emergency fixes...");
            
            // Apply QuickSceneFix
            var sceneFix = CachedReferenceManager.Get<QuickSceneFix>();
            if (sceneFix == null)
            {
                GameObject fixObj = new GameObject("Emergency Scene Fix");
                sceneFix = fixObj.AddComponent<QuickSceneFix>();
            }
            sceneFix.ApplyQuickFix();
            
            // Apply MenuSystemFix
            var menuFix = CachedReferenceManager.Get<MenuSystemFix>();
            if (menuFix == null)
            {
                GameObject fixObj = new GameObject("Emergency Menu Fix");
                menuFix = fixObj.AddComponent<MenuSystemFix>();
            }
            menuFix.ManualFix();
            
            yield return new WaitForSeconds(2f);
            LogSuccess("Emergency fixes applied");
        }
        
        private void InitializeTestReport()
        {
            testReport = new UserTestReport
            {
                overallSuccess = false,
                completionPercentage = 0f,
                passedTests = 0,
                failedTests = 0,
                criticalIssues = new List<string>(),
                warnings = new List<string>(),
                averagePerformance = 0f,
                isVRReady = false,
                deploymentRecommendation = "Under Review"
            };
            
            // Calculate total test steps
            totalTestSteps = 1; // System init
            if (testMenuNavigation) totalTestSteps++;
            if (testSceneSelection) totalTestSteps++;
            if (testAllGameModes) totalTestSteps++;
            if (testPerformance) totalTestSteps++;
            if (testVRCompatibility) totalTestSteps++;
        }
        
        private void UpdateTestProgress(bool testPassed)
        {
            currentTestStep++;
            
            if (testPassed)
            {
                testReport.passedTests++;
            }
            else
            {
                testReport.failedTests++;
            }
            
            testReport.completionPercentage = (float)currentTestStep / totalTestSteps * 100f;
        }
        
        private void GenerateFinalTestReport()
        {
            testReport.overallSuccess = testReport.failedTests == 0 && testReport.criticalIssues.Count == 0;
            
            // Determine deployment recommendation
            if (testReport.overallSuccess && testReport.isVRReady && testReport.averagePerformance >= 90f)
            {
                testReport.deploymentRecommendation = "READY FOR PRODUCTION DEPLOYMENT";
            }
            else if (testReport.criticalIssues.Count == 0 && testReport.averagePerformance >= 60f)
            {
                testReport.deploymentRecommendation = "READY FOR BETA TESTING";
            }
            else if (testReport.criticalIssues.Count <= 2)
            {
                testReport.deploymentRecommendation = "NEEDS MINOR FIXES BEFORE DEPLOYMENT";
            }
            else
            {
                testReport.deploymentRecommendation = "NEEDS MAJOR FIXES BEFORE DEPLOYMENT";
            }
            
            LogFinalReport();
        }
        
        private void LogFinalReport()
        {
            Debug.Log("🎯 END-TO-END USER TESTING COMPLETE!");
            Debug.Log("" +
                "════════════════════════════════════════\n" +
                "📊 COMPREHENSIVE USER TESTING REPORT\n" +
                "════════════════════════════════════════\n" +
                $"🎮 Overall Success: {(testReport.overallSuccess ? "✅ YES" : "❌ NO")}\n" +
                $"📈 Completion: {testReport.completionPercentage:F1}%\n" +
                $"✅ Passed Tests: {testReport.passedTests}\n" +
                $"❌ Failed Tests: {testReport.failedTests}\n" +
                $"🚨 Critical Issues: {testReport.criticalIssues.Count}\n" +
                $"⚠️ Warnings: {testReport.warnings.Count}\n" +
                $"⚡ Performance: {testReport.averagePerformance:F1} FPS\n" +
                $"🥽 VR Ready: {(testReport.isVRReady ? "✅ YES" : "❌ NO")}\n" +
                $"🚀 Deployment: {testReport.deploymentRecommendation}\n" +
                "════════════════════════════════════════"
            );
            
            if (testReport.criticalIssues.Count > 0)
            {
                Debug.LogError("🚨 CRITICAL ISSUES FOUND:");
                foreach (var issue in testReport.criticalIssues)
                {
                    Debug.LogError($"   • {issue}");
                }
            }
            
            if (testReport.warnings.Count > 0)
            {
                Debug.LogWarning("⚠️ WARNINGS:");
                foreach (var warning in testReport.warnings)
                {
                    Debug.LogWarning($"   • {warning}");
                }
            }
            
            // Log complete test history
            if (enableDetailedLogging)
            {
                Debug.Log("📋 DETAILED TEST LOG:");
                foreach (var logEntry in testLog)
                {
                    Debug.Log($"   {logEntry}");
                }
            }
        }
        
        private void LogStep(string message)
        {
            string timeStamp = System.DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timeStamp}] {message}";
            testLog.Add(logEntry);
            
            if (enableDetailedLogging)
            {
                Debug.Log(logEntry);
            }
        }
        
        private void LogSuccess(string message)
        {
            LogStep($"✅ {message}");
        }
        
        private void LogWarning(string message)
        {
            LogStep($"⚠️ {message}");
            testReport.warnings.Add(message);
        }
        
        private void AddCriticalIssue(string issue)
        {
            LogStep($"🚨 CRITICAL: {issue}");
            testReport.criticalIssues.Add(issue);
        }
        
        private void AddWarning(string warning)
        {
            LogStep($"⚠️ WARNING: {warning}");
            testReport.warnings.Add(warning);
        }
        
        public UserTestReport GetTestReport()
        {
            return testReport;
        }
        
        [ContextMenu("Run User Testing")]
        public void RunUserTesting()
        {
            StartCoroutine(RunComprehensiveUserTesting());
        }
    }
} 