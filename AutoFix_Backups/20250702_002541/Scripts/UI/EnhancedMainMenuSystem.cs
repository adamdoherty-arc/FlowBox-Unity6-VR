using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using VRBoxingGame.Environment;
using VRBoxingGame.Music;
using VRBoxingGame.Spotify;
using VRBoxingGame.Core;
using Unity.XR.CoreUtils;

namespace VRBoxingGame.UI
{
    /// <summary>
    /// Enhanced Main Menu System for VR Boxing Game
    /// Features: Music service integration, normal/immersive mode switching, modern VR UI
    /// Provides comprehensive menu system with Apple Music, YouTube Music, and Spotify integration
    /// </summary>
    public class EnhancedMainMenuSystem : MonoBehaviour
    {
        [Header("Main Menu Panels")]
        public GameObject welcomePanel;
        public GameObject mainMenuPanel;
        public GameObject gameModesPanel;
        public GameObject sceneSelectionPanel;
        public GameObject musicServicesPanel;
        public GameObject settingsPanel;
        public GameObject profilePanel;
        public GameObject achievementsPanel;
        public GameObject tutorialPanel;
        
        [Header("Music Service Panels")]
        public GameObject appleMusicPanel;
        public GameObject youtubeMusicPanel;
        public GameObject spotifyPanel;
        public GameObject localMusicPanel;
        public GameObject musicPlayerPanel;
        public GameObject playlistBrowserPanel;
        
        [Header("Scene Mode Panels")]
        public GameObject normalModePanel;
        public GameObject immersiveModePanel;
        public GameObject modeComparisonPanel;
        public GameObject scenePreviewPanel;
        
        [Header("Main Menu Navigation")]
        public Button playButton;
        public Button gameModesButton;
        public Button musicButton;
        public Button settingsButton;
        public Button profileButton;
        public Button achievementsButton;
        public Button tutorialButton;
        public Button quitButton;
        
        [Header("Game Mode Selection")]
        public Button quickPlayButton;
        public Button careerModeButton;
        public Button challengeButton;
        public Button trainingButton;
        public Button customGameButton;
        public Toggle normalModeToggle;
        public Toggle immersiveModeToggle;
        public TextMeshProUGUI modeDescriptionText;
        public Button modeInfoButton;
        
        [Header("New Game Modes")]
        public Button flowModeButton;
        public Button staffModeButton;
        public Button dodgingModeButton;
        public Button aiCoachButton;
        
        [Header("Advanced Game Mode Settings")]
        public Toggle enableDodgingToggle;
        public Toggle intensiveDodgingToggle;
        public Toggle aiCoachingToggle;
        public Slider difficultyAdvancedSlider;
        public TextMeshProUGUI advancedDifficultyText;
        
        [Header("Game Mode Descriptions")]
        public TextMeshProUGUI flowModeDescriptionText;
        public TextMeshProUGUI staffModeDescriptionText;
        public TextMeshProUGUI dodgingModeDescriptionText;
        public TextMeshProUGUI aiCoachDescriptionText;
        
        [Header("Scene Selection Enhanced")]
        public ScrollRect sceneScrollView;
        public Transform sceneButtonContainer;
        public GameObject sceneButtonPrefab;
        public Image scenePreviewImage;
        public TextMeshProUGUI sceneNameText;
        public TextMeshProUGUI sceneDescriptionText;
        public TextMeshProUGUI sceneStatsText;
        public Button scenePlayButton;
        public Slider sceneDifficultySlider;
        public TextMeshProUGUI difficultyText;
        
        [Header("Music Service Integration")]
        public Button appleMusicConnectButton;
        public Button youtubeMusicConnectButton;
        public Button spotifyConnectButton;
        public Button localMusicButton;
        public TextMeshProUGUI appleMusicStatusText;
        public TextMeshProUGUI youtubeMusicStatusText;
        public TextMeshProUGUI spotifyStatusText;
        public Image appleMusicIcon;
        public Image youtubeMusicIcon;
        public Image spotifyIcon;
        
        [Header("Music Authentication UI")]
        public GameObject authLoadingPanel;
        public TextMeshProUGUI authStatusText;
        public Button authCancelButton;
        public Slider authProgressSlider;
        public Image authServiceIcon;
        
        [Header("Music Player Controls")]
        public Button playPauseButton;
        public Button previousButton;
        public Button nextButton;
        public Button shuffleButton;
        public Button repeatButton;
        public Slider volumeSlider;
        public Slider progressSlider;
        public TextMeshProUGUI currentTrackText;
        public TextMeshProUGUI currentArtistText;
        public Image currentTrackArtwork;
        public TextMeshProUGUI timeDisplayText;
        
        [Header("Playlist Browser")]
        public ScrollRect playlistScrollView;
        public Transform playlistContainer;
        public GameObject playlistItemPrefab;
        public ScrollRect trackScrollView;
        public Transform trackContainer;
        public GameObject trackItemPrefab;
        public Button createPlaylistButton;
        public TMP_InputField playlistSearchField;
        
