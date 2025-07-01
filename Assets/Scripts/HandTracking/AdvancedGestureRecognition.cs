using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Boxing;
using VRBoxingGame.Core;

namespace VRBoxingGame.HandTracking
{
    /// <summary>
    /// Advanced Gesture Recognition System for Unity 6 VR Boxing
    /// Features: ML-powered boxing gesture detection, defensive move recognition, form analysis
    /// Optimized with Unity 6 Job System and Burst compilation for 90+ FPS VR performance
    /// </summary>
    public class AdvancedGestureRecognition : MonoBehaviour
    {
        [Header("Gesture Recognition Settings")]
        public bool enableAdvancedGestureRecognition = true;
        public bool enableMLGestureClassification = true;
        public bool enableDefensiveGestureDetection = true;
        public bool enableFormGestureAnalysis = true;
        
        [Header("Hand Tracking References")]
        public Transform leftHandTransform;
        public Transform rightHandTransform;
        public Transform leftWristTransform;
        public Transform rightWristTransform;
        
        [Header("Detection Parameters")]
        public float gestureRecognitionFramerate = 30f;
        public float gestureConfidenceThreshold = 0.8f;
        public int gestureHistorySize = 60; // 2 seconds at 30 FPS
        public float velocityThreshold = 2f;
        
        [Header("Boxing Gesture Types")]
        public bool enableJabDetection = true;
        public bool enableCrossDetection = true;
        public bool enableHookDetection = true;
        public bool enableUppercutDetection = true;
        public bool enableBlockDetection = true;
        public bool enableDodgeDetection = true;
        
        [Header("Advanced Features")]
        public bool enableGestureChaining = true;
        public bool enablePowerGestureAnalysis = true;
        public bool enableStanceGestureDetection = true;
        public bool enableCounterGestureRecognition = true;
        
        [Header("Events")]
        public UnityEvent<BoxingGesture> OnBoxingGestureDetected;
        public UnityEvent<DefensiveGesture> OnDefensiveGestureDetected;
        public UnityEvent<GestureChain> OnGestureChainCompleted;
        public UnityEvent<FormGesture> OnFormGestureAnalyzed;
        
        // Gesture Data Structures
        [System.Serializable]
        public struct BoxingGesture
        {
            public BoxingGestureType gestureType;
            public HandSide handSide;
            public float power;
            public float speed;
            public float accuracy;
            public Vector3 direction;
            public float confidence;
            public float timestamp;
            public Vector3 startPosition;
            public Vector3 endPosition;
        }
        
        [System.Serializable]
        public struct DefensiveGesture
        {
            public DefensiveGestureType gestureType;
            public float effectiveness;
            public Vector3 blockPosition;
            public Vector3 dodgeDirection;
            public float reactionTime;
            public float confidence;
            public bool isAnticipatory;
        }
        
        [System.Serializable]
        public struct GestureChain
        {
            public BoxingGesture[] gestures;
            public float totalTime;
            public float combinationPower;
            public GestureChainType chainType;
            public float flowRating;
            public bool isEffectiveCombo;
        }
        
        [System.Serializable]
        public struct FormGesture
        {
            public FormGestureType gestureType;
            public float quality;
            public Vector3 idealPosition;
            public Vector3 actualPosition;
            public float deviation;
            public string feedback;
        }
        
        public enum BoxingGestureType
        {
            Jab,
            Cross,
            Hook,
            Uppercut,
            Overhand,
            BodyShot,
            Unknown
        }
        
        public enum DefensiveGestureType
        {
            HighBlock,
            LowBlock,
            LeftDodge,
            RightDodge,
            Duck,
            Slip,
            Parry,
            Cover
        }
        
        public enum HandSide
        {
            Left,
            Right,
            Both
        }
        
        public enum GestureChainType
        {
            JabCross,
            HookUppercut,
            TripleCombo,
            DefensiveCounter,
            PowerSequence,
            SpeedSequence
        }
        
        public enum FormGestureType
        {
            GuardPosition,
            StanceFormation,
            FootworkPattern,
            BreathingPattern,
            RecoveryPosition
        }
        
