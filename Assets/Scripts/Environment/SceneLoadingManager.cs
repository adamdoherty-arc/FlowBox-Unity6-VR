using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using VRBoxingGame.Environment;
using VRBoxingGame.Audio;
using VRBoxingGame.Core;
using UnityEngine.Events;
using System.Threading.Tasks;
using System;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Professional scene loading system with async/await patterns for Unity 6
    /// Handles scene transitions, loading screens, and environment setup
    /// </summary>
    public class SceneLoadingManager : MonoBehaviour
    {
        [Header("Scene Configuration")]
        public bool enableTransitionEffects = true;
        public float transitionDuration = 2f;
        
        [Header("Loading Screen")]
        public Canvas loadingCanvas;
        public UnityEngine.UI.Slider loadingProgressBar;
        public TMPro.TextMeshProUGUI loadingText;
        
        [Header("Scene Environments")]
        public GameObject[] sceneEnvironments;
        
        public enum SceneType
        {
            DefaultArena = 0,
            RainStorm = 1,
            NeonCity = 2,
            SpaceStation = 3,
            CrystalCave = 4,
            UnderwaterWorld = 5,
            DesertOasis = 6,
            ForestGlade = 7
        }
        
        // Events
        public System.Action<SceneType> OnSceneChanged;
        public System.Action<float> OnLoadingProgress;
        
        // Singleton
        public static SceneLoadingManager Instance { get; private set; }
        
        // Private variables
        private bool isLoading = false;
        private SceneType currentScene = SceneType.DefaultArena;
        private RainSceneCreator rainSceneCreator;
        private DynamicBackgroundSystem backgroundSystem;
        private AdvancedAudioManager audioManager;
        
        // Scene descriptions
        private readonly string[] sceneDescriptions = {
            "Professional boxing arena with crowd atmosphere",
            "Intense thunderstorm with lightning and rain effects", 
            "Cyberpunk neon city with holographic elements",
            "Zero-gravity space station with cosmic views",
            "Crystal cave with harmonic resonance effects",
            "Underwater world with marine life and currents",
            "Desert oasis with heat mirages and sand effects",
            "Enchanted forest with magical spirits and seasons"
        };
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSceneManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSceneManager()
        {
            // Find required components
            rainSceneCreator = FindObjectOfType<RainSceneCreator>();
            backgroundSystem = FindObjectOfType<DynamicBackgroundSystem>();
            audioManager = FindObjectOfType<AdvancedAudioManager>();
            
            // Create loading screen if needed
            if (loadingCanvas == null)
            {
                CreateLoadingScreen();
            }
            
            Debug.Log("Scene Loading Manager initialized");
        }
        
        private void CreateLoadingScreen()
        {
            // Create loading canvas
            GameObject canvasObj = new GameObject("Loading Canvas");
            loadingCanvas = canvasObj.AddComponent<Canvas>();
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 1000;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Create background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            
            // Create loading text
            GameObject textObj = new GameObject("Loading Text");
            textObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.6f);
            textRect.anchorMax = new Vector2(0.5f, 0.6f);
            textRect.sizeDelta = new Vector2(400, 60);
            
            loadingText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            loadingText.text = "Loading...";
            loadingText.fontSize = 36;
            loadingText.color = Color.white;
            loadingText.alignment = TMPro.TextAlignmentOptions.Center;
            
            // Create progress bar
            GameObject progressObj = new GameObject("Progress Bar");
            progressObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform progressRect = progressObj.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.5f, 0.4f);
            progressRect.anchorMax = new Vector2(0.5f, 0.4f);
            progressRect.sizeDelta = new Vector2(400, 20);
            
            loadingProgressBar = progressObj.AddComponent<UnityEngine.UI.Slider>();
            loadingProgressBar.minValue = 0f;
            loadingProgressBar.maxValue = 1f;
            loadingProgressBar.value = 0f;
            
            // Initially hide loading screen
            canvasObj.SetActive(false);
        }
        
        /// <summary>
        /// Load a specific scene environment with async/await pattern
        /// </summary>
        public async Task LoadSceneAsync(SceneType sceneType)
        {
            if (isLoading)
            {
                Debug.LogWarning("Scene loading already in progress");
                return;
            }

            await LoadSceneAsyncInternal(sceneType);
        }

        /// <summary>
        /// Load scene by index (for UI buttons) 
        /// </summary>
        public async Task LoadScene(int sceneIndex)
        {
            if (sceneIndex >= 0 && sceneIndex < Enum.GetValues(typeof(SceneType)).Length)
            {
                await LoadSceneAsync((SceneType)sceneIndex);
            }
        }

        /// <summary>
        /// Synchronous wrapper for backward compatibility
        /// </summary>
        public void LoadScene(SceneType sceneType)
        {
            _ = LoadSceneAsync(sceneType);
        }

        private async Task LoadSceneAsyncInternal(SceneType sceneType)
        {
            isLoading = true;

            try
            {
                // Show loading screen
                if (loadingCanvas != null)
                {
                    loadingCanvas.gameObject.SetActive(true);
                    loadingText.text = $"Loading {GetSceneName(sceneType)}...";
                    loadingProgressBar.value = 0f;
                }

                // Fade out current scene
                if (enableTransitionEffects)
                {
                    await FadeTransitionAsync(true);
                }

                // Update progress
                UpdateLoadingProgress(0.2f, "Preparing environment...");
                await Task.Delay(500);

                // Unload current scene environment
                UnloadCurrentScene();
                UpdateLoadingProgress(0.4f, "Clearing previous environment...");
                await Task.Delay(300);

                // Load new scene environment
                await LoadSceneEnvironmentAsync(sceneType);
                UpdateLoadingProgress(0.7f, "Applying scene transformations...");

                // Configure scene transformation system
                if (SceneTransformationSystem.Instance != null)
                {
                    SceneTransformationSystem.Instance.SetSceneType((SceneTransformationSystem.SceneType)(int)sceneType);
                }

                UpdateLoadingProgress(0.8f, "Finalizing setup...");

                // Configure audio for new scene
                ConfigureAudioForScene(sceneType);

                // Update current scene
                currentScene = sceneType;
                PlayerPrefs.SetInt("CurrentScene", (int)sceneType);

                UpdateLoadingProgress(1f, "Complete!");
                await Task.Delay(500);

                // Fade in new scene
                if (enableTransitionEffects)
                {
                    await FadeTransitionAsync(false);
                }

                // Hide loading screen
                if (loadingCanvas != null)
                {
                    loadingCanvas.gameObject.SetActive(false);
                }

                // Notify listeners
                OnSceneChanged?.Invoke(sceneType);

                Debug.Log($"Scene loaded: {GetSceneName(sceneType)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading scene {sceneType}: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadSceneEnvironmentAsync(SceneType sceneType)
        {
            switch (sceneType)
            {
                case SceneType.DefaultArena:
                    await LoadDefaultArenaAsync();
                    break;
                    
                case SceneType.RainStorm:
                    await LoadRainStormAsync();
                    break;
                    
                case SceneType.NeonCity:
                    await LoadNeonCityAsync();
                    break;
                    
                case SceneType.SpaceStation:
                    await LoadSpaceStationAsync();
                    break;
                    
                case SceneType.CrystalCave:
                    await LoadCrystalCaveAsync();
                    break;
                    
                case SceneType.UnderwaterWorld:
                    await LoadUnderwaterWorldAsync();
                    break;
                    
                case SceneType.DesertOasis:
                    await LoadDesertOasisAsync();
                    break;
                    
                case SceneType.ForestGlade:
                    await LoadForestGladeAsync();
                    break;
            }
        }

        private async Task LoadDefaultArenaAsync()
        {
            // Activate default background system
            if (backgroundSystem != null)
            {
                backgroundSystem.LoadTheme(0); // Default theme
            }
            
            await Task.Delay(200);
        }

        private async Task LoadRainStormAsync()
        {
            // Create or activate rain scene
            if (rainSceneCreator == null)
            {
                GameObject rainObj = new GameObject("Rain Scene Creator");
                rainSceneCreator = rainObj.AddComponent<RainSceneCreator>();
            }

            // Create rain environment
            rainSceneCreator.CreateCompleteRainEnvironment();

            // Set to medium intensity by default
            rainSceneCreator.SetWeatherIntensity(RainSceneCreator.WeatherIntensity.Medium);

            await Task.Delay(500);
        }

        private async Task LoadNeonCityAsync()
        {
            // Load cyberpunk theme with light rain
            if (backgroundSystem != null)
            {
                backgroundSystem.LoadTheme(0); // Cyberpunk theme
            }

            // Add light rain effect
            if (rainSceneCreator != null)
            {
                rainSceneCreator.CreateCompleteRainEnvironment();
                rainSceneCreator.SetWeatherIntensity(RainSceneCreator.WeatherIntensity.Light);
            }

            await Task.Delay(300);
        }

        private async Task LoadSpaceStationAsync()
        {
            if (backgroundSystem != null)
            {
                backgroundSystem.LoadTheme(1); // Space theme
            }
            await Task.Delay(200);
        }

        private async Task LoadCrystalCaveAsync()
        {
            if (backgroundSystem != null)
            {
                backgroundSystem.LoadTheme(3); // Crystal theme
            }
            await Task.Delay(200);
        }

        private async Task LoadUnderwaterWorldAsync()
        {
            if (backgroundSystem != null)
            {
                backgroundSystem.LoadTheme(5); // Underwater theme
            }
            await Task.Delay(200);
        }

        private async Task LoadDesertOasisAsync()
        {
            if (backgroundSystem != null)
            {
                backgroundSystem.LoadTheme(2); // Desert-like abstract theme
            }
            await Task.Delay(200);
        }

        private async Task LoadForestGladeAsync()
        {
            if (backgroundSystem != null)
            {
                backgroundSystem.LoadTheme(4); // Aurora/nature theme
            }
            await Task.Delay(200);
        }

        private async Task FadeTransitionAsync(bool fadeOut)
        {
            // Simple screen fade implementation
            float duration = transitionDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = fadeOut ? elapsed / duration : 1f - (elapsed / duration);

                // Apply fade effect (you can enhance this with actual screen fade)
                await Task.Yield();
            }
        }
        
        private void UnloadCurrentScene()
        {
            // Deactivate current environment objects
            if (sceneEnvironments != null)
            {
                foreach (var env in sceneEnvironments)
                {
                    if (env != null)
                    {
                        env.SetActive(false);
                    }
                }
            }
            
            // Special handling for rain scene
            if (rainSceneCreator != null && currentScene == SceneType.RainStorm)
            {
                rainSceneCreator.DestroyRainEnvironment();
            }
        }
        
        private void ConfigureAudioForScene(SceneType sceneType)
        {
            if (audioManager == null) return;
            
            // Adjust audio settings based on scene
            switch (sceneType)
            {
                case SceneType.RainStorm:
                    // Enable rain audio effects
                    audioManager.SetEnvironmentalAudio(true);
                    break;
                    
                case SceneType.UnderwaterWorld:
                    // Add underwater audio filter
                    audioManager.SetUnderwaterMode(true);
                    break;
                    
                default:
                    // Reset to default audio
                    audioManager.SetEnvironmentalAudio(false);
                    audioManager.SetUnderwaterMode(false);
                    break;
            }
        }
        
        private void UpdateLoadingProgress(float progress, string message)
        {
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = progress;
            }
            
            if (loadingText != null)
            {
                loadingText.text = message;
            }
            
            OnLoadingProgress?.Invoke(progress);
        }
        
        public string GetSceneName(SceneType sceneType)
        {
            return sceneType.ToString().Replace("_", " ");
        }
        
        public string GetSceneDescription(SceneType sceneType)
        {
            int index = (int)sceneType;
            if (index >= 0 && index < sceneDescriptions.Length)
            {
                return sceneDescriptions[index];
            }
            return "Unknown scene";
        }
        
        public SceneType GetCurrentScene()
        {
            return currentScene;
        }
        
        public bool IsLoading()
        {
            return isLoading;
        }
        
        /// <summary>
        /// Quick scene switch without loading screen (for testing)
        /// </summary>
        public void QuickSwitchScene(SceneType sceneType)
        {
            UnloadCurrentScene();
            _ = LoadSceneEnvironmentAsync(sceneType);
            currentScene = sceneType;
            OnSceneChanged?.Invoke(sceneType);
        }
    }
} 