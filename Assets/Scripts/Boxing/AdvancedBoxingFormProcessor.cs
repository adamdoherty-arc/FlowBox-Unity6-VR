using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Advanced Boxing Form Processor using Unity 6 Job System + Burst compilation
    /// Optimized for 90+ FPS VR performance with real-time form analysis
    /// </summary>
    public class AdvancedBoxingFormProcessor : MonoBehaviour
    {
        [Header("Job System Settings")]
        public bool enableBurstCompilation = true;
        public int batchSize = 64;
        public float updateFrequency = 30f; // 30 FPS analysis
        
        [Header("Form Analysis Parameters")]
        public float hipRotationWeight = 0.3f;
        public float footSpacingWeight = 0.25f;
        public float weightDistributionWeight = 0.25f;
        public float hipVelocityWeight = 0.2f;
        
        [Header("Stance Detection")]
        public float stanceChangeThreshold = 0.7f;
        public int stanceHistorySize = 30;
        
        // Native Collections for high-performance data
        private NativeArray<float3> hipPositionHistory;
        private NativeArray<float3> leftFootHistory;
        private NativeArray<float3> rightFootHistory;
        private NativeArray<float> hipRotationHistory;
        private NativeArray<float> formScoreHistory;
        private NativeArray<BoxingFormJobData> formAnalysisResults;
        
        // Job handles for async processing
        private JobHandle formAnalysisJobHandle;
        private JobHandle stanceDetectionJobHandle;
        private bool isProcessingForm = false;
        
        // Results cache
        private BoxingFormTracker.BoxingFormData cachedFormData;
        private BoxingFormTracker.BoxingStance cachedStance;
        private float lastAnalysisTime;
        private int historyIndex = 0;
        
        // Singleton
        public static AdvancedBoxingFormProcessor Instance { get; private set; }
        
        // Native data structures
        [System.Serializable]
        public struct BoxingFormJobData
        {
            public float hipRotation;
            public float footSpacing;
            public float weightDistribution;
            public float hipVelocityMagnitude;
            public float formScore;
            public int stanceIndicator; // 0 = Orthodox, 1 = Southpaw
            public float powerMultiplier;
            public float accuracyBonus;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeJobSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeJobSystem()
        {
            // Initialize native arrays for high-performance processing
            int historySize = Mathf.RoundToInt(updateFrequency * 2f); // 2 seconds of history
            
            hipPositionHistory = new NativeArray<float3>(historySize, Allocator.Persistent);
            leftFootHistory = new NativeArray<float3>(historySize, Allocator.Persistent);
            rightFootHistory = new NativeArray<float3>(historySize, Allocator.Persistent);
            hipRotationHistory = new NativeArray<float>(historySize, Allocator.Persistent);
            formScoreHistory = new NativeArray<float>(historySize, Allocator.Persistent);
            formAnalysisResults = new NativeArray<BoxingFormJobData>(batchSize, Allocator.Persistent);
            
            Debug.Log($"🚀 Advanced Boxing Form Processor initialized with Job System (Burst: {enableBurstCompilation})");
        }
        
        private void Update()
        {
            if (Time.time - lastAnalysisTime >= 1f / updateFrequency)
            {
                if (!isProcessingForm)
                {
                    _ = ProcessFormAnalysisAsync();
                }
                lastAnalysisTime = Time.time;
            }
        }
        
        private async Task ProcessFormAnalysisAsync()
        {
            isProcessingForm = true;
            
            try
            {
                // Complete previous jobs
                formAnalysisJobHandle.Complete();
                stanceDetectionJobHandle.Complete();
                
                // Update tracking data
                UpdateTrackingData();
                
                // Schedule form analysis job
                var formAnalysisJob = new BoxingFormAnalysisJob
                {
                    hipPositions = hipPositionHistory,
                    leftFootPositions = leftFootHistory,
                    rightFootPositions = rightFootHistory,
                    hipRotations = hipRotationHistory,
                    results = formAnalysisResults,
                    hipRotationWeight = hipRotationWeight,
                    footSpacingWeight = footSpacingWeight,
                    weightDistributionWeight = weightDistributionWeight,
                    hipVelocityWeight = hipVelocityWeight,
                    stanceThreshold = stanceChangeThreshold,
                    deltaTime = Time.deltaTime
                };
                
                formAnalysisJobHandle = formAnalysisJob.Schedule(batchSize, 8);
                
                // Schedule stance detection job
                var stanceDetectionJob = new StanceDetectionJob
                {
                    leftFootPositions = leftFootHistory,
                    rightFootPositions = rightFootHistory,
                    hipRotations = hipRotationHistory,
                    stanceHistory = new NativeArray<int>(stanceHistorySize, Allocator.TempJob),
                    stanceThreshold = stanceChangeThreshold
                };
                
                stanceDetectionJobHandle = stanceDetectionJob.Schedule();
                
                // Wait for completion asynchronously
                while (!formAnalysisJobHandle.IsCompleted || !stanceDetectionJobHandle.IsCompleted)
                {
                    await Task.Yield();
                }
                
                // Complete jobs and get results
                formAnalysisJobHandle.Complete();
                stanceDetectionJobHandle.Complete();
                
                // Process results
                ProcessJobResults();
                
                // Clean up temporary arrays
                if (stanceDetectionJob.stanceHistory.IsCreated)
                {
                    stanceDetectionJob.stanceHistory.Dispose();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in form analysis: {ex.Message}");
            }
            finally
            {
                isProcessingForm = false;
            }
        }
        
        private void UpdateTrackingData()
        {
            var formTracker = BoxingFormTracker.Instance;
            if (formTracker == null) return;
            
            // Get current positions
            float3 hipPos = formTracker.hipReference != null ? 
                formTracker.hipReference.position : float3.zero;
            float3 leftFootPos = formTracker.leftFootReference != null ? 
                formTracker.leftFootReference.position : float3.zero;
            float3 rightFootPos = formTracker.rightFootReference != null ? 
                formTracker.rightFootReference.position : float3.zero;
            
            // Update history arrays
            hipPositionHistory[historyIndex] = hipPos;
            leftFootHistory[historyIndex] = leftFootPos;
            rightFootHistory[historyIndex] = rightFootPos;
            hipRotationHistory[historyIndex] = formTracker.CurrentHipRotation;
            
            historyIndex = (historyIndex + 1) % hipPositionHistory.Length;
        }
        
        private void ProcessJobResults()
        {
            if (formAnalysisResults.Length == 0) return;
            
            // Get the most recent result
            var result = formAnalysisResults[0];
            
            // Update cached form data
            cachedFormData = new BoxingFormTracker.BoxingFormData
            {
                stance = result.stanceIndicator == 0 ? 
                    BoxingFormTracker.BoxingStance.Orthodox : 
                    BoxingFormTracker.BoxingStance.Southpaw,
                hipRotation = result.hipRotation,
                footSpacing = result.footSpacing,
                weightDistribution = result.weightDistribution,
                hipVelocity = new Vector3(0, 0, result.hipVelocityMagnitude),
                overallForm = GetFormQualityFromScore(result.formScore),
                powerMultiplier = result.powerMultiplier,
                accuracyBonus = result.accuracyBonus,
                isProperStance = result.formScore > 0.7f,
                stanceQuality = result.formScore
            };
            
            // Update stance if changed
            var newStance = result.stanceIndicator == 0 ? 
                BoxingFormTracker.BoxingStance.Orthodox : 
                BoxingFormTracker.BoxingStance.Southpaw;
            
            if (newStance != cachedStance)
            {
                cachedStance = newStance;
                // Notify main form tracker
                if (BoxingFormTracker.Instance != null)
                {
                    BoxingFormTracker.Instance.OnStanceChanged?.Invoke(newStance);
                }
            }
            
            // Store in history for analysis
            formScoreHistory[historyIndex] = result.formScore;
        }
        
        private BoxingFormTracker.FormQuality GetFormQualityFromScore(float score)
        {
            if (score >= 0.9f) return BoxingFormTracker.FormQuality.Perfect;
            if (score >= 0.8f) return BoxingFormTracker.FormQuality.Excellent;
            if (score >= 0.7f) return BoxingFormTracker.FormQuality.Good;
            if (score >= 0.5f) return BoxingFormTracker.FormQuality.Fair;
            return BoxingFormTracker.FormQuality.Poor;
        }
        
        // Public API
        public BoxingFormTracker.BoxingFormData GetCurrentFormData()
        {
            return cachedFormData;
        }
        
        public BoxingFormTracker.BoxingStance GetCurrentStance()
        {
            return cachedStance;
        }
        
        public float GetFormScore()
        {
            return formAnalysisResults.Length > 0 ? formAnalysisResults[0].formScore : 0f;
        }
        
        public float GetAverageFormScore()
        {
            if (formScoreHistory.Length == 0) return 0f;
            
            float sum = 0f;
            for (int i = 0; i < formScoreHistory.Length; i++)
            {
                sum += formScoreHistory[i];
            }
            return sum / formScoreHistory.Length;
        }
        
        private void OnDestroy()
        {
            // Complete any running jobs
            if (formAnalysisJobHandle.IsCreated) formAnalysisJobHandle.Complete();
            if (stanceDetectionJobHandle.IsCreated) stanceDetectionJobHandle.Complete();
            
            // Dispose native arrays
            if (hipPositionHistory.IsCreated) hipPositionHistory.Dispose();
            if (leftFootHistory.IsCreated) leftFootHistory.Dispose();
            if (rightFootHistory.IsCreated) rightFootHistory.Dispose();
            if (hipRotationHistory.IsCreated) hipRotationHistory.Dispose();
            if (formScoreHistory.IsCreated) formScoreHistory.Dispose();
            if (formAnalysisResults.IsCreated) formAnalysisResults.Dispose();
        }
    }
    
    /// <summary>
    /// High-performance boxing form analysis job with Burst compilation
    /// </summary>
    [BurstCompile]
    public struct BoxingFormAnalysisJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> hipPositions;
        [ReadOnly] public NativeArray<float3> leftFootPositions;
        [ReadOnly] public NativeArray<float3> rightFootPositions;
        [ReadOnly] public NativeArray<float> hipRotations;
        [WriteOnly] public NativeArray<AdvancedBoxingFormProcessor.BoxingFormJobData> results;
        
        [ReadOnly] public float hipRotationWeight;
        [ReadOnly] public float footSpacingWeight;
        [ReadOnly] public float weightDistributionWeight;
        [ReadOnly] public float hipVelocityWeight;
        [ReadOnly] public float stanceThreshold;
        [ReadOnly] public float deltaTime;
        
        public void Execute(int index)
        {
            if (index >= results.Length || index >= hipPositions.Length) return;
            
            // Calculate hip rotation score
            float hipRotationScore = CalculateHipRotationScore(index);
            
            // Calculate foot spacing score
            float footSpacingScore = CalculateFootSpacingScore(index);
            
            // Calculate weight distribution score
            float weightDistributionScore = CalculateWeightDistributionScore(index);
            
            // Calculate hip velocity score
            float hipVelocityScore = CalculateHipVelocityScore(index);
            
            // Calculate overall form score
            float overallScore = (hipRotationScore * hipRotationWeight) +
                               (footSpacingScore * footSpacingWeight) +
                               (weightDistributionScore * weightDistributionWeight) +
                               (hipVelocityScore * hipVelocityWeight);
            
            // Detect stance
            int stanceIndicator = DetectStance(index);
            
            // Calculate bonuses
            float powerMultiplier = 1f + (overallScore * 1f); // Up to 2x power
            float accuracyBonus = overallScore * 0.3f; // Up to 30% accuracy bonus
            
            // Store results
            results[index] = new AdvancedBoxingFormProcessor.BoxingFormJobData
            {
                hipRotation = hipRotations[index],
                footSpacing = math.distance(leftFootPositions[index], rightFootPositions[index]),
                weightDistribution = weightDistributionScore,
                hipVelocityMagnitude = CalculateHipVelocityMagnitude(index),
                formScore = overallScore,
                stanceIndicator = stanceIndicator,
                powerMultiplier = powerMultiplier,
                accuracyBonus = accuracyBonus
            };
        }
        
        private float CalculateHipRotationScore(int index)
        {
            float rotation = math.abs(hipRotations[index]);
            // Optimal hip rotation is 15-45 degrees
            if (rotation >= 15f && rotation <= 45f)
            {
                return 1f;
            }
            else if (rotation < 15f)
            {
                return rotation / 15f;
            }
            else
            {
                return math.max(0f, 1f - ((rotation - 45f) / 45f));
            }
        }
        
        private float CalculateFootSpacingScore(int index)
        {
            float spacing = math.distance(leftFootPositions[index], rightFootPositions[index]);
            // Optimal spacing is 0.4-0.8 meters
            if (spacing >= 0.4f && spacing <= 0.8f)
            {
                return 1f;
            }
            else if (spacing < 0.4f)
            {
                return spacing / 0.4f;
            }
            else
            {
                return math.max(0f, 1f - ((spacing - 0.8f) / 0.4f));
            }
        }
        
        private float CalculateWeightDistributionScore(int index)
        {
            // Calculate weight distribution based on foot positions
            float3 leftFoot = leftFootPositions[index];
            float3 rightFoot = rightFootPositions[index];
            float3 center = (leftFoot + rightFoot) * 0.5f;
            
            // Good weight distribution should be roughly centered
            float leftDistance = math.distance(leftFoot, center);
            float rightDistance = math.distance(rightFoot, center);
            float balance = 1f - math.abs(leftDistance - rightDistance);
            
            return math.clamp(balance, 0f, 1f);
        }
        
        private float CalculateHipVelocityScore(int index)
        {
            float velocity = CalculateHipVelocityMagnitude(index);
            // Optimal hip velocity is 1-5 m/s
            if (velocity >= 1f && velocity <= 5f)
            {
                return 1f;
            }
            else if (velocity < 1f)
            {
                return velocity / 1f;
            }
            else
            {
                return math.max(0f, 1f - ((velocity - 5f) / 5f));
            }
        }
        
        private float CalculateHipVelocityMagnitude(int index)
        {
            if (index == 0 || index >= hipPositions.Length - 1) return 0f;
            
            float3 currentPos = hipPositions[index];
            float3 previousPos = hipPositions[index - 1];
            float3 velocity = (currentPos - previousPos) / deltaTime;
            
            return math.length(velocity);
        }
        
        private int DetectStance(int index)
        {
            float3 leftFoot = leftFootPositions[index];
            float3 rightFoot = rightFootPositions[index];
            
            // Orthodox stance: left foot forward (positive Z)
            // Southpaw stance: right foot forward (positive Z)
            float leftForwardness = leftFoot.z;
            float rightForwardness = rightFoot.z;
            
            return leftForwardness > rightForwardness ? 0 : 1; // 0 = Orthodox, 1 = Southpaw
        }
    }
    
    /// <summary>
    /// Stance detection job using statistical analysis
    /// </summary>
    [BurstCompile]
    public struct StanceDetectionJob : IJob
    {
        [ReadOnly] public NativeArray<float3> leftFootPositions;
        [ReadOnly] public NativeArray<float3> rightFootPositions;
        [ReadOnly] public NativeArray<float> hipRotations;
        [WriteOnly] public NativeArray<int> stanceHistory;
        [ReadOnly] public float stanceThreshold;
        
        public void Execute()
        {
            int orthodoxCount = 0;
            int southpawCount = 0;
            
            // Analyze stance over time
            for (int i = 0; i < leftFootPositions.Length && i < stanceHistory.Length; i++)
            {
                float3 leftFoot = leftFootPositions[i];
                float3 rightFoot = rightFootPositions[i];
                
                // Determine stance for this frame
                if (leftFoot.z > rightFoot.z)
                {
                    orthodoxCount++;
                    stanceHistory[i] = 0; // Orthodox
                }
                else
                {
                    southpawCount++;
                    stanceHistory[i] = 1; // Southpaw
                }
            }
            
            // Statistical confidence check
            float totalSamples = orthodoxCount + southpawCount;
            if (totalSamples > 0)
            {
                float orthodoxConfidence = orthodoxCount / totalSamples;
                float southpawConfidence = southpawCount / totalSamples;
                
                // Only switch stance if confidence exceeds threshold
                if (orthodoxConfidence > stanceThreshold || southpawConfidence > stanceThreshold)
                {
                    // Stance change detected with sufficient confidence
                }
            }
        }
    }
} 