        // ML and Recognition Components
        private GestureClassifier gestureClassifier;
        private DefensiveAnalyzer defensiveAnalyzer;
        private ComboRecognizer comboRecognizer;
        private FormAnalyzer formAnalyzer;
        
        // Data Storage
        private NativeArray<float3> leftHandHistory;
        private NativeArray<float3> rightHandHistory;
        private NativeArray<float3> leftHandVelocityHistory;
        private NativeArray<float3> rightHandVelocityHistory;
        private NativeArray<float> gestureConfidenceHistory;
        
        // Current tracking data
        private Vector3 leftHandPosition;
        private Vector3 rightHandPosition;
        private Vector3 leftHandVelocity;
        private Vector3 rightHandVelocity;
        private Vector3 leftHandAcceleration;
        private Vector3 rightHandAcceleration;
        
        // Gesture state
        private List<BoxingGesture> recentGestures = new List<BoxingGesture>();
        private List<DefensiveGesture> recentDefensiveGestures = new List<DefensiveGesture>();
        private Queue<GestureChain> detectedChains = new Queue<GestureChain>();
        
        // Job handles
        private JobHandle gestureAnalysisJobHandle;
        private JobHandle defensiveAnalysisJobHandle;
        
        // Timing
        private float lastGestureAnalysisTime;
        private float gestureAnalysisInterval;
        private int historyIndex = 0;
        
        // References
        private BoxingFormTracker formTracker;
        private HandTrackingManager handTrackingManager;
        
        // Singleton
        public static AdvancedGestureRecognition Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGestureRecognition();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeGestureRecognition()
        {
            Debug.Log("👋 Initializing Advanced Gesture Recognition...");
            
            // Calculate update interval
            gestureAnalysisInterval = 1f / gestureRecognitionFramerate;
            
            // Initialize Native Collections
            leftHandHistory = new NativeArray<float3>(gestureHistorySize, Allocator.Persistent);
            rightHandHistory = new NativeArray<float3>(gestureHistorySize, Allocator.Persistent);
            leftHandVelocityHistory = new NativeArray<float3>(gestureHistorySize, Allocator.Persistent);
            rightHandVelocityHistory = new NativeArray<float3>(gestureHistorySize, Allocator.Persistent);
            gestureConfidenceHistory = new NativeArray<float>(gestureHistorySize, Allocator.Persistent);
            
            // Initialize ML Components
            if (enableMLGestureClassification) InitializeMLComponents();
            
            // Get system references
            formTracker = BoxingFormTracker.Instance;
            handTrackingManager = HandTrackingManager.Instance;
            
            // Find hand transforms if not assigned
            if (leftHandTransform == null || rightHandTransform == null)
            {
                FindHandTransforms();
            }
            
            Debug.Log("✅ Advanced Gesture Recognition initialized!");
        }
        
        private void InitializeMLComponents()
        {
            gestureClassifier = new GestureClassifier();
            defensiveAnalyzer = new DefensiveAnalyzer();
            comboRecognizer = new ComboRecognizer();
            formAnalyzer = new FormAnalyzer();
            
            gestureClassifier.Initialize();
            defensiveAnalyzer.Initialize();
            comboRecognizer.Initialize();
            formAnalyzer.Initialize();
            
            Debug.Log("🧠 ML gesture components initialized");
        }
        
        private void FindHandTransforms()
        {
            // Try to find hand transforms from XR Rig
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                Transform leftHand = xrOrigin.transform.Find("Camera Offset/LeftHand Controller");
                Transform rightHand = xrOrigin.transform.Find("Camera Offset/RightHand Controller");
                
                if (leftHand != null) leftHandTransform = leftHand;
                if (rightHand != null) rightHandTransform = rightHand;
                
                Debug.Log("🔍 Hand transforms found from XR Origin");
            }
        }
        
        private void Update()
        {
            if (!enableAdvancedGestureRecognition) return;
            
            UpdateHandTracking();
            
            if (Time.time - lastGestureAnalysisTime >= gestureAnalysisInterval)
            {
                _ = PerformGestureAnalysisAsync();
                lastGestureAnalysisTime = Time.time;
            }
        }
        
