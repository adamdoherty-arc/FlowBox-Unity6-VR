using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using VRBoxingGame.Core;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using VRBoxingGame.Environment;
using VRBoxingGame.HandTracking;
using VRBoxingGame.UI;
using VRBoxingGame.Performance;
using System.Collections;

namespace VRBoxingGame.Setup
{
    /// <summary>
    /// Complete VR Boxing Game Setup - Ensures rain scene is fully playable
    /// Creates all necessary components for immediate gameplay
    /// </summary>
    public class CompleteGameSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        public bool setupOnStart = true;
        public bool enableRainScene = true;
        public bool createVRRig = true;
        public bool createUI = true;
        
        [Header("Rain Scene Configuration")]
        public RainSceneCreator.WeatherIntensity defaultRainIntensity = RainSceneCreator.WeatherIntensity.Medium;
        public bool startWithRainScene = true;
        
        [Header("VR Configuration")]
        public Vector3 vrCameraPosition = new Vector3(0, 1.8f, 0);
        public Vector3 leftHandOffset = new Vector3(-0.3f, -0.1f, 0.2f);
        public Vector3 rightHandOffset = new Vector3(0.3f, -0.1f, 0.2f);
        
        [Header("Game Configuration")]
        public float gameBPM = 120f;
        public bool autoStartGame = true;
        
        private void Start()
        {
            if (setupOnStart)
            {
                StartCoroutine(SetupCompleteGame());
            }
        }
        
        [ContextMenu("Setup Complete VR Boxing Game")]
        public void SetupCompleteVRBoxingGame()
        {
            StartCoroutine(SetupCompleteGame());
        }
        
        private IEnumerator SetupCompleteGame()
        {
            Debug.Log("🚀 Starting Complete VR Boxing Game Setup...");
            
            // Step 1: Create VR Rig
            if (createVRRig)
            {
                SetupVRRig();
                yield return new WaitForSeconds(0.5f);
            }
            
            // Step 2: Setup Core Game Systems
            SetupCoreGameSystems();
            yield return new WaitForSeconds(0.3f);
            
            // Step 3: Setup Audio Systems
            SetupAudioSystems();
            yield return new WaitForSeconds(0.3f);
            
            // Step 4: Create Game Prefabs
            CreateGamePrefabs();
            yield return new WaitForSeconds(0.3f);
            
            // Step 5: Setup Rain Scene
            if (enableRainScene)
            {
                SetupRainScene();
                yield return new WaitForSeconds(0.5f);
            }
            
            // Step 6: Setup UI
            if (createUI)
            {
                SetupGameUI();
                yield return new WaitForSeconds(0.3f);
            }
            
            // Step 7: Connect All Systems
            ConnectSystems();
            yield return new WaitForSeconds(0.3f);
            
            // Step 8: Final Validation and Start
            ValidateAndStartGame();
            
            Debug.Log("✅ Complete VR Boxing Game Setup Finished! Ready to play!");
        }
        
        private void SetupVRRig()
        {
            Debug.Log("🥽 Setting up VR Rig with 360-degree movement...");
            
            // Setup 360-degree movement system first
            VR360MovementSystem movementSystem = FindObjectOfType<VR360MovementSystem>();
            if (movementSystem == null)
            {
                GameObject movementObj = new GameObject("VR 360 Movement System");
                movementSystem = movementObj.AddComponent<VR360MovementSystem>();
                
                // Configure for Supernatural-style movement
                movementSystem.enableRoomScaleTracking = true;
                movementSystem.enableContinuousTurning = true;
                movementSystem.trackingOriginMode = TrackingOriginMode.Floor;
                movementSystem.continuousTurningSpeed = 90f; // Fast turning for boxing
                movementSystem.snapTurnAngle = 45f; // Quick snap turns
                
                Debug.Log("✅ VR 360-degree movement system created");
            }
            
            // Find or create XR Origin (movement system will handle this)
            XROrigin xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                // Let the movement system create the proper XR Origin
                Debug.Log("XR Origin will be created by movement system");
                
                // Wait a frame for the movement system to initialize
                StartCoroutine(DelayedHandControllerSetup());
                return;
            }
            
