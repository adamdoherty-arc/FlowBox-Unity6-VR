using UnityEngine;
using VRBoxingGame.Environment;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using VRBoxingGame.UI;

namespace VRBoxingGame.Testing
{
    /// <summary>
    /// Complete test scene combining HD rain environment with VR rhythm game
    /// Features: Left/Right/Center mechanics, spinning blocks, and weather intensity modes
    /// </summary>
    public class RainRhythmTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool startTestOnAwake = true;
        public RainSceneCreator.WeatherIntensity testWeatherMode = RainSceneCreator.WeatherIntensity.Medium;
        public float testDuration = 300f; // 5 minutes
        
        [Header("Rhythm Game Integration")]
        public bool enableRhythmGame = true;
        public float targetBPM = 120f;
        public float difficultyMultiplier = 1f;
        
        [Header("VR Setup")]
        public bool autoCreateVRRig = true;
        public Vector3 vrCameraPosition = new Vector3(0, 1.8f, 0);
        
        [Header("Test Controls")]
        [Space(10)]
        [Tooltip("Press these keys during testing")]
        public KeyCode switchWeatherKey = KeyCode.W;
        public KeyCode triggerLightningKey = KeyCode.L;
        public KeyCode increaseDifficultyKey = KeyCode.Plus;
        public KeyCode decreaseDifficultyKey = KeyCode.Minus;
        
        // Components
        private RainSceneCreator rainScene;
        private RhythmTargetSystem rhythmSystem;
        private AdvancedAudioManager audioManager;
        private TestTrack testTrack;
        private VRSceneSetup vrSetup;
        private GameUI gameUI;
        
        // Test state
        private float testStartTime;
        private bool isTestRunning = false;
        private int weatherModeIndex = 1; // Start with Medium
        private int totalTargetsSpawned = 0;
        private int targetsHit = 0;
        
        // Performance tracking
        private float[] frameTimeHistory = new float[60];
        private int frameTimeIndex = 0;
        private float averageFrameTime = 0f;
        
        private void Awake()
        {
            if (startTestOnAwake)
            {
                SetupRainRhythmTest();
            }
        }
        
        [ContextMenu("Setup Rain Rhythm Test")]
        public void SetupRainRhythmTest()
        {
            Debug.Log("🎮 Setting up Rain Rhythm Test Scene...");
            
            // Step 1: Create rain environment
            SetupRainEnvironment();
            
            // Step 2: Setup VR components
            if (autoCreateVRRig)
            {
                SetupVRRig();
            }
            
            // Step 3: Initialize rhythm game systems
            SetupRhythmGame();
            
            // Step 4: Connect systems together
            ConnectSystems();
            
            // Step 5: Start the test
            StartTest();
            
            Debug.Log("✅ Rain Rhythm Test Scene ready!");
        }
        
        private void SetupRainEnvironment()
        {
            // Create or find rain scene creator
            rainScene = FindObjectOfType<RainSceneCreator>();
            if (rainScene == null)
            {
                GameObject rainObj = new GameObject("Rain Scene Creator");
                rainScene = rainObj.AddComponent<RainSceneCreator>();
            }
            
            // Configure rain scene
            rainScene.defaultIntensity = testWeatherMode;
            rainScene.CreateRainScene();
            
            Debug.Log("Rain environment created");
        }
        
        private void SetupVRRig()
        {
            // Find or create VR setup
            vrSetup = FindObjectOfType<VRSceneSetup>();
            if (vrSetup == null)
            {
                GameObject vrObj = new GameObject("VR Scene Setup");
                vrSetup = vrObj.AddComponent<VRSceneSetup>();
            }
            
            // Setup complete VR scene
            vrSetup.SetupCompleteVRScene();
            
            Debug.Log("VR rig setup complete");
        }
        
        private void SetupRhythmGame()
        {
            // Setup audio system
            SetupAudioSystem();
            
            // Setup rhythm target system
            SetupTargetSystem();
            
            // Setup UI
            SetupGameUI();
            
            Debug.Log("Rhythm game systems initialized");
        }
        