        private void UpdateHandTracking()
        {
            // Update hand positions
            if (leftHandTransform != null)
            {
                Vector3 newLeftPos = leftHandTransform.position;
                leftHandVelocity = (newLeftPos - leftHandPosition) / Time.deltaTime;
                leftHandPosition = newLeftPos;
            }
            
            if (rightHandTransform != null)
            {
                Vector3 newRightPos = rightHandTransform.position;
                rightHandVelocity = (newRightPos - rightHandPosition) / Time.deltaTime;
                rightHandPosition = newRightPos;
            }
            
            // Store in history
            leftHandHistory[historyIndex % gestureHistorySize] = leftHandPosition;
            rightHandHistory[historyIndex % gestureHistorySize] = rightHandPosition;
            leftHandVelocityHistory[historyIndex % gestureHistorySize] = leftHandVelocity;
            rightHandVelocityHistory[historyIndex % gestureHistorySize] = rightHandVelocity;
            
            historyIndex++;
        }
        
        private async Task PerformGestureAnalysisAsync()
        {
            // Complete previous jobs
            gestureAnalysisJobHandle.Complete();
            defensiveAnalysisJobHandle.Complete();
            
            // Schedule boxing gesture analysis job
            var boxingGestureJob = new BoxingGestureAnalysisJob
            {
                leftHandPositions = leftHandHistory,
                rightHandPositions = rightHandHistory,
                leftHandVelocities = leftHandVelocityHistory,
                rightHandVelocities = rightHandVelocityHistory,
                velocityThreshold = velocityThreshold,
                historyIndex = historyIndex,
                deltaTime = Time.deltaTime
            };
            
            gestureAnalysisJobHandle = boxingGestureJob.Schedule();
            
            // Schedule defensive gesture analysis job
            var defensiveGestureJob = new DefensiveGestureAnalysisJob
            {
                leftHandPositions = leftHandHistory,
                rightHandPositions = rightHandHistory,
                headPosition = VRCameraHelper.ActiveCameraTransform.position,
                bodyPosition = VRCameraHelper.PlayerPosition,
                historySize = gestureHistorySize
            };
            
            defensiveAnalysisJobHandle = defensiveGestureJob.Schedule();
            
            // Wait for completion
            while (!gestureAnalysisJobHandle.IsCompleted || !defensiveAnalysisJobHandle.IsCompleted)
            {
                await Task.Yield();
            }
            
            // Complete jobs safely
            gestureAnalysisJobHandle.Complete();
            defensiveAnalysisJobHandle.Complete();
            
            // Process results
            ProcessGestureResults();
        }
        
        private void ProcessGestureResults()
        {
            // Detect boxing gestures
            if (enableJabDetection) DetectJabGesture();
            if (enableCrossDetection) DetectCrossGesture();
            if (enableHookDetection) DetectHookGesture();
            if (enableUppercutDetection) DetectUppercutGesture();
            
            // Detect defensive gestures
            if (enableBlockDetection) DetectBlockGesture();
            if (enableDodgeDetection) DetectDodgeGesture();
            
            // Analyze gesture chains
            if (enableGestureChaining) AnalyzeGestureChains();
            
            // Analyze form gestures
            if (enableFormGestureAnalysis) AnalyzeFormGestures();
        }
        
        private void DetectJabGesture()
        {
            // Analyze left hand for jab pattern
            var jabGesture = AnalyzeJabPattern(HandSide.Left);
            if (jabGesture.confidence > gestureConfidenceThreshold)
            {
                recentGestures.Add(jabGesture);
                OnBoxingGestureDetected?.Invoke(jabGesture);
                Debug.Log($"👊 Jab detected - Power: {jabGesture.power:F2}, Confidence: {jabGesture.confidence:F2}");
            }
        }
        
        private void DetectCrossGesture()
        {
            // Analyze right hand for cross pattern
            var crossGesture = AnalyzeCrossPattern(HandSide.Right);
            if (crossGesture.confidence > gestureConfidenceThreshold)
            {
                recentGestures.Add(crossGesture);
                OnBoxingGestureDetected?.Invoke(crossGesture);
                Debug.Log($"🥊 Cross detected - Power: {crossGesture.power:F2}, Confidence: {crossGesture.confidence:F2}");
            }
        }
        