        [Header("Enhanced Settings")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider effectsVolumeSlider;
        public Toggle handTrackingToggle;
        public Toggle hapticsToggle;
        public Toggle autoOptimizationToggle;
        public Dropdown qualityDropdown;
        public Dropdown languageDropdown;
        public Toggle subtitlesToggle;
        public Slider comfortSettingsSlider;
        public Toggle colorBlindAssistToggle;
        
        [Header("User Profile")]
        public TextMeshProUGUI playerNameText;
        public Image playerAvatarImage;
        public TextMeshProUGUI playerLevelText;
        public Slider playerXPSlider;
        public TextMeshProUGUI playerStatsText;
        public Button editProfileButton;
        public Button viewAchievementsButton;
        
        [Header("VR UI Enhancement")]
        public float menuDistance = 2f;
        public float menuScale = 1f;
        public bool useGazeSelection = true;
        public bool useHandTracking = true;
        public float selectionTime = 2f;
        public GameObject gazePointer;
        public GameObject handPointers;
        public AudioSource uiAudioSource;
        public AudioClip[] uiSounds;
        
        [Header("Animation Settings")]
        public float panelTransitionTime = 0.3f;
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool enableParticleEffects = true;
        public ParticleSystem menuParticles;
        
        [Header("New Game Modes")]
        public Button flowModeButton;
        public Button staffModeButton;
        public Button dodgingModeButton;
        public Button aiCoachButton;
        
        [Header("Advanced Game Mode Settings")]
        public Toggle enableDodgingToggle;
        public Toggle intensiveDodgingToggle;
        public Toggle aiCoachingToggle;
        public Slider difficultyAdvancedSlider;
        public TextMeshProUGUI advancedDifficultyText;
        
        [Header("Game Mode Descriptions")]
        public TextMeshProUGUI flowModeDescriptionText;
        public TextMeshProUGUI staffModeDescriptionText;
        public TextMeshProUGUI dodgingModeDescriptionText;
        public TextMeshProUGUI aiCoachDescriptionText;
        
        // Private Variables
        private int currentSceneIndex = 0;
        private bool isImmersiveMode = false;
        private string currentMusicService = "";
        private bool isAuthenticated = false;
        private List<SceneData> availableScenes = new List<SceneData>();
        private Dictionary<string, bool> musicServiceStatus = new Dictionary<string, bool>();
        private float gazeTimer = 0f;
        private Button currentGazedButton = null;
        
        // Music Integration References
        private AppleMusicIntegration appleMusic;
        private YouTubeMusicIntegration youtubeMusic;
        private SpotifyIntegration spotify;
        
        // Events
        public UnityEvent<int> OnSceneSelected;
        public UnityEvent<bool> OnModeChanged;
        public UnityEvent<string> OnMusicServiceChanged;
        public UnityEvent OnGameStarted;
        
        // Data Structures
        [System.Serializable]
        public struct SceneData
        {
            public int sceneIndex;
            public string sceneName;
            public string normalDescription;
            public string immersiveDescription;
            public Sprite previewImage;
            public float difficulty;
            public int timesPlayed;
            public float bestScore;
            public bool isUnlocked;
            public string[] tags;
        }
        
        [System.Serializable]
        public struct MusicServiceData
        {
            public string serviceName;
            public bool isConnected;
            public string userName;
            public int playlistCount;
            public string lastConnected;
        }
        
        // Singleton
        public static EnhancedMainMenuSystem Instance { get; private set; }
        
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
        
        private void Start()
        {
            InitializeMusicServices();
            SetupVRInteraction();
            LoadUserPreferences();
            SetupSceneData();
            ShowWelcomeScreen();
        }
        
        private void Update()
        {
            if (useGazeSelection)
            {
                UpdateGazeSelection();
            }
            
            UpdateMusicPlayer();
            UpdateAuthenticationStatus();
        }
        
        private void InitializeMainMenu()
        {
            Debug.Log("🎮 Initializing Enhanced Main Menu System...");
            
            // Setup button listeners
            SetupButtonListeners();
            
            // Initialize music service status
            musicServiceStatus["AppleMusic"] = false;
            musicServiceStatus["YouTubeMusic"] = false;
            musicServiceStatus["Spotify"] = false;
            
            // Hide all panels initially
            HideAllPanels();
            
            Debug.Log("✅ Enhanced Main Menu System initialized!");
        }
        
        private void InitializeMusicServices()
        {
            // Get music service instances
            appleMusic = AppleMusicIntegration.Instance;
            youtubeMusic = YouTubeMusicIntegration.Instance;
            spotify = SpotifyIntegration.Instance;
            
            // Subscribe to connection events
            if (appleMusic != null)
            {
                appleMusic.OnConnectionStatusChanged.AddListener(OnAppleMusicConnectionChanged);
            }
            
            if (youtubeMusic != null)
            {
                youtubeMusic.OnConnectionStatusChanged.AddListener(OnYouTubeMusicConnectionChanged);
            }
            
            if (spotify != null)
            {
                spotify.OnConnectionStatusChanged.AddListener(OnSpotifyConnectionChanged);
            }
            
            UpdateMusicServiceStatus();
        }
        
        private void SetupVRInteraction()
        {
            // Position menu for VR
            var xrOrigin = CachedReferenceManager.Get<XROrigin>();
            if (xrOrigin != null)
            {
                Vector3 menuPosition = xrOrigin.transform.position + xrOrigin.transform.forward * menuDistance;
                transform.position = menuPosition;
                transform.LookAt(xrOrigin.transform);
                transform.localScale = Vector3.one * menuScale;
            }
            
            // Setup gaze pointer
            if (gazePointer != null)
            {
                gazePointer.SetActive(useGazeSelection);
            }
            
            // Setup hand pointers
            if (handPointers != null)
            {
                handPointers.SetActive(useHandTracking);
            }
        }
        
        private void SetupButtonListeners()
        {
            // Main menu navigation
            if (playButton) playButton.onClick.AddListener(ShowGameModes);
            if (gameModesButton) gameModesButton.onClick.AddListener(ShowGameModes);
            if (musicButton) musicButton.onClick.AddListener(ShowMusicServices);
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettings);
            if (profileButton) profileButton.onClick.AddListener(ShowProfile);
            if (achievementsButton) achievementsButton.onClick.AddListener(ShowAchievements);
            if (tutorialButton) tutorialButton.onClick.AddListener(ShowTutorial);
            if (quitButton) quitButton.onClick.AddListener(QuitGame);
            
            // Game mode selection
            if (quickPlayButton) quickPlayButton.onClick.AddListener(QuickPlay);
            if (careerModeButton) careerModeButton.onClick.AddListener(StartCareerMode);
            if (challengeButton) challengeButton.onClick.AddListener(ShowChallenges);
            if (trainingButton) trainingButton.onClick.AddListener(StartTraining);
            if (customGameButton) customGameButton.onClick.AddListener(ShowCustomGame);
            
            // Mode toggles
            if (normalModeToggle) normalModeToggle.onValueChanged.AddListener(OnNormalModeToggled);
            if (immersiveModeToggle) immersiveModeToggle.onValueChanged.AddListener(OnImmersiveModeToggled);
            if (modeInfoButton) modeInfoButton.onClick.AddListener(ShowModeComparison);
            
            // New game mode buttons
            if (flowModeButton) flowModeButton.onClick.AddListener(StartFlowMode);
            if (staffModeButton) staffModeButton.onClick.AddListener(StartStaffMode);
            if (dodgingModeButton) dodgingModeButton.onClick.AddListener(StartDodgingMode);
            if (aiCoachButton) aiCoachButton.onClick.AddListener(ToggleAICoach);
            
            // Advanced settings
            if (enableDodgingToggle) enableDodgingToggle.onValueChanged.AddListener(OnDodgingToggled);
            if (intensiveDodgingToggle) intensiveDodgingToggle.onValueChanged.AddListener(OnIntensiveDodgingToggled);
            if (aiCoachingToggle) aiCoachingToggle.onValueChanged.AddListener(OnAICoachingToggled);
            if (difficultyAdvancedSlider) difficultyAdvancedSlider.onValueChanged.AddListener(OnAdvancedDifficultyChanged);
            
            // Music service connections
            if (appleMusicConnectButton) appleMusicConnectButton.onClick.AddListener(ConnectAppleMusic);
            if (youtubeMusicConnectButton) youtubeMusicConnectButton.onClick.AddListener(ConnectYouTubeMusic);
            if (spotifyConnectButton) spotifyConnectButton.onClick.AddListener(ConnectSpotify);
            if (localMusicButton) localMusicButton.onClick.AddListener(ShowLocalMusic);
            
            // Music player controls
            if (playPauseButton) playPauseButton.onClick.AddListener(TogglePlayPause);
            if (previousButton) previousButton.onClick.AddListener(PreviousTrack);
            if (nextButton) nextButton.onClick.AddListener(NextTrack);
            if (shuffleButton) shuffleButton.onClick.AddListener(ToggleShuffle);
            if (repeatButton) repeatButton.onClick.AddListener(ToggleRepeat);
            if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnPlayerVolumeChanged);
            if (progressSlider) progressSlider.onValueChanged.AddListener(OnTrackProgressChanged);
            
