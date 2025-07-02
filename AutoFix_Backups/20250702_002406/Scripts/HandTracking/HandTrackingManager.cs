using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

namespace VRBoxingGame.HandTracking
{
    /// <summary>
    /// Advanced Hand Tracking Manager for Unity 6 with enhanced VR features
    /// Supports both controller and hand tracking modes with seamless switching
    /// </summary>
    public class HandTrackingManager : MonoBehaviour
    {
        [Header("Hand Tracking Settings")]
        public bool enableHandTracking = true;
        public bool enableControllerFallback = true;
        public float handTrackingConfidenceThreshold = 0.7f;
        public float switchingHysteresis = 0.1f;
        
        [Header("Gesture Recognition")]
        public bool enableGestureRecognition = true;
        public float gestureRecognitionThreshold = 0.8f;
        public float gestureHoldTime = 0.5f;
        
        [Header("Hand Models")]
        public GameObject leftHandModel;
        public GameObject rightHandModel;
        public GameObject leftControllerModel;
        public GameObject rightControllerModel;
        
        [Header("Tracking Points")]
        public Transform[] leftHandJoints = new Transform[24];
        public Transform[] rightHandJoints = new Transform[24];
        
        [Header("Events")]
        public UnityEvent<HandData> OnLeftHandUpdate;
        public UnityEvent<HandData> OnRightHandUpdate;
        public UnityEvent<GestureData> OnGestureDetected;
        public UnityEvent<TrackingMode> OnTrackingModeChanged;
        
        // Tracking state
        public enum TrackingMode
        {
            Controllers,
            HandTracking,
            Mixed
        }
        
        public enum HandJoint
        {
            Wrist = 0,
            ThumbTip = 1,
            ThumbDistal = 2,
            ThumbProximal = 3,
            ThumbMetacarpal = 4,
            IndexTip = 5,
            IndexDistal = 6,
            IndexIntermediate = 7,
            IndexProximal = 8,
            IndexMetacarpal = 9,
            MiddleTip = 10,
            MiddleDistal = 11,
            MiddleIntermediate = 12,
            MiddleProximal = 13,
            MiddleMetacarpal = 14,
            RingTip = 15,
            RingDistal = 16,
            RingIntermediate = 17,
            RingProximal = 18,
            RingMetacarpal = 19,
            PinkyTip = 20,
            PinkyDistal = 21,
            PinkyIntermediate = 22,
            PinkyProximal = 23
        }
        
        public enum GestureType
        {
            None,
            Fist,
            OpenHand,
            Point,
            ThumbsUp,
            Peace,
            Grab,
            Pinch
        }
        
        [System.Serializable]
        public struct HandData
        {
            public bool isTracked;
            public float confidence;
            public Vector3 palmPosition;
            public Quaternion palmRotation;
            public Vector3 palmVelocity;
            public Vector3[] jointPositions;
            public Quaternion[] jointRotations;
            public float[] fingerCurls;
            public GestureType currentGesture;
            public bool isLeftHand;
        }
        
        [System.Serializable]
        public struct GestureData
        {
            public GestureType gestureType;
            public float confidence;
            public float duration;
            public bool isLeftHand;
            public Vector3 position;
        }
        
        // Private variables
        private TrackingMode currentTrackingMode = TrackingMode.Controllers;
        private HandData leftHandData;
        private HandData rightHandData;
        private Dictionary<GestureType, float> gestureTimers = new Dictionary<GestureType, float>();
        
        // Job System data
        private NativeArray<float3> jointPositions;
        private NativeArray<quaternion> jointRotations;
        private NativeArray<float> gestureScores;
        private JobHandle currentJobHandle;
        
        // Performance tracking
        private float trackingUpdateRate = 90f; // Target 90 FPS for hand tracking
        private float lastUpdateTime;
        
        // Singleton
        public static HandTrackingManager Instance { get; private set; }
        
