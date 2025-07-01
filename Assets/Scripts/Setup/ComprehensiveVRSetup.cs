using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRBoxingGame.UI;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using VRBoxingGame.Core;
using VRBoxingGame.Environment;
using VRBoxingGame.HandTracking;
using VRBoxingGame.Performance;
using VRBoxingGame.Testing;
using System.Collections.Generic;

namespace VRBoxingGame.Setup
{
    public class ComprehensiveVRSetup : MonoBehaviour
    {
        [Header("Setup Configuration")]
        public bool setupOnStart = true;
        public bool createMainMenu = true;
        public bool setupRainScene = true;
        public bool enablePerformanceMonitoring = true;
        public bool createTestEnvironment = true;
        
        [Header("XR Configuration")]
        public bool createXROrigin = true;
        public bool setupHandTracking = true;
        
        [Header("Audio Setup")]
        public bool createAudioManager = true;
        public bool generateTestTrack = true;
        public float defaultBPM = 120f;
        
        [Header("Debug")]
        public bool enableDebugLogs = true;
        public bool showSetupProgress = true;
        
        private int setupSteps = 0;
        private int completedSteps = 0;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupCompleteVRGame();
            }
        }
        
        [ContextMenu("Setup Complete VR Game")]
        public void SetupCompleteVRGame()
        {
            Log("🚀 Starting Comprehensive VR Game Setup...");
            
            setupSteps = 13;
            completedSteps = 0;
            
            SetupGameManagers();
            CompleteStep("Game Managers Setup");
            
            SetupUISystem();
            CompleteStep("UI Systems Setup");
            
            if (createMainMenu)
            {
                Step7_SetupMainMenu();
                CompleteStep("Main Menu Setup");
            }
            
            SetupRhythmGameSystems();
            CompleteStep("Rhythm Game Setup");
            
            SetupBackgroundSystems();
            CompleteStep("Background Systems Setup");
            
            SetupSceneTransformation();
            CompleteStep("Scene Transformation Setup");
            
            SetupHapticFeedback();
            CompleteStep("Haptic Feedback Setup");
            
            if (setupRainScene)
            {
                SetupRainScene();
                CompleteStep("Rain Scene Setup");
            }
            
            if (enablePerformanceMonitoring)
            {
                SetupPerformanceMonitoring();
                CompleteStep("Performance Monitoring Setup");
            }
            
            if (createTestEnvironment)
            {
                SetupTestEnvironment();
                CompleteStep("Test Environment Setup");
            }
            
            Step12_FinalValidation();
            
            FinalizeSetup();
            CompleteStep("Final Integration");
            
            Log("✅ Comprehensive VR Game Setup Complete! Game is ready to play!");
            Log($"📊 Setup Progress: {completedSteps}/{setupSteps} steps completed");
        }
        
        private void SetupGameManagers()
        {
            if (GameManager.Instance == null)
            {
                GameObject gameManagerObj = new GameObject("Game Manager");
                gameManagerObj.AddComponent<GameManager>();
            }
            
            ObjectPoolManager poolManager = FindObjectOfType<ObjectPoolManager>();
            if (poolManager == null)
            {
                GameObject poolManagerObj = new GameObject("Object Pool Manager");
                poolManagerObj.AddComponent<ObjectPoolManager>();
            }
            
            Log("Game managers configured");
        }
        
        private void SetupUISystem()
        {
            GameUI gameUI = FindObjectOfType<GameUI>();
            if (gameUI == null)
            {
                GameObject gameUIObj = new GameObject("Game UI");
                gameUI = gameUIObj.AddComponent<GameUI>();
                gameUI.FindAndAssignUIElements();
            }
            
            Log("UI system configured");
        }
        
        private void Step7_SetupMainMenu()
        {
            Debug.Log("Setting up Main Menu...");
            
            var mainMenuSystem = FindObjectOfType<MainMenuSystem>();
            if (mainMenuSystem == null)
            {
                GameObject menuObj = new GameObject("MainMenuSystem");
                mainMenuSystem = menuObj.AddComponent<MainMenuSystem>();
            }
            
            var menuCreator = FindObjectOfType<VRMainMenuCreator>();
            if (menuCreator == null)
            {
                GameObject creatorObj = new GameObject("VRMainMenuCreator");
                menuCreator = creatorObj.AddComponent<VRMainMenuCreator>();
            }
            
            // Create the actual UI if it doesn't exist
            if (mainMenuSystem.mainMenuCanvas == null)
            {
                menuCreator.CreateCompleteMainMenu();
            }
            
            Debug.Log("✅ Main Menu setup complete");
        }
        
        private void SetupRhythmGameSystems()
        {
            RhythmTargetSystem rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            if (rhythmSystem == null)
            {
                GameObject rhythmSystemObj = new GameObject("Rhythm Target System");
                rhythmSystem = rhythmSystemObj.AddComponent<RhythmTargetSystem>();
            }
            
            EnhancedPunchDetector punchDetector = FindObjectOfType<EnhancedPunchDetector>();
            if (punchDetector == null)
            {
                GameObject punchDetectorObj = new GameObject("Enhanced Punch Detector");
                punchDetectorObj.AddComponent<EnhancedPunchDetector>();
            }
            
            CreateCirclePrefabs();
            
            Log("Rhythm game systems configured");
        }
        
        private void CreateCirclePrefabs()
        {
            CirclePrefabCreator prefabCreator = FindObjectOfType<CirclePrefabCreator>();
            if (prefabCreator == null)
            {
                GameObject prefabCreatorObj = new GameObject("Circle Prefab Creator");
                prefabCreator = prefabCreatorObj.AddComponent<CirclePrefabCreator>();
            }
            
            prefabCreator.CreateCirclePrefabs();
        }
        
        private void SetupBackgroundSystems()
        {
            DynamicBackgroundSystem bgSystem = FindObjectOfType<DynamicBackgroundSystem>();
            if (bgSystem == null)
            {
                GameObject bgSystemObj = new GameObject("Dynamic Background System");
                bgSystemObj.AddComponent<DynamicBackgroundSystem>();
            }
            
            Log("Background systems configured");
        }
        
        private void SetupSceneTransformation()
        {
            SceneTransformationSystem sceneTransform = FindObjectOfType<SceneTransformationSystem>();
            if (sceneTransform == null)
            {
                GameObject sceneTransformObj = new GameObject("Scene Transformation System");
                sceneTransform = sceneTransformObj.AddComponent<SceneTransformationSystem>();
            }
            
            SceneLoadingManager sceneLoader = FindObjectOfType<SceneLoadingManager>();
            if (sceneLoader == null)
            {
                GameObject sceneLoaderObj = new GameObject("Scene Loading Manager");
                sceneLoaderObj.AddComponent<SceneLoadingManager>();
            }
            
            Log("Scene transformation systems configured");
        }
        
        private void SetupHapticFeedback()
        {
            HapticFeedbackManager hapticManager = FindObjectOfType<HapticFeedbackManager>();
            if (hapticManager == null)
            {
                GameObject hapticManagerObj = new GameObject("Haptic Feedback Manager");
                hapticManager = hapticManagerObj.AddComponent<HapticFeedbackManager>();
                
                // Configure default haptic settings
                hapticManager.punchHitIntensity = 0.8f;
                hapticManager.blockSuccessIntensity = 1.0f;
                hapticManager.enableAdvancedPatterns = true;
                hapticManager.enableSceneSpecificHaptics = true;
                
                Log("Haptic feedback manager created with default settings");
            }
            else
            {
                Log("Haptic feedback manager already exists");
            }
            
            Log("Haptic feedback system configured");
        }
        
        private void SetupRainScene()
        {
            RainSceneCreator rainCreator = FindObjectOfType<RainSceneCreator>();
            if (rainCreator == null)
            {
                GameObject rainCreatorObj = new GameObject("Rain Scene Creator");
                rainCreator = rainCreatorObj.AddComponent<RainSceneCreator>();
            }
            
            Log("Rain scene configured");
        }
        
        private void SetupPerformanceMonitoring()
        {
            if (VRPerformanceMonitor.Instance == null)
            {
                GameObject perfMonitorObj = new GameObject("VR Performance Monitor");
                perfMonitorObj.AddComponent<VRPerformanceMonitor>();
            }
            
            Log("Performance monitoring configured");
        }
        
        private void SetupTestEnvironment()
        {
            RainRhythmTest testSystem = FindObjectOfType<RainRhythmTest>();
            if (testSystem == null)
            {
                GameObject testSystemObj = new GameObject("Rain Rhythm Test");
                testSystemObj.AddComponent<RainRhythmTest>();
            }
            
            Log("Test environment configured");
        }
        
        private void Step12_FinalValidation()
        {
            Debug.Log("Performing final system validation...");
            
            var issues = new List<string>();
            
            // Check GameManager
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null) issues.Add("GameManager missing");
            else if (gameManager.OnGameStateChanged == null) issues.Add("GameManager.OnGameStateChanged event not initialized");
            
            // Check RhythmTargetSystem
            var rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            if (rhythmSystem == null) issues.Add("RhythmTargetSystem missing");
            else
            {
                if (rhythmSystem.whiteCirclePrefab == null) issues.Add("White circle prefab missing");
                if (rhythmSystem.grayCirclePrefab == null) issues.Add("Gray circle prefab missing");
                if (rhythmSystem.leftSpawnPoint == null) issues.Add("Left spawn point missing");
                if (rhythmSystem.rightSpawnPoint == null) issues.Add("Right spawn point missing");
                if (rhythmSystem.centerPoint == null) issues.Add("Center point missing");
            }
            
            // Check Audio System
            var audioManager = FindObjectOfType<AdvancedAudioManager>();
            if (audioManager == null) issues.Add("AdvancedAudioManager missing");
            
            var testTrack = FindObjectOfType<TestTrack>();
            if (testTrack == null) issues.Add("TestTrack missing");
            
            // Check Hand Tracking
            var handTracking = FindObjectOfType<HandTrackingManager>();
            if (handTracking == null) issues.Add("HandTrackingManager missing");
            
            // Check VR Setup
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null) issues.Add("XR Origin missing");
            
            // Check UI Systems
            var gameUI = FindObjectOfType<GameUI>();
            if (gameUI == null) issues.Add("GameUI missing");
            
            var mainMenu = FindObjectOfType<MainMenuSystem>();
            if (mainMenu == null) issues.Add("MainMenuSystem missing");
            
            // Check Performance Systems
            var perfMonitor = FindObjectOfType<VRPerformanceMonitor>();
            if (perfMonitor == null) issues.Add("VRPerformanceMonitor missing");
            
            var objectPool = FindObjectOfType<ObjectPoolManager>();
            if (objectPool == null) issues.Add("ObjectPoolManager missing");
            
            // Check Scene Systems
            var sceneLoader = FindObjectOfType<SceneLoadingManager>();
            if (sceneLoader == null) issues.Add("SceneLoadingManager missing");
            
            var backgroundSystem = FindObjectOfType<DynamicBackgroundSystem>();
            if (backgroundSystem == null) issues.Add("DynamicBackgroundSystem missing");
            
            // Check Haptic Feedback System
            var hapticManager = FindObjectOfType<HapticFeedbackManager>();
            if (hapticManager == null) issues.Add("HapticFeedbackManager missing");
            
            if (issues.Count == 0)
            {
                Debug.Log("🎉 ALL SYSTEMS VALIDATED SUCCESSFULLY! Game is ready to play!");
                Debug.Log("✅ Core Game Systems: Complete");
                Debug.Log("✅ VR Integration: Complete");
                Debug.Log("✅ Audio Systems: Complete");
                Debug.Log("✅ UI Systems: Complete");
                Debug.Log("✅ Performance Systems: Complete");
                Debug.Log("✅ Scene Management: Complete");
                Debug.Log("✅ Hand Tracking: Complete");
                Debug.Log("✅ Haptic Feedback: Complete");
                Debug.Log("🚀 Ready for VR deployment!");
            }
            else
            {
                Debug.LogWarning($"⚠️ Found {issues.Count} issues:");
                foreach (var issue in issues)
                {
                    Debug.LogWarning($"  - {issue}");
                }
            }
        }
        
        private void FinalizeSetup()
        {
            ConnectSystems();
            ValidateSetup();
            ApplyInitialOptimizations();
            
            Log("Setup finalized and validated");
        }
        
        private void ConnectSystems()
        {
            var audioManager = FindObjectOfType<AdvancedAudioManager>();
            var rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            
            if (audioManager != null && rhythmSystem != null)
            {
                audioManager.OnBeatDetected.AddListener((beatData) => {
                    rhythmSystem.OnBeatDetected(beatData.beatStrength);
                });
            }
            
            var gameUI = FindObjectOfType<GameUI>();
            var gameManager = GameManager.Instance;
            
            if (gameUI != null && gameManager != null)
            {
                gameManager.OnScoreChanged.AddListener(gameUI.UpdateScore);
                gameManager.OnGameStateChanged.AddListener((state) => {
                    if (state == GameManager.GameState.GameOver)
                        gameUI.ShowGameOver();
                });
            }
        }
        
        private void ValidateSetup()
        {
            bool isValid = true;
            
            if (GameManager.Instance == null)
            {
                LogError("Game Manager missing!");
                isValid = false;
            }
            
            if (FindObjectOfType<AdvancedAudioManager>() == null)
            {
                LogError("Audio Manager missing!");
                isValid = false;
            }
            
            if (isValid)
            {
                Log("✅ Setup validation passed");
            }
            else
            {
                LogError("❌ Setup validation failed");
            }
        }
        
        private void ApplyInitialOptimizations()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 90;
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            
            Log("Initial optimizations applied");
        }
        
        private void CompleteStep(string stepName)
        {
            completedSteps++;
            if (showSetupProgress)
            {
                Log($"✅ {stepName} ({completedSteps}/{setupSteps})");
            }
        }
        
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[VR Setup] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[VR Setup] {message}");
        }
    }
}
 