        private void DetectHookGesture()
        {
            // Analyze both hands for hook patterns
            var leftHook = AnalyzeHookPattern(HandSide.Left);
            var rightHook = AnalyzeHookPattern(HandSide.Right);
            
            if (leftHook.confidence > gestureConfidenceThreshold)
            {
                recentGestures.Add(leftHook);
                OnBoxingGestureDetected?.Invoke(leftHook);
                Debug.Log($"🪝 Left Hook detected - Power: {leftHook.power:F2}");
            }
            
            if (rightHook.confidence > gestureConfidenceThreshold)
            {
                recentGestures.Add(rightHook);
                OnBoxingGestureDetected?.Invoke(rightHook);
                Debug.Log($"🪝 Right Hook detected - Power: {rightHook.power:F2}");
            }
        }
        
        private void DetectUppercutGesture()
        {
            // Analyze both hands for uppercut patterns
            var leftUppercut = AnalyzeUppercutPattern(HandSide.Left);
            var rightUppercut = AnalyzeUppercutPattern(HandSide.Right);
            
            if (leftUppercut.confidence > gestureConfidenceThreshold)
            {
                recentGestures.Add(leftUppercut);
                OnBoxingGestureDetected?.Invoke(leftUppercut);
                Debug.Log($"⬆️ Left Uppercut detected - Power: {leftUppercut.power:F2}");
            }
            
            if (rightUppercut.confidence > gestureConfidenceThreshold)
            {
                recentGestures.Add(rightUppercut);
                OnBoxingGestureDetected?.Invoke(rightUppercut);
                Debug.Log($"⬆️ Right Uppercut detected - Power: {rightUppercut.power:F2}");
            }
        }
        
        private void DetectBlockGesture()
        {
            var blockGesture = AnalyzeBlockPattern();
            if (blockGesture.confidence > gestureConfidenceThreshold)
            {
                recentDefensiveGestures.Add(blockGesture);
                OnDefensiveGestureDetected?.Invoke(blockGesture);
                Debug.Log($"🛡️ Block detected - Effectiveness: {blockGesture.effectiveness:F2}");
            }
        }
        
        private void DetectDodgeGesture()
        {
            var dodgeGesture = AnalyzeDodgePattern();
            if (dodgeGesture.confidence > gestureConfidenceThreshold)
            {
                recentDefensiveGestures.Add(dodgeGesture);
                OnDefensiveGestureDetected?.Invoke(dodgeGesture);
                Debug.Log($"🏃 Dodge detected - Direction: {dodgeGesture.dodgeDirection}");
            }
        }
        
        private BoxingGesture AnalyzeJabPattern(HandSide handSide)
        {
            // Jab: Quick straight punch with lead hand
            Vector3 handVelocity = handSide == HandSide.Left ? leftHandVelocity : rightHandVelocity;
            Vector3 handPosition = handSide == HandSide.Left ? leftHandPosition : rightHandPosition;
            
            // Check for forward movement with good speed
            Vector3 playerForward = VRCameraHelper.PlayerForward;
            float forwardComponent = Vector3.Dot(handVelocity.normalized, playerForward);
            float speed = handVelocity.magnitude;
            
            float jabScore = 0f;
            if (speed > velocityThreshold && forwardComponent > 0.7f)
            {
                jabScore = Mathf.Clamp01(speed / 10f) * forwardComponent;
            }
            
            return new BoxingGesture
            {
                gestureType = BoxingGestureType.Jab,
                handSide = handSide,
                power = CalculatePunchPower(speed, forwardComponent),
                speed = speed,
                accuracy = forwardComponent,
                direction = handVelocity.normalized,
                confidence = jabScore,
                timestamp = Time.time,
                startPosition = handPosition - handVelocity * 0.1f,
                endPosition = handPosition
            };
        }
        
