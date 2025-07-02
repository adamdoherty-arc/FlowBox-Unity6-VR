using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Environment;
using VRBoxingGame.Core;
using VRBoxingGame.Boxing;

namespace VRBoxingGame.UI
{
    /// <summary>
    /// OPTIMIZED Enhanced Main Menu System - Integrates with all new optimized systems
    /// Fixes duplicate definitions, integrates with CachedReferenceManager, OptimizedUpdateManager
    /// Performance optimized for VR with proper scene and game mode integration
    /// </summary>
    public class EnhancedMainMenuSystemOptimized : MonoBehaviour, IOptimizedUpdatable
    {
        [Header("Main Menu Panels")]
        public GameObject welcomePanel;
        public GameObject mainMenuPanel;
        public GameObject gameModesPanel;
        public GameObject sceneSelectionPanel;
        public GameObject settingsPanel;
        public GameObject profilePanel;
        
        [Header("Main Menu Navigation")]
        public Button playButton;
        public Button gameModesButton;
        public Button sceneSelectionButton;
        public Button settingsButton;
        public Button profileButton;
        public Button quitButton;
        
        [Header("Game Mode Selection")]
        public Button traditionalModeButton;
        public Button flowModeButton;
        public Button staffModeButton;
        public Button dodgingModeButton;
        public Button aiCoachButton;
        
        [Header("Mode Configuration")]
        public Toggle normalModeToggle;
        public Toggle immersiveModeToggle;
        public TextMeshProUGUI modeDescriptionText;
        public Slider difficultySlider;
        public TextMeshProUGUI difficultyText;
        
        [Header("Scene Selection")]
        public ScrollRect sceneScrollView;
        public Transform sceneButtonContainer;
        public GameObject sceneButtonPrefab;
        public TextMeshProUGUI sceneNameText;
        public TextMeshProUGUI sceneDescriptionText;
        public Button scenePlayButton;
        
        [Header("VR UI Configuration")]
        public float menuDistance = 2f;
        public bool useGazeSelection = true;
        public float selectionTime = 2f;
        public AudioSource uiAudioSource;
        public AudioClip[] uiSounds;
        
        // Optimized System References (cached)
        private SceneAssetManager sceneAssetManager;
        private SceneGameModeIntegrator gameModeIntegrator;
        private CriticalSystemIntegrator systemIntegrator;
        private AdvancedAudioManager audioManager;
        
        // State Management
        private int currentSceneIndex = 0;
        private bool isImmersiveMode = false;
        private SceneGameModeIntegrator.GameMode currentGameMode = SceneGameModeIntegrator.GameMode.Traditional;
        private List<SceneData> availableScenes = new List<SceneData>();
        
        // Performance Tracking
        private float lastUIUpdateTime = 0f;
        private float gazeTimer = 0f;
        private Button currentGazedButton = null;
        
        // Events
        public UnityEvent<int> OnSceneSelected;
        public UnityEvent<SceneGameModeIntegrator.GameMode> OnGameModeChanged;
        public UnityEvent OnGameStarted;
        
        [System.Serializable]
        public struct SceneData
        {
            public int sceneIndex;
            public string sceneName;
            public string normalDescription;
            public string immersiveDescription;
            public Sprite previewImage;
            public float difficulty;
            public bool isUnlocked;
            public string[] supportedModes;
        }
        
        public static EnhancedMainMenuSystemOptimized Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeOptimizedMenu();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Register with OptimizedUpdateManager
            if (OptimizedUpdateManager.Instance != null)
            {
                OptimizedUpdateManager.Instance.RegisterSystem(this);
            }
            
            // Wait for systems to initialize
            StartCoroutine(WaitForSystemsAndInitialize());
        }
        
        private System.Collections.IEnumerator WaitForSystemsAndInitialize()
        {
            // Wait for CriticalSystemIntegrator to finish initialization
            yield return new WaitUntil(() => 
                CriticalSystemIntegrator.Instance != null && 
                CriticalSystemIntegrator.Instance.AreSystemsInitialized());
            
            // Get cached references to optimized systems
            CacheSystemReferences();
            
            // Initialize menu with optimized systems
            SetupMenuWithOptimizedSystems();
            
            // Load scene data
            LoadOptimizedSceneData();
            
            Debug.Log("✅ Enhanced Main Menu System (Optimized) initialized");
        }
        