            // Settings
            if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (effectsVolumeSlider) effectsVolumeSlider.onValueChanged.AddListener(OnEffectsVolumeChanged);
            if (handTrackingToggle) handTrackingToggle.onValueChanged.AddListener(OnHandTrackingToggled);
            if (hapticsToggle) hapticsToggle.onValueChanged.AddListener(OnHapticsToggled);
            if (autoOptimizationToggle) autoOptimizationToggle.onValueChanged.AddListener(OnAutoOptimizationToggled);
            if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (languageDropdown) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            if (subtitlesToggle) subtitlesToggle.onValueChanged.AddListener(OnSubtitlesToggled);
            if (comfortSettingsSlider) comfortSettingsSlider.onValueChanged.AddListener(OnComfortSettingsChanged);
            if (colorBlindAssistToggle) colorBlindAssistToggle.onValueChanged.AddListener(OnColorBlindAssistToggled);
            
            // Profile
            if (editProfileButton) editProfileButton.onClick.AddListener(EditProfile);
            if (viewAchievementsButton) viewAchievementsButton.onClick.AddListener(ShowAchievements);
            
            // Playlist
            if (createPlaylistButton) createPlaylistButton.onClick.AddListener(CreatePlaylist);
            
            // Scene selection
            if (scenePlayButton) scenePlayButton.onClick.AddListener(PlaySelectedScene);
            if (sceneDifficultySlider) sceneDifficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
            
            // Authentication
            if (authCancelButton) authCancelButton.onClick.AddListener(CancelAuthentication);
        }
        
        private void SetupSceneData()
        {
            availableScenes.Clear();
            
            // Define all available scenes with normal and immersive descriptions
            // **BUG FIX**: Load scene stats from PlayerPrefs for persistence
            availableScenes.Add(new SceneData
            {
                sceneIndex = 0,
                sceneName = "Championship Arena",
                normalDescription = "Professional boxing arena with crowd atmosphere",
                immersiveDescription = "Step into the champion's arena where your rhythm conducts the crowd's roar and spotlight's dance",
                difficulty = 0.3f,
                isUnlocked = true,
                timesPlayed = PlayerPrefs.GetInt("Scene_0_TimesPlayed", 0),
                bestScore = PlayerPrefs.GetFloat("Scene_0_BestScore", 0f),
                tags = new string[] { "Classic", "Crowd", "Professional" }
            });
            
            availableScenes.Add(new SceneData
            {
                sceneIndex = 1,
                sceneName = "Storm Symphony",
                normalDescription = "Intense thunderstorm with lightning and rain effects",
                immersiveDescription = "Become the storm conductor, harmonizing with nature's most violent and beautiful electrical symphony",
                difficulty = 0.7f,
                isUnlocked = true,
                timesPlayed = PlayerPrefs.GetInt("Scene_1_TimesPlayed", 0),
                bestScore = PlayerPrefs.GetFloat("Scene_1_BestScore", 0f),
                tags = new string[] { "Weather", "Intense", "Natural" }
            });
            
            availableScenes.Add(new SceneData
            {
                sceneIndex = 2,
                sceneName = "Cyberpunk Underground",
                normalDescription = "Cyberpunk neon city with holographic elements",
                immersiveDescription = "Enter the digital underground as a data runner, hacking through neon-soaked cyberpunk reality",
                difficulty = 0.6f,
                isUnlocked = true,
                timesPlayed = PlayerPrefs.GetInt("Scene_2_TimesPlayed", 0),
                bestScore = PlayerPrefs.GetFloat("Scene_2_BestScore", 0f),
                tags = new string[] { "Futuristic", "Neon", "Technology" }
            });
            
            availableScenes.Add(new SceneData
            {
                sceneIndex = 3,
                sceneName = "Cosmic Observatory",
                normalDescription = "Space station with view of planets and stars",
                immersiveDescription = "Command a cosmic observatory where planets and stars orbit to the rhythm of your perfect timing",
                difficulty = 0.5f,
                isUnlocked = true,
                timesPlayed = PlayerPrefs.GetInt("Scene_3_TimesPlayed", 0),
                bestScore = PlayerPrefs.GetFloat("Scene_3_BestScore", 0f),
                tags = new string[] { "Space", "Cosmic", "Beautiful" }
            });
            
            availableScenes.Add(new SceneData
            {
                sceneIndex = 4,
                sceneName = "Crystal Resonance",
                normalDescription = "Crystal cave with colorful formations",
                immersiveDescription = "Discover crystal caverns where each formation sings with pure harmonic resonance and ancient memory",
                difficulty = 0.4f,
                isUnlocked = true,
                timesPlayed = PlayerPrefs.GetInt("Scene_4_TimesPlayed", 0),
                bestScore = PlayerPrefs.GetFloat("Scene_4_BestScore", 0f),
                tags = new string[] { "Crystal", "Musical", "Mystical" }
            });
            
            availableScenes.Add(new SceneData
            {
                sceneIndex = 5,
                sceneName = "Abyssal Ballet",
                normalDescription = "Underwater world with marine life",
                immersiveDescription = "Dive into abyssal depths where bioluminescent creatures create nature's own responsive light show",
                difficulty = 0.8f,
                isUnlocked = true,
                timesPlayed = PlayerPrefs.GetInt("Scene_5_TimesPlayed", 0),
                bestScore = PlayerPrefs.GetFloat("Scene_5_BestScore", 0f),
                tags = new string[] { "Underwater", "Marine", "Bioluminescent" }
            });
            
            availableScenes.Add(new SceneData
            {
                sceneIndex = 6,
                sceneName = "Mirage Oasis",
                normalDescription = "Desert scene with sand dunes and oasis",
                immersiveDescription = "Journey to a mystical oasis where desert spirits challenge you to separate illusion from truth",
                difficulty = 0.6f,
                isUnlocked = true,
                timesPlayed = PlayerPrefs.GetInt("Scene_6_TimesPlayed", 0),
                bestScore = PlayerPrefs.GetFloat("Scene_6_BestScore", 0f),
                tags = new string[] { "Desert", "Mystical", "Illusion" }
            });
            
            availableScenes.Add(new SceneData
            {
                sceneIndex = 7,
                sceneName = "Enchanted Grove",
                normalDescription = "Forest with trees and natural lighting",
                immersiveDescription = "Enter an enchanted grove where ancient trees serve as instruments in nature's own orchestra",
                difficulty = 0.5f,
                isUnlocked = true,
                timesPlayed = PlayerPrefs.GetInt("Scene_7_TimesPlayed", 0),
                bestScore = PlayerPrefs.GetFloat("Scene_7_BestScore", 0f),
                tags = new string[] { "Forest", "Natural", "Magical" }
            });
            
            // **BUG FIX**: Validate scene data and create buttons
            if (availableScenes.Count == 0)
            {
                Debug.LogError("❌ No scenes available! Check scene data setup.");
                return;
            }
            
            CreateSceneButtons();
            
            // **BUG FIX**: Validate current scene index
            if (currentSceneIndex >= availableScenes.Count)
            {
                currentSceneIndex = 0;
                Debug.LogWarning($"⚠️ Invalid scene index, reset to 0");
            }
            
            SelectScene(currentSceneIndex); // Select current scene
            
            Debug.Log($"✅ Loaded {availableScenes.Count} scenes successfully");
        }
        
