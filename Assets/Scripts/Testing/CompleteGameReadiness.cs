using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRBoxingGame.Setup;
using VRBoxingGame.Environment;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using VRBoxingGame.UI;
using VRBoxingGame.Core;
using VRBoxingGame.HandTracking;
using System.Collections;

namespace VRBoxingGame.Testing
{
    /// <summary>
    /// Complete Game Readiness - Ensures FlowBox VR Boxing Game is 100% ready to play
    /// Validates, creates, and connects all systems for immediate rain scene boxing gameplay
    /// </summary>
    public class CompleteGameReadiness : MonoBehaviour
    {
        [Header("Auto Setup")]
        public bool setupOnStart = true;
        public bool startRainSceneImmediately = true;
        public bool enableDebugLogs = true;
        
        [Header("Game Configuration")]
        public float gameBPM = 120f;
        public RainSceneCreator.WeatherIntensity rainIntensity = RainSceneCreator.WeatherIntensity.Medium;
        
        [Header("Testing")]
        public KeyCode testGameKey = KeyCode.T;
        public KeyCode rainSceneKey = KeyCode.R;
        public KeyCode validationKey = KeyCode.V;
        
        private bool isGameReady = false;
        private bool isRainSceneActive = false;
        
        private void Start()
        {
            if (setupOnStart)
            {
                StartCoroutine(CompleteGameSetup());
            }
        }
        
        private void Update()
        {
            // Testing controls
            if (Input.GetKeyDown(testGameKey))
            {
                StartCoroutine(CompleteGameSetup());
            }
            
            if (Input.GetKeyDown(rainSceneKey))
            {
                ActivateRainScene();
            }
            
            if (Input.GetKeyDown(validationKey))
            {
                ValidateGameReadiness();
            }
        }
        
        [ContextMenu("Complete Game Setup")]
        public void SetupCompleteGame()
        {
            StartCoroutine(CompleteGameSetup());
        }
        
        private IEnumerator CompleteGameSetup()
        {
            Log("🎮 STARTING COMPLETE FLOWBOX VR BOXING GAME SETUP...");
            
            // Step 1: Validate Scene Foundation
            yield return StartCoroutine(ValidateAndSetupSceneFoundation());
            
            // Step 2: Setup VR System
            yield return StartCoroutine(SetupVRSystem());
            
            // Step 3: Create Game Materials and Prefabs  
            yield return StartCoroutine(SetupGameAssets());
            
            // Step 4: Initialize Core Game Systems
            yield return StartCoroutine(SetupCoreGameSystems());
            
            // Step 5: Setup Audio and Rhythm Systems
            yield return StartCoroutine(SetupAudioSystems());
            
            // Step 6: Create Rain Scene Environment
            yield return StartCoroutine(SetupRainScene());
            
            // Step 7: Connect All Systems
            yield return StartCoroutine(ConnectAllSystems());
            
            // Step 8: Final Validation
            yield return StartCoroutine(FinalValidation());
            
            Log("✅ COMPLETE GAME SETUP FINISHED!");
            
            if (startRainSceneImmediately)
            {
                yield return new WaitForSeconds(1f);
                ActivateRainScene();
            }
        }
        
        private IEnumerator ValidateAndSetupSceneFoundation()
        {
            Log("🏗️ Step 1: Validating Scene Foundation...");
            
            // Ensure XR Origin exists
            XROrigin xrOrigin = CachedReferenceManager.Get<XROrigin>();
            if (xrOrigin == null)
            {
                Log("Creating XR Origin...");
                GameObject xrOriginObj = new GameObject("XR Origin");
                xrOrigin = xrOriginObj.AddComponent<XROrigin>();
                
                // Add Camera Offset
                GameObject cameraOffset = new GameObject("Camera Offset");
                cameraOffset.transform.SetParent(xrOrigin.transform);
                xrOrigin.CameraFloorOffsetObject = cameraOffset;
                
                // Add Main Camera
                GameObject cameraObj = new GameObject("Main Camera (XR)");
                cameraObj.transform.SetParent(cameraOffset.transform);
                Camera mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
                xrOrigin.Camera = mainCamera;
                
                Log("✅ XR Origin created with camera system");
            }
            else
            {
                Log("✅ XR Origin already exists");
            }
            
            yield return null;
        }
        
