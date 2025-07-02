using UnityEngine;
using VRBoxingGame.Testing;
using VRBoxingGame.Setup;

namespace VRBoxingGame.Setup
{
    /// <summary>
    /// TestScene Initializer - Add this to TestScene to ensure immediate rain scene boxing gameplay
    /// Automatically sets up and validates all systems for VR boxing
    /// </summary>
    public class TestSceneInitializer : MonoBehaviour
    {
        [Header("Initialization")]
        public bool initializeOnAwake = true;
        public bool showWelcomeMessage = true;
        
        [Header("Quick Setup")]
        [Tooltip("Automatically creates CompleteGameReadiness if missing")]
        public bool autoCreateGameReadiness = true;
        
        private CompleteGameReadiness gameReadiness;
        
        private void Awake()
        {
            if (initializeOnAwake)
            {
                InitializeTestScene();
            }
        }
        
        [ContextMenu("Initialize Test Scene")]
        public void InitializeTestScene()
        {
            Debug.Log("🎮 TestScene Initializer: Starting FlowBox VR Boxing Game...");
            
            // Find or create CompleteGameReadiness
            gameReadiness = FindObjectOfType<CompleteGameReadiness>();
            if (gameReadiness == null && autoCreateGameReadiness)
            {
                GameObject readinessObj = new GameObject("Complete Game Readiness");
                gameReadiness = readinessObj.AddComponent<CompleteGameReadiness>();
                
                // Configure for immediate rain scene
                gameReadiness.setupOnStart = true;
                gameReadiness.startRainSceneImmediately = true;
                gameReadiness.enableDebugLogs = true;
                
                Debug.Log("✅ CompleteGameReadiness created and configured");
            }
            
            // Add TestSceneSetup if missing
            TestSceneSetup testSetup = FindObjectOfType<TestSceneSetup>();
            if (testSetup == null)
            {
                GameObject testSetupObj = new GameObject("Test Scene Setup");
                testSetup = testSetupObj.AddComponent<TestSceneSetup>();
                testSetup.setupOnAwake = true;
                testSetup.enableRainSceneByDefault = true;
                
                Debug.Log("✅ TestSceneSetup created");
            }
            
            if (showWelcomeMessage)
            {
                ShowWelcomeMessage();
            }
        }
        
        private void ShowWelcomeMessage()
        {
            Debug.Log("🌧️ ======================================");
            Debug.Log("🥊 FLOWBOX VR BOXING GAME - RAIN SCENE");
            Debug.Log("🌧️ ======================================");
            Debug.Log("🎯 READY TO PLAY!");
            Debug.Log(""); 
            Debug.Log("📱 CONTROLS:");
            Debug.Log("   • T = Run Complete Setup");
            Debug.Log("   • R = Activate Rain Scene");
            Debug.Log("   • V = Validate Game Readiness");
            Debug.Log("");
            Debug.Log("🥽 VR INSTRUCTIONS:");
            Debug.Log("   1. Put on your VR headset");
            Debug.Log("   2. Grab your controllers");
            Debug.Log("   3. Punch white circles with LEFT hand");
            Debug.Log("   4. Punch gray circles with RIGHT hand");
            Debug.Log("   5. Block red spinning cubes with BOTH hands");
            Debug.Log("");
            Debug.Log("🌧️ Rain scene will auto-activate!");
            Debug.Log("🎵 Music starts automatically!");
            Debug.Log("⚡ Lightning and thunder included!");
            Debug.Log("🌧️ ======================================");
        }
        
        [ContextMenu("Show Game Status")]
        public void ShowGameStatus()
        {
            if (gameReadiness != null)
            {
                Debug.Log($"🎮 Game Ready: {gameReadiness.IsGameReady}");
                Debug.Log($"🌧️ Rain Scene Active: {gameReadiness.IsRainSceneActive}");
                gameReadiness.ValidateReadiness();
            }
            else
            {
                Debug.Log("⚠️ CompleteGameReadiness not found - run initialization");
            }
        }
        
        [ContextMenu("Force Rain Scene")]
        public void ForceRainScene()
        {
            if (gameReadiness != null)
            {
                gameReadiness.ActivateRain();
            }
            else
            {
                Debug.Log("⚠️ CompleteGameReadiness not found");
            }
        }
    }
} 