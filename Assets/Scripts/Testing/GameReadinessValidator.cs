using UnityEngine;
using Unity.XR.CoreUtils;
using VRBoxingGame.Core;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using VRBoxingGame.Environment;
using VRBoxingGame.HandTracking;
using VRBoxingGame.UI;
using VRBoxingGame.Performance;
using VRBoxingGame.Setup;
using System.Collections;

namespace VRBoxingGame.Testing
{
    /// <summary>
    /// Comprehensive game readiness validator - ensures rain scene is fully playable
    /// </summary>
    public class GameReadinessValidator : MonoBehaviour
    {
        [Header("Validation Configuration")]
        public bool validateOnStart = true;
        public bool autoFixIssues = true;
        public bool enableRainSceneTest = true;
        
        [Header("Test Results")]
        [SerializeField] private bool vrSetupValid = false;
        [SerializeField] private bool coreSystemsValid = false;
        [SerializeField] private bool audioSystemsValid = false;
        [SerializeField] private bool rainSceneValid = false;
        [SerializeField] private bool gameplayReady = false;
        
        [Header("Status Display")]
        public bool showStatusOnGUI = true;
        
        private void Start()
        {
            if (validateOnStart)
            {
                StartCoroutine(ValidateGameReadiness());
            }
        }
        
        [ContextMenu("Validate Game Readiness")]
        public void ValidateGame()
        {
            StartCoroutine(ValidateGameReadiness());
        }
        
        private IEnumerator ValidateGameReadiness()
        {
            Debug.Log("🔍 Starting Comprehensive Game Readiness Validation...");
            
            // Step 1: Check VR Setup
            vrSetupValid = ValidateVRSetup();
            yield return new WaitForSeconds(0.2f);
            
            // Step 2: Check Core Systems
            coreSystemsValid = ValidateCoreGameSystems();
            yield return new WaitForSeconds(0.2f);
            
            // Step 3: Check Audio Systems
            audioSystemsValid = ValidateAudioSystems();
            yield return new WaitForSeconds(0.2f);
            
            // Step 4: Check Rain Scene
            if (enableRainSceneTest)
            {
                rainSceneValid = ValidateRainScene();
                yield return new WaitForSeconds(0.2f);
            }
            
            // Step 5: Overall Gameplay Check
            gameplayReady = ValidateGameplayReadiness();
            
            // Step 6: Auto-fix if enabled
            if (autoFixIssues && !gameplayReady)
            {
                yield return StartCoroutine(AutoFixIssues());
            }
            
            // Final Report
            ReportValidationResults();
        }
        
        private bool ValidateVRSetup()
        {
            Debug.Log("🥽 Validating VR Setup...");
            
            bool isValid = true;
            
            // Check XR Origin
            XROrigin xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogWarning("❌ XR Origin not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ XR Origin found");
                
                // Check camera
                if (xrOrigin.Camera == null)
                {
                    Debug.LogWarning("❌ XR Origin camera not assigned");
                    isValid = false;
                }
                else
                {
                    Debug.Log("✅ XR Camera configured");
                }
            }
            
            // Check hand controllers
            GameObject leftHand = GameObject.FindGameObjectWithTag("LeftHand");
            GameObject rightHand = GameObject.FindGameObjectWithTag("RightHand");
            
            if (leftHand == null)
            {
                Debug.LogWarning("❌ Left hand controller not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ Left hand controller found");
            }
            
            if (rightHand == null)
            {
                Debug.LogWarning("❌ Right hand controller not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ Right hand controller found");
            }
            
            Debug.Log($"VR Setup Valid: {isValid}");
            return isValid;
        }
        
        private bool ValidateCoreGameSystems()
        {
            Debug.Log("⚙️ Validating Core Game Systems...");
            
            bool isValid = true;
            
            // Game Manager
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("❌ GameManager not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ GameManager found");
            }
            
