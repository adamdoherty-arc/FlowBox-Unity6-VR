using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Core;
using VRBoxingGame.Boxing;
using VRBoxingGame.Performance;
using VRBoxingGame.Audio;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Advanced Game State Manager with AI Coaching, Predictive Analytics, and Player Modeling
    /// Unity 6 optimized with ML inference and real-time performance adaptation
    /// </summary>
    public class AdvancedGameStateManager : MonoBehaviour
    {
        [Header("AI Coaching System")]
        public bool enableAICoaching = true;
        public bool enablePredictiveAnalytics = true;
        public bool enableRealTimeAnalysis = true;
        public float coachingUpdateFrequency = 5f;
        
        [Header("Player Performance Modeling")]
        public int performanceHistorySize = 1000;
        public float skillAnalysisWindow = 30f;
        public bool enableProgressTracking = true;
        public bool enablePersonalizedTraining = true;
        
        [Header("Adaptive Difficulty")]
        public bool enableAdvancedDifficulty = true;
        public float difficultyUpdateInterval = 10f;
        public AnimationCurve difficultyResponseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float maxDifficultyIncrease = 2f;
        
        [Header("Session Analytics")]
        public bool enableSessionRecording = true;
        public bool enableHeatmapGeneration = true;
        public bool enableFormAnalytics = true;
        
        [Header("Events")]
        public UnityEvent<PlayerPerformanceData> OnPerformanceAnalysis;
        public UnityEvent<CoachingInstruction> OnCoachingInstruction;
        public UnityEvent<SkillProgression> OnSkillProgression;
        public UnityEvent<SessionSummary> OnSessionComplete;
        
        // Player Performance Data Structures
        [System.Serializable]
        public struct PlayerPerformanceData
        {
            public float accuracy;
            public float averageReactionTime;
            public float punchPower;
            public float formConsistency;
            public float endurance;
            public float overallSkillLevel;
            public BoxingFormTracker.BoxingStance preferredStance;
            public Vector3[] heatmapData;
            public float sessionDuration;
            public int totalPunches;
            public float powerGeneration;
        }
        
        [System.Serializable]
        public struct CoachingInstruction
        {
            public CoachingType instructionType;
            public string instruction;
            public float urgency;
            public Vector3 visualCuePosition;
            public Color instructionColor;
            public float duration;
            public bool useAudioCue;
        }
        
        [System.Serializable]
        public struct SkillProgression
        {
            public float currentLevel;
            public float progressToNext;
            public SkillArea primaryImprovement;
            public SkillArea secondaryImprovement;
            public float timeToNextLevel;
            public string progressDescription;
        }
        
        [System.Serializable]
        public struct SessionSummary
        {
            public float sessionDuration;
            public int totalTargetsHit;
            public int totalTargetsMissed;
            public float averageAccuracy;
            public float maxCombo;
            public float totalCaloriesBurned;
            public SkillArea[] improvedAreas;
            public SkillArea[] areasForImprovement;
            public float overallPerformanceRating;
        }
        
        public enum CoachingType
        {
            StanceCorrection,
            FormImprovement,
            PowerGeneration,
            Accuracy,
            EnduranceGuidance,
            MotivationalCue,
            TechniqueTip,
            BreathingGuidance
        }
        
        public enum SkillArea
        {
            Accuracy,
            Power,
            Speed,
            Endurance,
            Form,
            Footwork,
            Defense,
            Combinations
        }
        
        // AI Analysis Components
        private PlayerPerformanceModel performanceModel;
        private AdaptiveDifficultyEngine difficultyEngine;
        private RealTimeCoach realTimeCoach;
        private SessionAnalytics sessionAnalytics;
        
        // Performance Tracking
        private NativeArray<float> performanceHistory;
        private NativeArray<float3> punchPositionHistory;
        private NativeArray<float> reactionTimeHistory;
        private JobHandle analyticsJobHandle;
        
        // Current Session Data
        private PlayerPerformanceData currentSessionData;
        private List<CoachingInstruction> pendingInstructions = new List<CoachingInstruction>();
        private float sessionStartTime;
        private int currentCombo = 0;
        private int maxCombo = 0;
        
        // Singleton
        public static AdvancedGameStateManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAdvancedSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeAdvancedSystems()
        {
            Debug.Log("🧠 Initializing Advanced Game State Manager...");
            
            // Initialize Native Collections
            performanceHistory = new NativeArray<float>(performanceHistorySize, Allocator.Persistent);
            punchPositionHistory = new NativeArray<float3>(performanceHistorySize, Allocator.Persistent);
            reactionTimeHistory = new NativeArray<float>(performanceHistorySize, Allocator.Persistent);
            
            // Initialize AI Components
            performanceModel = new PlayerPerformanceModel();
            difficultyEngine = new AdaptiveDifficultyEngine();
            realTimeCoach = new RealTimeCoach();
            sessionAnalytics = new SessionAnalytics();
            
            // Initialize session data
            InitializeSession();
            
            // Subscribe to game events
            SubscribeToGameEvents();
            
            Debug.Log("✅ Advanced Game State Manager initialized!");
        }
        
        private void InitializeSession()
        {
            sessionStartTime = Time.time;
            currentSessionData = new PlayerPerformanceData
            {
                accuracy = 0f,
                averageReactionTime = 0f,
                punchPower = 0f,
                formConsistency = 0f,
                endurance = 1f,
                overallSkillLevel = 1f,
                preferredStance = BoxingFormTracker.BoxingStance.Orthodox,
                sessionDuration = 0f,
                totalPunches = 0,
                powerGeneration = 0f
            };
            
            currentCombo = 0;
            maxCombo = 0;
            pendingInstructions.Clear();
        }
        
        private void SubscribeToGameEvents()
        {
            // Subscribe to boxing events
            if (BoxingFormTracker.Instance != null)
            {
                BoxingFormTracker.Instance.OnFormAnalyzed.AddListener(OnFormAnalyzed);
                BoxingFormTracker.Instance.OnHipPowerGenerated.AddListener(OnPowerGenerated);
            }
            
            // Subscribe to target system events
            if (RhythmTargetSystem.Instance != null)
            {
                RhythmTargetSystem.Instance.OnCircleHit.AddListener(OnTargetHit);
            }
            
            // Subscribe to audio events for rhythm analysis
            if (AdvancedAudioManager.Instance != null)
            {
                AdvancedAudioManager.Instance.OnBeatDetected.AddListener(OnBeatDetected);
            }
        }
        
        private void Update()
        {
            if (enableRealTimeAnalysis)
            {
                UpdateRealTimeAnalysis();
            }
            
            if (enableAICoaching)
            {
                UpdateAICoaching();
            }
            
            UpdateSessionData();
            ProcessPendingInstructions();
        }
        
        private void UpdateRealTimeAnalysis()
        {
            // Complete previous analytics job
            analyticsJobHandle.Complete();
            
            // Schedule new analytics job
            var analyticsJob = new PlayerAnalyticsJob
            {
                performanceHistory = performanceHistory,
                punchPositions = punchPositionHistory,
                reactionTimes = reactionTimeHistory,
                currentTime = Time.time,
                sessionStartTime = sessionStartTime
            };
            
            analyticsJobHandle = analyticsJob.Schedule();
        }
        
        private void UpdateAICoaching()
        {
            if (Time.time % coachingUpdateFrequency < Time.deltaTime)
            {
                _ = GenerateCoachingInstructionAsync();
            }
        }
        
        private async Task GenerateCoachingInstructionAsync()
        {
            // Analyze current performance
            var performanceAnalysis = await AnalyzeCurrentPerformanceAsync();
            
            // Generate coaching instruction based on analysis
            var instruction = await realTimeCoach.GenerateInstructionAsync(performanceAnalysis);
            
            if (instruction.HasValue)
            {
                pendingInstructions.Add(instruction.Value);
                OnCoachingInstruction?.Invoke(instruction.Value);
            }
        }
        
        private async Task<PlayerPerformanceData> AnalyzeCurrentPerformanceAsync()
        {
            await Task.Yield(); // Simulate ML inference
            
            // Calculate current performance metrics
            float accuracy = CalculateAccuracy();
            float reactionTime = CalculateAverageReactionTime();
            float formQuality = GetCurrentFormQuality();
            float powerGeneration = GetCurrentPowerGeneration();
            
            return new PlayerPerformanceData
            {
                accuracy = accuracy,
                averageReactionTime = reactionTime,
                formConsistency = formQuality,
                powerGeneration = powerGeneration,
                sessionDuration = Time.time - sessionStartTime,
                totalPunches = currentSessionData.totalPunches,
                overallSkillLevel = CalculateOverallSkillLevel()
            };
        }
        
        private void UpdateSessionData()
        {
            currentSessionData.sessionDuration = Time.time - sessionStartTime;
            currentSessionData.endurance = CalculateEndurance();
            currentSessionData.overallSkillLevel = CalculateOverallSkillLevel();
        }
        
        private void ProcessPendingInstructions()
        {
            for (int i = pendingInstructions.Count - 1; i >= 0; i--)
            {
                var instruction = pendingInstructions[i];
                instruction.duration -= Time.deltaTime;
                
                if (instruction.duration <= 0)
                {
                    pendingInstructions.RemoveAt(i);
                }
                else
                {
                    pendingInstructions[i] = instruction;
                }
            }
        }
        
        private void OnFormAnalyzed(BoxingFormTracker.BoxingFormData formData)
        {
            // Update form consistency tracking
            float formQuality = (float)formData.overallForm / 4f; // Convert to 0-1 scale
            currentSessionData.formConsistency = Mathf.Lerp(currentSessionData.formConsistency, formQuality, 0.1f);
        }
        
        private void OnPowerGenerated(float power)
        {
            currentSessionData.powerGeneration += power;
            currentSessionData.punchPower = Mathf.Max(currentSessionData.punchPower, power);
        }
        
        private void OnTargetHit(RhythmTargetSystem.CircleHitData hitData)
        {
            currentSessionData.totalPunches++;
            currentCombo++;
            maxCombo = Mathf.Max(maxCombo, currentCombo);
            
            // Update accuracy
            float hitAccuracy = hitData.accuracy;
            currentSessionData.accuracy = Mathf.Lerp(currentSessionData.accuracy, hitAccuracy, 0.1f);
            
            // Record hit position for heatmap
            int historyIndex = currentSessionData.totalPunches % performanceHistorySize;
            punchPositionHistory[historyIndex] = hitData.hitPosition;
        }
        
        private void OnBeatDetected(AdvancedAudioManager.BeatData beatData)
        {
            // Analyze rhythm timing for coaching
            if (enableAICoaching)
            {
                realTimeCoach.AnalyzeRhythmTiming(beatData);
            }
        }
        
        private float CalculateAccuracy()
        {
            return currentSessionData.accuracy;
        }
        
        private float CalculateAverageReactionTime()
        {
            float total = 0f;
            int count = 0;
            
            for (int i = 0; i < reactionTimeHistory.Length; i++)
            {
                if (reactionTimeHistory[i] > 0)
                {
                    total += reactionTimeHistory[i];
                    count++;
                }
            }
            
            return count > 0 ? total / count : 0f;
        }
        
        private float GetCurrentFormQuality()
        {
            return BoxingFormTracker.Instance != null ? 
                BoxingFormTracker.Instance.CurrentFormData.stanceQuality : 0.5f;
        }
        
        private float GetCurrentPowerGeneration()
        {
            return BoxingFormTracker.Instance != null ?
                BoxingFormTracker.Instance.CurrentFormData.powerMultiplier : 1f;
        }
        
        private float CalculateEndurance()
        {
            float sessionTime = Time.time - sessionStartTime;
            float enduranceFactor = Mathf.Exp(-sessionTime / 300f); // Exponential decay over 5 minutes
            return Mathf.Clamp01(enduranceFactor);
        }
        
        private float CalculateOverallSkillLevel()
        {
            float accuracyWeight = 0.3f;
            float powerWeight = 0.25f;
            float formWeight = 0.25f;
            float enduranceWeight = 0.2f;
            
            return (currentSessionData.accuracy * accuracyWeight) +
                   (currentSessionData.punchPower * powerWeight) +
                   (currentSessionData.formConsistency * formWeight) +
                   (currentSessionData.endurance * enduranceWeight);
        }
        
        public void EndSession()
        {
            var sessionSummary = GenerateSessionSummary();
            OnSessionComplete?.Invoke(sessionSummary);
            
            if (enableSessionRecording)
            {
                SaveSessionData(sessionSummary);
            }
        }
        
        private SessionSummary GenerateSessionSummary()
        {
            return new SessionSummary
            {
                sessionDuration = currentSessionData.sessionDuration,
                totalTargetsHit = currentSessionData.totalPunches,
                averageAccuracy = currentSessionData.accuracy,
                maxCombo = maxCombo,
                overallPerformanceRating = currentSessionData.overallSkillLevel,
                improvedAreas = IdentifyImprovedAreas(),
                areasForImprovement = IdentifyAreasForImprovement()
            };
        }
        
        private SkillArea[] IdentifyImprovedAreas()
        {
            List<SkillArea> improvedAreas = new List<SkillArea>();
            
            if (currentSessionData.accuracy > 0.8f) improvedAreas.Add(SkillArea.Accuracy);
            if (currentSessionData.punchPower > 1.5f) improvedAreas.Add(SkillArea.Power);
            if (currentSessionData.formConsistency > 0.8f) improvedAreas.Add(SkillArea.Form);
            
            return improvedAreas.ToArray();
        }
        
        private SkillArea[] IdentifyAreasForImprovement()
        {
            List<SkillArea> improvementAreas = new List<SkillArea>();
            
            if (currentSessionData.accuracy < 0.6f) improvementAreas.Add(SkillArea.Accuracy);
            if (currentSessionData.punchPower < 1.2f) improvementAreas.Add(SkillArea.Power);
            if (currentSessionData.formConsistency < 0.6f) improvementAreas.Add(SkillArea.Form);
            if (currentSessionData.endurance < 0.7f) improvementAreas.Add(SkillArea.Endurance);
            
            return improvementAreas.ToArray();
        }
        
        private void SaveSessionData(SessionSummary summary)
        {
            // In a real implementation, this would save to persistent storage
            Debug.Log($"Session saved: {summary.overallPerformanceRating:F2} rating, {summary.totalTargetsHit} targets hit");
        }
        
        private void OnDestroy()
        {
            // Complete any running jobs
            if (analyticsJobHandle.IsCreated) analyticsJobHandle.Complete();
            
            // Dispose native arrays
            if (performanceHistory.IsCreated) performanceHistory.Dispose();
            if (punchPositionHistory.IsCreated) punchPositionHistory.Dispose();
            if (reactionTimeHistory.IsCreated) reactionTimeHistory.Dispose();
        }
    }
    
    // Supporting Classes
    public class PlayerPerformanceModel
    {
        public async Task<float> PredictPerformance(AdvancedGameStateManager.PlayerPerformanceData data)
        {
            await Task.Delay(10); // Simulate ML inference
            return data.overallSkillLevel * 0.9f + UnityEngine.Random.Range(-0.1f, 0.1f);
        }
    }
    
    public class AdaptiveDifficultyEngine
    {
        public float CalculateOptimalDifficulty(AdvancedGameStateManager.PlayerPerformanceData data)
        {
            // Target 70-80% accuracy for optimal challenge
            float targetAccuracy = 0.75f;
            float difficultyAdjustment = (data.accuracy - targetAccuracy) * 2f;
            return Mathf.Clamp(1f + difficultyAdjustment, 0.5f, 2f);
        }
    }
    
    public class RealTimeCoach
    {
        private float lastInstructionTime = 0f;
        private const float instructionCooldown = 5f;
        
        public async Task<AdvancedGameStateManager.CoachingInstruction?> GenerateInstructionAsync(
            AdvancedGameStateManager.PlayerPerformanceData performance)
        {
            if (Time.time - lastInstructionTime < instructionCooldown)
                return null;
            
            await Task.Yield();
            
            var instruction = new AdvancedGameStateManager.CoachingInstruction();
            
            // Determine coaching need
            if (performance.formConsistency < 0.6f)
            {
                instruction.instructionType = AdvancedGameStateManager.CoachingType.FormImprovement;
                instruction.instruction = "Focus on your stance - keep feet shoulder-width apart!";
                instruction.urgency = 0.8f;
            }
            else if (performance.accuracy < 0.6f)
            {
                instruction.instructionType = AdvancedGameStateManager.CoachingType.Accuracy;
                instruction.instruction = "Take your time - focus on precision over speed!";
                instruction.urgency = 0.7f;
            }
            else if (performance.punchPower < 1.2f)
            {
                instruction.instructionType = AdvancedGameStateManager.CoachingType.PowerGeneration;
                instruction.instruction = "Rotate your hips for more power!";
                instruction.urgency = 0.6f;
            }
            else
            {
                instruction.instructionType = AdvancedGameStateManager.CoachingType.MotivationalCue;
                instruction.instruction = "Great work! Keep up the intensity!";
                instruction.urgency = 0.3f;
            }
            
            instruction.duration = 3f;
            instruction.instructionColor = Color.yellow;
            instruction.useAudioCue = true;
            
            lastInstructionTime = Time.time;
            return instruction;
        }
        
        public void AnalyzeRhythmTiming(AdvancedAudioManager.BeatData beatData)
        {
            // Analyze player timing vs. music beats for rhythm coaching
        }
    }
    
    public class SessionAnalytics
    {
        public void GenerateHeatmap(NativeArray<float3> punchPositions)
        {
            // Generate 3D heatmap of punch locations
        }
        
        public void AnalyzeProgressionTrends(NativeArray<float> performanceHistory)
        {
            // Analyze performance trends over time
        }
    }
    
    // Unity 6 Job System for Player Analytics
    [BurstCompile]
    public struct PlayerAnalyticsJob : IJob
    {
        [ReadOnly] public NativeArray<float> performanceHistory;
        [ReadOnly] public NativeArray<float3> punchPositions;
        [ReadOnly] public NativeArray<float> reactionTimes;
        [ReadOnly] public float currentTime;
        [ReadOnly] public float sessionStartTime;
        
        public void Execute()
        {
            // Perform real-time analytics calculations
            float sessionDuration = currentTime - sessionStartTime;
            
            // Calculate performance trends
            float recentPerformanceAvg = 0f;
            int recentSamples = math.min(60, performanceHistory.Length); // Last 60 samples
            
            for (int i = performanceHistory.Length - recentSamples; i < performanceHistory.Length; i++)
            {
                recentPerformanceAvg += performanceHistory[i];
            }
            recentPerformanceAvg /= recentSamples;
            
            // Calculate spatial distribution of punches
            float3 punchCentroid = float3.zero;
            int validPunches = 0;
            
            for (int i = 0; i < punchPositions.Length; i++)
            {
                if (math.length(punchPositions[i]) > 0.01f) // Valid punch position
                {
                    punchCentroid += punchPositions[i];
                    validPunches++;
                }
            }
            
            if (validPunches > 0)
            {
                punchCentroid /= validPunches;
            }
            
            // Analyze reaction time patterns
            float avgReactionTime = 0f;
            int validReactions = 0;
            
            for (int i = 0; i < reactionTimes.Length; i++)
            {
                if (reactionTimes[i] > 0f)
                {
                    avgReactionTime += reactionTimes[i];
                    validReactions++;
                }
            }
            
            if (validReactions > 0)
            {
                avgReactionTime /= validReactions;
            }
        }
    }
} 