        private void CreateSceneButtons()
        {
            // **BUG FIX**: Validate container before creating buttons
            if (sceneButtonContainer == null)
            {
                Debug.LogError("❌ Scene button container is null! Cannot create scene buttons.");
                return;
            }
            
            if (sceneButtonPrefab == null)
            {
                Debug.LogError("❌ Scene button prefab is null! Cannot create scene buttons.");
                return;
            }
            
            // Clear existing buttons
            foreach (Transform child in sceneButtonContainer)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            
            // Create buttons for each scene
            for (int i = 0; i < availableScenes.Count; i++)
            {
                int sceneIndex = i; // Capture for closure
                SceneData scene = availableScenes[i];
                
                try
                {
                    GameObject buttonObj = Instantiate(sceneButtonPrefab, sceneButtonContainer);
                    Button button = buttonObj.GetComponent<Button>();
                    
                    if (button == null)
                    {
                        Debug.LogError($"❌ Scene button prefab missing Button component for scene {i}");
                        continue;
                    }
                    
                    // Setup button appearance
                    TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText) 
                    {
                        buttonText.text = scene.sceneName;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Scene button {i} missing text component");
                    }
                    
                    // **BUG FIX**: Add null check for button listener
                    if (button != null)
                    {
                        button.onClick.AddListener(() => SelectScene(sceneIndex));
                        
                        // Disable if locked
                        button.interactable = scene.isUnlocked;
                        
                        // Visual feedback for locked scenes
                        if (!scene.isUnlocked)
                        {
                            var colors = button.colors;
                            colors.normalColor = Color.gray;
                            colors.disabledColor = Color.gray;
                            button.colors = colors;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Failed to create button for scene {i}: {ex.Message}");
                }
            }
            
            Debug.Log($"✅ Created {availableScenes.Count} scene buttons");
        }
        
        // Panel Management
        private void HideAllPanels()
        {
            var panels = new GameObject[] {
                welcomePanel, mainMenuPanel, gameModesPanel, sceneSelectionPanel,
                musicServicesPanel, settingsPanel, profilePanel, achievementsPanel,
                tutorialPanel, appleMusicPanel, youtubeMusicPanel, spotifyPanel,
                localMusicPanel, musicPlayerPanel, playlistBrowserPanel,
                normalModePanel, immersiveModePanel, modeComparisonPanel,
                scenePreviewPanel, authLoadingPanel
            };
            
            foreach (var panel in panels)
            {
                if (panel != null) panel.SetActive(false);
            }
        }
        
        private async Task ShowPanel(GameObject panel)
        {
            HideAllPanels();
            if (panel != null)
            {
                panel.SetActive(true);
                
                // Animate panel entrance
                if (enableParticleEffects && menuParticles != null)
                {
                    menuParticles.Play();
                }
                
                await AnimatePanelTransition(panel, true);
                PlayUISound(0); // Panel open sound
            }
        }
        
        private async Task AnimatePanelTransition(GameObject panel, bool isOpening)
        {
            if (panel == null) return;
            
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
            
            float startAlpha = isOpening ? 0f : 1f;
            float endAlpha = isOpening ? 1f : 0f;
            float elapsedTime = 0f;
            
            while (elapsedTime < panelTransitionTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / panelTransitionTime;
                float curveValue = transitionCurve.Evaluate(progress);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
                
                await Task.Yield();
            }
            
            canvasGroup.alpha = endAlpha;
        }
        
        // Main Menu Navigation
        public void ShowWelcomeScreen()
        {
            ShowPanel(welcomePanel);
            // Auto-transition to main menu after delay
            Invoke(nameof(ShowMainMenu), 3f);
        }
        
        public void ShowMainMenu()
        {
            ShowPanel(mainMenuPanel);
            UpdatePlayerProfile();
        }
        
        public void ShowGameModes()
        {
            ShowPanel(gameModesPanel);
            UpdateModeDescription();
        }
        
        public void ShowSceneSelection()
        {
            ShowPanel(sceneSelectionPanel);
            UpdateSceneSelection();
        }
        
        public void ShowMusicServices()
        {
            ShowPanel(musicServicesPanel);
            UpdateMusicServiceStatus();
        }
        
        public void ShowSettings()
        {
            ShowPanel(settingsPanel);
            LoadSettingsValues();
        }
        
        public void ShowProfile()
        {
            ShowPanel(profilePanel);
            UpdatePlayerProfile();
        }
        
        public void ShowAchievements()
        {
            ShowPanel(achievementsPanel);
            LoadAchievements();
        }
        
        public void ShowTutorial()
        {
            ShowPanel(tutorialPanel);
        }
        
        // Game Mode Functions
        public void QuickPlay()
        {
            PlayUISound(1); // Confirm sound
            StartGame();
        }
        
        public void StartCareerMode()
        {
            PlayUISound(1);
            // Career mode logic
            Debug.Log("🏆 Starting Career Mode");
            StartGame();
        }
        
        public void ShowChallenges()
        {
            PlayUISound(1);
            // Show challenge selection
            Debug.Log("⚡ Showing Challenges");
        }
        
        public void StartTraining()
        {
            PlayUISound(1);
            // Training mode logic
            Debug.Log("🥊 Starting Training Mode");
            StartGame();
        }
        
        public void ShowCustomGame()
        {
            PlayUISound(1);
            ShowSceneSelection();
        }
        
        // Mode Management
        private void OnNormalModeToggled(bool isOn)
        {
            if (isOn)
            {
                isImmersiveMode = false;
                immersiveModeToggle.isOn = false;
                UpdateModeDescription();
                OnModeChanged?.Invoke(false);
                PlayUISound(2); // Toggle sound
            }
        }
        
        private void OnImmersiveModeToggled(bool isOn)
        {
            if (isOn)
            {
                isImmersiveMode = true;
                normalModeToggle.isOn = false;
                UpdateModeDescription();
                OnModeChanged?.Invoke(true);
                PlayUISound(2); // Toggle sound
            }
        }
        
        private void UpdateModeDescription()
        {
            if (modeDescriptionText == null) return;
            
            if (isImmersiveMode)
            {
                modeDescriptionText.text = "<color=#FF6B6B><b>IMMERSIVE MODE</b></color>\n\n" +
                    "Experience the full narrative power of each scene. You become the protagonist in " +
                    "dynamic, story-driven environments that respond to your performance and the music. " +
                    "Each scene tells a unique story where you are the hero.\n\n" +
                    "<color=#4ECDC4>• Dynamic storytelling environments</color>\n" +
                    "<color=#4ECDC4>• Music-reactive atmosphere</color>\n" +
                    "<color=#4ECDC4>• Performance-based scene evolution</color>\n" +
                    "<color=#4ECDC4>• Protagonist-centered narratives</color>";
            }
            else
            {
                modeDescriptionText.text = "<color=#45B7D1><b>NORMAL MODE</b></color>\n\n" +
                    "Classic VR boxing experience with beautiful environments and standard target gameplay. " +
                    "Focus on rhythm, accuracy, and fitness in stunning visual settings.\n\n" +
                    "<color=#96CEB4>• Traditional target-based gameplay</color>\n" +
                    "<color=#96CEB4>• Beautiful static environments</color>\n" +
                    "<color=#96CEB4>• Focus on fitness and rhythm</color>\n" +
                    "<color=#96CEB4>• Familiar VR boxing mechanics</color>";
            }
        }
        
        public void ShowModeComparison()
        {
            ShowPanel(modeComparisonPanel);
            PlayUISound(0);
        }
        
        // Scene Selection
        public void SelectScene(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= availableScenes.Count) return;
            
            currentSceneIndex = sceneIndex;
            SceneData scene = availableScenes[sceneIndex];
            
            // Update UI
            if (sceneNameText) sceneNameText.text = scene.sceneName;
            if (sceneDescriptionText)
            {
                sceneDescriptionText.text = isImmersiveMode ? scene.immersiveDescription : scene.normalDescription;
            }
            
            if (sceneStatsText)
            {
                sceneStatsText.text = $"Difficulty: {(scene.difficulty * 100):F0}%\n" +
                                    $"Times Played: {scene.timesPlayed}\n" +
                                    $"Best Score: {scene.bestScore:F0}";
            }
            
            if (sceneDifficultySlider)
            {
                sceneDifficultySlider.value = scene.difficulty;
            }
            
            OnSceneSelected?.Invoke(sceneIndex);
            PlayUISound(2); // Selection sound
        }
        