        private void InitializeOptimizedMenu()
        {
            // Setup button listeners
            SetupButtonListeners();
            
            // Initialize UI state
            InitializeUIState();
            
            // Show main menu by default
            ShowMainMenu();
        }
        
        private void CacheSystemReferences()
        {
            // Use CachedReferenceManager for all system references
            sceneAssetManager = CachedReferenceManager.Get<SceneAssetManager>();
            gameModeIntegrator = CachedReferenceManager.Get<SceneGameModeIntegrator>();
            systemIntegrator = CachedReferenceManager.Get<CriticalSystemIntegrator>();
            audioManager = CachedReferenceManager.Get<AdvancedAudioManager>();
            
            Debug.Log("📝 Menu system cached all optimized system references");
        }
        
        private void SetupMenuWithOptimizedSystems()
        {
            // Subscribe to system events
            if (sceneAssetManager != null)
            {
                sceneAssetManager.OnSceneLoaded += OnSceneLoadedHandler;
                sceneAssetManager.OnLoadingProgress += OnLoadingProgressHandler;
            }
            
            if (systemIntegrator != null)
            {
                systemIntegrator.OnPerformanceWarning += OnPerformanceWarningHandler;
            }
        }
        
        private void SetupButtonListeners()
        {
            // Main navigation
            if (playButton) playButton.onClick.AddListener(QuickPlay);
            if (gameModesButton) gameModesButton.onClick.AddListener(ShowGameModes);
            if (sceneSelectionButton) sceneSelectionButton.onClick.AddListener(ShowSceneSelection);
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettings);
            if (profileButton) profileButton.onClick.AddListener(ShowProfile);
            if (quitButton) quitButton.onClick.AddListener(QuitGame);
            
            // Game mode selection
            if (traditionalModeButton) traditionalModeButton.onClick.AddListener(() => SelectGameMode(SceneGameModeIntegrator.GameMode.Traditional));
            if (flowModeButton) flowModeButton.onClick.AddListener(() => SelectGameMode(SceneGameModeIntegrator.GameMode.Flow));
            if (staffModeButton) staffModeButton.onClick.AddListener(() => SelectGameMode(SceneGameModeIntegrator.GameMode.Staff));
            if (dodgingModeButton) dodgingModeButton.onClick.AddListener(() => SelectGameMode(SceneGameModeIntegrator.GameMode.Dodging));
            if (aiCoachButton) aiCoachButton.onClick.AddListener(() => SelectGameMode(SceneGameModeIntegrator.GameMode.AICoach));
            
            // Mode toggles
            if (normalModeToggle) normalModeToggle.onValueChanged.AddListener(OnNormalModeToggled);
            if (immersiveModeToggle) immersiveModeToggle.onValueChanged.AddListener(OnImmersiveModeToggled);
            
            // Scene play button
            if (scenePlayButton) scenePlayButton.onClick.AddListener(PlaySelectedScene);
            
