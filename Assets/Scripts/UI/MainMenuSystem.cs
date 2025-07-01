using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using VRBoxingGame.Environment;
using VRBoxingGame.Spotify;

namespace VRBoxingGame.UI
{
    /// <summary>
    /// Main Menu System for VR Rhythm Boxing Game
    /// Provides options for background selection, Spotify connection, and game settings
    /// </summary>
    public class MainMenuSystem : MonoBehaviour
    {
        [Header("Menu Panels")]
        public GameObject mainMenuPanel;
        public GameObject settingsPanel;
        public GameObject backgroundSelectionPanel;
        public GameObject sceneSelectionPanel;
        public GameObject spotifyPanel;
        public GameObject creditsPanel;
        
        [Header("Main Menu Buttons")]
        public Button startGameButton;
        public Button settingsButton;
        public Button backgroundsButton;
        public Button scenesButton;
        public Button spotifyButton;
        public Button creditsButton;
        public Button quitButton;
        
        [Header("Settings Controls")]
        public Slider volumeSlider;
        public Slider difficultySlider;
        public Toggle handTrackingToggle;
        public Toggle autoOptimizationToggle;
        public TextMeshProUGUI volumeValueText;
        public TextMeshProUGUI difficultyValueText;
        
        [Header("Background Selection")]
        public Button[] backgroundButtons;
        public TextMeshProUGUI currentBackgroundText;
        public Image backgroundPreview;
        public Sprite[] backgroundPreviews;
        
        [Header("Scene Selection")]
        public Button[] sceneButtons;
        public TextMeshProUGUI currentSceneText;
        public Image scenePreview;
        public Sprite[] scenePreviews;
        public TextMeshProUGUI sceneDescriptionText;
        
        [Header("Spotify Integration")]
        public Button connectSpotifyButton;
        public Button disconnectSpotifyButton;
        public TextMeshProUGUI spotifyStatusText;
        public ScrollRect playlistScrollRect;
        public GameObject playlistItemPrefab;
        public Transform playlistContent;
        
        [Header("Target Mode Toggle")]
        public Toggle traditionalTargetsToggle;
        public TextMeshProUGUI targetModeText;
        public Button targetModeInfoButton;
        public GameObject targetModeInfoPanel;
        public TextMeshProUGUI targetModeInfoText;
        
        [Header("Events")]
        public UnityEvent OnGameStart;
        public UnityEvent<int> OnBackgroundChanged;
        public UnityEvent<int> OnSceneChanged;
        public UnityEvent<float> OnVolumeChanged;
        public UnityEvent<float> OnDifficultyChanged;
        
        // Private variables
        private int currentBackgroundIndex = 0;
        private int currentSceneIndex = 0;
        private bool useTraditionalTargets = false;
        private string[] backgroundNames = {
            "Cyberpunk City", "Space Station", "Abstract Geometry", 
            "Crystal Cave", "Aurora Fields", "Underwater Realm"
        };
        
        private string[] sceneNames = {
            "Default Arena", "Rain Storm", "Neon City", "Space Station",
            "Crystal Cave", "Underwater World", "Desert Oasis", "Forest Glade"
        };
        
        private string[] sceneDescriptions = {
            "Step into the champion's arena where your rhythm conducts the crowd's roar and spotlight's dance",
            "Become the storm conductor, harmonizing with nature's most violent and beautiful electrical symphony",
            "Enter the digital underground as a data runner, hacking through neon-soaked cyberpunk reality",
            "Command a cosmic observatory where planets and stars orbit to the rhythm of your perfect timing",
            "Discover crystal caverns where each formation sings with pure harmonic resonance and ancient memory",
            "Dive into abyssal depths where bioluminescent creatures create nature's own responsive light show",
            "Journey to a mirage oasis where desert spirits challenge you to separate illusion from truth",
            "Enter an enchanted grove where ancient trees become living instruments in a magical symphony"
        };
        
        // Singleton
        public static MainMenuSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeMainMenu();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeMainMenu()
        {
            // Setup button listeners
            SetupButtonListeners();
            
            // Initialize UI values
            InitializeUIValues();
            
            // Show main menu by default
            ShowMainMenu();
            
            // Subscribe to Spotify events
            if (SpotifyIntegration.Instance != null)
            {
                SpotifyIntegration.Instance.OnConnectionStatusChanged.AddListener(OnSpotifyConnectionChanged);
            }
            
            Debug.Log("Main Menu System initialized");
        }
        