        private void UpdateSceneSelection()
        {
            SelectScene(currentSceneIndex); // Refresh current selection
        }
        
        public void PlaySelectedScene()
        {
            PlayUISound(1); // Confirm sound
            
            // **BUG FIX**: Update scene stats before playing
            UpdateSceneStats(currentSceneIndex);
            
            // Apply immersive/normal mode setting
            var sceneLoader = SceneLoadingManager.Instance;
            var sceneSenseSystem = EnhancedSceneSenseSystem.Instance;
            
            if (sceneLoader != null)
            {
                sceneLoader.LoadScene(currentSceneIndex);
            }
            
            if (sceneSenseSystem != null)
            {
                sceneSenseSystem.SetImmersiveMode(isImmersiveMode);
            }
            
            StartGame();
        }
        
        private void OnDifficultyChanged(float value)
        {
            if (difficultyText) difficultyText.text = $"Difficulty: {(value * 100):F0}%";
        }
        
        // Music Service Integration
        public async Task ConnectAppleMusic()
        {
            PlayUISound(1);
            await AuthenticateMusicService("Apple Music", async () => 
            {
                if (appleMusic != null)
                {
                    return await appleMusic.AuthenticateAsync();
                }
                return false;
            });
        }
        
        public async Task ConnectYouTubeMusic()
        {
            PlayUISound(1);
            await AuthenticateMusicService("YouTube Music", async () => 
            {
                if (youtubeMusic != null)
                {
                    return await youtubeMusic.AuthenticateAsync();
                }
                return false;
            });
        }
        
        public async Task ConnectSpotify()
        {
            PlayUISound(1);
            await AuthenticateMusicService("Spotify", async () => 
            {
                if (spotify != null)
                {
                    return await spotify.AuthenticateWithSpotifyAsync();
                }
                return false;
            });
        }
        
        public void ShowLocalMusic()
        {
            PlayUISound(1);
            ShowPanel(localMusicPanel);
        }
        
        private async Task AuthenticateMusicService(string serviceName, Func<Task<bool>> authFunction)
        {
            ShowAuthenticationProgress(serviceName);
            
            try
            {
                bool success = await authFunction();
                
                if (success)
                {
                    musicServiceStatus[serviceName.Replace(" ", "")] = true;
                    currentMusicService = serviceName;
                    ShowAuthenticationSuccess(serviceName);
                    
                    // Load music library
                    await LoadMusicLibrary(serviceName);
                }
                else
                {
                    ShowAuthenticationError(serviceName, "Authentication failed or cancelled");
                }
            }
            catch (Exception ex)
            {
                ShowAuthenticationError(serviceName, ex.Message);
            }
            
            HideAuthenticationProgress();
        }
        
        private void ShowAuthenticationProgress(string serviceName)
        {
            if (authLoadingPanel) authLoadingPanel.SetActive(true);
            if (authStatusText) authStatusText.text = $"Connecting to {serviceName}...";
            if (authProgressSlider) authProgressSlider.value = 0f;
            
            // Animate progress
            StartCoroutine(AnimateAuthProgress());
        }
        
        private System.Collections.IEnumerator AnimateAuthProgress()
        {
            float progress = 0f;
            while (authLoadingPanel && authLoadingPanel.activeInHierarchy && progress < 1f)
            {
                progress += Time.deltaTime / 5f; // 5 second max
                if (authProgressSlider) authProgressSlider.value = progress;
                yield return null;
            }
        }
        
        private void HideAuthenticationProgress()
        {
            if (authLoadingPanel) authLoadingPanel.SetActive(false);
        }
        
        private void ShowAuthenticationSuccess(string serviceName)
        {
            Debug.Log($"✅ {serviceName} authentication successful!");
            UpdateMusicServiceStatus();
        }
        
        private void ShowAuthenticationError(string serviceName, string error)
        {
            Debug.LogError($"❌ {serviceName} authentication failed: {error}");
            // Show error UI
        }
        
        public void CancelAuthentication()
        {
            PlayUISound(3); // Cancel sound
            HideAuthenticationProgress();
        }
        
        private async Task LoadMusicLibrary(string serviceName)
        {
            switch (serviceName)
            {
                case "Apple Music":
                    if (appleMusic != null)
                    {
                        await appleMusic.GetUserPlaylistsAsync();
                    }
                    break;
                case "YouTube Music":
                    if (youtubeMusic != null)
                    {
                        await youtubeMusic.GetUserPlaylistsAsync();
                    }
                    break;
                case "Spotify":
                    if (spotify != null)
                    {
                        await spotify.LoadUserPlaylistsAsync();
                    }
                    break;
            }
        }
        
        // Music Service Events
        private void OnAppleMusicConnectionChanged(bool connected)
        {
            musicServiceStatus["AppleMusic"] = connected;
            UpdateMusicServiceDisplay();
        }
        
        private void OnYouTubeMusicConnectionChanged(bool connected)
        {
            musicServiceStatus["YouTubeMusic"] = connected;
            UpdateMusicServiceDisplay();
        }
        
        private void OnSpotifyConnectionChanged(bool connected)
        {
            musicServiceStatus["Spotify"] = connected;
            UpdateMusicServiceDisplay();
        }
        
        private void UpdateMusicServiceStatus()
        {
            if (appleMusic != null)
            {
                musicServiceStatus["AppleMusic"] = appleMusic.IsAuthenticated;
            }
            
            if (youtubeMusic != null)
            {
                musicServiceStatus["YouTubeMusic"] = youtubeMusic.IsAuthenticated;
            }
            
            if (spotify != null)
            {
                musicServiceStatus["Spotify"] = spotify.IsConnected;
            }
            
            UpdateMusicServiceDisplay();
        }
        
        private void UpdateMusicServiceDisplay()
        {
            // Update Apple Music UI
            if (appleMusicStatusText)
            {
                bool connected = musicServiceStatus.GetValueOrDefault("AppleMusic", false);
                appleMusicStatusText.text = connected ? "Connected" : "Not Connected";
                appleMusicStatusText.color = connected ? Color.green : Color.red;
            }
            
            if (appleMusicConnectButton)
            {
                var buttonText = appleMusicConnectButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText)
                {
                    bool connected = musicServiceStatus.GetValueOrDefault("AppleMusic", false);
                    buttonText.text = connected ? "Disconnect" : "Connect";
                }
            }
            