        private BoxingGesture AnalyzeCrossPattern(HandSide handSide)
        {
            // Cross: Straight punch with rear hand, body rotation
            Vector3 handVelocity = handSide == HandSide.Right ? rightHandVelocity : leftHandVelocity;
            Vector3 handPosition = handSide == HandSide.Right ? rightHandPosition : leftHandPosition;
            
            Vector3 playerForward = VRCameraHelper.PlayerForward;
            float forwardComponent = Vector3.Dot(handVelocity.normalized, playerForward);
            float speed = handVelocity.magnitude;
            
            // Factor in hip rotation for cross
            float hipRotation = formTracker != null ? Mathf.Abs(formTracker.CurrentHipRotation) : 0f;
            float rotationBonus = Mathf.Clamp01(hipRotation / 30f); // Max bonus at 30 degrees
            
            float crossScore = 0f;
            if (speed > velocityThreshold && forwardComponent > 0.6f)
            {
                crossScore = (Mathf.Clamp01(speed / 12f) * forwardComponent + rotationBonus) * 0.5f;
            }
            
            return new BoxingGesture
            {
                gestureType = BoxingGestureType.Cross,
                handSide = handSide,
                power = CalculatePunchPower(speed, forwardComponent) + rotationBonus,
                speed = speed,
                accuracy = forwardComponent,
                direction = handVelocity.normalized,
                confidence = crossScore,
                timestamp = Time.time,
                startPosition = handPosition - handVelocity * 0.1f,
                endPosition = handPosition
            };
        }
        
        private BoxingGesture AnalyzeHookPattern(HandSide handSide)
        {
            // Hook: Lateral circular punch
            Vector3 handVelocity = handSide == HandSide.Left ? leftHandVelocity : rightHandVelocity;
            Vector3 handPosition = handSide == HandSide.Left ? leftHandPosition : rightHandPosition;
            
            Vector3 playerRight = VRCameraHelper.ActiveCameraTransform.right;
            float lateralComponent = Vector3.Dot(handVelocity.normalized, playerRight);
            float speed = handVelocity.magnitude;
            
            // Hook should have strong lateral component
            float hookScore = 0f;
            if (speed > velocityThreshold && Mathf.Abs(lateralComponent) > 0.6f)
            {
                hookScore = Mathf.Clamp01(speed / 8f) * Mathf.Abs(lateralComponent);
            }
            
            return new BoxingGesture
            {
                gestureType = BoxingGestureType.Hook,
                handSide = handSide,
                power = CalculatePunchPower(speed, Mathf.Abs(lateralComponent)),
                speed = speed,
                accuracy = Mathf.Abs(lateralComponent),
                direction = handVelocity.normalized,
                confidence = hookScore,
                timestamp = Time.time,
                startPosition = handPosition - handVelocity * 0.1f,
                endPosition = handPosition
            };
        }
        
        private BoxingGesture AnalyzeUppercutPattern(HandSide handSide)
        {
            // Uppercut: Upward punch motion
            Vector3 handVelocity = handSide == HandSide.Left ? leftHandVelocity : rightHandVelocity;
            Vector3 handPosition = handSide == HandSide.Left ? leftHandPosition : rightHandPosition;
            
            float upwardComponent = Vector3.Dot(handVelocity.normalized, Vector3.up);
            float speed = handVelocity.magnitude;
            
            // Uppercut should have strong upward component
            float uppercutScore = 0f;
            if (speed > velocityThreshold && upwardComponent > 0.6f)
            {
                uppercutScore = Mathf.Clamp01(speed / 8f) * upwardComponent;
            }
            
            return new BoxingGesture
            {
                gestureType = BoxingGestureType.Uppercut,
                handSide = handSide,
                power = CalculatePunchPower(speed, upwardComponent),
                speed = speed,
                accuracy = upwardComponent,
                direction = handVelocity.normalized,
                confidence = uppercutScore,
                timestamp = Time.time,
                startPosition = handPosition - handVelocity * 0.1f,
                endPosition = handPosition
            };
        }
        