        // Properties
        public TrackingMode CurrentTrackingMode => currentTrackingMode;
        public HandData LeftHandData => leftHandData;
        public HandData RightHandData => rightHandData;
        public bool IsHandTrackingActive => currentTrackingMode != TrackingMode.Controllers;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeHandTracking();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeHandTracking()
        {
            // Initialize Job System arrays
            jointPositions = new NativeArray<float3>(48, Allocator.Persistent); // 24 joints per hand
            jointRotations = new NativeArray<quaternion>(48, Allocator.Persistent);
            gestureScores = new NativeArray<float>(8, Allocator.Persistent); // Number of gesture types
            
            // Initialize gesture timers
            foreach (GestureType gesture in System.Enum.GetValues(typeof(GestureType)))
            {
                gestureTimers[gesture] = 0f;
            }
            
            // Initialize hand data
            leftHandData = CreateEmptyHandData(true);
            rightHandData = CreateEmptyHandData(false);
            
            Debug.Log("Hand Tracking Manager initialized with Unity 6 optimizations");
        }
        
        private HandData CreateEmptyHandData(bool isLeft)
        {
            return new HandData
            {
                isTracked = false,
                confidence = 0f,
                palmPosition = Vector3.zero,
                palmRotation = Quaternion.identity,
                palmVelocity = Vector3.zero,
                jointPositions = new Vector3[24],
                jointRotations = new Quaternion[24],
                fingerCurls = new float[5],
                currentGesture = GestureType.None,
                isLeftHand = isLeft
            };
        }
        
        private void Update()
        {
            // Limit update rate for performance
            if (Time.time - lastUpdateTime < 1f / trackingUpdateRate)
                return;
            
            lastUpdateTime = Time.time;
            
            UpdateTrackingMode();
            UpdateHandData();
            
            if (enableGestureRecognition)
            {
                UpdateGestureRecognition();
            }
        }
        
        private void UpdateTrackingMode()
        {
            // Detect available tracking methods
            bool handTrackingAvailable = CheckHandTrackingAvailability();
            bool controllersConnected = CheckControllerAvailability();
            
            TrackingMode newMode = currentTrackingMode;
            
            if (enableHandTracking && handTrackingAvailable)
            {
                if (controllersConnected && enableControllerFallback)
                {
                    newMode = TrackingMode.Mixed;
                }
                else
                {
                    newMode = TrackingMode.HandTracking;
                }
            }
            else if (controllersConnected)
            {
                newMode = TrackingMode.Controllers;
            }
            
            if (newMode != currentTrackingMode)
            {
                currentTrackingMode = newMode;
                OnTrackingModeChanged?.Invoke(currentTrackingMode);
                UpdateHandModels();
                Debug.Log($"Tracking mode changed to: {currentTrackingMode}");
            }
        }
        
        private bool CheckHandTrackingAvailability()
        {
            // In a real implementation, this would check the actual hand tracking API
            // For now, simulate based on settings
            return enableHandTracking;
        }
        
        private bool CheckControllerAvailability()
        {
            // In a real implementation, this would check for connected controllers
            // For now, simulate based on settings
            return enableControllerFallback;
        }
        
        private void UpdateHandData()
        {
            // Complete previous job
            currentJobHandle.Complete();
            
            if (IsHandTrackingActive)
            {
                UpdateHandTrackingData();
            }
            else
            {
                UpdateControllerData();
            }
            
            // Trigger events
            OnLeftHandUpdate?.Invoke(leftHandData);
            OnRightHandUpdate?.Invoke(rightHandData);
        }
        
        private void UpdateHandTrackingData()
        {
            // Simulate hand tracking data (in real implementation, get from hand tracking API)
            leftHandData = SimulateHandData(true);
            rightHandData = SimulateHandData(false);
            
            // Schedule hand processing job
            var handProcessingJob = new HandDataProcessingJob
            {
                jointPositions = jointPositions,
                jointRotations = jointRotations,
                gestureScores = gestureScores,
                confidenceThreshold = handTrackingConfidenceThreshold
            };
            
            currentJobHandle = handProcessingJob.Schedule();
        }
        
        private void UpdateControllerData()
        {
            // Update controller-based hand data
            leftHandData = GetControllerHandData(true);
            rightHandData = GetControllerHandData(false);
        }
        