        private void SetupButtonListeners()
        {
            // Main menu buttons
            if (startGameButton) startGameButton.onClick.AddListener(StartGame);
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettings);
            if (backgroundsButton) backgroundsButton.onClick.AddListener(ShowBackgroundSelection);
            if (scenesButton) scenesButton.onClick.AddListener(ShowSceneSelection);
            if (spotifyButton) spotifyButton.onClick.AddListener(ShowSpotifyPanel);
            if (creditsButton) creditsButton.onClick.AddListener(ShowCredits);
            if (quitButton) quitButton.onClick.AddListener(QuitGame);
            
            // Settings controls
            if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            if (difficultySlider) difficultySlider.onValueChanged.AddListener(OnDifficultySliderChanged);
            if (handTrackingToggle) handTrackingToggle.onValueChanged.AddListener(OnHandTrackingToggled);
            if (autoOptimizationToggle) autoOptimizationToggle.onValueChanged.AddListener(OnAutoOptimizationToggled);
            
            // Background selection buttons
            for (int i = 0; i < backgroundButtons.Length; i++)
            {
                int index = i; // Capture for closure
                if (backgroundButtons[i] != null)
                {
                    backgroundButtons[i].onClick.AddListener(() => SelectBackground(index));
                }
            }
            
            // Scene selection buttons
            for (int i = 0; i < sceneButtons.Length; i++)
            {
                int index = i; // Capture for closure
                if (sceneButtons[i] != null)
                {
                    sceneButtons[i].onClick.AddListener(() => SelectScene(index));
                }
            }
            
            // Spotify buttons
            if (connectSpotifyButton) connectSpotifyButton.onClick.AddListener(ConnectToSpotify);
            if (disconnectSpotifyButton) disconnectSpotifyButton.onClick.AddListener(DisconnectFromSpotify);
            
            // Scene selection
            if (scenesButton != null)
                scenesButton.onClick.AddListener(ToggleSceneSelection);
            
            // Target mode toggle
            if (traditionalTargetsToggle != null)
                traditionalTargetsToggle.onValueChanged.AddListener(OnTargetModeToggled);
        }
        
        private void InitializeUIValues()
        {
            // Load saved settings
            float savedVolume = PlayerPrefs.GetFloat("GameVolume", 0.8f);
            float savedDifficulty = PlayerPrefs.GetFloat("GameDifficulty", 0.5f);
            bool savedHandTracking = PlayerPrefs.GetInt("HandTracking", 1) == 1;
            bool savedAutoOptimization = PlayerPrefs.GetInt("AutoOptimization", 1) == 1;
            int savedBackground = PlayerPrefs.GetInt("BackgroundIndex", 0);
            int savedScene = PlayerPrefs.GetInt("SceneIndex", 0);
            bool savedUseTraditionalTargets = PlayerPrefs.GetInt("UseTraditionalTargets", 0) == 1;
            
            // Apply to UI
            if (volumeSlider) volumeSlider.value = savedVolume;
            if (difficultySlider) difficultySlider.value = savedDifficulty;
            if (handTrackingToggle) handTrackingToggle.isOn = savedHandTracking;
            if (autoOptimizationToggle) autoOptimizationToggle.isOn = savedAutoOptimization;
            
            // Update text displays
            UpdateVolumeDisplay(savedVolume);
            UpdateDifficultyDisplay(savedDifficulty);
            
            // Set background and scene
            currentBackgroundIndex = savedBackground;
            currentSceneIndex = savedScene;
            useTraditionalTargets = savedUseTraditionalTargets;
            UpdateBackgroundDisplay();
            UpdateSceneDisplay();
            UpdateTargetModeDisplay();
        }
        