            // Difficulty slider
            if (difficultySlider) difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
        }
        
        private void InitializeUIState()
        {
            // Load saved settings
            currentSceneIndex = PlayerPrefs.GetInt("SelectedSceneIndex", 0);
            isImmersiveMode = PlayerPrefs.GetInt("ImmersiveMode", 0) == 1;
            int savedGameMode = PlayerPrefs.GetInt("SelectedGameMode", 0);
            currentGameMode = (SceneGameModeIntegrator.GameMode)savedGameMode;
            
            // Apply to UI
            if (normalModeToggle) normalModeToggle.isOn = !isImmersiveMode;
            if (immersiveModeToggle) immersiveModeToggle.isOn = isImmersiveMode;
            
            UpdateModeDescription();
        }
        
        private void LoadOptimizedSceneData()
        {
            availableScenes.Clear();
            
            // Define all 8 scenes with proper integration
            var sceneDefinitions = new (string name, string normal, string immersive, string[] modes)[]
            {
                ("Championship Arena", "Professional boxing arena with crowd atmosphere", 
                 "Step into the champion's arena where your rhythm conducts the crowd's roar", 
                 new string[] {"Traditional", "Flow", "Staff", "Dodging", "AICoach"}),
                
                ("Storm Symphony", "Intense thunderstorm with lightning and rain effects",
                 "Become the storm conductor, harmonizing with nature's electrical symphony",
                 new string[] {"Traditional", "Flow", "Dodging", "AICoach"}),
                
                ("Cyberpunk Underground", "Cyberpunk neon city with holographic elements",
                 "Enter the digital underground as a data runner hacking reality",
                 new string[] {"Traditional", "Flow", "Staff", "Dodging", "AICoach"}),
                
                ("Cosmic Observatory", "Space station with view of planets and stars",
                 "Command a cosmic observatory where planets orbit to your rhythm",
                 new string[] {"Traditional", "Flow", "Staff", "AICoach"}),
                
                ("Crystal Resonance", "Crystal cave with colorful formations",
                 "Discover crystal caverns where formations sing with harmonic resonance",
                 new string[] {"Traditional", "Flow", "Staff", "Dodging", "AICoach"}),
                
                ("Abyssal Ballet", "Underwater world with marine life",
                 "Dive into abyssal depths where bioluminescent creatures respond",
                 new string[] {"Traditional", "Flow", "Dodging", "AICoach"}),
                
                ("Mirage Oasis", "Desert scene with sand dunes and oasis",
                 "Journey to a mystical oasis where spirits test your perception",
                 new string[] {"Traditional", "Flow", "Staff", "Dodging", "AICoach"}),
                
                ("Enchanted Grove", "Forest with trees and natural lighting",
                 "Enter an enchanted grove where trees are living instruments",
                 new string[] {"Traditional", "Flow", "Staff", "Dodging", "AICoach"})
            };
            
            for (int i = 0; i < sceneDefinitions.Length; i++)
            {
                var scene = sceneDefinitions[i];
                availableScenes.Add(new SceneData
                {
                    sceneIndex = i,
                    sceneName = scene.name,
                    normalDescription = scene.normal,
                    immersiveDescription = scene.immersive,
                    difficulty = 0.3f + (i * 0.1f), // Progressive difficulty
                    isUnlocked = true,
                    supportedModes = scene.modes
                });
            }
            
            CreateOptimizedSceneButtons();
            UpdateSceneSelection();
        }
        
        private void CreateOptimizedSceneButtons()
        {
            if (sceneButtonContainer == null || sceneButtonPrefab == null) return;
            
            // Clear existing buttons efficiently
            for (int i = sceneButtonContainer.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(sceneButtonContainer.GetChild(i).gameObject);
                else
                    DestroyImmediate(sceneButtonContainer.GetChild(i).gameObject);
            }
            
            // Create buttons for each scene
            for (int i = 0; i < availableScenes.Count; i++)
            {
                int sceneIndex = i; // Capture for closure
                var scene = availableScenes[i];
                
                GameObject buttonObj = Instantiate(sceneButtonPrefab, sceneButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                
                if (button != null)
                {
                    // Setup button
                    button.onClick.AddListener(() => SelectScene(sceneIndex));
                    
                    // Setup text
                    TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText) buttonText.text = scene.sceneName;
                    
                    // Check if current game mode is supported
                    bool isSupported = IsGameModeSupportedForScene(currentGameMode, sceneIndex);
                    button.interactable = isSupported;
                    
                    // Visual feedback for unsupported modes
                    if (!isSupported)
                    {
                        var colors = button.colors;
                        colors.normalColor = Color.gray;
                        button.colors = colors;
                    }
                }
            }
        }
        
        // IOptimizedUpdatable Implementation
        public void OptimizedUpdate()
        {
            // Efficient UI updates at 30Hz (slow update frequency)
            if (useGazeSelection)
            {
                UpdateGazeSelection();
            }
            
            // Update UI elements that need refreshing
            if (Time.time - lastUIUpdateTime > 0.1f) // 10Hz UI refresh
            {
                UpdateUIElements();
                lastUIUpdateTime = Time.time;
            }
        }
        
        public UpdateFrequency GetUpdateFrequency()
        {
            return UpdateFrequency.Slow; // 30Hz for UI
        }
        
        public bool IsUpdateEnabled()
        {
            return gameObject.activeInHierarchy && enabled;
        }
        
        private void UpdateGazeSelection()
        {
            // Simplified gaze selection logic
            if (currentGazedButton != null)
            {
                gazeTimer += Time.deltaTime;
                if (gazeTimer >= selectionTime)
                {
                    currentGazedButton.onClick.Invoke();
                    gazeTimer = 0f;
                    currentGazedButton = null;
                }
            }
        }
        
        private void UpdateUIElements()
        {
            // Update dynamic UI elements
            UpdateModeDescription();
            UpdateGameModeAvailability();
        }
        
        // Game Mode Selection
        private void SelectGameMode(SceneGameModeIntegrator.GameMode gameMode)
        {
            currentGameMode = gameMode;
            PlayerPrefs.SetInt("SelectedGameMode", (int)gameMode);
            
            // Update game mode integrator
            if (gameModeIntegrator != null)
            {
                gameModeIntegrator.SetGameMode(gameMode);
            }
            
            // Update scene button availability
            CreateOptimizedSceneButtons();
            
            // Update UI
            UpdateModeDescription();
            
            OnGameModeChanged?.Invoke(gameMode);
            PlayUISound(2);
            
            Debug.Log($"🎮 Selected game mode: {gameMode}");
        }
        
        private void UpdateGameModeAvailability()
        {
            // Update game mode buttons based on selected scene
            if (gameModeIntegrator != null)
            {
                bool flowSupported = gameModeIntegrator.IsGameModeSupported(SceneGameModeIntegrator.GameMode.Flow, currentSceneIndex);
                bool staffSupported = gameModeIntegrator.IsGameModeSupported(SceneGameModeIntegrator.GameMode.Staff, currentSceneIndex);
                bool dodgingSupported = gameModeIntegrator.IsGameModeSupported(SceneGameModeIntegrator.GameMode.Dodging, currentSceneIndex);
                
                if (flowModeButton) flowModeButton.interactable = flowSupported;
                if (staffModeButton) staffModeButton.interactable = staffSupported;
                if (dodgingModeButton) dodgingModeButton.interactable = dodgingSupported;
            }
        }
        
        private bool IsGameModeSupportedForScene(SceneGameModeIntegrator.GameMode gameMode, int sceneIndex)
        {
            if (gameModeIntegrator != null)
            {
                return gameModeIntegrator.IsGameModeSupported(gameMode, sceneIndex);
            }
            return true; // Default to supported if integrator not available
        }
        
        // Scene Selection
        private void SelectScene(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= availableScenes.Count) return;
            
            currentSceneIndex = sceneIndex;
            PlayerPrefs.SetInt("SelectedSceneIndex", sceneIndex);
            
            UpdateSceneSelection();
            UpdateGameModeAvailability();
            
            OnSceneSelected?.Invoke(sceneIndex);
            PlayUISound(2);
        }
        
        private void UpdateSceneSelection()
        {
            if (currentSceneIndex >= 0 && currentSceneIndex < availableScenes.Count)
            {
                var scene = availableScenes[currentSceneIndex];
                
                if (sceneNameText) sceneNameText.text = scene.sceneName;
                if (sceneDescriptionText)
                {
                    sceneDescriptionText.text = isImmersiveMode ? scene.immersiveDescription : scene.normalDescription;
                }
            }
        }
        
        private void UpdateModeDescription()
        {
            if (modeDescriptionText == null) return;
            
            string modeText = currentGameMode switch
            {
                SceneGameModeIntegrator.GameMode.Traditional => "Classic VR boxing with rhythm-based targets",
                SceneGameModeIntegrator.GameMode.Flow => "Beat Saber-style flowing targets in multiple lanes",
                SceneGameModeIntegrator.GameMode.Staff => "Two-handed staff combat with physics simulation",
                SceneGameModeIntegrator.GameMode.Dodging => "Full-body dodging with squats, ducks, and spins",
                SceneGameModeIntegrator.GameMode.AICoach => "Personalized training with AI coaching feedback",
                _ => "Select a game mode"
            };
            
            string sceneText = isImmersiveMode ? " (Immersive Mode)" : " (Normal Mode)";
            modeDescriptionText.text = modeText + sceneText;
        }
        
        // Panel Management
        private void ShowMainMenu()
        {
            HideAllPanels();
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
        }
        
        private void ShowGameModes()
        {
            HideAllPanels();
            if (gameModesPanel) gameModesPanel.SetActive(true);
            UpdateGameModeAvailability();
        }
        
        private void ShowSceneSelection()
        {
            HideAllPanels();
            if (sceneSelectionPanel) sceneSelectionPanel.SetActive(true);
            UpdateSceneSelection();
        }
        
        private void ShowSettings()
        {
            HideAllPanels();
            if (settingsPanel) settingsPanel.SetActive(true);
        }
        
        private void ShowProfile()
        {
            HideAllPanels();
            if (profilePanel) profilePanel.SetActive(true);
        }
        
        private void HideAllPanels()
        {
            var panels = new GameObject[] { welcomePanel, mainMenuPanel, gameModesPanel, sceneSelectionPanel, settingsPanel, profilePanel };
            foreach (var panel in panels)
            {
                if (panel != null) panel.SetActive(false);
            }
        }
        
        // Game Control
        private void QuickPlay()
        {
            // Quick play with current settings
            PlaySelectedScene();
        }
        
        private async Task PlaySelectedScene()
        {
            PlayUISound(1);
            
            // Update scene stats
            var scene = availableScenes[currentSceneIndex];
            int timesPlayed = PlayerPrefs.GetInt($"Scene_{currentSceneIndex}_TimesPlayed", 0) + 1;
            PlayerPrefs.SetInt($"Scene_{currentSceneIndex}_TimesPlayed", timesPlayed);
            
            // Load scene using optimized scene asset manager
            if (sceneAssetManager != null)
            {
                bool success = await sceneAssetManager.LoadSceneAsync(currentSceneIndex);
                if (success)
                {
                    StartGame();
                }
                else
                {
                    Debug.LogError("❌ Failed to load scene");
                }
            }
            else
            {
                Debug.LogError("❌ SceneAssetManager not available");
            }
        }
        
        private void StartGame()
        {
            // Hide menu
            gameObject.SetActive(false);
            
            // Start game through GameManager
            var gameManager = CachedReferenceManager.Get<GameManager>();
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
            
            OnGameStarted?.Invoke();
            Debug.Log($"🎮 Game started: {currentGameMode} mode in scene {currentSceneIndex}");
        }
        
        private void QuitGame()
        {
            Debug.Log("🔚 Quitting game...");
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        
        // Event Handlers
        private void OnNormalModeToggled(bool value)
        {
            if (value)
            {
                isImmersiveMode = false;
                PlayerPrefs.SetInt("ImmersiveMode", 0);
                UpdateModeDescription();
                UpdateSceneSelection();
            }
        }
        
        private void OnImmersiveModeToggled(bool value)
        {
            if (value)
            {
                isImmersiveMode = true;
                PlayerPrefs.SetInt("ImmersiveMode", 1);
                UpdateModeDescription();
                UpdateSceneSelection();
            }
        }
        
        private void OnDifficultyChanged(float value)
        {
            if (difficultyText) difficultyText.text = $"Difficulty: {value:P0}";
            PlayerPrefs.SetFloat("GameDifficulty", value);
        }
        
        private void OnSceneLoadedHandler(int sceneIndex)
        {
            Debug.Log($"✅ Scene {sceneIndex} loaded successfully");
        }
        
        private void OnLoadingProgressHandler(float progress)
        {
            // Update loading UI if needed
        }
        
        private void OnPerformanceWarningHandler(float frameTime)
        {
            Debug.LogWarning($"⚠️ Performance warning in menu: {frameTime * 1000:F2}ms");
        }
        
        // Audio
        private void PlayUISound(int soundIndex)
        {
            if (uiAudioSource != null && uiSounds != null && soundIndex < uiSounds.Length)
            {
                uiAudioSource.PlayOneShot(uiSounds[soundIndex]);
            }
        }
        
        // Public API
        public bool IsImmersiveMode => isImmersiveMode;
        public int CurrentSceneIndex => currentSceneIndex;
        public SceneGameModeIntegrator.GameMode CurrentGameMode => currentGameMode;
        
        public void SetGameMode(SceneGameModeIntegrator.GameMode gameMode)
        {
            SelectGameMode(gameMode);
        }
        
        public void SetScene(int sceneIndex)
        {
            SelectScene(sceneIndex);
        }
        
        private void OnDestroy()
        {
            // Unregister from OptimizedUpdateManager
            if (OptimizedUpdateManager.Instance != null)
            {
                OptimizedUpdateManager.Instance.UnregisterSystem(this);
            }
        }
    }
} 