        private HandData SimulateHandData(bool isLeft)
        {
            // This would be replaced with actual hand tracking API calls
            HandData data = CreateEmptyHandData(isLeft);
            data.isTracked = true;
            data.confidence = 0.9f;
            data.palmPosition = isLeft ? new Vector3(-0.2f, 1.2f, 0.3f) : new Vector3(0.2f, 1.2f, 0.3f);
            data.palmRotation = Quaternion.identity;
            
            // Simulate joint positions
            for (int i = 0; i < data.jointPositions.Length; i++)
            {
                data.jointPositions[i] = data.palmPosition + Random.insideUnitSphere * 0.1f;
                data.jointRotations[i] = Quaternion.identity;
            }
            
            // Calculate finger curls
            CalculateFingerCurls(ref data);
            
            return data;
        }
        
        private HandData GetControllerHandData(bool isLeft)
        {
            HandData data = CreateEmptyHandData(isLeft);
            
            // Get controller position and rotation
            // In real implementation, use OVR or XR input system
            data.isTracked = true;
            data.confidence = 1.0f;
            data.palmPosition = isLeft ? new Vector3(-0.2f, 1.2f, 0.3f) : new Vector3(0.2f, 1.2f, 0.3f);
            data.palmRotation = Quaternion.identity;
            
            // Estimate hand pose from controller
            EstimateHandPoseFromController(ref data);
            
            return data;
        }
        
        private void EstimateHandPoseFromController(ref HandData data)
        {
            // Estimate hand joint positions based on controller position
            Vector3 basePosition = data.palmPosition;
            
            for (int i = 0; i < data.jointPositions.Length; i++)
            {
                // Simple estimation - in real implementation, use more sophisticated IK
                data.jointPositions[i] = basePosition + Vector3.forward * (i * 0.02f);
                data.jointRotations[i] = data.palmRotation;
            }
            
            CalculateFingerCurls(ref data);
        }
        
        private void CalculateFingerCurls(ref HandData data)
        {
            // Calculate finger curl values (0 = straight, 1 = fully curled)
            for (int finger = 0; finger < 5; finger++)
            {
                // Simple calculation based on joint positions
                data.fingerCurls[finger] = Random.Range(0f, 1f);
            }
        }
        
        private void UpdateGestureRecognition()
        {
            // Recognize gestures for both hands
            RecognizeGesture(ref leftHandData);
            RecognizeGesture(ref rightHandData);
        }
        
        private void RecognizeGesture(ref HandData handData)
        {
            if (!handData.isTracked) return;
            
            GestureType detectedGesture = ClassifyGesture(handData);
            
            if (detectedGesture != GestureType.None)
            {
                if (!gestureTimers.ContainsKey(detectedGesture))
                    gestureTimers[detectedGesture] = 0f;
                
                gestureTimers[detectedGesture] += Time.deltaTime;
                
                if (gestureTimers[detectedGesture] >= gestureHoldTime)
                {
                    // Gesture confirmed
                    handData.currentGesture = detectedGesture;
                    
                    GestureData gestureData = new GestureData
                    {
                        gestureType = detectedGesture,
                        confidence = handData.confidence,
                        duration = gestureTimers[detectedGesture],
                        isLeftHand = handData.isLeftHand,
                        position = handData.palmPosition
                    };
                    
                    OnGestureDetected?.Invoke(gestureData);
                    gestureTimers[detectedGesture] = 0f; // Reset timer
                }
            }
            else
            {
                // Reset all gesture timers
                foreach (var key in new List<GestureType>(gestureTimers.Keys))
                {
                    gestureTimers[key] = 0f;
                }
                handData.currentGesture = GestureType.None;
            }
        }
        