        // Menu Navigation
        public void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(backgroundSelectionPanel, false);
            SetPanelActive(sceneSelectionPanel, false);
            SetPanelActive(spotifyPanel, false);
            SetPanelActive(creditsPanel, false);
        }
        
        public void ShowSettings()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, true);
            SetPanelActive(backgroundSelectionPanel, false);
            SetPanelActive(sceneSelectionPanel, false);
            SetPanelActive(spotifyPanel, false);
            SetPanelActive(creditsPanel, false);
        }
        
        public void ShowBackgroundSelection()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(backgroundSelectionPanel, true);
            SetPanelActive(sceneSelectionPanel, false);
            SetPanelActive(spotifyPanel, false);
            SetPanelActive(creditsPanel, false);
            
            UpdateBackgroundDisplay();
        }
        
        public void ShowSceneSelection()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(backgroundSelectionPanel, false);
            SetPanelActive(sceneSelectionPanel, true);
            SetPanelActive(spotifyPanel, false);
            SetPanelActive(creditsPanel, false);
            
            UpdateSceneDisplay();
        }
        
        public void ShowSpotifyPanel()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(backgroundSelectionPanel, false);
            SetPanelActive(sceneSelectionPanel, false);
            SetPanelActive(spotifyPanel, true);
            SetPanelActive(creditsPanel, false);
            
            UpdateSpotifyDisplay();
        }
        
        public void ShowCredits()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(backgroundSelectionPanel, false);
            SetPanelActive(sceneSelectionPanel, false);
            SetPanelActive(spotifyPanel, false);
            SetPanelActive(creditsPanel, true);
        }
        
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        // Game Controls
        public void StartGame()
        {
            Debug.Log("Starting VR Rhythm Game...");
            
            // Apply current settings
            ApplyGameSettings();
            
            // Load selected scene and background
            LoadSelectedScene();
            if (DynamicBackgroundSystem.Instance != null)
            {
                DynamicBackgroundSystem.Instance.LoadTheme(currentBackgroundIndex);
            }
            
            // Hide menu
            gameObject.SetActive(false);
            
            // Start game
            OnGameStart?.Invoke();
            
            if (VRBoxingGame.Core.GameManager.Instance != null)
            {
                VRBoxingGame.Core.GameManager.Instance.StartGame();
            }
            
            Debug.Log($"Game started with scene: {sceneNames[currentSceneIndex]}");
        }
        
        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        // Settings Controls
        private void OnVolumeSliderChanged(float value)
        {
            UpdateVolumeDisplay(value);
            OnVolumeChanged?.Invoke(value);
            PlayerPrefs.SetFloat("GameVolume", value);
        }
        
        private void OnDifficultySliderChanged(float value)
        {
            UpdateDifficultyDisplay(value);
            OnDifficultyChanged?.Invoke(value);
            PlayerPrefs.SetFloat("GameDifficulty", value);
        }
        
        private void OnHandTrackingToggled(bool enabled)
        {
            PlayerPrefs.SetInt("HandTracking", enabled ? 1 : 0);
            Debug.Log($"Hand tracking: {(enabled ? "Enabled" : "Disabled")}");
        }
        
        private void OnAutoOptimizationToggled(bool enabled)
        {
            PlayerPrefs.SetInt("AutoOptimization", enabled ? 1 : 0);
            Debug.Log($"Auto optimization: {(enabled ? "Enabled" : "Disabled")}");
        }
        
        private void UpdateVolumeDisplay(float value)
        {
            if (volumeValueText)
            {
                volumeValueText.text = $"{(value * 100):F0}%";
            }
        }
        
        private void UpdateDifficultyDisplay(float value)
        {
            if (difficultyValueText)
            {
                string difficultyName = value < 0.33f ? "Easy" : 
                                       value < 0.66f ? "Medium" : "Hard";
                difficultyValueText.text = difficultyName;
            }
        }
        
        // Background Selection
        public void SelectBackground(int index)
        {
            if (index >= 0 && index < backgroundNames.Length)
            {
                currentBackgroundIndex = index;
                UpdateBackgroundDisplay();
                OnBackgroundChanged?.Invoke(index);
                PlayerPrefs.SetInt("BackgroundIndex", index);
                
                Debug.Log($"Selected background: {backgroundNames[index]}");
            }
        }
        
        private void UpdateBackgroundDisplay()
        {
            if (currentBackgroundText)
            {
                currentBackgroundText.text = backgroundNames[currentBackgroundIndex];
            }
            
            if (backgroundPreview && backgroundPreviews.Length > currentBackgroundIndex)
            {
                backgroundPreview.sprite = backgroundPreviews[currentBackgroundIndex];
            }
            
            // Update button highlights
            for (int i = 0; i < backgroundButtons.Length; i++)
            {
                if (backgroundButtons[i] != null)
                {
                    var colors = backgroundButtons[i].colors;
                    colors.normalColor = i == currentBackgroundIndex ? Color.green : Color.white;
                    backgroundButtons[i].colors = colors;
                }
            }
        }
        
        // Scene Selection
        public void SelectScene(int index)
        {
            if (index >= 0 && index < sceneNames.Length)
            {
                currentSceneIndex = index;
                UpdateSceneDisplay();
                OnSceneChanged?.Invoke(index);
                PlayerPrefs.SetInt("SceneIndex", index);
                
                Debug.Log($"Selected scene: {sceneNames[index]}");
            }
        }
        
        private void UpdateSceneDisplay()
        {
            if (currentSceneText)
            {
                currentSceneText.text = sceneNames[currentSceneIndex];
            }
            
            if (scenePreview && scenePreviews.Length > currentSceneIndex)
            {
                scenePreview.sprite = scenePreviews[currentSceneIndex];
            }
            
            if (sceneDescriptionText)
            {
                sceneDescriptionText.text = sceneDescriptions[currentSceneIndex];
            }
            
            // Update button highlights
            for (int i = 0; i < sceneButtons.Length; i++)
            {
                if (sceneButtons[i] != null)
                {
                    var colors = sceneButtons[i].colors;
                    colors.normalColor = i == currentSceneIndex ? Color.green : Color.white;
                    sceneButtons[i].colors = colors;
                }
            }
        }
        
        public void ToggleSceneSelection()
        {
            ShowSceneSelection();
        }
        
        private void LoadSelectedScene()
        {
            var sceneLoadingManager = FindObjectOfType<SceneLoadingManager>();
            if (sceneLoadingManager != null)
            {
                sceneLoadingManager.LoadScene(currentSceneIndex);
                
                // Ensure target mode is applied after scene load
                if (SceneTransformationSystem.Instance != null)
                {
                    SceneTransformationSystem.Instance.SetUseTraditionalTargets(useTraditionalTargets);
                }
            }
            else
            {
                Debug.LogWarning("SceneLoadingManager not found!");
            }
        }
        
        // Spotify Integration
        public void ConnectToSpotify()
        {
            Debug.Log("Attempting to connect to Spotify...");
            
            if (SpotifyIntegration.Instance != null)
            {
                // Enable Spotify integration
                SpotifyIntegration.Instance.enableSpotifyIntegration = true;
                
                // In a real implementation, this would:
                // 1. Open OAuth flow
                // 2. Get user authorization
                // 3. Fetch playlists
                
                UpdateSpotifyDisplay();
            }
        }
        
        public void DisconnectFromSpotify()
        {
            Debug.Log("Disconnecting from Spotify...");
            
            if (SpotifyIntegration.Instance != null)
            {
                SpotifyIntegration.Instance.enableSpotifyIntegration = false;
                UpdateSpotifyDisplay();
            }
        }
        
        private void OnSpotifyConnectionChanged(bool connected)
        {
            UpdateSpotifyDisplay();
            
            if (connected)
            {
                LoadSpotifyPlaylists();
            }
        }
        
        private void UpdateSpotifyDisplay()
        {
            bool isConnected = SpotifyIntegration.Instance != null && 
                              SpotifyIntegration.Instance.IsConnected;
            
            if (spotifyStatusText)
            {
                spotifyStatusText.text = isConnected ? "Connected" : "Disconnected";
                spotifyStatusText.color = isConnected ? Color.green : Color.red;
            }
            
            if (connectSpotifyButton) connectSpotifyButton.gameObject.SetActive(!isConnected);
            if (disconnectSpotifyButton) disconnectSpotifyButton.gameObject.SetActive(isConnected);
        }
        
        private void LoadSpotifyPlaylists()
        {
            // Clear existing playlist items
            foreach (Transform child in playlistContent)
            {
                Destroy(child.gameObject);
            }
            
            // In a real implementation, this would load actual Spotify playlists
            string[] demoPlaylists = {
                "Workout Hits 2024",
                "Electronic Dance",
                "High Energy Mix",
                "Rhythm Gaming",
                "Bass Drops"
            };
            
            foreach (string playlistName in demoPlaylists)
            {
                CreatePlaylistItem(playlistName);
            }
        }
        
        private void CreatePlaylistItem(string playlistName)
        {
            if (playlistItemPrefab == null) return;
            
            GameObject item = Instantiate(playlistItemPrefab, playlistContent);
            
            // Setup playlist item UI
            TextMeshProUGUI nameText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText) nameText.text = playlistName;
            
            Button selectButton = item.GetComponentInChildren<Button>();
            if (selectButton)
            {
                selectButton.onClick.AddListener(() => SelectPlaylist(playlistName));
            }
        }
        
        private void SelectPlaylist(string playlistName)
        {
            Debug.Log($"Selected playlist: {playlistName}");
            
            // In a real implementation, this would load the playlist tracks
            if (SpotifyIntegration.Instance != null)
            {
                // Load tracks from selected playlist
                // SpotifyIntegration.Instance.LoadPlaylist(playlistName);
            }
        }
        
        private void ApplyGameSettings()
        {
            // Apply volume
            if (volumeSlider && VRBoxingGame.Audio.AdvancedAudioManager.Instance != null)
            {
                VRBoxingGame.Audio.AdvancedAudioManager.Instance.SetMasterVolume(volumeSlider.value);
            }
            
            // Apply difficulty
            if (difficultySlider && VRBoxingGame.Core.GameManager.Instance != null)
            {
                // Set difficulty in game manager
                // GameManager.Instance.SetDifficulty(difficultySlider.value);
            }
        }
        
        public void OnTargetModeToggled(bool useTraditional)
        {
            useTraditionalTargets = useTraditional;
            
            // Save preference
            PlayerPrefs.SetInt("UseTraditionalTargets", useTraditional ? 1 : 0);
            PlayerPrefs.Save();
            
            // Update display
            UpdateTargetModeDisplay();
            
            // Apply to scene transformation system
            if (SceneTransformationSystem.Instance != null)
            {
                SceneTransformationSystem.Instance.SetUseTraditionalTargets(useTraditional);
                Debug.Log($"🎯 Target mode changed to: {(useTraditional ? "Traditional" : "Immersive")}");
            }
            
            // Special handling for underwater scene
            if (currentSceneIndex == 5 && !useTraditional) // Underwater World with immersive mode
            {
                EnsureUnderwaterFishSystem();
            }
            
            // Provide haptic feedback if available
            ProvideHapticFeedback();
            
            // Show confirmation message
            ShowTargetModeChangeConfirmation(useTraditional);
        }
        
        private void UpdateTargetModeDisplay()
        {
            if (traditionalTargetsToggle != null)
            {
                traditionalTargetsToggle.isOn = useTraditionalTargets;
            }
            
            if (targetModeText != null)
            {
                string modeText = useTraditionalTargets ? "Traditional Blocks" : "Immersive Elements";
                string sceneSpecific = GetSceneSpecificTargetDescription();
                targetModeText.text = $"{modeText}\n{sceneSpecific}";
            }
            
            // Update info panel text
            UpdateTargetModeInfoText();
        }
        
        private string GetSceneSpecificTargetDescription()
        {
            if (useTraditionalTargets)
            {
                return "Standard white/gray circles and blocks";
            }
            
            // Scene-specific immersive elements
            switch (currentSceneIndex)
            {
                case 0: // Default Arena
                    return "Enhanced visual targets";
                case 1: // Rain Storm
                    return "Lightning-charged raindrops";
                case 2: // Neon City
                    return "Glowing neon holograms";
                case 3: // Space Station
                    return "Floating space debris";
                case 4: // Crystal Cave
                    return "Resonating crystal formations";
                case 5: // Underwater World
                    return "🐟 Swimming fish with AI behavior";
                case 6: // Desert Oasis
                    return "Sand spirits and mirages";
                case 7: // Forest Glade
                    return "Forest spirits and magical lights";
                default:
                    return "Immersive scene elements";
            }
        }
        
        private void UpdateTargetModeInfoText()
        {
            if (targetModeInfoText == null) return;
            
            if (useTraditionalTargets)
            {
                targetModeInfoText.text = @"<color=#FFD700><b>Traditional Mode</b></color>

<color=#FFFFFF>Classic VR boxing experience with:</color>
• Standard white and gray target circles
• Combination blocks for defensive moves
• Consistent behavior across all scenes
• Optimal performance for lower-end devices

<color=#00FF00>✓ Best for competitive gameplay</color>
<color=#00FF00>✓ Predictable target behavior</color>
<color=#00FF00>✓ Maximum performance</color>";
            }
            else
            {
                string immersiveDescription = GetImmersiveDescription();
                targetModeInfoText.text = $@"<color=#00BFFF><b>Immersive Mode</b></color>

<color=#FFFFFF>Dynamic scene-specific targets:</color>
{immersiveDescription}

<color=#FFD700>⭐ Unique AI behaviors per scene</color>
<color=#FFD700>⭐ Enhanced visual effects</color>
<color=#FFD700>⭐ Immersive storytelling</color>

<color=#FF6B6B>Note: May impact performance on Quest 2</color>";
            }
        }
        
        private string GetImmersiveDescription()
        {
            switch (currentSceneIndex)
            {
                case 5: // Underwater World
                    return @"🐟 <color=#87CEEB><b>Underwater Fish System</b></color>
• Small fish scatter when hit
• Medium fish retreat and regroup
• Large fish become aggressive
• Sharks block with water disturbance
• Bioluminescent trail effects
• Ocean current simulation";
                    
                case 4: // Crystal Cave
                    return @"💎 <color=#DDA0DD><b>Crystal Harmonics</b></color>
• Resonating crystal formations
• Musical frequency oscillations
• Harmonic cluster blocks
• Gem particle effects";
                    
                case 7: // Forest Glade
                    return @"🌲 <color=#98FB98><b>Forest Spirits</b></color>
• Magical forest spirits
• Seasonal adaptation effects
• Thorn vine defensive blocks
• Nature particle systems";
                    
                case 1: // Rain Storm
                    return @"⚡ <color=#4682B4><b>Storm Elements</b></color>
• Lightning-charged particles
• Thunder-synced effects
• Rain-based interactions
• Weather-reactive behavior";
                    
                default:
                    return "• Scene-specific visual elements\n• Enhanced particle effects\n• Unique interaction behaviors";
            }
        }
        
        private void EnsureUnderwaterFishSystem()
        {
            // Ensure underwater fish system is available and initialized
            var fishSystem = UnderwaterFishSystem.Instance;
            if (fishSystem == null)
            {
                // Create fish system if it doesn't exist
                GameObject fishSystemGO = new GameObject("Underwater Fish System");
                fishSystem = fishSystemGO.AddComponent<UnderwaterFishSystem>();
                Debug.Log("🐟 Created Underwater Fish System for immersive mode");
            }
            
            // Enable the fish system
            if (fishSystem != null && !fishSystem.enabled)
            {
                fishSystem.enabled = true;
                Debug.Log("🌊 Enabled Underwater Fish System");
            }
        }
        
        private void ProvideHapticFeedback()
        {
            // Provide haptic feedback for VR controllers
            var handTracking = FindObjectOfType<HandTrackingManager>();
            if (handTracking != null)
            {
                // Light haptic pulse to confirm selection
                handTracking.TriggerHapticFeedback(0.1f, 0.2f);
            }
        }
        
        private void ShowTargetModeChangeConfirmation(bool useTraditional)
        {
            string mode = useTraditional ? "Traditional" : "Immersive";
            string sceneSpecific = GetSceneSpecificTargetDescription();
            
            // Show temporary confirmation message
            var confirmationGO = new GameObject("TargetModeConfirmation");
            var canvas = confirmationGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = VRBoxingGame.Core.VRCameraHelper.ActiveCamera;
            
            var text = confirmationGO.AddComponent<TextMeshProUGUI>();
            text.text = $"<color=#FFD700>Target Mode: {mode}</color>\n<color=#FFFFFF>{sceneSpecific}</color>";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            
            // Position in front of player
            var cameraTransform = VRBoxingGame.Core.VRCameraHelper.ActiveCameraTransform;
            confirmationGO.transform.position = cameraTransform.position + cameraTransform.forward * 3f;
            confirmationGO.transform.LookAt(cameraTransform);
            confirmationGO.transform.Rotate(0, 180, 0);
            
            // Auto-destroy after 3 seconds
            Destroy(confirmationGO, 3f);
        }
        
        public void ShowTargetModeInfo()
        {
            if (targetModeInfoPanel != null)
            {
                bool isActive = targetModeInfoPanel.activeSelf;
                targetModeInfoPanel.SetActive(!isActive);
                
                if (!isActive)
                {
                    UpdateTargetModeInfoText();
                }
            }
        }
        
        public void HideTargetModeInfo()
        {
            if (targetModeInfoPanel != null)
            {
                targetModeInfoPanel.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            // Save settings
            PlayerPrefs.Save();
        }
    }
} 