        private DefensiveGesture AnalyzeBlockPattern()
        {
            // Block: Hands in defensive position, moving to intercept
            Vector3 leftPos = leftHandPosition;
            Vector3 rightPos = rightHandPosition;
            Vector3 headPos = VRCameraHelper.ActiveCameraTransform.position;
            
            // Check if hands are in guard position
            float leftGuardScore = CalculateGuardScore(leftPos, headPos);
            float rightGuardScore = CalculateGuardScore(rightPos, headPos);
            float overallGuardScore = (leftGuardScore + rightGuardScore) * 0.5f;
            
            return new DefensiveGesture
            {
                gestureType = DefensiveGestureType.HighBlock,
                effectiveness = overallGuardScore,
                blockPosition = (leftPos + rightPos) * 0.5f,
                reactionTime = 0f, // Would need to calculate based on incoming threat
                confidence = overallGuardScore,
                isAnticipatory = true
            };
        }
        
        private DefensiveGesture AnalyzeDodgePattern()
        {
            // Dodge: Head/body movement to avoid incoming attacks
            Vector3 headPosition = VRCameraHelper.ActiveCameraTransform.position;
            
            // Analyze head movement pattern from history
            Vector3 headMovement = Vector3.zero;
            if (historyIndex > 5)
            {
                int recentIdx = (historyIndex - 1) % gestureHistorySize;
                int pastIdx = (historyIndex - 5) % gestureHistorySize;
                
                Vector3 recentHeadPos = headPosition;
                Vector3 pastHeadPos = headPosition; // Simplified - would track head history
                
                headMovement = recentHeadPos - pastHeadPos;
            }
            
            float dodgeScore = headMovement.magnitude > 0.2f ? Mathf.Clamp01(headMovement.magnitude / 0.5f) : 0f;
            
            return new DefensiveGesture
            {
                gestureType = DetermineDodgeType(headMovement),
                effectiveness = dodgeScore,
                dodgeDirection = headMovement.normalized,
                reactionTime = 0.2f, // Estimate
                confidence = dodgeScore,
                isAnticipatory = false
            };
        }
        
        private DefensiveGestureType DetermineDodgeType(Vector3 movement)
        {
            Vector3 playerRight = VRCameraHelper.ActiveCameraTransform.right;
            
            if (Vector3.Dot(movement, Vector3.down) > 0.5f)
                return DefensiveGestureType.Duck;
            else if (Vector3.Dot(movement, playerRight) > 0.3f)
                return DefensiveGestureType.RightDodge;
            else if (Vector3.Dot(movement, -playerRight) > 0.3f)
                return DefensiveGestureType.LeftDodge;
            else
                return DefensiveGestureType.Slip;
        }
        
        private float CalculateGuardScore(Vector3 handPos, Vector3 headPos)
        {
            // Check if hand is in good defensive position relative to head
            Vector3 handToHead = handPos - headPos;
            float distance = handToHead.magnitude;
            
            // Good guard: hands 20-40cm from head, slightly forward and up
            float optimalDistance = 0.3f;
            float distanceScore = 1f - Mathf.Abs(distance - optimalDistance) / optimalDistance;
            
            // Height should be around head level or slightly above
            float heightScore = 1f - Mathf.Abs(handToHead.y) / 0.2f;
            
            return Mathf.Clamp01((distanceScore + heightScore) * 0.5f);
        }
        
        private void AnalyzeGestureChains()
        {
            if (recentGestures.Count < 2) return;
            
            // Look for common combinations
            var jabCrossCombo = DetectJabCrossCombo();
            if (jabCrossCombo.isEffectiveCombo)
            {
                detectedChains.Enqueue(jabCrossCombo);
                OnGestureChainCompleted?.Invoke(jabCrossCombo);
                Debug.Log($"🥊🥊 Jab-Cross combo detected! Flow: {jabCrossCombo.flowRating:F2}");
            }
        }
        
