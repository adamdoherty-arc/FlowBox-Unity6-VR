using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using VRBoxingGame.Core;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using VRBoxingGame.Environment;
using System.Threading.Tasks;
using System.Threading;

namespace VRBoxingGame.UI
{
    /// <summary>
    /// Main UI controller for the VR Rhythm Game
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Score Display")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI comboText;
        public TextMeshProUGUI multiplierText;
        public Slider comboMeter;
        
        [Header("Game Info")]
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI accuracyText;
        public TextMeshProUGUI bpmText;
        public Slider healthBar;
        
        [Header("Visual Effects")]
        public GameObject perfectHitEffect;
        public GameObject comboBreakEffect;
        public Image screenFlash;
        public AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Game State Panels")]
        public GameObject menuPanel;
        public GameObject gamePanel;
        public GameObject pausePanel;
        public GameObject gameOverPanel;
        
        [Header("Game Over Screen")]
        public TextMeshProUGUI finalScoreText;
        public TextMeshProUGUI finalAccuracyText;
        public TextMeshProUGUI highScoreText;
        public Button playAgainButton;
        public Button menuButton;
        
        [Header("Settings")]
        public Slider volumeSlider;
        public Slider difficultySlider;
        public Toggle handTrackingToggle;
        public Dropdown backgroundSelector;
        
        // Private variables
        private int currentScore = 0;
        private int currentCombo = 0;
        private float currentMultiplier = 1f;
        private float currentAccuracy = 0f;
        private Coroutine flashCoroutine;
        
