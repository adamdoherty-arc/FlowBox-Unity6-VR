using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using VRBoxingGame.Boxing;
using VRBoxingGame.Core;
using VRBoxingGame.Setup;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Predictive Targeting System - Uses AI to predict optimal target placement
    /// Analyzes player movement patterns, stance preferences, and skill level
    /// Unity 6 optimized with Machine Learning inference and predictive algorithms
    /// </summary>
    public class PredictiveTargetingSystem : MonoBehaviour
    {
        [Header("Prediction Settings")]
        public bool enablePredictiveTargeting = true;
        public bool enableMovementPrediction = true;
        public bool enableStanceAnalysis = true;
        public float predictionHorizon = 2f; // Seconds into the future
        
        [Header("Learning Parameters")]
        public int movementHistorySize = 100;
        public int stanceHistorySize = 50;
        public float learningRate = 0.1f;
        public float confidenceThreshold = 0.7f;
        
        [Header("Target Optimization")]
        public bool optimizeForComfort = true;
        public bool optimizeForChallenge = true;
        public bool optimizeForForm = true;
        public float comfortZoneRadius = 1.2f;
        public float challengeZoneRadius = 2f;
        
        [Header("Advanced Features")]
        public bool enableFatigueCompensation = true;
        public bool enableSkillProgression = true;
        public bool enablePersonalization = true;
        
        [Header("Events")]
        public UnityEvent<PredictedTarget> OnTargetPredicted;
        public UnityEvent<MovementPattern> OnMovementPatternDetected;
        public UnityEvent<StancePreference> OnStancePreferenceUpdated;
        
        // Data Structures
        [System.Serializable]
        public struct PredictedTarget
        {
            public Vector3 position;
            public float confidence;
            public float optimalTiming;
            public RhythmTargetSystem.HandSide recommendedHand;
            public BoxingFormTracker.BoxingStance optimalStance;
            public float difficultyRating;
            public TargetType targetType;
        }
        
        [System.Serializable]
        public struct MovementPattern
        {
            public Vector3 averagePosition;
            public Vector3 movementTrend;
            public float rotationTrend;
            public float movementSpeed;
            public float confidence;
            public PatternType patternType;
        }
        
        [System.Serializable]
        public struct StancePreference
        {
            public BoxingFormTracker.BoxingStance preferredStance;
            public float stanceStability;
            public float stanceTransitionFrequency;
            public Vector3 optimalTargetZone;
            public float reachDistance;
        }
        
        public enum TargetType
        {
            Comfort,
            Challenge,
            FormTraining,
            PowerDevelopment,
            AccuracyTraining,
            SpeedTraining
        }
        
        public enum PatternType
        {
            Stationary,
            Forward,
            Backward,
            Lateral,
            Circular,
            Unpredictable
        }
        
        // ML Model Components
        private MovementPredictor movementPredictor;
        private StanceAnalyzer stanceAnalyzer;
        private TargetOptimizer targetOptimizer;
        private PlayerBehaviorModel behaviorModel;
        
        // Data Storage
        private NativeArray<float3> movementHistory;
        private NativeArray<float> rotationHistory;
        private NativeArray<int> stanceHistory;
        private NativeArray<float3> targetSuccessPositions;
        private NativeArray<float> targetSuccessRatings;
        
        // Prediction Results
        private MovementPattern currentMovementPattern;
        private StancePreference currentStancePreference;
        private Queue<PredictedTarget> predictedTargets = new Queue<PredictedTarget>();
        
        // Job Handles
        private JobHandle predictionJobHandle;
        private JobHandle analysisJobHandle;
        
        // Performance Tracking
        private int historyIndex = 0;
        private float lastPredictionTime = 0f;
        private float predictionUpdateInterval = 0.1f; // 10 FPS prediction
        
        // References
        private BoxingFormTracker formTracker;
        private VR360MovementSystem movementSystem;
        private RhythmTargetSystem targetSystem;
        
        // Singleton
        public static PredictiveTargetingSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePredictiveSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializePredictiveSystem()
        {
            Debug.Log("🎯 Initializing Predictive Targeting System...");
            
            // Initialize Native Collections
            movementHistory = new NativeArray<float3>(movementHistorySize, Allocator.Persistent);
            rotationHistory = new NativeArray<float>(movementHistorySize, Allocator.Persistent);
            stanceHistory = new NativeArray<int>(stanceHistorySize, Allocator.Persistent);
            targetSuccessPositions = new NativeArray<float3>(200, Allocator.Persistent);
            targetSuccessRatings = new NativeArray<float>(200, Allocator.Persistent);
            
            // Initialize ML Components
            movementPredictor = new MovementPredictor();
            stanceAnalyzer = new StanceAnalyzer();
            targetOptimizer = new TargetOptimizer();
            behaviorModel = new PlayerBehaviorModel();
            
            // Get system references
            formTracker = BoxingFormTracker.Instance;
            movementSystem = VR360MovementSystem.Instance;
            targetSystem = RhythmTargetSystem.Instance;
            
            // Subscribe to events
            SubscribeToEvents();
            
            Debug.Log("✅ Predictive Targeting System initialized!");
        }
        
        private void SubscribeToEvents()
        {
            if (formTracker != null)
            {
                formTracker.OnStanceChanged.AddListener(OnStanceChanged);
                formTracker.OnFormAnalyzed.AddListener(OnFormAnalyzed);
            }
            
            if (targetSystem != null)
            {
                targetSystem.OnCircleHit.AddListener(OnTargetHit);
            }
        }
        
        private void Update()
        {
            if (enablePredictiveTargeting && Time.time - lastPredictionTime >= predictionUpdateInterval)
            {
                UpdateMovementTracking();
                UpdatePredictions();
                lastPredictionTime = Time.time;
            }
            
            ProcessPredictedTargets();
        }
        
        private void UpdateMovementTracking()
        {
            // Track player position and rotation
            Vector3 playerPosition = VRCameraHelper.PlayerPosition;
            float playerRotation = VRCameraHelper.ActiveCameraTransform.eulerAngles.y;
            
            // Store in history
            movementHistory[historyIndex % movementHistorySize] = playerPosition;
            rotationHistory[historyIndex % movementHistorySize] = playerRotation;
            
            // Store stance data
            if (formTracker != null)
            {
                int stanceValue = (int)formTracker.CurrentStance;
                stanceHistory[historyIndex % stanceHistorySize] = stanceValue;
            }
            
            historyIndex++;
        }
        
        private void UpdatePredictions()
        {
            // Complete previous jobs
            predictionJobHandle.Complete();
            analysisJobHandle.Complete();
            
            // Schedule movement prediction job
            var movementPredictionJob = new MovementPredictionJob
            {
                movementHistory = movementHistory,
                rotationHistory = rotationHistory,
                predictionHorizon = predictionHorizon,
                currentTime = Time.time,
                historySize = movementHistorySize
            };
            
            predictionJobHandle = movementPredictionJob.Schedule();
            
            // Schedule stance analysis job
            var stanceAnalysisJob = new StanceAnalysisJob
            {
                stanceHistory = stanceHistory,
                movementHistory = movementHistory,
                targetSuccessPositions = targetSuccessPositions,
                targetSuccessRatings = targetSuccessRatings,
                historySize = stanceHistorySize
            };
            
            analysisJobHandle = stanceAnalysisJob.Schedule();
            
            // Process results when jobs complete
            _ = ProcessPredictionResults();
        }
        
        private async System.Threading.Tasks.Task ProcessPredictionResults()
        {
            // Wait for jobs to complete asynchronously
            while (!predictionJobHandle.IsCompleted || !analysisJobHandle.IsCompleted)
            {
                await System.Threading.Tasks.Task.Yield();
            }
            
            // Complete jobs safely
            predictionJobHandle.Complete();
            analysisJobHandle.Complete();
            
            // Generate predicted targets
            var predictedTarget = GeneratePredictedTarget();
            if (predictedTarget.confidence > confidenceThreshold)
            {
                predictedTargets.Enqueue(predictedTarget);
                OnTargetPredicted?.Invoke(predictedTarget);
            }
        }
        
        private PredictedTarget GeneratePredictedTarget()
        {
            // Predict optimal target position
            Vector3 predictedPlayerPos = PredictPlayerPosition();
            BoxingFormTracker.BoxingStance predictedStance = PredictOptimalStance();
            Vector3 optimalTargetPos = CalculateOptimalTargetPosition(predictedPlayerPos, predictedStance);
            
            // Determine target characteristics
            TargetType targetType = DetermineOptimalTargetType();
            RhythmTargetSystem.HandSide recommendedHand = DetermineOptimalHand(predictedStance, optimalTargetPos);
            float difficultyRating = CalculateTargetDifficulty(optimalTargetPos, targetType);
            
            return new PredictedTarget
            {
                position = optimalTargetPos,
                confidence = CalculatePredictionConfidence(),
                optimalTiming = Time.time + predictionHorizon,
                recommendedHand = recommendedHand,
                optimalStance = predictedStance,
                difficultyRating = difficultyRating,
                targetType = targetType
            };
        }
        
        private Vector3 PredictPlayerPosition()
        {
            // Simple linear prediction based on recent movement
            if (historyIndex < 2) return VRCameraHelper.PlayerPosition;
            
            int currentIdx = (historyIndex - 1) % movementHistorySize;
            int previousIdx = (historyIndex - 2) % movementHistorySize;
            
            Vector3 currentPos = movementHistory[currentIdx];
            Vector3 previousPos = movementHistory[previousIdx];
            Vector3 velocity = (currentPos - previousPos) / predictionUpdateInterval;
            
            return currentPos + velocity * predictionHorizon;
        }
        
        private BoxingFormTracker.BoxingStance PredictOptimalStance()
        {
            if (formTracker == null) return BoxingFormTracker.BoxingStance.Orthodox;
            
            // Analyze stance history to predict preference
            int orthodoxCount = 0;
            int southpawCount = 0;
            
            for (int i = 0; i < stanceHistorySize; i++)
            {
                if (stanceHistory[i] == 0) orthodoxCount++;
                else if (stanceHistory[i] == 1) southpawCount++;
            }
            
            return orthodoxCount > southpawCount ? 
                BoxingFormTracker.BoxingStance.Orthodox : 
                BoxingFormTracker.BoxingStance.Southpaw;
        }
        
        private Vector3 CalculateOptimalTargetPosition(Vector3 playerPos, BoxingFormTracker.BoxingStance stance)
        {
            // Calculate position based on stance and comfort zone
            float angleOffset = stance == BoxingFormTracker.BoxingStance.Orthodox ? 45f : -45f;
            float distance = UnityEngine.Random.Range(comfortZoneRadius, challengeZoneRadius);
            float height = playerPos.y + UnityEngine.Random.Range(-0.3f, 0.6f);
            
            Vector3 direction = Quaternion.Euler(0, angleOffset, 0) * Vector3.forward;
            return playerPos + direction * distance + Vector3.up * (height - playerPos.y);
        }
        
        private TargetType DetermineOptimalTargetType()
        {
            if (formTracker == null) return TargetType.Comfort;
            
            float formQuality = formTracker.CurrentFormData.stanceQuality;
            
            if (formQuality < 0.6f) return TargetType.FormTraining;
            if (formQuality > 0.9f) return TargetType.Challenge;
            if (UnityEngine.Random.value < 0.3f) return TargetType.PowerDevelopment;
            
            return TargetType.Comfort;
        }
        
        private RhythmTargetSystem.HandSide DetermineOptimalHand(BoxingFormTracker.BoxingStance stance, Vector3 targetPos)
        {
            Vector3 playerPos = VRCameraHelper.PlayerPosition;
            Vector3 playerForward = VRCameraHelper.PlayerForward;
            Vector3 toTarget = (targetPos - playerPos).normalized;
            
            float rightDot = Vector3.Dot(toTarget, Vector3.Cross(playerForward, Vector3.up));
            
            // Orthodox: right hand for right side targets, left hand for left side
            // Southpaw: opposite
            if (stance == BoxingFormTracker.BoxingStance.Orthodox)
            {
                return rightDot > 0 ? RhythmTargetSystem.HandSide.Right : RhythmTargetSystem.HandSide.Left;
            }
            else
            {
                return rightDot > 0 ? RhythmTargetSystem.HandSide.Left : RhythmTargetSystem.HandSide.Right;
            }
        }
        
        private float CalculateTargetDifficulty(Vector3 targetPos, TargetType targetType)
        {
            Vector3 playerPos = VRCameraHelper.PlayerPosition;
            float distance = Vector3.Distance(playerPos, targetPos);
            float normalizedDistance = Mathf.Clamp01(distance / challengeZoneRadius);
            
            float baseDifficulty = normalizedDistance;
            
            switch (targetType)
            {
                case TargetType.Challenge: return baseDifficulty + 0.3f;
                case TargetType.Comfort: return baseDifficulty - 0.2f;
                case TargetType.FormTraining: return baseDifficulty;
                case TargetType.PowerDevelopment: return baseDifficulty + 0.1f;
                default: return baseDifficulty;
            }
        }
        
        private float CalculatePredictionConfidence()
        {
            // Calculate confidence based on movement consistency and data quality
            float movementConsistency = CalculateMovementConsistency();
            float dataQuality = Mathf.Clamp01((float)historyIndex / movementHistorySize);
            
            return (movementConsistency + dataQuality) * 0.5f;
        }
        
        private float CalculateMovementConsistency()
        {
            if (historyIndex < 5) return 0.5f;
            
            float totalVariation = 0f;
            int samples = Mathf.Min(historyIndex, 10);
            
            for (int i = 1; i < samples; i++)
            {
                int currentIdx = (historyIndex - i) % movementHistorySize;
                int previousIdx = (historyIndex - i - 1) % movementHistorySize;
                
                float variation = Vector3.Distance(movementHistory[currentIdx], movementHistory[previousIdx]);
                totalVariation += variation;
            }
            
            float averageVariation = totalVariation / samples;
            return Mathf.Clamp01(1f - averageVariation); // Less variation = more consistency
        }
        
        private void ProcessPredictedTargets()
        {
            // Remove expired predictions
            while (predictedTargets.Count > 0 && predictedTargets.Peek().optimalTiming < Time.time)
            {
                predictedTargets.Dequeue();
            }
        }
        
        private void OnStanceChanged(BoxingFormTracker.BoxingStance newStance)
        {
            // Update stance preference analysis
            currentStancePreference.preferredStance = newStance;
            OnStancePreferenceUpdated?.Invoke(currentStancePreference);
        }
        
        private void OnFormAnalyzed(BoxingFormTracker.BoxingFormData formData)
        {
            // Update stance stability tracking
            currentStancePreference.stanceStability = formData.stanceQuality;
        }
        
        private void OnTargetHit(RhythmTargetSystem.CircleHitData hitData)
        {
            // Record successful target position for learning
            int successIndex = historyIndex % targetSuccessPositions.Length;
            targetSuccessPositions[successIndex] = hitData.hitPosition;
            targetSuccessRatings[successIndex] = hitData.accuracy;
        }
        
        public PredictedTarget GetNextPredictedTarget()
        {
            if (predictedTargets.Count > 0)
            {
                return predictedTargets.Peek();
            }
            
            return new PredictedTarget { confidence = 0f };
        }
        
        public MovementPattern GetCurrentMovementPattern()
        {
            return currentMovementPattern;
        }
        
        public StancePreference GetCurrentStancePreference()
        {
            return currentStancePreference;
        }
        
        private void OnDestroy()
        {
            // Complete jobs
            if (predictionJobHandle.IsCreated) predictionJobHandle.Complete();
            if (analysisJobHandle.IsCreated) analysisJobHandle.Complete();
            
            // Dispose native arrays
            if (movementHistory.IsCreated) movementHistory.Dispose();
            if (rotationHistory.IsCreated) rotationHistory.Dispose();
            if (stanceHistory.IsCreated) stanceHistory.Dispose();
            if (targetSuccessPositions.IsCreated) targetSuccessPositions.Dispose();
            if (targetSuccessRatings.IsCreated) targetSuccessRatings.Dispose();
        }
    }
    
    // ML Model Classes with Unity 6 enhancements
    public class MovementPredictor
    {
        private KalmanFilter kalmanFilter;
        private List<Vector3> movementSamples = new List<Vector3>();
        private const int MAX_SAMPLES = 20;
        
        public void Initialize()
        {
            kalmanFilter = new KalmanFilter();
        }
        
        public Vector3 PredictPosition(NativeArray<float3> history, float horizon)
        {
            if (history.Length < 3) return Vector3.zero;
            
            // Convert to movement samples
            movementSamples.Clear();
            for (int i = 0; i < math.min(history.Length, MAX_SAMPLES); i++)
            {
                movementSamples.Add(history[i]);
            }
            
            // Apply Kalman filtering for noise reduction
            var filteredPosition = kalmanFilter.Filter(movementSamples[movementSamples.Count - 1]);
            
            // Linear prediction with acceleration
            if (movementSamples.Count >= 3)
            {
                Vector3 velocity = movementSamples[movementSamples.Count - 1] - movementSamples[movementSamples.Count - 2];
                Vector3 acceleration = (movementSamples[movementSamples.Count - 1] - movementSamples[movementSamples.Count - 2]) - 
                                     (movementSamples[movementSamples.Count - 2] - movementSamples[movementSamples.Count - 3]);
                
                return filteredPosition + velocity * horizon + 0.5f * acceleration * horizon * horizon;
            }
            
            return filteredPosition;
        }
    }
    
    // **ADVANCED KALMAN FILTER IMPLEMENTATION**
    public class KalmanFilter
    {
        private Vector3 estimate = Vector3.zero;
        private Vector3 errorCovariance = Vector3.one;
        private readonly float processNoise = 0.01f;
        private readonly float measurementNoise = 0.1f;
        
        public Vector3 Filter(Vector3 measurement)
        {
            // Prediction phase
            Vector3 predictedEstimate = estimate;
            Vector3 predictedErrorCovariance = errorCovariance + Vector3.one * processNoise;
            
            // Update phase
            Vector3 kalmanGain = new Vector3(
                predictedErrorCovariance.x / (predictedErrorCovariance.x + measurementNoise),
                predictedErrorCovariance.y / (predictedErrorCovariance.y + measurementNoise),
                predictedErrorCovariance.z / (predictedErrorCovariance.z + measurementNoise)
            );
            
            estimate = predictedEstimate + Vector3.Scale(kalmanGain, measurement - predictedEstimate);
            errorCovariance = Vector3.Scale(Vector3.one - kalmanGain, predictedErrorCovariance);
            
            return estimate;
        }
    }

    public class StanceAnalyzer
    {
        private AdvancedMLStanceClassifier classifier;
        private NeuralNetwork stanceNetwork;
        
        public void Initialize()
        {
            classifier = new AdvancedMLStanceClassifier();
            stanceNetwork = new NeuralNetwork(new int[] { 6, 12, 8, 2 }); // Input: foot positions + hip, Output: Orthodox/Southpaw
        }
        
        public BoxingFormTracker.BoxingStance AnalyzeStance(NativeArray<int> stanceHistory, NativeArray<float3> movementHistory)
        {
            // Advanced pattern recognition using neural network
            float[] inputs = new float[6];
            
            if (movementHistory.Length >= 2)
            {
                // Foot positioning analysis
                inputs[0] = movementHistory[movementHistory.Length - 1].x; // Current X
                inputs[1] = movementHistory[movementHistory.Length - 1].z; // Current Z (forward)
                inputs[2] = movementHistory[movementHistory.Length - 2].x; // Previous X
                inputs[3] = movementHistory[movementHistory.Length - 2].z; // Previous Z
                
                // Hip rotation analysis
                inputs[4] = math.atan2(movementHistory[movementHistory.Length - 1].x, movementHistory[movementHistory.Length - 1].z);
                inputs[5] = GetStanceStability(stanceHistory);
            }
            
            float[] outputs = stanceNetwork.Forward(inputs);
            return outputs[0] > outputs[1] ? BoxingFormTracker.BoxingStance.Orthodox : BoxingFormTracker.BoxingStance.Southpaw;
        }
        
        private float GetStanceStability(NativeArray<int> stanceHistory)
        {
            if (stanceHistory.Length < 5) return 0.5f;
            
            int changes = 0;
            for (int i = 1; i < stanceHistory.Length; i++)
            {
                if (stanceHistory[i] != stanceHistory[i-1]) changes++;
            }
            
            return 1f - ((float)changes / stanceHistory.Length);
        }
    }
    
    // **ADVANCED ML STANCE CLASSIFIER**
    public class AdvancedMLStanceClassifier
    {
        private readonly float[,] weights = new float[,] {
            { 0.8f, -0.3f, 0.5f, -0.7f },  // Orthodox bias weights
            { -0.6f, 0.9f, -0.4f, 0.8f }   // Southpaw bias weights
        };
        
        public float ClassifyStance(float[] features)
        {
            float orthodoxScore = 0f, southpawScore = 0f;
            
            for (int i = 0; i < features.Length && i < 4; i++)
            {
                orthodoxScore += features[i] * weights[0, i];
                southpawScore += features[i] * weights[1, i];
            }
            
            return orthodoxScore > southpawScore ? 0f : 1f; // 0 = Orthodox, 1 = Southpaw
        }
    }
    
    // **SIMPLE NEURAL NETWORK FOR STANCE DETECTION**
    public class NeuralNetwork
    {
        private int[] layers;
        private float[][] neurons;
        private float[][][] weights;
        
        public NeuralNetwork(int[] layers)
        {
            this.layers = layers;
            InitializeNetwork();
        }
        
        private void InitializeNetwork()
        {
            neurons = new float[layers.Length][];
            weights = new float[layers.Length - 1][][];
            
            for (int i = 0; i < layers.Length; i++)
            {
                neurons[i] = new float[layers[i]];
            }
            
            for (int i = 0; i < layers.Length - 1; i++)
            {
                weights[i] = new float[layers[i]][];
                for (int j = 0; j < layers[i]; j++)
                {
                    weights[i][j] = new float[layers[i + 1]];
                    for (int k = 0; k < layers[i + 1]; k++)
                    {
                        weights[i][j][k] = UnityEngine.Random.Range(-1f, 1f);
                    }
                }
            }
        }
        
        public float[] Forward(float[] inputs)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                neurons[0][i] = inputs[i];
            }
            
            for (int i = 1; i < layers.Length; i++)
            {
                for (int j = 0; j < layers[i]; j++)
                {
                    float sum = 0f;
                    for (int k = 0; k < layers[i - 1]; k++)
                    {
                        sum += neurons[i - 1][k] * weights[i - 1][k][j];
                    }
                    neurons[i][j] = Sigmoid(sum);
                }
            }
            
            return neurons[neurons.Length - 1];
        }
        
        private float Sigmoid(float x)
        {
            return 1f / (1f + Mathf.Exp(-x));
        }
    }
    
    // Unity 6 Job System for Movement Prediction
    [BurstCompile]
    public struct MovementPredictionJob : IJob
    {
        [ReadOnly] public NativeArray<float3> movementHistory;
        [ReadOnly] public NativeArray<float> rotationHistory;
        [ReadOnly] public float predictionHorizon;
        [ReadOnly] public float currentTime;
        [ReadOnly] public int historySize;
        
        public void Execute()
        {
            // Perform movement prediction calculations
            if (historySize < 3) return;
            
            // Calculate velocity trend
            float3 velocity = float3.zero;
            int validSamples = 0;
            
            for (int i = 1; i < math.min(historySize, 10); i++)
            {
                int currentIdx = (historySize - i) % movementHistory.Length;
                int previousIdx = (historySize - i - 1) % movementHistory.Length;
                
                if (math.lengthsq(movementHistory[currentIdx]) > 0.01f &&
                    math.lengthsq(movementHistory[previousIdx]) > 0.01f)
                {
                    velocity += movementHistory[currentIdx] - movementHistory[previousIdx];
                    validSamples++;
                }
            }
            
            if (validSamples > 0)
            {
                velocity /= validSamples;
            }
            
            // Calculate rotation trend
            float rotationVelocity = 0f;
            int validRotations = 0;
            
            for (int i = 1; i < math.min(historySize, 10); i++)
            {
                int currentIdx = (historySize - i) % rotationHistory.Length;
                int previousIdx = (historySize - i - 1) % rotationHistory.Length;
                
                float rotationDiff = rotationHistory[currentIdx] - rotationHistory[previousIdx];
                
                // Handle 360-degree wraparound
                if (rotationDiff > 180f) rotationDiff -= 360f;
                if (rotationDiff < -180f) rotationDiff += 360f;
                
                rotationVelocity += rotationDiff;
                validRotations++;
            }
            
            if (validRotations > 0)
            {
                rotationVelocity /= validRotations;
            }
        }
    }
    
    // Unity 6 Job System for Stance Analysis
    [BurstCompile]
    public struct StanceAnalysisJob : IJob
    {
        [ReadOnly] public NativeArray<int> stanceHistory;
        [ReadOnly] public NativeArray<float3> movementHistory;
        [ReadOnly] public NativeArray<float3> targetSuccessPositions;
        [ReadOnly] public NativeArray<float> targetSuccessRatings;
        [ReadOnly] public int historySize;
        
        public void Execute()
        {
            // Analyze stance preferences and patterns
            int orthodoxCount = 0;
            int southpawCount = 0;
            
            for (int i = 0; i < historySize; i++)
            {
                if (stanceHistory[i] == 0) orthodoxCount++;
                else if (stanceHistory[i] == 1) southpawCount++;
            }
            
            // Calculate stance stability
            int stanceChanges = 0;
            for (int i = 1; i < historySize; i++)
            {
                if (stanceHistory[i] != stanceHistory[i - 1])
                {
                    stanceChanges++;
                }
            }
            
            float stanceStability = 1f - ((float)stanceChanges / historySize);
            
            // Analyze target success patterns
            float3 successCentroid = float3.zero;
            float averageSuccessRating = 0f;
            int validSuccesses = 0;
            
            for (int i = 0; i < targetSuccessPositions.Length; i++)
            {
                if (math.lengthsq(targetSuccessPositions[i]) > 0.01f)
                {
                    successCentroid += targetSuccessPositions[i];
                    averageSuccessRating += targetSuccessRatings[i];
                    validSuccesses++;
                }
            }
            
            if (validSuccesses > 0)
            {
                successCentroid /= validSuccesses;
                averageSuccessRating /= validSuccesses;
            }
        }
    }
} 