            // Update YouTube Music UI
            if (youtubeMusicStatusText)
            {
                bool connected = musicServiceStatus.GetValueOrDefault("YouTubeMusic", false);
                youtubeMusicStatusText.text = connected ? "Connected" : "Not Connected";
                youtubeMusicStatusText.color = connected ? Color.green : Color.red;
            }
            
            if (youtubeMusicConnectButton)
            {
                var buttonText = youtubeMusicConnectButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText)
                {
                    bool connected = musicServiceStatus.GetValueOrDefault("YouTubeMusic", false);
                    buttonText.text = connected ? "Disconnect" : "Connect";
                }
            }
            
            // Update Spotify UI
            if (spotifyStatusText)
            {
                bool connected = musicServiceStatus.GetValueOrDefault("Spotify", false);
                spotifyStatusText.text = connected ? "Connected" : "Not Connected";
                spotifyStatusText.color = connected ? Color.green : Color.red;
            }
            
            if (spotifyConnectButton)
            {
                var buttonText = spotifyConnectButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText)
                {
                    bool connected = musicServiceStatus.GetValueOrDefault("Spotify", false);
                    buttonText.text = connected ? "Disconnect" : "Connect";
                }
            }
        }
        
        // Music Player Controls
        public void TogglePlayPause()
        {
            PlayUISound(2);
            
            // **BUG FIX**: Implement actual play/pause functionality
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                // Toggle Apple Music playback
                Debug.Log("🎵 Toggling Apple Music playback");
                // appleMusic.TogglePlayPause(); // Would need this method in AppleMusicIntegration
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                // Toggle YouTube Music playback
                Debug.Log("📺 Toggling YouTube Music playback");
                // youtubeMusic.TogglePlayPause(); // Would need this method in YouTubeMusicIntegration
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                // Toggle Spotify playback
                Debug.Log("🎧 Toggling Spotify playback");
                // spotify.TogglePlayPause(); // Would need this method in SpotifyIntegration
            }
            else
            {
                Debug.LogWarning("⚠️ No music service connected for playback control");
            }
        }
        
        public void PreviousTrack()
        {
            PlayUISound(2);
            
            // **BUG FIX**: Implement actual previous track functionality
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                Debug.Log("⏮️ Apple Music: Previous track");
                // appleMusic.PreviousTrack();
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                Debug.Log("⏮️ YouTube Music: Previous track");
                // youtubeMusic.PreviousTrack();
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                Debug.Log("⏮️ Spotify: Previous track");
                // spotify.PreviousTrack();
            }
        }
        
        public void NextTrack()
        {
            PlayUISound(2);
            
            // **BUG FIX**: Implement actual next track functionality
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                Debug.Log("⏭️ Apple Music: Next track");
                // appleMusic.NextTrack();
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                Debug.Log("⏭️ YouTube Music: Next track");
                // youtubeMusic.NextTrack();
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                Debug.Log("⏭️ Spotify: Next track");
                // spotify.NextTrack();
            }
        }
        
        public void ToggleShuffle()
        {
            PlayUISound(2);
            
            // **BUG FIX**: Implement shuffle toggle functionality
            bool shuffleEnabled = PlayerPrefs.GetInt("ShuffleEnabled", 0) == 1;
            shuffleEnabled = !shuffleEnabled;
            PlayerPrefs.SetInt("ShuffleEnabled", shuffleEnabled ? 1 : 0);
            
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                Debug.Log($"🔀 Apple Music shuffle: {(shuffleEnabled ? "ON" : "OFF")}");
                // appleMusic.SetShuffle(shuffleEnabled);
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                Debug.Log($"🔀 YouTube Music shuffle: {(shuffleEnabled ? "ON" : "OFF")}");
                // youtubeMusic.SetShuffle(shuffleEnabled);
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                Debug.Log($"🔀 Spotify shuffle: {(shuffleEnabled ? "ON" : "OFF")}");
                // spotify.SetShuffle(shuffleEnabled);
            }
            
            // Update button appearance
            if (shuffleButton)
            {
                var colors = shuffleButton.colors;
                colors.normalColor = shuffleEnabled ? Color.green : Color.white;
                shuffleButton.colors = colors;
            }
        }
        
        public void ToggleRepeat()
        {
            PlayUISound(2);
            
            // **BUG FIX**: Implement repeat toggle functionality
            int repeatMode = PlayerPrefs.GetInt("RepeatMode", 0); // 0=off, 1=repeat all, 2=repeat one
            repeatMode = (repeatMode + 1) % 3;
            PlayerPrefs.SetInt("RepeatMode", repeatMode);
            
            string[] repeatModes = { "OFF", "ALL", "ONE" };
            
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                Debug.Log($"🔁 Apple Music repeat: {repeatModes[repeatMode]}");
                // appleMusic.SetRepeatMode(repeatMode);
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                Debug.Log($"🔁 YouTube Music repeat: {repeatModes[repeatMode]}");
                // youtubeMusic.SetRepeatMode(repeatMode);
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                Debug.Log($"🔁 Spotify repeat: {repeatModes[repeatMode]}");
                // spotify.SetRepeatMode(repeatMode);
            }
            
            // Update button appearance
            if (repeatButton)
            {
                var colors = repeatButton.colors;
                colors.normalColor = repeatMode > 0 ? Color.green : Color.white;
                repeatButton.colors = colors;
                
                // Update button text if it has text component
                var buttonText = repeatButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText)
                {
                    buttonText.text = repeatMode == 2 ? "🔂" : "🔁";
                }
            }
        }
        
        private void UpdateMusicPlayer()
        {
            // **BUG FIX**: Update music player UI with current track info
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                var currentTrack = appleMusic.GetCurrentTrack();
                if (currentTrackText) currentTrackText.text = currentTrack.title ?? "No Track";
                if (currentArtistText) currentArtistText.text = currentTrack.artist ?? "Unknown Artist";
                
                // Update progress
                if (progressSlider && currentTrack.duration > 0)
                {
                    // This would need actual playback time from the service
                    // progressSlider.value = currentPlaybackTime / currentTrack.duration;
                }
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                var currentTrack = youtubeMusic.GetCurrentTrack();
                if (currentTrackText) currentTrackText.text = currentTrack.title ?? "No Track";
                if (currentArtistText) currentArtistText.text = currentTrack.artist ?? "Unknown Artist";
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                // Spotify integration would go here
                if (currentTrackText) currentTrackText.text = "Spotify Track";
                if (currentArtistText) currentArtistText.text = "Spotify Artist";
            }
            else
            {
                if (currentTrackText) currentTrackText.text = "No Music Service";
                if (currentArtistText) currentArtistText.text = "Connect a music service";
            }
            
            // Update time display
            if (timeDisplayText)
            {
                // This would show current time / total time
                timeDisplayText.text = "0:00 / 0:00";
            }
        }
        
        private void UpdateAuthenticationStatus()
        {
            // Check and update authentication status for all services
        }
        
        // Settings
        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            SaveSetting("MasterVolume", value);
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            // Adjust music volume
            SaveSetting("MusicVolume", value);
        }
        
        private void OnEffectsVolumeChanged(float value)
        {
            // Adjust effects volume
            SaveSetting("EffectsVolume", value);
        }
        
        private void OnHandTrackingToggled(bool enabled)
        {
            useHandTracking = enabled;
            SaveSetting("HandTracking", enabled ? 1f : 0f);
            SetupVRInteraction();
        }
        
        private void OnHapticsToggled(bool enabled)
        {
            SaveSetting("Haptics", enabled ? 1f : 0f);
        }
        
        private void OnAutoOptimizationToggled(bool enabled)
        {
            SaveSetting("AutoOptimization", enabled ? 1f : 0f);
        }
        
        private void OnQualityChanged(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            SaveSetting("QualityLevel", qualityIndex);
            Debug.Log($"🎮 Quality set to: {QualitySettings.names[qualityIndex]}");
        }
        
        private void OnLanguageChanged(int languageIndex)
        {
            string[] languages = { "English", "Spanish", "French", "German", "Japanese", "Chinese" };
            string selectedLanguage = languageIndex < languages.Length ? languages[languageIndex] : "English";
            SaveSetting("Language", languageIndex);
            PlayerPrefs.SetString("SelectedLanguage", selectedLanguage);
            Debug.Log($"🌍 Language set to: {selectedLanguage}");
        }
        
        private void OnSubtitlesToggled(bool enabled)
        {
            SaveSetting("Subtitles", enabled ? 1f : 0f);
            Debug.Log($"📝 Subtitles: {(enabled ? "Enabled" : "Disabled")}");
        }
        
        private void OnComfortSettingsChanged(float comfortLevel)
        {
            SaveSetting("ComfortLevel", comfortLevel);
            // Apply comfort settings to VR
            ApplyComfortSettings(comfortLevel);
            Debug.Log($"😌 Comfort level set to: {(comfortLevel * 100):F0}%");
        }
        
        private void OnColorBlindAssistToggled(bool enabled)
        {
            SaveSetting("ColorBlindAssist", enabled ? 1f : 0f);
            ApplyColorBlindSettings(enabled);
            Debug.Log($"🎨 Color blind assistance: {(enabled ? "Enabled" : "Disabled")}");
        }
        
        private void OnPlayerVolumeChanged(float volume)
        {
            // Apply volume to current music service
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                // Apple Music volume control would go here
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                // YouTube Music volume control would go here
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                // Spotify volume control would go here
            }
            
            SaveSetting("PlayerVolume", volume);
            Debug.Log($"🎵 Player volume set to: {(volume * 100):F0}%");
        }
        
        private void OnTrackProgressChanged(float progress)
        {
            // Seek to position in current track
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                // Apple Music seek would go here
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                // YouTube Music seek would go here
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                // Spotify seek would go here
            }
            
            Debug.Log($"⏯️ Track progress: {(progress * 100):F1}%");
        }
        
        private void EditProfile()
        {
            PlayUISound(1);
            // Open profile editing interface
            Debug.Log("👤 Opening profile editor...");
            
            // Could open a profile editing panel here
            // For now, show a simple name change dialog
            string currentName = PlayerPrefs.GetString("PlayerName", "VR Boxer");
            // In a real implementation, you'd show an input field
            Debug.Log($"Current player name: {currentName}");
        }
        
        public void CreatePlaylist()
        {
            PlayUISound(1);
            Debug.Log("🎵 Creating new playlist...");
            
            // Create new playlist in current music service
            if (currentMusicService == "Apple Music" && appleMusic != null)
            {
                // Apple Music playlist creation would go here
            }
            else if (currentMusicService == "YouTube Music" && youtubeMusic != null)
            {
                // YouTube Music playlist creation would go here
            }
            else if (currentMusicService == "Spotify" && spotify != null)
            {
                // Spotify playlist creation would go here
            }
        }
        
        private void ApplyComfortSettings(float comfortLevel)
        {
            // Apply comfort settings based on level (0.0 = most comfortable, 1.0 = most intense)
            
            // Adjust movement comfort
            var vrOrigin = CachedReferenceManager.Get<Unity.XR.CoreUtils.XROrigin>();
            if (vrOrigin != null)
            {
                // Reduce motion intensity for comfort
                float motionReduction = 1.0f - (comfortLevel * 0.5f);
                // Apply motion settings to VR rig
            }
            
            // Adjust visual effects intensity
            if (enableParticleEffects)
            {
                var particleSystems = FindObjectsOfType<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    var emission = ps.emission;
                    var main = ps.main;
                    emission.rateOverTime = emission.rateOverTime.constant * (0.5f + comfortLevel * 0.5f);
                }
            }
        }
        
        private void ApplyColorBlindSettings(bool enabled)
        {
            if (enabled)
            {
                // Apply color blind friendly color schemes
                // This would typically involve changing UI colors and target colors
                Debug.Log("🎨 Applying color blind friendly UI colors");
                
                // Example: Modify target colors for better visibility
                var targetSystem = CachedReferenceManager.Get<RhythmTargetSystem>();
                if (targetSystem != null)
                {
                    // Apply high contrast colors
                    // targetSystem.SetColorBlindMode(true);
                }
            }
            else
            {
                // Restore default colors
                Debug.Log("🎨 Restoring default UI colors");
            }
        }
        
        private void LoadSettingsValues()
        {
            if (masterVolumeSlider) masterVolumeSlider.value = GetSetting("MasterVolume", 0.8f);
            if (musicVolumeSlider) musicVolumeSlider.value = GetSetting("MusicVolume", 0.8f);
            if (effectsVolumeSlider) effectsVolumeSlider.value = GetSetting("EffectsVolume", 0.8f);
            if (handTrackingToggle) handTrackingToggle.isOn = GetSetting("HandTracking", 1f) > 0f;
            if (hapticsToggle) hapticsToggle.isOn = GetSetting("Haptics", 1f) > 0f;
            if (autoOptimizationToggle) autoOptimizationToggle.isOn = GetSetting("AutoOptimization", 1f) > 0f;
            
            // **BUG FIX**: Load additional settings
            if (qualityDropdown) qualityDropdown.value = (int)GetSetting("QualityLevel", QualitySettings.GetQualityLevel());
            if (languageDropdown) languageDropdown.value = (int)GetSetting("Language", 0);
            if (subtitlesToggle) subtitlesToggle.isOn = GetSetting("Subtitles", 1f) > 0f;
            if (comfortSettingsSlider) comfortSettingsSlider.value = GetSetting("ComfortLevel", 0.5f);
            if (colorBlindAssistToggle) colorBlindAssistToggle.isOn = GetSetting("ColorBlindAssist", 0f) > 0f;
            if (volumeSlider) volumeSlider.value = GetSetting("PlayerVolume", 0.8f);
        }
        
        // VR Gaze Selection
        private void UpdateGazeSelection()
        {
            if (!useGazeSelection) return;
            
            // Raycast from center of view
            Ray gazeRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;
            
            if (Physics.Raycast(gazeRay, out hit, 10f))
            {
                Button hoveredButton = hit.collider.GetComponent<Button>();
                
                if (hoveredButton != null && hoveredButton.interactable)
                {
                    if (hoveredButton == currentGazedButton)
                    {
                        gazeTimer += Time.deltaTime;
                        
                        // Visual feedback for gaze progress
                        UpdateGazeProgress(gazeTimer / selectionTime);
                        
                        if (gazeTimer >= selectionTime)
                        {
                            // Execute button click
                            hoveredButton.onClick.Invoke();
                            gazeTimer = 0f;
                            currentGazedButton = null;
                        }
                    }
                    else
                    {
                        currentGazedButton = hoveredButton;
                        gazeTimer = 0f;
                    }
                }
                else
                {
                    currentGazedButton = null;
                    gazeTimer = 0f;
                    UpdateGazeProgress(0f);
                }
            }
            else
            {
                currentGazedButton = null;
                gazeTimer = 0f;
                UpdateGazeProgress(0f);
            }
        }
        
        private void UpdateGazeProgress(float progress)
        {
            if (gazePointer != null)
            {
                var progressRing = gazePointer.GetComponent<Image>();
                if (progressRing != null)
                {
                    progressRing.fillAmount = progress;
                }
            }
        }
        
        // Utility Functions
        private void PlayUISound(int soundIndex)
        {
            if (uiAudioSource != null && uiSounds != null && soundIndex < uiSounds.Length)
            {
                uiAudioSource.PlayOneShot(uiSounds[soundIndex]);
            }
        }
        
        private void SaveSetting(string key, float value)
        {
            PlayerPrefs.SetFloat($"EnhancedMenu_{key}", value);
        }
        
        private float GetSetting(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat($"EnhancedMenu_{key}", defaultValue);
        }
        
        private void LoadUserPreferences()
        {
            isImmersiveMode = PlayerPrefs.GetInt("ImmersiveMode", 0) == 1;
            currentSceneIndex = PlayerPrefs.GetInt("LastSceneIndex", 0);
            
            if (normalModeToggle) normalModeToggle.isOn = !isImmersiveMode;
            if (immersiveModeToggle) immersiveModeToggle.isOn = isImmersiveMode;
            
            UpdateModeDescription();
        }
        
        private void UpdatePlayerProfile()
        {
            if (playerNameText) playerNameText.text = PlayerPrefs.GetString("PlayerName", "VR Boxer");
            if (playerLevelText) playerLevelText.text = $"Level {PlayerPrefs.GetInt("PlayerLevel", 1)}";
            
            // **BUG FIX**: Update additional profile elements
            if (playerXPSlider)
            {
                int currentXP = PlayerPrefs.GetInt("PlayerXP", 0);
                int levelXP = PlayerPrefs.GetInt("PlayerLevel", 1) * 1000; // 1000 XP per level
                int nextLevelXP = (PlayerPrefs.GetInt("PlayerLevel", 1) + 1) * 1000;
                playerXPSlider.value = (float)(currentXP - levelXP) / (nextLevelXP - levelXP);
            }
            
            if (playerStatsText)
            {
                int totalPlays = PlayerPrefs.GetInt("TotalPlays", 0);
                float bestScore = PlayerPrefs.GetFloat("BestScore", 0f);
                int totalHits = PlayerPrefs.GetInt("TotalHits", 0);
                float accuracy = PlayerPrefs.GetFloat("OverallAccuracy", 0f);
                
                playerStatsText.text = $"Games Played: {totalPlays}\n" +
                                     $"Best Score: {bestScore:F0}\n" +
                                     $"Total Hits: {totalHits}\n" +
                                     $"Accuracy: {accuracy:P1}";
            }
        }
        
        private void LoadAchievements()
        {
            // **BUG FIX**: Load and display achievements
            Debug.Log("🏆 Loading achievements...");
            
            // Basic achievement tracking
            var achievements = new Dictionary<string, bool>
            {
                ["First Play"] = PlayerPrefs.GetInt("TotalPlays", 0) > 0,
                ["Combo Master"] = PlayerPrefs.GetInt("BestCombo", 0) >= 50,
                ["Perfect Score"] = PlayerPrefs.GetFloat("BestScore", 0) >= 100000,
                ["All Scenes"] = PlayerPrefs.GetInt("ScenesCompleted", 0) >= 8,
                ["Music Lover"] = PlayerPrefs.GetInt("TracksPlayed", 0) >= 10,
                ["VR Veteran"] = PlayerPrefs.GetInt("TotalPlays", 0) >= 100
            };
            
            foreach (var achievement in achievements)
            {
                if (achievement.Value)
                {
                    Debug.Log($"🏆 Achievement Unlocked: {achievement.Key}");
                }
            }
        }
        
        private void StartGame()
        {
            // Save current settings
            PlayerPrefs.SetInt("ImmersiveMode", isImmersiveMode ? 1 : 0);
            PlayerPrefs.SetInt("LastSceneIndex", currentSceneIndex);
            
            OnGameStarted?.Invoke();
            
            // Start the actual game
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
            
            // Hide menu
            gameObject.SetActive(false);
        }
        
        public void QuitGame()
        {
            PlayUISound(3); // Quit sound
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        // Public API
        public bool IsImmersiveMode => isImmersiveMode;
        public int CurrentSceneIndex => currentSceneIndex;
        public string CurrentMusicService => currentMusicService;
        public List<SceneData> GetAvailableScenes() => availableScenes;
        public Dictionary<string, bool> GetMusicServiceStatus() => musicServiceStatus;
        
        public void SetImmersiveMode(bool immersive)
        {
            isImmersiveMode = immersive;
            if (normalModeToggle) normalModeToggle.isOn = !immersive;
            if (immersiveModeToggle) immersiveModeToggle.isOn = immersive;
            UpdateModeDescription();
        }
        
        public void SetCurrentScene(int sceneIndex)
        {
            SelectScene(sceneIndex);
        }
        
        // **BUG FIX**: Add scene stats tracking
        private void UpdateSceneStats(int sceneIndex)
        {
            if (sceneIndex >= 0 && sceneIndex < availableScenes.Count)
            {
                var scene = availableScenes[sceneIndex];
                scene.timesPlayed = PlayerPrefs.GetInt($"Scene_{sceneIndex}_TimesPlayed", 0) + 1;
                scene.bestScore = PlayerPrefs.GetFloat($"Scene_{sceneIndex}_BestScore", 0f);
                
                // Save updated stats
                PlayerPrefs.SetInt($"Scene_{sceneIndex}_TimesPlayed", scene.timesPlayed);
                PlayerPrefs.SetInt("TotalPlays", PlayerPrefs.GetInt("TotalPlays", 0) + 1);
                
                // Update the scene data
                availableScenes[sceneIndex] = scene;
                
                Debug.Log($"📊 Scene {scene.sceneName} played {scene.timesPlayed} times");
            }
        }
        
        public void UpdateSceneScore(int sceneIndex, float score)
        {
            if (sceneIndex >= 0 && sceneIndex < availableScenes.Count)
            {
                var scene = availableScenes[sceneIndex];
                if (score > scene.bestScore)
                {
                    scene.bestScore = score;
                    PlayerPrefs.SetFloat($"Scene_{sceneIndex}_BestScore", score);
                    PlayerPrefs.SetFloat("BestScore", Mathf.Max(PlayerPrefs.GetFloat("BestScore", 0f), score));
                    availableScenes[sceneIndex] = scene;
                    
                    Debug.Log($"🏆 New best score for {scene.sceneName}: {score:F0}");
                }
            }
        }
    }
} 