        // Animation
        private Vector3 originalScoreScale;
        private Vector3 originalComboScale;
        private CancellationTokenSource screenFlashCancellation;
        
        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }
        
        private void InitializeUI()
        {
            // Auto-find UI elements if not assigned
            FindAndAssignUIElements();
            
            // Store original scales for animations
            if (scoreText) originalScoreScale = scoreText.transform.localScale;
            if (comboText) originalComboScale = comboText.transform.localScale;
            
            // Initialize UI elements
            UpdateScore(0);
            UpdateCombo(0, 1f);
            UpdateTime(180f); // Default game time
            UpdateAccuracy(0f);
            UpdateBPM(120f);
            
            // Setup button events
            if (playAgainButton) playAgainButton.onClick.AddListener(RestartGame);
            if (menuButton) menuButton.onClick.AddListener(ReturnToMenu);
            
            // Setup sliders
            if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (difficultySlider) difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
            if (handTrackingToggle) handTrackingToggle.onValueChanged.AddListener(OnHandTrackingToggle);
            if (backgroundSelector) backgroundSelector.onValueChanged.AddListener(OnBackgroundChanged);
            
            // Show game by default for VR
            ShowGame();
        }
        
        /// <summary>
        /// Auto-finds and assigns UI elements if they're not manually assigned
        /// Creates missing UI elements if needed
        /// </summary>
        public void FindAndAssignUIElements()
        {
            // Find score text
            if (scoreText == null)
            {
                scoreText = FindUIComponent<TextMeshProUGUI>("ScoreText");
                if (scoreText == null) scoreText = CreateUIText("ScoreText", "Score: 0", new Vector2(0, 100));
            }
            
            // Find combo text
            if (comboText == null)
            {
                comboText = FindUIComponent<TextMeshProUGUI>("ComboText");
                if (comboText == null) comboText = CreateUIText("ComboText", "Combo: 0", new Vector2(0, 50));
            }
            
            // Find multiplier text
            if (multiplierText == null)
            {
                multiplierText = FindUIComponent<TextMeshProUGUI>("MultiplierText");
                if (multiplierText == null) multiplierText = CreateUIText("MultiplierText", "x1.0", new Vector2(0, 0));
            }
            
            // Find time text
            if (timeText == null)
            {
                timeText = FindUIComponent<TextMeshProUGUI>("TimeText");
                if (timeText == null) timeText = CreateUIText("TimeText", "3:00", new Vector2(0, -50));
            }
            
            // Find accuracy text
            if (accuracyText == null)
            {
                accuracyText = FindUIComponent<TextMeshProUGUI>("AccuracyText");
                if (accuracyText == null) accuracyText = CreateUIText("AccuracyText", "Accuracy: 100%", new Vector2(0, -100));
            }
            
            // Find BPM text
            if (bpmText == null)
            {
                bpmText = FindUIComponent<TextMeshProUGUI>("BPMText");
                if (bpmText == null) bpmText = CreateUIText("BPMText", "BPM: 120", new Vector2(0, -150));
            }
            
            // Find combo meter
            if (comboMeter == null)
            {
                comboMeter = FindUIComponent<Slider>("ComboMeter");
                if (comboMeter == null) comboMeter = CreateUISlider("ComboMeter", new Vector2(0, 150));
            }
            
            // Find health bar
            if (healthBar == null)
            {
                healthBar = FindUIComponent<Slider>("HealthBar");
                if (healthBar == null) healthBar = CreateUISlider("HealthBar", new Vector2(0, 200));
            }
            
            // Ensure we have a Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                CreateGameUICanvas();
            }
            
            Debug.Log("✅ GameUI elements found and assigned successfully!");
        }
        
        private T FindUIComponent<T>(string name) where T : Component
        {
            // Try to find in children first
            T component = GetComponentInChildren<T>();
            if (component != null && component.name.Contains(name))
                return component;
            
            // Try to find by name in UI hierarchy (more efficient than GameObject.Find)
            Transform found = transform.root.Find(name);
            if (found == null)
            {
                // Search in all canvases as fallback
                Canvas[] canvases = FindObjectsOfType<Canvas>();
                foreach (var canvas in canvases)
                {
                    found = canvas.transform.Find(name);
                    if (found != null) break;
                }
            }
            if (found != null)
                return found.GetComponent<T>();
            
            return null;
        }
        
        private TextMeshProUGUI CreateUIText(string name, string text, Vector2 position)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(transform, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 50);
            
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 24;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.Center;
            
            return textComp;
        }
        
        private Slider CreateUISlider(string name, Vector2 position)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(transform, false);
            
            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 20);
            
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            
            // Create background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = Color.gray;
            
            // Create fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(sliderObj.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = Color.blue;
            
            slider.fillRect = fillRect;
            
            return slider;
        }
        
        private void CreateGameUICanvas()
        {
            // Create main UI canvas for the game
            GameObject canvasObj = new GameObject("GameUI_Canvas");
            canvasObj.transform.SetParent(transform.parent);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            
            // Configure for VR world space
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.transform.position = new Vector3(0, 2, 2);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920, 1080);
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Move this GameUI to be a child of the canvas
            transform.SetParent(canvasObj.transform, false);
            
            Debug.Log("Created GameUI Canvas for VR");
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to game manager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged.AddListener(UpdateScore);
                GameManager.Instance.OnTimeChanged.AddListener(UpdateTime);
                GameManager.Instance.OnGameStart.AddListener(OnGameStart);
                GameManager.Instance.OnGameEnd.AddListener(OnGameEnd);
            }
            
            // Subscribe to rhythm system events
            if (RhythmTargetSystem.Instance != null)
            {
                RhythmTargetSystem.Instance.OnCircleHit.AddListener(OnCircleHit);
                RhythmTargetSystem.Instance.OnBlockSuccess.AddListener(OnBlockSuccess);
            }
            
            // Subscribe to audio events
            if (AdvancedAudioManager.Instance != null)
            {
                AdvancedAudioManager.Instance.OnBeatDetected.AddListener(OnBeatDetected);
            }
        }
        
        public void UpdateScore(int score)
        {
            currentScore = score;
            if (scoreText)
            {
                scoreText.text = $"Score: {score:N0}";
                _ = AnimateScoreTextAsync();
            }
        }
        
        public void UpdateCombo(int combo, float multiplier)
        {
            currentCombo = combo;
            currentMultiplier = multiplier;
            
            if (comboText)
            {
                if (combo > 0)
                {
                    comboText.text = $"Combo: {combo}x";
                    comboText.gameObject.SetActive(true);
                    _ = AnimateComboTextAsync();
                }
                else
                {
                    comboText.gameObject.SetActive(false);
                    
                    // Show combo break effect
                    if (comboBreakEffect)
                    {
                        GameObject effect = Instantiate(comboBreakEffect, transform);
                        Destroy(effect, 2f);
                    }
                }
            }
            
            if (multiplierText)
            {
                multiplierText.text = $"x{multiplier:F1}";
                
                // Change color based on multiplier
                if (multiplier >= 3f)
                    multiplierText.color = Color.red;
                else if (multiplier >= 2f)
                    multiplierText.color = Color.yellow;
                else
                    multiplierText.color = Color.white;
            }
            
            if (comboMeter)
            {
                comboMeter.value = Mathf.Min(combo / 50f, 1f); // Max at 50 combo
            }
        }
        
        public void UpdateTime(float timeRemaining)
        {
            if (timeText)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timeText.text = $"{minutes:00}:{seconds:00}";
                
                // Flash red when time is low
                if (timeRemaining < 30f)
                {
                    timeText.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 2f, 1f));
                }
                else
                {
                    timeText.color = Color.white;
                }
            }
        }
        
        public void UpdateAccuracy(float accuracy)
        {
            currentAccuracy = accuracy;
            if (accuracyText)
            {
                accuracyText.text = $"Accuracy: {accuracy:P1}";
                
                // Color based on accuracy
                if (accuracy >= 0.9f) accuracyText.color = Color.green;
                else if (accuracy >= 0.7f) accuracyText.color = Color.yellow;
                else accuracyText.color = Color.red;
            }
        }
        
        public void UpdateBPM(float bpm)
        {
            if (bpmText)
            {
                bpmText.text = $"BPM: {bpm:F0}";
            }
        }
        
        public void UpdateHealth(float health)
        {
            if (healthBar)
            {
                healthBar.value = health;
                
                // Change color based on health
                Image fillImage = healthBar.fillRect.GetComponent<Image>();
                if (fillImage)
                {
                    if (health > 0.6f) fillImage.color = Color.green;
                    else if (health > 0.3f) fillImage.color = Color.yellow;
                    else fillImage.color = Color.red;
                }
            }
        }
        
        private void OnCircleHit(RhythmTargetSystem.CircleHitData hitData)
        {
            if (hitData.isPerfectTiming)
            {
                ShowPerfectHitEffect();
            }
            
            // Flash screen on hit
            FlashScreen(Color.white, 0.1f);
        }
        
        private void OnBlockSuccess(RhythmTargetSystem.BlockData blockData)
        {
            // Special effect for successful blocks
            FlashScreen(Color.blue, 0.2f);
            ShowPerfectHitEffect();
        }
        
        private void OnBeatDetected(AdvancedAudioManager.BeatData beatData)
        {
            if (beatData.isStrongBeat)
            {
                // Subtle pulse on strong beats
                _ = PulseUIAsync();
            }
        }
        
        private void ShowPerfectHitEffect()
        {
            if (perfectHitEffect)
            {
                GameObject effect = Instantiate(perfectHitEffect, transform);
                Destroy(effect, 2f);
            }
        }
        
        private void FlashScreen(Color color, float duration)
        {
            if (screenFlash)
            {
                // Cancel previous flash if running
                screenFlashCancellation?.Cancel();
                screenFlashCancellation = new CancellationTokenSource();
                
                _ = ScreenFlashEffectAsync(color, duration, screenFlashCancellation.Token);
            }
        }
        
        private async Task ScreenFlashEffectAsync(Color color, float duration, CancellationToken cancellationToken)
        {
            try
            {
                screenFlash.color = color;
                float elapsedTime = 0f;
                
                while (elapsedTime < duration && !cancellationToken.IsCancellationRequested)
                {
                    elapsedTime += Time.deltaTime;
                    float alpha = flashCurve.Evaluate(elapsedTime / duration);
                    
                    Color currentColor = color;
                    currentColor.a = alpha;
                    screenFlash.color = currentColor;
                    
                    await Task.Yield();
                }
                
                // Ensure fully transparent at end
                if (!cancellationToken.IsCancellationRequested)
                {
                    Color finalColor = color;
                    finalColor.a = 0f;
                    screenFlash.color = finalColor;
                }
            }
            catch (System.OperationCanceledException)
            {
                // Flash was cancelled, this is expected
            }
        }
        
        private async Task AnimateScoreTextAsync()
        {
            if (!scoreText) return;
            
            try
            {
                Vector3 targetScale = originalScoreScale * 1.2f;
                float duration = 0.2f;
                float elapsedTime = 0f;
                
                // Scale up
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / duration;
                    scoreText.transform.localScale = Vector3.Lerp(originalScoreScale, targetScale, progress);
                    await Task.Yield();
                }
                
                // Scale back down
                elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / duration;
                    scoreText.transform.localScale = Vector3.Lerp(targetScale, originalScoreScale, progress);
                    await Task.Yield();
                }
                
                scoreText.transform.localScale = originalScoreScale;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in score text animation: {ex.Message}");
            }
        }
        
        private async Task AnimateComboTextAsync()
        {
            if (!comboText) return;
            
            try
            {
                Vector3 targetScale = originalComboScale * 1.3f;
                float duration = 0.15f;
                float elapsedTime = 0f;
                
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / duration;
                    comboText.transform.localScale = Vector3.Lerp(originalComboScale, targetScale, progress);
                    await Task.Yield();
                }
                
                elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / duration;
                    comboText.transform.localScale = Vector3.Lerp(targetScale, originalComboScale, progress);
                    await Task.Yield();
                }
                
                comboText.transform.localScale = originalComboScale;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in combo text animation: {ex.Message}");
            }
        }
        
        private async Task PulseUIAsync()
        {
            try
            {
                float pulseDuration = 0.1f;
                float pulseScale = 1.05f;
                
                // Pulse game panel
                if (gamePanel)
                {
                    Vector3 originalScale = gamePanel.transform.localScale;
                    Vector3 pulseScaleVector = originalScale * pulseScale;
                    
                    float elapsedTime = 0f;
                    while (elapsedTime < pulseDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float progress = elapsedTime / pulseDuration;
                        gamePanel.transform.localScale = Vector3.Lerp(originalScale, pulseScaleVector, Mathf.Sin(progress * Mathf.PI));
                        await Task.Yield();
                    }
                    
                    gamePanel.transform.localScale = originalScale;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in UI pulse animation: {ex.Message}");
            }
        }
        
        // Game State Management
        public void ShowMenu()
        {
            SetPanelActive(menuPanel, true);
            SetPanelActive(gamePanel, false);
            SetPanelActive(pausePanel, false);
            SetPanelActive(gameOverPanel, false);
        }
        
        public void ShowGame()
        {
            SetPanelActive(menuPanel, false);
            SetPanelActive(gamePanel, true);
            SetPanelActive(pausePanel, false);
            SetPanelActive(gameOverPanel, false);
        }
        
        public void ShowPause()
        {
            SetPanelActive(pausePanel, true);
        }
        
        public void ShowGameOver()
        {
            SetPanelActive(gamePanel, false);
            SetPanelActive(gameOverPanel, true);
            
            // Update final scores
            if (finalScoreText) finalScoreText.text = $"Final Score: {currentScore:N0}";
            if (finalAccuracyText) finalAccuracyText.text = $"Accuracy: {currentAccuracy:P1}";
            
            // Check for high score
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (currentScore > highScore)
            {
                PlayerPrefs.SetInt("HighScore", currentScore);
                if (highScoreText) highScoreText.text = "NEW HIGH SCORE!";
            }
            else
            {
                if (highScoreText) highScoreText.text = $"High Score: {highScore:N0}";
            }
        }
        
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel) panel.SetActive(active);
        }
        
        // Event Handlers
        private void OnGameStart()
        {
            ShowGame();
        }
        
        private void OnGameEnd()
        {
            ShowGameOver();
        }
        
        public void StartGame()
        {
            GameManager.Instance?.StartGame();
        }
        
        public void PauseGame()
        {
            GameManager.Instance?.PauseGame();
            ShowPause();
        }
        
        public void ResumeGame()
        {
            GameManager.Instance?.ResumeGame();
            SetPanelActive(pausePanel, false);
        }
        
        public void RestartGame()
        {
            GameManager.Instance?.StartGame();
        }
        
        public void ReturnToMenu()
        {
            GameManager.Instance?.ReturnToMenu();
            ShowMenu();
        }
        
        // Settings Event Handlers
        private void OnVolumeChanged(float volume)
        {
            AdvancedAudioManager.Instance?.SetMasterVolume(volume);
        }
        
        private void OnDifficultyChanged(float difficulty)
        {
            // Implement difficulty scaling
            Debug.Log($"Difficulty changed to: {difficulty}");
        }
        
        private void OnHandTrackingToggle(bool enabled)
        {
            // Toggle hand tracking
            if (VRBoxingGame.HandTracking.HandTrackingManager.Instance != null)
            {
                VRBoxingGame.HandTracking.HandTrackingManager.Instance.enableHandTracking = enabled;
            }
        }
        
        private void OnBackgroundChanged(int backgroundIndex)
        {
            // Change background theme
            if (VRBoxingGame.Environment.DynamicBackgroundSystem.Instance != null)
            {
                VRBoxingGame.Environment.DynamicBackgroundSystem.Instance.LoadTheme(backgroundIndex);
            }
        }
        
        private void OnDestroy()
        {
            // Cancel any running animations
            screenFlashCancellation?.Cancel();
            screenFlashCancellation?.Dispose();
            
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged.RemoveListener(UpdateScore);
                GameManager.Instance.OnTimeChanged.RemoveListener(UpdateTime);
                GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
                GameManager.Instance.OnGameEnd.RemoveListener(OnGameEnd);
            }
        }
    }
} 