        private void SetupAudioSystem()
        {
            // Find or create audio manager
            audioManager = FindObjectOfType<AdvancedAudioManager>();
            if (audioManager == null)
            {
                GameObject audioObj = new GameObject("Advanced Audio Manager");
                audioManager = audioObj.AddComponent<AdvancedAudioManager>();
            }
            
            // Find or create test track
            testTrack = FindObjectOfType<TestTrack>();
            if (testTrack == null)
            {
                GameObject trackObj = new GameObject("Test Track");
                testTrack = trackObj.AddComponent<TestTrack>();
                trackObj.AddComponent<AudioSource>();
            }
            
            // Configure test track
            testTrack.bpm = targetBPM;
            testTrack.GenerateTestTrack();
            
            // Connect to audio manager
            audioManager.SetMusicSource(testTrack.GetComponent<AudioSource>());
        }
        
        private void SetupTargetSystem()
        {
            // Find or create rhythm target system
            rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            if (rhythmSystem == null)
            {
                GameObject rhythmObj = new GameObject("Rhythm Target System");
                rhythmSystem = rhythmObj.AddComponent<RhythmTargetSystem>();
            }
            
            // Configure spawn points for dramatic effect in rain
            SetupSpawnPoints();
            
            // Generate prefabs if needed
            var prefabCreator = FindObjectOfType<CirclePrefabCreator>();
            if (prefabCreator != null)
            {
                prefabCreator.CreateCirclePrefabs();
            }
        }
        
        private void SetupSpawnPoints()
        {
            // Create dramatic spawn points that work well with rain environment
            
            // Left spawn (white circles)
            if (rhythmSystem.leftSpawnPoint == null)
            {
                GameObject leftSpawn = new GameObject("Left Spawn Point");
                leftSpawn.transform.SetParent(rhythmSystem.transform);
                leftSpawn.transform.position = new Vector3(-8f, 2f, 12f);
                rhythmSystem.leftSpawnPoint = leftSpawn.transform;
            }
            
            // Right spawn (gray circles)
            if (rhythmSystem.rightSpawnPoint == null)
            {
                GameObject rightSpawn = new GameObject("Right Spawn Point");
                rightSpawn.transform.SetParent(rhythmSystem.transform);
                rightSpawn.transform.position = new Vector3(8f, 2f, 12f);
                rhythmSystem.rightSpawnPoint = rightSpawn.transform;
            }
            
            // Center point (where circles combine)
            if (rhythmSystem.centerPoint == null)
            {
                GameObject centerPoint = new GameObject("Center Point");
                centerPoint.transform.SetParent(rhythmSystem.transform);
                centerPoint.transform.position = new Vector3(0f, 1.8f, 2f);
                rhythmSystem.centerPoint = centerPoint.transform;
            }
        }
        
        private void SetupGameUI()
        {
            // Find or create game UI
            gameUI = FindObjectOfType<GameUI>();
            if (gameUI == null)
            {
                GameObject uiObj = new GameObject("Game UI");
                gameUI = uiObj.AddComponent<GameUI>();
            }
            
            // Auto-assign UI elements
            gameUI.FindAndAssignUIElements();
        }
        
        private void ConnectSystems()
        {
            // Connect rain scene to rhythm system for beat reactivity
            if (audioManager != null && rainScene != null)
            {
                audioManager.OnBeatDetected.AddListener((beatData) => {
                    rainScene.OnBeatDetected(beatData.intensity);
                });
            }
            
            // Connect rhythm system events for statistics
            if (rhythmSystem != null)
            {
                rhythmSystem.OnCircleHit.AddListener(OnCircleHit);
                rhythmSystem.OnBlockSuccess.AddListener(OnBlockSuccess);
                rhythmSystem.OnBlockFailed.AddListener(OnBlockFailed);
            }
            
            Debug.Log("Systems connected successfully");
        }
        