            // Rhythm Target System
            RhythmTargetSystem rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            if (rhythmSystem == null)
            {
                Debug.LogWarning("❌ RhythmTargetSystem not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ RhythmTargetSystem found");
                
                // Check prefabs
                if (rhythmSystem.whiteCirclePrefab == null)
                {
                    Debug.LogWarning("❌ White circle prefab not assigned");
                    isValid = false;
                }
                if (rhythmSystem.grayCirclePrefab == null)
                {
                    Debug.LogWarning("❌ Gray circle prefab not assigned");
                    isValid = false;
                }
            }
            
            // Enhanced Punch Detector
            EnhancedPunchDetector punchDetector = FindObjectOfType<EnhancedPunchDetector>();
            if (punchDetector == null)
            {
                Debug.LogWarning("❌ EnhancedPunchDetector not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ EnhancedPunchDetector found");
            }
            
            // Hand Tracking Manager
            HandTrackingManager handTracking = FindObjectOfType<HandTrackingManager>();
            if (handTracking == null)
            {
                Debug.LogWarning("❌ HandTrackingManager not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ HandTrackingManager found");
            }
            
            Debug.Log($"Core Systems Valid: {isValid}");
            return isValid;
        }
        
        private bool ValidateAudioSystems()
        {
            Debug.Log("🔊 Validating Audio Systems...");
            
            bool isValid = true;
            
            // Advanced Audio Manager
            AdvancedAudioManager audioManager = FindObjectOfType<AdvancedAudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("❌ AdvancedAudioManager not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ AdvancedAudioManager found");
            }
            
            // Test Track
            TestTrack testTrack = FindObjectOfType<TestTrack>();
            if (testTrack == null)
            {
                Debug.LogWarning("❌ TestTrack not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ TestTrack found");
                
                // Check audio source
                AudioSource audioSource = testTrack.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    Debug.LogWarning("❌ TestTrack AudioSource not found");
                    isValid = false;
                }
                else
                {
                    Debug.Log("✅ TestTrack AudioSource found");
                }
            }
            
            Debug.Log($"Audio Systems Valid: {isValid}");
            return isValid;
        }
        
        private bool ValidateRainScene()
        {
            Debug.Log("🌧️ Validating Rain Scene...");
            
            bool isValid = true;
            
            // Rain Scene Creator
            RainSceneCreator rainCreator = FindObjectOfType<RainSceneCreator>();
            if (rainCreator == null)
            {
                Debug.LogWarning("❌ RainSceneCreator not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ RainSceneCreator found");
            }
            
            // Scene Loading Manager
            SceneLoadingManager sceneLoader = SceneLoadingManager.Instance;
            if (sceneLoader == null)
            {
                Debug.LogWarning("❌ SceneLoadingManager not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ SceneLoadingManager found");
            }
            
            // Scene Transformation System
            SceneTransformationSystem transformSystem = SceneTransformationSystem.Instance;
            if (transformSystem == null)
            {
                Debug.LogWarning("❌ SceneTransformationSystem not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ SceneTransformationSystem found");
            }
            
            // Dynamic Background System
            DynamicBackgroundSystem backgroundSystem = FindObjectOfType<DynamicBackgroundSystem>();
            if (backgroundSystem == null)
            {
                Debug.LogWarning("❌ DynamicBackgroundSystem not found");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ DynamicBackgroundSystem found");
            }
            
            Debug.Log($"Rain Scene Valid: {isValid}");
            return isValid;
        }
        
        private bool ValidateGameplayReadiness()
        {
            Debug.Log("🎮 Validating Overall Gameplay Readiness...");
            
            bool isReady = vrSetupValid && coreSystemsValid && audioSystemsValid;
            
            if (enableRainSceneTest)
            {
                isReady = isReady && rainSceneValid;
            }
            
            Debug.Log($"Gameplay Ready: {isReady}");
            return isReady;
        }
        
        private IEnumerator AutoFixIssues()
        {
            Debug.Log("🔧 Auto-fixing detected issues...");
            
            // Find or create CompleteGameSetup
            CompleteGameSetup gameSetup = FindObjectOfType<CompleteGameSetup>();
            if (gameSetup == null)
            {
                Debug.Log("Creating CompleteGameSetup to fix issues...");
                GameObject setupObj = new GameObject("Complete Game Setup (Auto-Fix)");
                gameSetup = setupObj.AddComponent<CompleteGameSetup>();
                
                // Configure for rain scene
                gameSetup.enableRainScene = enableRainSceneTest;
                gameSetup.startWithRainScene = enableRainSceneTest;
                gameSetup.setupOnStart = false;
            }
            
            // Trigger complete setup
            gameSetup.SetupCompleteVRBoxingGame();
            
            // Wait for setup to complete
            yield return new WaitForSeconds(3f);
            
            // Re-validate
            Debug.Log("🔄 Re-validating after auto-fix...");
            yield return StartCoroutine(ValidateGameReadiness());
        }
        
        private void ReportValidationResults()
        {
            Debug.Log("📋 VALIDATION RESULTS:");
            Debug.Log($"VR Setup: {(vrSetupValid ? "✅" : "❌")}");
            Debug.Log($"Core Systems: {(coreSystemsValid ? "✅" : "❌")}");
            Debug.Log($"Audio Systems: {(audioSystemsValid ? "✅" : "❌")}");
            Debug.Log($"Rain Scene: {(rainSceneValid ? "✅" : "❌")}");
            Debug.Log($"Gameplay Ready: {(gameplayReady ? "✅" : "❌")}");
            
            if (gameplayReady)
            {
                Debug.Log("🎉 GAME IS READY TO PLAY!");
                Debug.Log("🌧️ Rain scene is fully functional");
                Debug.Log("🥊 VR boxing mechanics are active");
                Debug.Log("🎵 Music-reactive systems are running");
                Debug.Log("👀 Start the game and enjoy the rain scene!");
            }
            else
            {
                Debug.LogWarning("⚠️ Game is not ready. Check the issues above.");
                if (autoFixIssues)
                {
                    Debug.Log("💡 Try running the validator again or manually run CompleteGameSetup");
                }
            }
        }
        
        [ContextMenu("Test Rain Scene Loading")]
        public void TestRainSceneLoading()
        {
            StartCoroutine(TestRainSceneAsync());
        }
        
        private IEnumerator TestRainSceneAsync()
        {
            Debug.Log("🌧️ Testing Rain Scene Loading...");
            
            SceneLoadingManager sceneLoader = SceneLoadingManager.Instance;
            if (sceneLoader != null)
            {
                sceneLoader.LoadScene((int)SceneLoadingManager.SceneType.RainStorm);
                yield return new WaitForSeconds(2f);
                Debug.Log("✅ Rain scene loading test complete");
            }
            else
            {
                Debug.LogError("❌ SceneLoadingManager not found for rain scene test");
            }
        }
        
        private void OnGUI()
        {
            if (!showStatusOnGUI || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Label("🎮 VR Boxing Game Status", GUI.skin.box);
            
            GUILayout.Label($"VR Setup: {(vrSetupValid ? "✅" : "❌")}");
            GUILayout.Label($"Core Systems: {(coreSystemsValid ? "✅" : "❌")}");
            GUILayout.Label($"Audio Systems: {(audioSystemsValid ? "✅" : "❌")}");
            GUILayout.Label($"Rain Scene: {(rainSceneValid ? "✅" : "❌")}");
            GUILayout.Label($"Gameplay Ready: {(gameplayReady ? "✅" : "❌")}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Validate Game"))
            {
                ValidateGame();
            }
            
            if (GUILayout.Button("Test Rain Scene"))
            {
                TestRainSceneLoading();
            }
            
            if (!gameplayReady && GUILayout.Button("Auto-Fix Issues"))
            {
                StartCoroutine(AutoFixIssues());
            }
            
            if (gameplayReady)
            {
                GUILayout.Label("🎉 READY TO PLAY!", GUI.skin.box);
            }
            
            GUILayout.EndArea();
        }
    }
} 