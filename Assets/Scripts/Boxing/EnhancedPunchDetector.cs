using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Enhanced Punch Detector for Unity 6 with advanced VR tracking and performance optimizations
    /// </summary>
    public class EnhancedPunchDetector : MonoBehaviour
    {
        [Header("Punch Detection Settings")]
        public float minPunchVelocity = 2.0f;
        public float maxPunchVelocity = 15.0f;
        public float punchCooldown = 0.08f;
        public float velocitySmoothing = 0.1f;
        
        [Header("Advanced Detection")]
        public bool enableAccelerationDetection = true;
        public bool enableDirectionAnalysis = true;
        public bool enableFormAnalysis = true;
        public float minAcceleration = 5.0f;
        public float formAccuracyThreshold = 0.8f;
        
        [Header("Hand Settings")]
        public bool isLeftHand = true;
        public Transform wristTransform;
        public Transform knuckleTransform;
        
        [Header("Unity 6 Features")]
        public bool enableJobSystemOptimization = true;
        public bool enableBurstCompilation = true;
        public int velocityHistorySize = 10;
        
        [Header("Events")]
        public UnityEvent<PunchData> OnPunchDetected;
        public UnityEvent<Vector3> OnPunchDirection;
        public UnityEvent<float> OnFormAnalysis;
        
        // Tracking data
        private Queue<Vector3> positionHistory = new Queue<Vector3>();
        private Queue<Vector3> velocityHistory = new Queue<Vector3>();
        private Queue<float> timeHistory = new Queue<float>();
        
        // Current state
        private Vector3 currentVelocity;
        private Vector3 currentAcceleration;
        private Vector3 smoothedVelocity;
        private float lastPunchTime;
        private bool canPunch = true;
        
        // Job System data
        private NativeArray<float3> positionData;
        private NativeArray<float> timeData;
        private NativeArray<float3> velocityResults;
        private JobHandle currentJobHandle;
        
        // Performance tracking
        private int totalPunches = 0;
        private float averagePunchPower = 0f;
        private float bestPunchPower = 0f;
        
        // Components
        private Transform handTransform;
        
        // Properties
        public Vector3 CurrentVelocity => currentVelocity;
        public Vector3 CurrentAcceleration => currentAcceleration;
        public float AveragePunchPower => averagePunchPower;
        public float BestPunchPower => bestPunchPower;
        public int TotalPunches => totalPunches;
        public bool CanPunch => canPunch;
        
        [System.Serializable]
        public struct PunchData
        {
            public float power;
            public float accuracy;
            public Vector3 direction;
            public Vector3 velocity;
            public float acceleration;
            public float formScore;
            public bool isPerfectForm;
            public HandType hand;
        }
        
        public enum HandType
        {
            Left,
            Right
        }
        
        private void Start()
        {
            handTransform = transform;
            
            // Initialize Job System arrays
            if (enableJobSystemOptimization)
            {
                positionData = new NativeArray<float3>(velocityHistorySize, Allocator.Persistent);
                timeData = new NativeArray<float>(velocityHistorySize, Allocator.Persistent);
                velocityResults = new NativeArray<float3>(velocityHistorySize, Allocator.Persistent);
            }
            
            Debug.Log($"Enhanced Punch Detector initialized for {(isLeftHand ? "Left" : "Right")} hand");
        }
        
        private void Update()
        {
            UpdateTrackingData();
            
            if (enableJobSystemOptimization)
            {
                UpdateVelocityWithJobs();
            }
            else
            {
                UpdateVelocityTraditional();
            }
            
            DetectPunch();
            UpdateCooldown();
            
            if (enableFormAnalysis)
            {
                AnalyzePunchForm();
            }
        }
        
        private void UpdateTrackingData()
        {
            Vector3 currentPosition = handTransform.position;
            float currentTime = Time.time;
            
            // Add to history
            positionHistory.Enqueue(currentPosition);
            timeHistory.Enqueue(currentTime);
            
            // Maintain history size
            if (positionHistory.Count > velocityHistorySize)
            {
                positionHistory.Dequeue();
                timeHistory.Dequeue();
            }
        }
        
        private void UpdateVelocityWithJobs()
        {
            if (positionHistory.Count < 2) return;
            
            // Complete previous job
            currentJobHandle.Complete();
            
            // Copy data to native arrays
            Vector3[] positions = positionHistory.ToArray();
            float[] times = timeHistory.ToArray();
            
            for (int i = 0; i < positions.Length; i++)
            {
                positionData[i] = positions[i];
                timeData[i] = times[i];
            }
            
            // Schedule velocity calculation job
            var velocityJob = new VelocityCalculationJob
            {
                positions = positionData,
                times = timeData,
                velocities = velocityResults,
                smoothingFactor = velocitySmoothing
            };
            
            currentJobHandle = velocityJob.Schedule(positions.Length - 1, 1);
            currentJobHandle.Complete(); // Complete immediately for this frame
            
            // Get results
            if (velocityResults.Length > 0)
            {
                currentVelocity = velocityResults[velocityResults.Length - 1];
                smoothedVelocity = Vector3.Lerp(smoothedVelocity, currentVelocity, velocitySmoothing);
            }
        }
        
        private void UpdateVelocityTraditional()
        {
            if (positionHistory.Count < 2) return;
            
            Vector3[] positions = positionHistory.ToArray();
            float[] times = timeHistory.ToArray();
            
            int lastIndex = positions.Length - 1;
            if (lastIndex > 0)
            {
                float deltaTime = times[lastIndex] - times[lastIndex - 1];
                if (deltaTime > 0)
                {
                    Vector3 newVelocity = (positions[lastIndex] - positions[lastIndex - 1]) / deltaTime;
                    
                    // Calculate acceleration
                    currentAcceleration = (newVelocity - currentVelocity) / deltaTime;
                    currentVelocity = newVelocity;
                    smoothedVelocity = Vector3.Lerp(smoothedVelocity, currentVelocity, velocitySmoothing);
                }
            }
        }
        
        private void DetectPunch()
        {
            if (!canPunch) return;
            
            float velocityMagnitude = smoothedVelocity.magnitude;
            float accelerationMagnitude = currentAcceleration.magnitude;
            
            bool velocityThresholdMet = velocityMagnitude >= minPunchVelocity;
            bool accelerationThresholdMet = !enableAccelerationDetection || accelerationMagnitude >= minAcceleration;
            
            if (velocityThresholdMet && accelerationThresholdMet)
            {
                ExecutePunch(velocityMagnitude, accelerationMagnitude);
            }
        }
        
        private void ExecutePunch(float velocityMagnitude, float accelerationMagnitude)
        {
            // Calculate punch power (0-1 normalized)
            float velocityPower = Mathf.Clamp01((velocityMagnitude - minPunchVelocity) / (maxPunchVelocity - minPunchVelocity));
            float accelerationPower = enableAccelerationDetection ? 
                Mathf.Clamp01(accelerationMagnitude / (minAcceleration * 3f)) : 1f;
            
            float combinedPower = (velocityPower + accelerationPower) * 0.5f;
            
            // Analyze punch direction and form
            float directionAccuracy = AnalyzePunchDirection();
            float formScore = enableFormAnalysis ? AnalyzePunchForm() : 1f;
            
            // Create punch data
            PunchData punchData = new PunchData
            {
                power = combinedPower,
                accuracy = directionAccuracy,
                direction = smoothedVelocity.normalized,
                velocity = smoothedVelocity,
                acceleration = accelerationMagnitude,
                formScore = formScore,
                isPerfectForm = formScore >= formAccuracyThreshold,
                hand = isLeftHand ? HandType.Left : HandType.Right
            };
            
            // Update statistics
            UpdatePunchStatistics(combinedPower);
            
            // Trigger events
            OnPunchDetected?.Invoke(punchData);
            OnPunchDirection?.Invoke(punchData.direction);
            OnFormAnalysis?.Invoke(formScore);
            
            // Start cooldown
            lastPunchTime = Time.time;
            canPunch = false;
            
            Debug.Log($"{(isLeftHand ? "Left" : "Right")} punch: Power={combinedPower:F2}, Form={formScore:F2}, Velocity={velocityMagnitude:F1}");
        }
        
        private float AnalyzePunchDirection()
        {
            if (!enableDirectionAnalysis) return 1f;
            
            // Analyze if punch is moving forward (away from body)
                            Vector3 forwardDirection = VRBoxingGame.Core.VRCameraHelper.PlayerForward;
            float forwardAlignment = Vector3.Dot(smoothedVelocity.normalized, forwardDirection);
            
            return Mathf.Clamp01(forwardAlignment);
        }
        
        private float AnalyzePunchForm()
        {
            if (!enableFormAnalysis)
                return 1f;
            
            float formScore = 1f;
            
            // Basic wrist alignment if transforms available
            if (wristTransform != null && knuckleTransform != null)
            {
                Vector3 wristToKnuckle = (knuckleTransform.position - wristTransform.position).normalized;
                Vector3 punchDirection = smoothedVelocity.normalized;
                float alignment = Vector3.Dot(wristToKnuckle, punchDirection);
                formScore *= Mathf.Clamp01(alignment);
            }
            
            // Integrate with BoxingFormTracker for enhanced analysis
            if (BoxingFormTracker.Instance != null)
            {
                float stanceMultiplier = BoxingFormTracker.Instance.GetCurrentPowerMultiplier();
                float accuracyBonus = BoxingFormTracker.Instance.GetCurrentAccuracyBonus();
                
                // Apply boxing form bonuses
                formScore *= stanceMultiplier;
                formScore += accuracyBonus;
                
                // Check if punch matches current stance
                var currentStance = BoxingFormTracker.Instance.CurrentStance;
                bool correctHandForStance = 
                    (currentStance == BoxingFormTracker.BoxingStance.Orthodox && !isLeftHand) ||
                    (currentStance == BoxingFormTracker.BoxingStance.Southpaw && isLeftHand);
                
                if (correctHandForStance)
                {
                    formScore *= 1.2f; // 20% bonus for using dominant hand
                }
            }
            
            return Mathf.Clamp01(formScore);
        }
        
        private void UpdatePunchStatistics(float punchPower)
        {
            totalPunches++;
            averagePunchPower = ((averagePunchPower * (totalPunches - 1)) + punchPower) / totalPunches;
            
            if (punchPower > bestPunchPower)
            {
                bestPunchPower = punchPower;
            }
        }
        
        private void UpdateCooldown()
        {
            if (!canPunch && Time.time - lastPunchTime >= punchCooldown)
            {
                canPunch = true;
            }
        }
        
        // Unity 6 Job System for velocity calculations
        [BurstCompile]
        public struct VelocityCalculationJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<float> times;
            [ReadOnly] public float smoothingFactor;
            
            [WriteOnly] public NativeArray<float3> velocities;
            
            public void Execute(int index)
            {
                if (index >= positions.Length - 1) return;
                
                float deltaTime = times[index + 1] - times[index];
                if (deltaTime > 0)
                {
                    float3 velocity = (positions[index + 1] - positions[index]) / deltaTime;
                    velocities[index] = velocity;
                }
            }
        }
        
        // Public methods
        public void ResetStatistics()
        {
            totalPunches = 0;
            averagePunchPower = 0f;
            bestPunchPower = 0f;
        }
        
        public void SetSensitivity(float minVel, float maxVel)
        {
            minPunchVelocity = Mathf.Max(0.1f, minVel);
            maxPunchVelocity = Mathf.Max(minPunchVelocity + 1f, maxVel);
        }
        
        public PunchAnalytics GetAnalytics()
        {
            return new PunchAnalytics
            {
                totalPunches = totalPunches,
                averagePower = averagePunchPower,
                bestPower = bestPunchPower,
                currentVelocity = currentVelocity.magnitude,
                isLeftHand = isLeftHand
            };
        }
        
        private void OnDestroy()
        {
            // Clean up Job System resources
            if (enableJobSystemOptimization)
            {
                currentJobHandle.Complete();
                
                if (positionData.IsCreated) positionData.Dispose();
                if (timeData.IsCreated) timeData.Dispose();
                if (velocityResults.IsCreated) velocityResults.Dispose();
            }
        }
        
        // Gizmos for debugging
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            // Draw velocity vector
            Gizmos.color = canPunch ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
            
            // Draw velocity direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, smoothedVelocity * 0.1f);
            
            // Draw acceleration direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, currentAcceleration * 0.01f);
            
            // Draw form analysis if enabled
            if (enableFormAnalysis && wristTransform != null && knuckleTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(wristTransform.position, knuckleTransform.position);
            }
        }
        
        [System.Serializable]
        public struct PunchAnalytics
        {
            public int totalPunches;
            public float averagePower;
            public float bestPower;
            public float currentVelocity;
            public bool isLeftHand;
        }
    }
}