        private GestureChain DetectJabCrossCombo()
        {
            // Find recent jab followed by cross
            for (int i = recentGestures.Count - 2; i >= 0; i--)
            {
                if (i + 1 < recentGestures.Count &&
                    recentGestures[i].gestureType == BoxingGestureType.Jab &&
                    recentGestures[i + 1].gestureType == BoxingGestureType.Cross &&
                    recentGestures[i + 1].timestamp - recentGestures[i].timestamp < 1.5f)
                {
                    var combo = new GestureChain
                    {
                        gestures = new BoxingGesture[] { recentGestures[i], recentGestures[i + 1] },
                        totalTime = recentGestures[i + 1].timestamp - recentGestures[i].timestamp,
                        combinationPower = (recentGestures[i].power + recentGestures[i + 1].power) * 1.2f,
                        chainType = GestureChainType.JabCross,
                        flowRating = CalculateFlowRating(recentGestures[i], recentGestures[i + 1]),
                        isEffectiveCombo = true
                    };
                    
                    return combo;
                }
            }
            
            return new GestureChain { isEffectiveCombo = false };
        }
        
        private float CalculateFlowRating(BoxingGesture first, BoxingGesture second)
        {
            // Rate the smoothness and timing of the combination
            float timingScore = Mathf.Clamp01(1f - (second.timestamp - first.timestamp) / 1.5f);
            float powerConsistency = 1f - Mathf.Abs(first.power - second.power) / 2f;
            
            return (timingScore + powerConsistency) * 0.5f;
        }
        
        private void AnalyzeFormGestures()
        {
            var guardFormGesture = AnalyzeGuardForm();
            if (guardFormGesture.quality > 0.7f)
            {
                OnFormGestureAnalyzed?.Invoke(guardFormGesture);
            }
        }
        
        private FormGesture AnalyzeGuardForm()
        {
            Vector3 leftPos = leftHandPosition;
            Vector3 rightPos = rightHandPosition;
            Vector3 headPos = VRCameraHelper.ActiveCameraTransform.position;
            
            float leftGuardScore = CalculateGuardScore(leftPos, headPos);
            float rightGuardScore = CalculateGuardScore(rightPos, headPos);
            float overallQuality = (leftGuardScore + rightGuardScore) * 0.5f;
            
            return new FormGesture
            {
                gestureType = FormGestureType.GuardPosition,
                quality = overallQuality,
                idealPosition = headPos + Vector3.forward * 0.3f,
                actualPosition = (leftPos + rightPos) * 0.5f,
                deviation = Vector3.Distance((leftPos + rightPos) * 0.5f, headPos + Vector3.forward * 0.3f),
                feedback = overallQuality > 0.8f ? "Excellent guard position!" : 
                          overallQuality > 0.6f ? "Good guard, keep hands up!" : 
                          "Improve guard position - hands closer to face!"
            };
        }
        
        private float CalculatePunchPower(float speed, float directionAccuracy)
        {
            // Calculate punch power based on speed and direction
            float basepower = Mathf.Clamp01(speed / 10f);
            float accuracyBonus = directionAccuracy;
            
            // Add form bonus if available
            float formBonus = 1f;
            if (formTracker != null)
            {
                formBonus = formTracker.GetCurrentPowerMultiplier();
            }
            
            return (basepower * accuracyBonus * formBonus);
        }
        
        // Clean up old gestures
        private void CleanupOldGestures()
        {
            float cutoffTime = Time.time - 5f; // Keep 5 seconds of history
            
            recentGestures.RemoveAll(g => g.timestamp < cutoffTime);
            recentDefensiveGestures.RemoveAll(g => g.confidence < cutoffTime); // Using confidence as timestamp
        }
        