        private void StartTest()
        {
            testStartTime = Time.time;
            isTestRunning = true;
            
            // Start music
            if (testTrack != null)
            {
                testTrack.PlayTestTrack();
            }
            
            // Start game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
            
            Debug.Log($"🎵 Rain Rhythm Test started! Duration: {testDuration}s, BPM: {targetBPM}, Weather: {testWeatherMode}");
        }
        
        private void Update()
        {
            if (!isTestRunning) return;
            
            // Handle test controls
            HandleTestControls();
            
            // Update performance tracking
            UpdatePerformanceTracking();
            
            // Check test completion
            CheckTestCompletion();
            
            // Update spinning block mechanics
            UpdateSpinningBlocks();
        }
        
        private void HandleTestControls()
        {
            // Switch weather intensity
            if (Input.GetKeyDown(switchWeatherKey))
            {
                SwitchWeatherMode();
            }
            
            // Trigger lightning manually
            if (Input.GetKeyDown(triggerLightningKey))
            {
                rainScene?.TriggerLightningManual();
            }
            
            // Adjust difficulty
            if (Input.GetKeyDown(increaseDifficultyKey))
            {
                difficultyMultiplier = Mathf.Min(difficultyMultiplier + 0.2f, 3f);
                Debug.Log($"Difficulty increased to: {difficultyMultiplier:F1}x");
            }
            
            if (Input.GetKeyDown(decreaseDifficultyKey))
            {
                difficultyMultiplier = Mathf.Max(difficultyMultiplier - 0.2f, 0.2f);
                Debug.Log($"Difficulty decreased to: {difficultyMultiplier:F1}x");
            }
        }
        
        private void SwitchWeatherMode()
        {
            weatherModeIndex = (weatherModeIndex + 1) % 3;
            RainSceneCreator.WeatherIntensity newMode = (RainSceneCreator.WeatherIntensity)weatherModeIndex;
            
            rainScene?.SetWeatherIntensity(newMode);
            
            Debug.Log($"🌧️ Weather switched to: {newMode}");
        }
        
        private void UpdatePerformanceTracking()
        {
            // Track frame time for performance monitoring
            frameTimeHistory[frameTimeIndex] = Time.unscaledDeltaTime;
            frameTimeIndex = (frameTimeIndex + 1) % frameTimeHistory.Length;
            
            // Calculate average every second
            if (Time.frameCount % 60 == 0)
            {
                float sum = 0f;
                for (int i = 0; i < frameTimeHistory.Length; i++)
                {
                    sum += frameTimeHistory[i];
                }
                averageFrameTime = sum / frameTimeHistory.Length;
                
                float fps = 1f / averageFrameTime;
                if (fps < 72f) // Below Quest 2 minimum
                {
                    Debug.LogWarning($"⚠️ Performance below target: {fps:F1} FPS");
                }
            }
        }
        
        private void CheckTestCompletion()
        {
            float elapsed = Time.time - testStartTime;
            if (elapsed >= testDuration)
            {
                CompleteTest();
            }
        }
        