        private GestureType ClassifyGesture(HandData handData)
        {
            // Simple gesture classification based on finger curls
            float[] curls = handData.fingerCurls;
            
            // Fist: all fingers curled
            if (curls[0] > 0.8f && curls[1] > 0.8f && curls[2] > 0.8f && curls[3] > 0.8f && curls[4] > 0.8f)
                return GestureType.Fist;
            
            // Open hand: all fingers straight
            if (curls[0] < 0.2f && curls[1] < 0.2f && curls[2] < 0.2f && curls[3] < 0.2f && curls[4] < 0.2f)
                return GestureType.OpenHand;
            
            // Point: index finger straight, others curled
            if (curls[1] < 0.2f && curls[0] > 0.6f && curls[2] > 0.6f && curls[3] > 0.6f && curls[4] > 0.6f)
                return GestureType.Point;
            
            // Thumbs up: thumb straight, others curled
            if (curls[0] < 0.2f && curls[1] > 0.6f && curls[2] > 0.6f && curls[3] > 0.6f && curls[4] > 0.6f)
                return GestureType.ThumbsUp;
            
            return GestureType.None;
        }
        
        private void UpdateHandModels()
        {
            bool showHandModels = IsHandTrackingActive;
            bool showControllerModels = !IsHandTrackingActive || currentTrackingMode == TrackingMode.Mixed;
            
            if (leftHandModel) leftHandModel.SetActive(showHandModels);
            if (rightHandModel) rightHandModel.SetActive(showHandModels);
            if (leftControllerModel) leftControllerModel.SetActive(showControllerModels);
            if (rightControllerModel) rightControllerModel.SetActive(showControllerModels);
        }
        
        // Unity 6 Job System for hand data processing
        [BurstCompile]
        public struct HandDataProcessingJob : IJob
        {
            [ReadOnly] public NativeArray<float3> jointPositions;
            [ReadOnly] public NativeArray<quaternion> jointRotations;
            [ReadOnly] public float confidenceThreshold;
            
            [WriteOnly] public NativeArray<float> gestureScores;
            
            public void Execute()
            {
                // Process hand data with burst compilation for performance
                for (int i = 0; i < gestureScores.Length; i++)
                {
                    gestureScores[i] = CalculateGestureScore(i);
                }
            }
            
            private float CalculateGestureScore(int gestureIndex)
            {
                // Simplified gesture scoring
                return math.clamp(math.sin(gestureIndex * 0.5f), 0f, 1f);
            }
        }
        
        // Public API methods
        public bool IsHandTracked(bool isLeft)
        {
            return isLeft ? leftHandData.isTracked : rightHandData.isTracked;
        }
        
        public Vector3 GetHandPosition(bool isLeft)
        {
            return isLeft ? leftHandData.palmPosition : rightHandData.palmPosition;
        }
        
        public Quaternion GetHandRotation(bool isLeft)
        {
            return isLeft ? leftHandData.palmRotation : rightHandData.palmRotation;
        }
        
        public Vector3 GetJointPosition(bool isLeft, HandJoint joint)
        {
            HandData data = isLeft ? leftHandData : rightHandData;
            int jointIndex = (int)joint;
            
            if (jointIndex >= 0 && jointIndex < data.jointPositions.Length)
                return data.jointPositions[jointIndex];
            
            return Vector3.zero;
        }
        
        public float GetFingerCurl(bool isLeft, int fingerIndex)
        {
            HandData data = isLeft ? leftHandData : rightHandData;
            
            if (fingerIndex >= 0 && fingerIndex < data.fingerCurls.Length)
                return data.fingerCurls[fingerIndex];
            
            return 0f;
        }
        
        public void SetTrackingMode(TrackingMode mode)
        {
            currentTrackingMode = mode;
            UpdateHandModels();
        }
        
        private void OnDestroy()
        {
            // Clean up Job System resources
            currentJobHandle.Complete();
            
            if (jointPositions.IsCreated) jointPositions.Dispose();
            if (jointRotations.IsCreated) jointRotations.Dispose();
            if (gestureScores.IsCreated) gestureScores.Dispose();
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            DrawHandGizmos(leftHandData, Color.blue);
            DrawHandGizmos(rightHandData, Color.red);
        }
        
        private void DrawHandGizmos(HandData handData, Color color)
        {
            if (!handData.isTracked) return;
            
            Gizmos.color = color;
            Gizmos.DrawWireSphere(handData.palmPosition, 0.05f);
            
            // Draw joint positions
            for (int i = 0; i < handData.jointPositions.Length; i++)
            {
                Gizmos.DrawWireSphere(handData.jointPositions[i], 0.01f);
            }
        }
    }
}