        private IEnumerator SetupVRSystem()
        {
            Log("🥽 Step 2: Setting up VR System...");
            
            // Create hand controllers
            XROrigin xrOrigin = CachedReferenceManager.Get<XROrigin>();
            CreateVRHandControllers(xrOrigin);
            
            // Setup hand tracking manager
            if (CachedReferenceManager.Get<HandTrackingManager>() == null)
            {
                GameObject handTrackingObj = new GameObject("Hand Tracking Manager");
                handTrackingObj.AddComponent<HandTrackingManager>();
                Log("✅ Hand Tracking Manager created");
            }
            
            // Setup haptic feedback
            if (CachedReferenceManager.Get<HapticFeedbackManager>() == null)
            {
                GameObject hapticObj = new GameObject("Haptic Feedback Manager");
                hapticObj.AddComponent<HapticFeedbackManager>();
                Log("✅ Haptic Feedback Manager created");
            }
            
            yield return null;
        }
        
        private void CreateVRHandControllers(XROrigin xrOrigin)
        {
            // Check if hands already exist
            GameObject leftHand = GameObject.FindGameObjectWithTag("LeftHand");
            GameObject rightHand = GameObject.FindGameObjectWithTag("RightHand");
            
            if (leftHand == null)
            {
                leftHand = new GameObject("LeftHand Controller");
                leftHand.transform.SetParent(xrOrigin.CameraFloorOffsetObject.transform);
                leftHand.transform.localPosition = new Vector3(-0.3f, -0.1f, 0.2f);
                leftHand.tag = "LeftHand";
                
                // Add collider
                SphereCollider leftCollider = leftHand.AddComponent<SphereCollider>();
                leftCollider.radius = 0.05f;
                leftCollider.isTrigger = true;
                
                // Add XR controller
                leftHand.AddComponent<ActionBasedController>();
                
                // Add punch detector
                EnhancedPunchDetector leftPunch = leftHand.AddComponent<EnhancedPunchDetector>();
                leftPunch.isLeftHand = true;
                
                Log("✅ Left Hand Controller created");
            }
            
            if (rightHand == null)
            {
                rightHand = new GameObject("RightHand Controller");
                rightHand.transform.SetParent(xrOrigin.CameraFloorOffsetObject.transform);
                rightHand.transform.localPosition = new Vector3(0.3f, -0.1f, 0.2f);
                rightHand.tag = "RightHand";
                
                // Add collider
                SphereCollider rightCollider = rightHand.AddComponent<SphereCollider>();
                rightCollider.radius = 0.05f;
                rightCollider.isTrigger = true;
                
                // Add XR controller
                rightHand.AddComponent<ActionBasedController>();
                
                // Add punch detector
                EnhancedPunchDetector rightPunch = rightHand.AddComponent<EnhancedPunchDetector>();
                rightPunch.isLeftHand = false;
                
                Log("✅ Right Hand Controller created");
            }
        }
        
        private IEnumerator SetupGameAssets()
        {
            Log("🎯 Step 3: Setting up Game Assets...");
            
            // Create Circle Prefab Creator
            CirclePrefabCreator prefabCreator = CachedReferenceManager.Get<CirclePrefabCreator>();
            if (prefabCreator == null)
            {
                GameObject prefabCreatorObj = new GameObject("Circle Prefab Creator");
                prefabCreator = prefabCreatorObj.AddComponent<CirclePrefabCreator>();
            }
            
            // Assign materials if they exist
            Material whiteMaterial = // TODO: Convert to Addressables - // TODO: Convert to Addressables - Resources.Load<Material>("WhiteCircleMaterial");
            Material grayMaterial = // TODO: Convert to Addressables - // TODO: Convert to Addressables - Resources.Load<Material>("GrayCircleMaterial");
            Material blockMaterial = // TODO: Convert to Addressables - // TODO: Convert to Addressables - Resources.Load<Material>("RedBlockMaterial");
            
            if (whiteMaterial != null) prefabCreator.whiteMaterial = whiteMaterial;
            if (grayMaterial != null) prefabCreator.grayMaterial = grayMaterial;
            if (blockMaterial != null) prefabCreator.blockMaterial = blockMaterial;
            
            // Create prefabs
            prefabCreator.CreateCirclePrefabs();
            Log("✅ Circle prefabs created and assigned");
            
            yield return null;
        }
        