        private void UpdateSpinningBlocks()
        {
            // Enhanced spinning block mechanics based on approach speed
            // This ensures blocks spin faster when they approach faster (per requirements)
            
            if (rhythmSystem != null)
            {
                // The RhythmTargetSystem already handles this, but we can add visual feedback
                var activeBlocks = GameObject.FindGameObjectsWithTag("SpinningBlock");
                
                foreach (var block in activeBlocks)
                {
                    // Get approach speed and correlate spin speed
                    float distanceToCenter = Vector3.Distance(block.transform.position, rhythmSystem.centerPoint.position);
                    float approachSpeed = rhythmSystem.baseSpeed * difficultyMultiplier;
                    
                    // Spin speed correlates with approach speed (requirement)
                    float spinSpeed = approachSpeed * rhythmSystem.spinSpeedMultiplier;
                    
                    // Apply spinning
                    block.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
                    
                    // Visual effect: faster spin = more intense glow
                    var renderer = block.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        float intensity = Mathf.Clamp01(spinSpeed / 100f);
                        Color emissionColor = Color.red * intensity;
                        renderer.material.SetColor("_EmissionColor", emissionColor);
                    }
                }
            }
        }
        
        private void OnCircleHit(RhythmTargetSystem.CircleHitData hitData)
        {
            targetsHit++;
            
            // Visual feedback in rain
            if (rainScene != null)
            {
                rainScene.OnBeatDetected(hitData.accuracy);
            }
            
            Debug.Log($"✨ Circle hit! Type: {hitData.circleType}, Hand: {hitData.requiredHand}, Accuracy: {hitData.accuracy:F2}");
        }
        
        private void OnBlockSuccess(RhythmTargetSystem.BlockData blockData)
        {
            Debug.Log($"🛡️ Block successful! Spin speed was: {blockData.spinSpeed:F1}");
        }
        
        private void OnBlockFailed(RhythmTargetSystem.BlockData blockData)
        {
            Debug.Log($"💥 Block failed! Spin speed was: {blockData.spinSpeed:F1}");
        }
        
        private void CompleteTest()
        {
            isTestRunning = false;
            
            // Calculate final statistics
            float accuracy = totalTargetsSpawned > 0 ? (float)targetsHit / totalTargetsSpawned : 0f;
            float finalFPS = 1f / averageFrameTime;
            
            Debug.Log("🏁 Rain Rhythm Test Complete!");
            Debug.Log($"📊 Final Stats:");
            Debug.Log($"   • Duration: {testDuration}s");
            Debug.Log($"   • Targets Hit: {targetsHit}/{totalTargetsSpawned}");
            Debug.Log($"   • Accuracy: {accuracy:P1}");
            Debug.Log($"   • Average FPS: {finalFPS:F1}");
            Debug.Log($"   • Weather Mode: {(RainSceneCreator.WeatherIntensity)weatherModeIndex}");
            Debug.Log($"   • Difficulty: {difficultyMultiplier:F1}x");
            
            // Stop music
            if (testTrack != null)
            {
                testTrack.StopTestTrack();
            }
        }
        
        // Public API for external control
        public void SetWeatherMode(int mode)
        {
            weatherModeIndex = Mathf.Clamp(mode, 0, 2);
            rainScene?.SetWeatherIntensity((RainSceneCreator.WeatherIntensity)weatherModeIndex);
        }
        
        public void SetDifficulty(float multiplier)
        {
            difficultyMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
        }
        
        public void RestartTest()
        {
            CompleteTest();
            SetupRainRhythmTest();
        }
        
        // Debug information
        private void OnGUI()
        {
            if (!isTestRunning) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("🌧️ Rain Rhythm Test", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.Space(10);
            
            float elapsed = Time.time - testStartTime;
            GUILayout.Label($"Time: {elapsed:F1}s / {testDuration:F1}s");
            GUILayout.Label($"FPS: {1f / averageFrameTime:F1}");
            GUILayout.Label($"Weather: {(RainSceneCreator.WeatherIntensity)weatherModeIndex}");
            GUILayout.Label($"Difficulty: {difficultyMultiplier:F1}x");
            GUILayout.Label($"Hits: {targetsHit}/{totalTargetsSpawned}");
            
            GUILayout.Space(10);
            GUILayout.Label("Controls:");
            GUILayout.Label($"[{switchWeatherKey}] Switch Weather");
            GUILayout.Label($"[{triggerLightningKey}] Lightning");
            GUILayout.Label($"[{increaseDifficultyKey}/{decreaseDifficultyKey}] Difficulty");
            
            GUILayout.EndArea();
        }
        
        private void OnDestroy()
        {
            if (isTestRunning)
            {
                CompleteTest();
            }
        }
    }
} 