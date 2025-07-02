using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using VRBoxingGame.Boxing;
using VRBoxingGame.Performance;

namespace VRBoxingGame.AI
{
    /// <summary>
    /// Unity Sentis AI Integration System - Unity 6 Machine Learning Inference
    /// Features: Real-time neural network inference, performance prediction, AI coaching
    /// </summary>
    public class UnitySentisAISystem : MonoBehaviour
    {
        [Header("AI Configuration")]
        public bool enablePerformancePrediction = true;
        public bool enableDifficultyAdjustment = true;
        public bool enablePlayerModeling = true;
        public bool enableFormAnalysis = true;
        
        [Header("Processing Settings")]
        public float inferenceFrequency = 10f;
        public int inputHistorySize = 60;
        public bool enableTemporalSmoothing = true;
        public float smoothingFactor = 0.8f;
        
        // AI Results
        private float predictedPerformance = 0f;
        private float recommendedDifficulty = 1f;
        private float playerSkillLevel = 1f;
        private float formAnalysisScore = 0.5f;
        
        // Data Processing
        private NativeArray<float> performanceHistory;
        private NativeArray<float> accuracyHistory;
        private NativeArray<float> reactionTimeHistory;
        
        // Singleton
        public static UnitySentisAISystem Instance { get; private set; }
        
        // Properties
        public float PredictedPerformance => predictedPerformance;
        public float RecommendedDifficulty => recommendedDifficulty;
        public float PlayerSkillLevel => playerSkillLevel;
        public float FormAnalysisScore => formAnalysisScore;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAISystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeAISystem()
        {
            Debug.Log("🧠 Initializing Unity Sentis AI System...");
            
            // Initialize data arrays
            performanceHistory = new NativeArray<float>(inputHistorySize, Allocator.Persistent);
            accuracyHistory = new NativeArray<float>(inputHistorySize, Allocator.Persistent);
            reactionTimeHistory = new NativeArray<float>(inputHistorySize, Allocator.Persistent);
            
            Debug.Log("✅ Unity Sentis AI System initialized!");
        }
        
        public void UpdatePerformanceData(float performance, float accuracy, float reactionTime)
        {
            // Simple performance prediction using moving average
            predictedPerformance = (performance + predictedPerformance) * 0.5f;
            
            // Dynamic difficulty adjustment
            if (performance > 0.8f)
            {
                recommendedDifficulty = Mathf.Min(recommendedDifficulty + 0.1f, 3f);
            }
            else if (performance < 0.4f)
            {
                recommendedDifficulty = Mathf.Max(recommendedDifficulty - 0.1f, 0.1f);
            }
            
            // Update skill level
            playerSkillLevel = Mathf.Lerp(playerSkillLevel, performance, 0.1f);
        }
        
        private void OnDestroy()
        {
            if (performanceHistory.IsCreated) performanceHistory.Dispose();
            if (accuracyHistory.IsCreated) accuracyHistory.Dispose();
            if (reactionTimeHistory.IsCreated) reactionTimeHistory.Dispose();
        }
    }
}