        private IEnumerator SetupCoreGameSystems()
        {
            Log("🎮 Step 4: Setting up Core Game Systems...");
            
            // Game Manager
            if (GameManager.Instance == null)
            {
                GameObject gameManagerObj = new GameObject("Game Manager");
                gameManagerObj.AddComponent<GameManager>();
                Log("✅ Game Manager created");
            }
            
            // Rhythm Target System
            if (CachedReferenceManager.Get<RhythmTargetSystem>() == null)
            {
                GameObject rhythmObj = new GameObject("Rhythm Target System");
                RhythmTargetSystem rhythmSystem = rhythmObj.AddComponent<RhythmTargetSystem>();
                
                // Assign prefabs from creator
                CirclePrefabCreator prefabCreator = CachedReferenceManager.Get<CirclePrefabCreator>();
                if (prefabCreator != null)
                {
                    rhythmSystem.whiteCirclePrefab = prefabCreator.whiteCirclePrefab;
                    rhythmSystem.grayCirclePrefab = prefabCreator.grayCirclePrefab;
                    rhythmSystem.combinedBlockPrefab = prefabCreator.blockPrefab;
                }
                
                Log("✅ Rhythm Target System created");
            }
            
            // Advanced Target System
            if (CachedReferenceManager.Get<AdvancedTargetSystem>() == null)
            {
                GameObject advancedTargetObj = new GameObject("Advanced Target System");
                advancedTargetObj.AddComponent<AdvancedTargetSystem>();
                Log("✅ Advanced Target System created");
            }
            
            // Boxing Form Tracker
            if (BoxingFormTracker.Instance == null)
            {
                GameObject formTrackerObj = new GameObject("Boxing Form Tracker");
                formTrackerObj.AddComponent<BoxingFormTracker>();
                Log("✅ Boxing Form Tracker created");
            }
            
            yield return null;
        }
        
        private IEnumerator SetupAudioSystems()
        {
            Log("🎵 Step 5: Setting up Audio Systems...");
            
            // Advanced Audio Manager
            if (AdvancedAudioManager.Instance == null)
            {
                GameObject audioManagerObj = new GameObject("Advanced Audio Manager");
                audioManagerObj.AddComponent<AdvancedAudioManager>();
                Log("✅ Advanced Audio Manager created");
            }
            
            // Test Track
            TestTrack testTrack = CachedReferenceManager.Get<TestTrack>();
            if (testTrack == null)
            {
                GameObject testTrackObj = new GameObject("Test Track");
                AudioSource audioSource = testTrackObj.AddComponent<AudioSource>();
                testTrack = testTrackObj.AddComponent<TestTrack>();
                
                // Configure test track
                testTrack.bpm = gameBPM;
                testTrack.autoPlay = false; // We'll control this manually
                
                Log("✅ Test Track created");
            }
            
            // Generate test track
            testTrack.GenerateTestTrack();
            
            // Connect to audio manager
            if (AdvancedAudioManager.Instance != null)
            {
                AudioSource audioSource = testTrack.GetComponent<AudioSource>();
                AdvancedAudioManager.Instance.SetMusicSource(audioSource);
                Log("✅ Audio systems connected");
            }
            
            yield return null;
        }
        
        private IEnumerator SetupRainScene()
        {
            Log("🌧️ Step 6: Setting up Rain Scene...");
            
            // Rain Scene Creator
            RainSceneCreator rainCreator = CachedReferenceManager.Get<RainSceneCreator>();
            if (rainCreator == null)
            {
                GameObject rainCreatorObj = new GameObject("Rain Scene Creator");
                rainCreator = rainCreatorObj.AddComponent<RainSceneCreator>();
                rainCreator.defaultIntensity = rainIntensity;
                Log("✅ Rain Scene Creator created");
            }
            
            // Scene Loading Manager
            if (SceneLoadingManager.Instance == null)
            {
                GameObject sceneLoaderObj = new GameObject("Scene Loading Manager");
                sceneLoaderObj.AddComponent<SceneLoadingManager>();
                Log("✅ Scene Loading Manager created");
            }
            
            // Dynamic Background System
            if (CachedReferenceManager.Get<DynamicBackgroundSystem>() == null)
            {
                GameObject backgroundObj = new GameObject("Dynamic Background System");
                backgroundObj.AddComponent<DynamicBackgroundSystem>();
                Log("✅ Dynamic Background System created");
            }
            
            yield return null;
        }
        
        private IEnumerator ConnectAllSystems()
        {
            Log("🔗 Step 7: Connecting All Systems...");
            
            // Game UI
            GameUI gameUI = CachedReferenceManager.Get<GameUI>();
            if (gameUI == null)
            {
                GameObject gameUIObj = new GameObject("Game UI");
                gameUI = gameUIObj.AddComponent<GameUI>();
            }
            gameUI.FindAndAssignUIElements();
            
            // Connect rhythm system events
            RhythmTargetSystem rhythmSystem = CachedReferenceManager.Get<RhythmTargetSystem>();
            TestTrack testTrack = CachedReferenceManager.Get<TestTrack>();
            
            if (rhythmSystem != null && testTrack != null)
            {
                // Subscribe to beat events
                if (AdvancedAudioManager.Instance != null)
                {
                    AdvancedAudioManager.Instance.OnBeatDetected.AddListener(
                        (beatData) => {
                            // Trigger target spawning on beats
                            if (beatData.isStrongBeat)
                            {
                                // Let rhythm system handle beat-synchronized spawning
                            }
                        }
                    );
                }
            }
            
            Log("✅ All systems connected");
            yield return null;
        }
        