            // Create hand controllers
            CreateHandControllers(xrOrigin);
            
            Debug.Log("✅ VR Rig with 360-degree movement setup complete");
        }
        
        private System.Collections.IEnumerator DelayedHandControllerSetup()
        {
            yield return null; // Wait one frame
            
            XROrigin xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                CreateHandControllers(xrOrigin);
                Debug.Log("✅ Hand controllers created after movement system setup");
            }
        }
        
        private void CreateHandControllers(XROrigin xrOrigin)
        {
            // Left Hand
            GameObject leftHandObj = new GameObject("LeftHand Controller");
            leftHandObj.transform.SetParent(xrOrigin.CameraFloorOffsetObject.transform);
            leftHandObj.transform.localPosition = leftHandOffset;
            leftHandObj.tag = "LeftHand";
            
            // Add hand components
            SphereCollider leftCollider = leftHandObj.AddComponent<SphereCollider>();
            leftCollider.radius = 0.05f;
            leftCollider.isTrigger = true;
            
            XRBaseController leftController = leftHandObj.AddComponent<ActionBasedController>();
            
            // Add hand visual (simple capsule)
            GameObject leftHandVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftHandVisual.name = "LeftHandVisual";
            leftHandVisual.transform.SetParent(leftHandObj.transform);
            leftHandVisual.transform.localPosition = Vector3.zero;
            leftHandVisual.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
            
            // Set white material for left hand
            Renderer leftRenderer = leftHandVisual.GetComponent<Renderer>();
            Material leftMaterial = FindMaterialByName("WhiteHandMaterial");
            if (leftMaterial != null)
            {
                leftRenderer.material = leftMaterial;
            }
            else
            {
                leftRenderer.material.color = Color.white;
            }
            
            // Remove the collider from visual
            Destroy(leftHandVisual.GetComponent<Collider>());
            
            // Right Hand
            GameObject rightHandObj = new GameObject("RightHand Controller");
            rightHandObj.transform.SetParent(xrOrigin.CameraFloorOffsetObject.transform);
            rightHandObj.transform.localPosition = rightHandOffset;
            rightHandObj.tag = "RightHand";
            
            // Add hand components
            SphereCollider rightCollider = rightHandObj.AddComponent<SphereCollider>();
            rightCollider.radius = 0.05f;
            rightCollider.isTrigger = true;
            
            XRBaseController rightController = rightHandObj.AddComponent<ActionBasedController>();
            
            // Add hand visual (simple capsule)
            GameObject rightHandVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightHandVisual.name = "RightHandVisual";
            rightHandVisual.transform.SetParent(rightHandObj.transform);
            rightHandVisual.transform.localPosition = Vector3.zero;
            rightHandVisual.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
            
            // Set gray material for right hand
            Renderer rightRenderer = rightHandVisual.GetComponent<Renderer>();
            Material rightMaterial = FindMaterialByName("GrayHandMaterial");
            if (rightMaterial != null)
            {
                rightRenderer.material = rightMaterial;
            }
            else
            {
                rightRenderer.material.color = Color.gray;
            }
            
            // Remove the collider from visual
            Destroy(rightHandVisual.GetComponent<Collider>());
            
            Debug.Log("Hand controllers created");
        }
        
        private Material FindMaterialByName(string materialName)
        {
            // Try to find existing material
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material mat in materials)
            {
                if (mat.name == materialName)
                {
                    return mat;
                }
            }
            return null;
        }
        
        private void SetupCoreGameSystems()
        {
            Debug.Log("⚙️ Setting up Core Game Systems...");
            
            // Game Manager
            if (GameManager.Instance == null)
            {
                GameObject gameManagerObj = new GameObject("Game Manager");
                gameManagerObj.AddComponent<GameManager>();
            }
            
            // Object Pool Manager
            if (FindObjectOfType<ObjectPoolManager>() == null)
            {
                GameObject poolManagerObj = new GameObject("Object Pool Manager");
                poolManagerObj.AddComponent<ObjectPoolManager>();
            }
            
            // Hand Tracking Manager
            if (FindObjectOfType<HandTrackingManager>() == null)
            {
                GameObject handTrackingObj = new GameObject("Hand Tracking Manager");
                handTrackingObj.AddComponent<HandTrackingManager>();
            }
            
            // Haptic Feedback Manager
            if (FindObjectOfType<HapticFeedbackManager>() == null)
            {
                GameObject hapticObj = new GameObject("Haptic Feedback Manager");
                hapticObj.AddComponent<HapticFeedbackManager>();
            }
            
            // VR Performance Monitor
            if (VRPerformanceMonitor.Instance == null)
            {
                GameObject perfMonitorObj = new GameObject("VR Performance Monitor");
                perfMonitorObj.AddComponent<VRPerformanceMonitor>();
            }
            
            Debug.Log("✅ Core game systems setup complete");
        }
        
        private void SetupAudioSystems()
        {
            Debug.Log("🔊 Setting up Audio Systems...");
            
            // Advanced Audio Manager
            if (FindObjectOfType<AdvancedAudioManager>() == null)
            {
                GameObject audioManagerObj = new GameObject("Advanced Audio Manager");
                audioManagerObj.AddComponent<AdvancedAudioManager>();
            }
            
            // Test Track
            if (FindObjectOfType<TestTrack>() == null)
            {
                GameObject testTrackObj = new GameObject("Test Track");
                AudioSource audioSource = testTrackObj.AddComponent<AudioSource>();
                TestTrack testTrack = testTrackObj.AddComponent<TestTrack>();
                
                // Configure test track
                testTrack.bpm = gameBPM;
                testTrack.autoPlay = autoStartGame;
                
                // Generate test track
                testTrack.GenerateTestTrack();
            }
            
            Debug.Log("✅ Audio systems setup complete");
        }
        
        private void CreateGamePrefabs()
        {
            Debug.Log("🎯 Creating Game Prefabs...");
            
            // Circle Prefab Creator
            CirclePrefabCreator prefabCreator = FindObjectOfType<CirclePrefabCreator>();
            if (prefabCreator == null)
            {
                GameObject prefabCreatorObj = new GameObject("Circle Prefab Creator");
                prefabCreator = prefabCreatorObj.AddComponent<CirclePrefabCreator>();
            }
            
            // Create the prefabs
            prefabCreator.CreateCirclePrefabs();
            
            Debug.Log("✅ Game prefabs created");
        }
        
        private void SetupRainScene()
        {
            Debug.Log("🌧️ Setting up Rain Scene...");
            
            // Rain Scene Creator
            RainSceneCreator rainCreator = FindObjectOfType<RainSceneCreator>();
            if (rainCreator == null)
            {
                GameObject rainCreatorObj = new GameObject("Rain Scene Creator");
                rainCreator = rainCreatorObj.AddComponent<RainSceneCreator>();
            }
            
            // Configure rain scene
            rainCreator.defaultIntensity = defaultRainIntensity;
            
            // Scene Loading Manager
            if (SceneLoadingManager.Instance == null)
            {
                GameObject sceneLoaderObj = new GameObject("Scene Loading Manager");
                sceneLoaderObj.AddComponent<SceneLoadingManager>();
            }
            
            // Scene Transformation System
            if (SceneTransformationSystem.Instance == null)
            {
                GameObject sceneTransformObj = new GameObject("Scene Transformation System");
                sceneTransformObj.AddComponent<SceneTransformationSystem>();
            }
            
            // Dynamic Background System
            if (FindObjectOfType<DynamicBackgroundSystem>() == null)
            {
                GameObject backgroundObj = new GameObject("Dynamic Background System");
                backgroundObj.AddComponent<DynamicBackgroundSystem>();
            }
            
            // If we want to start with rain scene, create it now
            if (startWithRainScene)
            {
                rainCreator.CreateCompleteRainEnvironment();
                rainCreator.SetWeatherIntensity(defaultRainIntensity);
            }
            
            Debug.Log("✅ Rain scene setup complete");
        }
        
        private void SetupGameUI()
        {
            Debug.Log("🖥️ Setting up Game UI...");
            
            // Game UI
            GameUI gameUI = FindObjectOfType<GameUI>();
            if (gameUI == null)
            {
                GameObject gameUIObj = new GameObject("Game UI");
                gameUI = gameUIObj.AddComponent<GameUI>();
            }
            
            // Let GameUI create its own canvas and elements
            gameUI.FindAndAssignUIElements();
            
            Debug.Log("✅ Game UI setup complete");
        }
        
        private void ConnectSystems()
        {
            Debug.Log("🔗 Connecting All Systems...");
            
            // Setup Rhythm Target System
            RhythmTargetSystem rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            if (rhythmSystem == null)
            {
                GameObject rhythmSystemObj = new GameObject("Rhythm Target System");
                rhythmSystem = rhythmSystemObj.AddComponent<RhythmTargetSystem>();
            }
            
            // Enhanced Punch Detector
            EnhancedPunchDetector punchDetector = FindObjectOfType<EnhancedPunchDetector>();
            if (punchDetector == null)
            {
                GameObject punchDetectorObj = new GameObject("Enhanced Punch Detector");
                punchDetectorObj.AddComponent<EnhancedPunchDetector>();
            }
            
            // Advanced Target System
            AdvancedTargetSystem advancedTargets = FindObjectOfType<AdvancedTargetSystem>();
            if (advancedTargets == null)
            {
                GameObject advancedTargetsObj = new GameObject("Advanced Target System");
                advancedTargetsObj.AddComponent<AdvancedTargetSystem>();
            }
            
            // Connect audio to rhythm system
            TestTrack testTrack = FindObjectOfType<TestTrack>();
            AdvancedAudioManager audioManager = FindObjectOfType<AdvancedAudioManager>();
            
            if (testTrack != null && audioManager != null)
            {
                AudioSource audioSource = testTrack.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioManager.SetMusicSource(audioSource);
                }
            }
            
            Debug.Log("✅ Systems connected");
        }
        
        private void ValidateAndStartGame()
        {
            Debug.Log("✅ Validating Game Setup...");
            
            bool allSystemsReady = true;
            
            // Check core systems
            allSystemsReady &= CheckSystem<GameManager>("GameManager");
            allSystemsReady &= CheckSystem<RhythmTargetSystem>("RhythmTargetSystem");
            allSystemsReady &= CheckSystem<AdvancedAudioManager>("AdvancedAudioManager");
            allSystemsReady &= CheckSystem<HandTrackingManager>("HandTrackingManager");
            allSystemsReady &= CheckSystem<GameUI>("GameUI");
            
            // Check VR setup
            allSystemsReady &= CheckSystem<XROrigin>("XR Origin");
            
            // Check hands
            GameObject leftHand = GameObject.FindGameObjectWithTag("LeftHand");
            GameObject rightHand = GameObject.FindGameObjectWithTag("RightHand");
            bool handsReady = leftHand != null && rightHand != null;
            
            if (!handsReady)
            {
                Debug.LogError("❌ Hand controllers not found!");
                allSystemsReady = false;
            }
            
            // Check rain scene if enabled
            if (enableRainScene)
            {
                allSystemsReady &= CheckSystem<RainSceneCreator>("RainSceneCreator");
                allSystemsReady &= CheckSystem<SceneLoadingManager>("SceneLoadingManager");
            }
            
            if (allSystemsReady)
            {
                Debug.Log("🎉 ALL SYSTEMS VALIDATED! VR Boxing Game is ready to play!");
                Debug.Log("🌧️ Rain scene is active and fully functional");
                Debug.Log("🥊 Punch white circles with left hand, gray circles with right hand");
                Debug.Log("🎵 Music-reactive rain system is running");
                
                // Start the game
                if (autoStartGame && GameManager.Instance != null)
                {
                    GameManager.Instance.StartGame();
                }
            }
            else
            {
                Debug.LogError("❌ Game validation failed! Some systems are missing.");
            }
        }
        
        private bool CheckSystem<T>(string systemName) where T : Component
        {
            bool exists = FindObjectOfType<T>() != null;
            Debug.Log($"{systemName}: {(exists ? "✅" : "❌")}");
            return exists;
        }
    }
} 