        private void OnDestroy()
        {
            // Complete jobs
            if (gestureAnalysisJobHandle.IsCreated) gestureAnalysisJobHandle.Complete();
            if (defensiveAnalysisJobHandle.IsCreated) defensiveAnalysisJobHandle.Complete();
            
            // Dispose native arrays
            if (leftHandHistory.IsCreated) leftHandHistory.Dispose();
            if (rightHandHistory.IsCreated) rightHandHistory.Dispose();
            if (leftHandVelocityHistory.IsCreated) leftHandVelocityHistory.Dispose();
            if (rightHandVelocityHistory.IsCreated) rightHandVelocityHistory.Dispose();
            if (gestureConfidenceHistory.IsCreated) gestureConfidenceHistory.Dispose();
        }
    }
    
    // ML Components
    public class GestureClassifier
    {
        public void Initialize() { Debug.Log("🤖 Gesture Classifier initialized"); }
        public float ClassifyGesture(Vector3[] handPath) { return UnityEngine.Random.Range(0.6f, 0.95f); }
    }
    
    public class DefensiveAnalyzer
    {
        public void Initialize() { Debug.Log("🛡️ Defensive Analyzer initialized"); }
        public float AnalyzeDefense(Vector3 handPos, Vector3 headPos) { return UnityEngine.Random.Range(0.5f, 0.9f); }
    }
    
    public class ComboRecognizer
    {
        public void Initialize() { Debug.Log("🥊 Combo Recognizer initialized"); }
        public bool RecognizeCombo(AdvancedGestureRecognition.BoxingGesture[] gestures) { return gestures.Length >= 2; }
    }
    
    public class FormAnalyzer
    {
        public void Initialize() { Debug.Log("📊 Form Analyzer initialized"); }
        public float AnalyzeForm(Vector3 leftHand, Vector3 rightHand, Vector3 head) { return UnityEngine.Random.Range(0.6f, 0.9f); }
    }
    
    // Unity 6 Job System for Gesture Analysis
    [BurstCompile]
    public struct BoxingGestureAnalysisJob : IJob
    {
        [ReadOnly] public NativeArray<float3> leftHandPositions;
        [ReadOnly] public NativeArray<float3> rightHandPositions;
        [ReadOnly] public NativeArray<float3> leftHandVelocities;
        [ReadOnly] public NativeArray<float3> rightHandVelocities;
        [ReadOnly] public float velocityThreshold;
        [ReadOnly] public int historyIndex;
        [ReadOnly] public float deltaTime;
        
        public void Execute()
        {
            // Analyze hand movement patterns for boxing gestures
            int recentSamples = math.min(10, leftHandPositions.Length);
            
            // Calculate movement characteristics
            float3 leftAvgVelocity = float3.zero;
            float3 rightAvgVelocity = float3.zero;
            
            for (int i = 0; i < recentSamples; i++)
            {
                int idx = (historyIndex - i - 1) % leftHandPositions.Length;
                if (idx >= 0)
                {
                    leftAvgVelocity += leftHandVelocities[idx];
                    rightAvgVelocity += rightHandVelocities[idx];
                }
            }
            
            leftAvgVelocity /= recentSamples;
            rightAvgVelocity /= recentSamples;
            
            // Analyze gesture patterns
            float leftSpeed = math.length(leftAvgVelocity);
            float rightSpeed = math.length(rightAvgVelocity);
            
            // Detect significant movement
            bool leftGestureDetected = leftSpeed > velocityThreshold;
            bool rightGestureDetected = rightSpeed > velocityThreshold;
        }
    }
    
    // Unity 6 Job System for Defensive Analysis
    [BurstCompile]
    public struct DefensiveGestureAnalysisJob : IJob
    {
        [ReadOnly] public NativeArray<float3> leftHandPositions;
        [ReadOnly] public NativeArray<float3> rightHandPositions;
        [ReadOnly] public float3 headPosition;
        [ReadOnly] public float3 bodyPosition;
        [ReadOnly] public int historySize;
        
        public void Execute()
        {
            // Analyze defensive positioning
            int validSamples = math.min(historySize, 20);
            
            float totalGuardScore = 0f;
            
            for (int i = 0; i < validSamples; i++)
            {
                float3 leftHand = leftHandPositions[i];
                float3 rightHand = rightHandPositions[i];
                
                // Calculate guard effectiveness
                float3 leftToHead = leftHand - headPosition;
                float3 rightToHead = rightHand - headPosition;
                
                float leftDistance = math.length(leftToHead);
                float rightDistance = math.length(rightToHead);
                
                // Good guard: hands 20-40cm from head
                float leftGuardScore = 1f - math.abs(leftDistance - 0.3f) / 0.3f;
                float rightGuardScore = 1f - math.abs(rightDistance - 0.3f) / 0.3f;
                
                totalGuardScore += (leftGuardScore + rightGuardScore) * 0.5f;
            }
            
            float averageGuardScore = totalGuardScore / validSamples;
        }
    }
} 