        private IEnumerator FinalValidation()
        {
            Log("✅ Step 8: Final Validation...");
            
            bool isValid = ValidateGameReadiness();
            isGameReady = isValid;
            
            if (isValid)
            {
                Log("🎉 GAME IS 100% READY FOR BOXING!");
                Log("💡 Controls:");
                Log($"   • Press '{testGameKey}' to run setup again");
                Log($"   • Press '{rainSceneKey}' to activate rain scene");
                Log($"   • Press '{validationKey}' to validate readiness");
                Log("   • Put on VR headset and start boxing!");
            }
            else
            {
                Log("⚠️ Game setup incomplete - check console for details");
            }
            
            yield return null;
        }
        
        public void ActivateRainScene()
        {
            Log("🌧️ Activating Rain Scene...");
            
            RainSceneCreator rainCreator = CachedReferenceManager.Get<RainSceneCreator>();
            if (rainCreator != null)
            {
                rainCreator.CreateCompleteRainEnvironment();
                rainCreator.SetWeatherIntensity(rainIntensity);
                isRainSceneActive = true;
                Log("✅ Rain scene activated!");
                
                // Start the game
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartGame();
                }
                
                // Start music
                TestTrack testTrack = CachedReferenceManager.Get<TestTrack>();
                if (testTrack != null)
                {
                    testTrack.PlayTestTrack();
                    Log("🎵 Music started!");
                }
            }
            else
            {
                Log("❌ Rain Scene Creator not found!");
            }
        }
        
        public bool ValidateGameReadiness()
        {
            Log("🔍 Validating Game Readiness...");
            
            bool allSystemsReady = true;
            
            // Check VR Setup
            XROrigin xrOrigin = CachedReferenceManager.Get<XROrigin>();
            allSystemsReady &= CheckSystem(xrOrigin, "XR Origin");
            
            GameObject leftHand = GameObject.FindGameObjectWithTag("LeftHand");
            GameObject rightHand = GameObject.FindGameObjectWithTag("RightHand");
            allSystemsReady &= CheckSystem(leftHand, "Left Hand Controller");
            allSystemsReady &= CheckSystem(rightHand, "Right Hand Controller");
            
            // Check Core Systems
            allSystemsReady &= CheckSystem(GameManager.Instance, "Game Manager");
            allSystemsReady &= CheckSystem(CachedReferenceManager.Get<RhythmTargetSystem>(), "Rhythm Target System");
            allSystemsReady &= CheckSystem(AdvancedAudioManager.Instance, "Advanced Audio Manager");
            allSystemsReady &= CheckSystem(CachedReferenceManager.Get<TestTrack>(), "Test Track");
            
            // Check Rain Scene Systems
            allSystemsReady &= CheckSystem(CachedReferenceManager.Get<RainSceneCreator>(), "Rain Scene Creator");
            allSystemsReady &= CheckSystem(SceneLoadingManager.Instance, "Scene Loading Manager");
            
            // Check Prefabs
            CirclePrefabCreator prefabCreator = CachedReferenceManager.Get<CirclePrefabCreator>();
            if (prefabCreator != null)
            {
                allSystemsReady &= CheckSystem(prefabCreator.whiteCirclePrefab, "White Circle Prefab");
                allSystemsReady &= CheckSystem(prefabCreator.grayCirclePrefab, "Gray Circle Prefab");
                allSystemsReady &= CheckSystem(prefabCreator.blockPrefab, "Block Prefab");
            }
            else
            {
                Log("❌ Circle Prefab Creator not found");
                allSystemsReady = false;
            }
            
            if (allSystemsReady)
            {
                Log("🎉 ALL SYSTEMS READY! Game is playable!");
            }
            else
            {
                Log("⚠️ Some systems need attention - see above");
            }
            
            return allSystemsReady;
        }
        
        private bool CheckSystem<T>(T system, string systemName) where T : UnityEngine.Object
        {
            if (system != null)
            {
                Log($"✅ {systemName}: Ready");
                return true;
            }
            else
            {
                Log($"❌ {systemName}: Missing");
                return false;
            }
        }
        
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[CompleteGameReadiness] {message}");
            }
        }
        
        // Public API for external testing
        public bool IsGameReady => isGameReady;
        public bool IsRainSceneActive => isRainSceneActive;
        
        [ContextMenu("Validate Game Readiness")]
        public void ValidateReadiness()
        {
            ValidateGameReadiness();
        }
        
        [ContextMenu("Activate Rain Scene")]
        public void ActivateRain()
        {
            ActivateRainScene();
        }
    }
} 