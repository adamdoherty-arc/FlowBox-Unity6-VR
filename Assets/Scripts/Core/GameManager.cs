using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Enhanced Game Manager for Unity 6 with performance optimizations and new features
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public float gameSessionDuration = 180f;
        public int targetScore = 1000;
        public bool enableAdaptiveDifficulty = true;
        public bool enablePerformanceMonitoring = true;
        
        [Header("Unity 6 Features")]
        public bool enableGPUResidentDrawer = true;
        public bool enableRenderGraph = true;
        public bool enableJobSystem = true;
        
        [Header("Events")]
        public UnityEvent OnGameStart;
        public UnityEvent OnGameEnd;
        public UnityEvent<int> OnScoreChanged;
        public UnityEvent<float> OnTimeChanged;
        public UnityEvent<float> OnPerformanceUpdate;
        public UnityEvent<GameState> OnGameStateChanged;
        
        // Game state
        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            Finished,
            Loading
        }
        
        [SerializeField] private GameState currentState = GameState.Menu;
        [SerializeField] private int currentScore = 0;
        [SerializeField] private float timeRemaining;
        [SerializeField] private int currentCombo = 0;
        [SerializeField] private float currentMultiplier = 1.0f;
        
        // Performance tracking
        private float frameTime;
        private float averageFrameTime;
        private int frameCount;
        private Queue<float> frameTimeHistory = new Queue<float>();
        private const int maxFrameHistory = 60;
        
        // Adaptive difficulty
        private float playerSkillLevel = 1.0f;
        private float hitAccuracy = 1.0f;
        private int totalTargets = 0;
        private int hitTargets = 0;
        
        // Singleton pattern
        public static GameManager Instance { get; private set; }
        
        // Properties
        public GameState CurrentState => currentState;
        public int CurrentScore => currentScore;
        public float TimeRemaining => timeRemaining;
        public bool IsGameActive => currentState == GameState.Playing;
        public int CurrentCombo => currentCombo;
        public float CurrentMultiplier => currentMultiplier;
        public float PlayerSkillLevel => playerSkillLevel;
        public float HitAccuracy => hitAccuracy;
        public float AverageFrameTime => averageFrameTime;
        
        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeUnity6Features();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            timeRemaining = gameSessionDuration;
        }
        
        private void InitializeUnity6Features()
        {
            // Enable Unity 6 performance features
            if (enableGPUResidentDrawer)
            {
                // GPU Resident Drawer for better rendering performance
                QualitySettings.enableLODCrossFade = true;
            }
            
            if (enableRenderGraph)
            {
                // Render Graph optimizations
                QualitySettings.streamingMipmapsActive = true;
                QualitySettings.streamingMipmapsMemoryBudget = 512;
            }
            
            // Set optimal VR settings for Unity 6
            Application.targetFrameRate = 90; // Quest 3 target
            QualitySettings.vSyncCount = 0; // VR handles its own sync
            
            Debug.Log("Unity 6 features initialized for VR Boxing Game");
        }
        
        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                UpdateGameTimer();
                UpdateAdaptiveDifficulty();
            }
            
            if (enablePerformanceMonitoring)
            {
                UpdatePerformanceMetrics();
            }
        }
        
        private void UpdateGameTimer()
        {
            timeRemaining -= Time.deltaTime;
            OnTimeChanged?.Invoke(timeRemaining);
            
            if (timeRemaining <= 0)
            {
                EndGame();
            }
        }
        
        private void UpdateAdaptiveDifficulty()
        {
            if (!enableAdaptiveDifficulty) return;
            
            // Calculate hit accuracy
            if (totalTargets > 0)
            {
                hitAccuracy = (float)hitTargets / totalTargets;
            }
            
            // Adjust skill level based on performance
            float targetAccuracy = 0.75f; // Target 75% accuracy
            if (hitAccuracy > targetAccuracy + 0.1f)
            {
                playerSkillLevel = Mathf.Min(playerSkillLevel + Time.deltaTime * 0.1f, 3.0f);
            }
            else if (hitAccuracy < targetAccuracy - 0.1f)
            {
                playerSkillLevel = Mathf.Max(playerSkillLevel - Time.deltaTime * 0.05f, 0.5f);
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            frameTime = Time.unscaledDeltaTime;
            frameTimeHistory.Enqueue(frameTime);
            
            if (frameTimeHistory.Count > maxFrameHistory)
            {
                frameTimeHistory.Dequeue();
            }
            
            // Calculate average frame time
            float total = 0f;
            foreach (float time in frameTimeHistory)
            {
                total += time;
            }
            averageFrameTime = total / frameTimeHistory.Count;
            
            frameCount++;
            if (frameCount % 30 == 0) // Update every 30 frames
            {
                OnPerformanceUpdate?.Invoke(1f / averageFrameTime); // FPS
            }
        }
        
        public void StartGame()
        {
            currentState = GameState.Playing;
            currentScore = 0;
            currentCombo = 0;
            currentMultiplier = 1.0f;
            timeRemaining = gameSessionDuration;
            totalTargets = 0;
            hitTargets = 0;
            
            OnGameStart?.Invoke();
            OnGameStateChanged?.Invoke(currentState);
            OnScoreChanged?.Invoke(currentScore);
            OnTimeChanged?.Invoke(timeRemaining);
            
            Debug.Log("Enhanced Game Started with Unity 6 optimizations!");
        }
        
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                currentState = GameState.Paused;
                OnGameStateChanged?.Invoke(currentState);
                Time.timeScale = 0f;
                Debug.Log("Game Paused");
            }
        }
        
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                currentState = GameState.Playing;
                OnGameStateChanged?.Invoke(currentState);
                Time.timeScale = 1f;
                Debug.Log("Game Resumed");
            }
        }
        
        public void EndGame()
        {
            currentState = GameState.Finished;
            OnGameStateChanged?.Invoke(currentState);
            Time.timeScale = 1f;
            
            // Save performance metrics
            SaveGameStats();
            
            OnGameEnd?.Invoke();
            Debug.Log($"Game Ended! Final Score: {currentScore}, Accuracy: {hitAccuracy:P1}, Skill Level: {playerSkillLevel:F2}");
        }
        
        public void AddScore(int points, bool isPerfectHit = false)
        {
            if (currentState != GameState.Playing) return;
            
            // Enhanced scoring with combo system
            if (isPerfectHit)
            {
                currentCombo++;
                currentMultiplier = 1.0f + (currentCombo * 0.1f);
                currentMultiplier = Mathf.Min(currentMultiplier, 3.0f); // Cap at 3x
            }
            else
            {
                // Don't reset combo for non-perfect hits, only for misses
                // This allows building combos with good hits too
            }
            
            int finalPoints = Mathf.RoundToInt(points * currentMultiplier);
            currentScore += finalPoints;
            hitTargets++;
            
            OnScoreChanged?.Invoke(currentScore);
            
            // Notify UI of combo change
            var gameUI = FindObjectOfType<VRBoxingGame.UI.GameUI>();
            if (gameUI != null)
            {
                gameUI.UpdateCombo(currentCombo, currentMultiplier);
            }
            
            // Check for target score achievement
            if (currentScore >= targetScore)
            {
                Debug.Log("Target score reached!");
            }
        }
        
        public void RegisterTargetSpawned()
        {
            totalTargets++;
        }
        
        public void RegisterTargetMissed()
        {
            currentCombo = 0;
            currentMultiplier = 1.0f;
            
            // Notify UI of combo break
            var gameUI = FindObjectOfType<VRBoxingGame.UI.GameUI>();
            if (gameUI != null)
            {
                gameUI.UpdateCombo(currentCombo, currentMultiplier);
            }
        }
        
        private void SaveGameStats()
        {
            // Save enhanced statistics
            PlayerPrefs.SetInt("LastScore", currentScore);
            PlayerPrefs.SetFloat("LastAccuracy", hitAccuracy);
            PlayerPrefs.SetFloat("SkillLevel", playerSkillLevel);
            PlayerPrefs.SetFloat("AverageFrameTime", averageFrameTime);
            PlayerPrefs.SetString("LastPlayDate", System.DateTime.Now.ToString());
            PlayerPrefs.Save();
        }
        
        public void ReturnToMenu()
        {
            currentState = GameState.Menu;
            OnGameStateChanged?.Invoke(currentState);
            Time.timeScale = 1f;
            currentScore = 0;
            currentCombo = 0;
            currentMultiplier = 1.0f;
            timeRemaining = gameSessionDuration;
        }
        
        // Unity 6 Job System for performance-critical calculations
        [BurstCompile]
        public struct ScoreCalculationJob : IJob
        {
            public int baseScore;
            public float multiplier;
            public float accuracy;
            
            [WriteOnly]
            public NativeArray<int> result;
            
            public void Execute()
            {
                float finalScore = baseScore * multiplier * (1.0f + accuracy);
                result[0] = Mathf.RoundToInt(finalScore);
            }
        }
        
        // Performance optimization methods
        public void OptimizeForCurrentHardware()
        {
            // Detect Quest model and optimize accordingly
            string deviceModel = SystemInfo.deviceModel;
            
            if (deviceModel.Contains("Quest 3"))
            {
                // Quest 3 optimizations
                Application.targetFrameRate = 90;
                QualitySettings.antiAliasing = 4;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            }
            else if (deviceModel.Contains("Quest 2"))
            {
                // Quest 2 optimizations
                Application.targetFrameRate = 72;
                QualitySettings.antiAliasing = 2;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }
            
            Debug.Log($"Optimized for device: {deviceModel}");
        }
        
        // Advanced difficulty adjustment
        public float GetDynamicDifficulty()
        {
            float baseDifficulty = 1.0f;
            float skillAdjustment = (playerSkillLevel - 1.0f) * 0.5f;
            float timeAdjustment = (gameSessionDuration - timeRemaining) / gameSessionDuration * 0.3f;
            
            return Mathf.Clamp(baseDifficulty + skillAdjustment + timeAdjustment, 0.5f, 2.5f);
        }
    }
}

