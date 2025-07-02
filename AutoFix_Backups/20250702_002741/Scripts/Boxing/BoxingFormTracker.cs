using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using VRBoxingGame.HandTracking;
using VRBoxingGame.Setup;
using System.Collections.Generic;
using System.Collections;
using VRBoxingGame.Core;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Boxing Form Tracker for Unity 6 VR - Monitors proper boxing form including stance, hip movement, and foot positioning
    /// Essential for authentic boxing experience in 360-degree VR environment
    /// </summary>
    public class BoxingFormTracker : MonoBehaviour
    {
        [Header("Boxing Stance Settings")]
        public BoxingStance currentStance = BoxingStance.Orthodox;
        public bool enableAutomaticStanceDetection = true;
        public float stanceChangeDelay = 2f;
        public bool requireProperStance = true;
        
        [Header("Hip Movement Tracking")]
        public bool enableHipTracking = true;
        public float hipRotationThreshold = 15f;
        public float hipPowerMultiplier = 1.5f;
        public Transform hipReference;
        
        [Header("Foot Positioning")]
        public bool enableFootTracking = true;
        public float footSpacingMin = 0.4f;
        public float footSpacingMax = 0.8f;
        public float stanceWidthOptimal = 0.6f;
        public Transform leftFootReference;
        public Transform rightFootReference;
        
        [Header("Weight Distribution")]
        public bool enableWeightTracking = true;
        public float weightShiftThreshold = 0.3f;
        public AnimationCurve weightTransferCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Form Analysis")]
        public float formAnalysisFrequency = 30f; // 30 FPS analysis
        public bool providePowerBonus = true;
        public float maxPowerBonus = 2f;
        public bool provideRealTimeFeedback = true;
        
        [Header("360-Degree Integration")]
        public bool adaptToPlayerRotation = true;
        public float rotationCompensationSmoothing = 5f;
        
        public enum BoxingStance
        {
            Orthodox,    // Left foot forward, right hand dominant
            Southpaw     // Right foot forward, left hand dominant
        }
        
        public enum FormQuality
        {
            Poor,
            Fair,
            Good,
            Excellent,
            Perfect
        }
        
        [System.Serializable]
        public struct BoxingFormData
        {
            public BoxingStance stance;
            public float hipRotation;
            public float footSpacing;
            public float weightDistribution;
            public Vector3 hipVelocity;
            public Vector3 footAlignment;
            public FormQuality overallForm;
            public float powerMultiplier;
            public float accuracyBonus;
            public bool isProperStance;
            public float stanceQuality;
        }
        
        [System.Serializable]
        public struct StanceTransition
        {
            public BoxingStance fromStance;
            public BoxingStance toStance;
            public float transitionTime;
            public float completionPercentage;
            public bool isComplete;
        }
        
        // Events
        public UnityEvent<BoxingFormData> OnFormAnalyzed;
        public UnityEvent<BoxingStance> OnStanceChanged;
        public UnityEvent<float> OnHipPowerGenerated;
        public UnityEvent<FormQuality> OnFormQualityChanged;
        public UnityEvent<StanceTransition> OnStanceTransition;
        
        // Private tracking variables
        private BoxingFormData currentFormData;
        private StanceTransition activeTransition;
        private bool isTransitioning = false;
        private float lastStanceChangeTime;
        private float lastFormAnalysisTime;
        
        // Reference tracking
        private Transform playerHead;
        private Transform playerBody;
        private VR360MovementSystem movementSystem;
        private HandTrackingManager handTracking;
        
        // Hip tracking
        private Vector3 lastHipPosition;
        private Vector3 hipVelocity;
        private float currentHipRotation;
        private Queue<float> hipRotationHistory = new Queue<float>();
        
        // Foot tracking
        private Vector3 lastLeftFootPosition;
        private Vector3 lastRightFootPosition;
        private float currentFootSpacing;
        private float currentWeightDistribution;
        
        // Form quality history
        private Queue<FormQuality> formQualityHistory = new Queue<FormQuality>();
        private FormQuality lastFormQuality = FormQuality.Fair;
        
        public static BoxingFormTracker Instance { get; private set; }
        
        // Properties
        public BoxingStance CurrentStance => currentStance;
        public BoxingFormData CurrentFormData => currentFormData;
        public bool IsTransitioning => isTransitioning;
        public float CurrentHipRotation => currentHipRotation;
        public float CurrentFootSpacing => currentFootSpacing;
        public FormQuality CurrentFormQuality => currentFormData.overallForm;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeFormTracking();
        }
        
        private void InitializeFormTracking()
        {
            AdvancedLoggingSystem.LogInfo(AdvancedLoggingSystem.LogCategory.Boxing, "BoxingFormTracker", "🥊 Initializing Boxing Form Tracker...");
            
            // Find required references
            movementSystem = VR360MovementSystem.Instance;
            handTracking = HandTrackingManager.Instance;
            
            // Setup tracking references
            SetupTrackingReferences();
            
            // Initialize form data
            InitializeFormData();
            
            // Start form analysis loop
            StartCoroutine(FormAnalysisLoop());
            
            AdvancedLoggingSystem.LogInfo(AdvancedLoggingSystem.LogCategory.Boxing, "BoxingFormTracker", "✅ Boxing Form Tracker initialized!");
        }
        
        private void SetupTrackingReferences()
        {
            // Find player references
            var xrOrigin = CachedReferenceManager.Get<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                playerHead = xrOrigin.Camera.transform;
                playerBody = xrOrigin.CameraFloorOffsetObject.transform;
            }
            
            // Setup hip reference if not assigned
            if (hipReference == null && playerBody != null)
            {
                GameObject hipObj = new GameObject("Hip Reference");
                hipObj.transform.SetParent(playerBody);
                hipObj.transform.localPosition = new Vector3(0, -0.3f, 0); // Approximate hip level
                hipReference = hipObj.transform;
            }
            
            // Setup foot references if not assigned
            if (leftFootReference == null && playerBody != null)
            {
                GameObject leftFootObj = new GameObject("Left Foot Reference");
                leftFootObj.transform.SetParent(playerBody);
                leftFootObj.transform.localPosition = new Vector3(-0.2f, -1f, 0);
                leftFootReference = leftFootObj.transform;
            }
            
            if (rightFootReference == null && playerBody != null)
            {
                GameObject rightFootObj = new GameObject("Right Foot Reference");
                rightFootObj.transform.SetParent(playerBody);
                rightFootObj.transform.localPosition = new Vector3(0.2f, -1f, 0);
                rightFootReference = rightFootObj.transform;
            }
        }
        
        private void InitializeFormData()
        {
            currentFormData = new BoxingFormData
            {
                stance = currentStance,
                hipRotation = 0f,
                footSpacing = stanceWidthOptimal,
                weightDistribution = 0.5f,
                hipVelocity = Vector3.zero,
                footAlignment = Vector3.zero,
                overallForm = FormQuality.Fair,
                powerMultiplier = 1f,
                accuracyBonus = 0f,
                isProperStance = true,
                stanceQuality = 1f
            };
            
            lastStanceChangeTime = Time.time;
        }
        
        private IEnumerator FormAnalysisLoop()
        {
            while (true)
            {
                float deltaTime = 1f / formAnalysisFrequency;
                yield return new WaitForSeconds(deltaTime);
                
                if (Time.time - lastFormAnalysisTime >= deltaTime)
                {
                    AnalyzeBoxingForm();
                    lastFormAnalysisTime = Time.time;
                }
            }
        }
        
        private void AnalyzeBoxingForm()
        {
            // Update tracking data
            UpdateHipTracking();
            UpdateFootTracking();
            UpdateWeightDistribution();
            
            // Detect stance changes
            if (enableAutomaticStanceDetection)
            {
                DetectStanceChange();
            }
            
            // Analyze form quality
            AnalyzeFormQuality();
            
            // Calculate bonuses
            CalculateFormBonuses();
            
            // Trigger events
            OnFormAnalyzed?.Invoke(currentFormData);
            
            // Check for form quality changes
            if (currentFormData.overallForm != lastFormQuality)
            {
                OnFormQualityChanged?.Invoke(currentFormData.overallForm);
                lastFormQuality = currentFormData.overallForm;
            }
        }
        
        private void UpdateHipTracking()
        {
            if (!enableHipTracking || hipReference == null) return;
            
            Vector3 currentHipPosition = hipReference.position;
            
            // Calculate hip velocity
            if (lastHipPosition != Vector3.zero)
            {
                hipVelocity = (currentHipPosition - lastHipPosition) / Time.deltaTime;
                currentFormData.hipVelocity = hipVelocity;
            }
            
            // Calculate hip rotation relative to body
            Vector3 bodyForward = playerBody != null ? playerBody.forward : Vector3.forward;
            Vector3 hipForward = hipReference.forward;
            
            currentHipRotation = Vector3.SignedAngle(bodyForward, hipForward, Vector3.up);
            currentFormData.hipRotation = currentHipRotation;
            
            // Track hip rotation history
            hipRotationHistory.Enqueue(currentHipRotation);
            if (hipRotationHistory.Count > 30) // Keep 1 second of history at 30 FPS
            {
                hipRotationHistory.Dequeue();
            }
            
            lastHipPosition = currentHipPosition;
            
            // Generate hip power events
            float hipPower = Mathf.Abs(hipVelocity.magnitude) * hipPowerMultiplier;
            if (hipPower > hipRotationThreshold)
            {
                OnHipPowerGenerated?.Invoke(hipPower);
            }
        }
        
        private void UpdateFootTracking()
        {
            if (!enableFootTracking || leftFootReference == null || rightFootReference == null) return;
            
            Vector3 leftFootPos = leftFootReference.position;
            Vector3 rightFootPos = rightFootReference.position;
            
            // Calculate foot spacing
            currentFootSpacing = Vector3.Distance(leftFootPos, rightFootPos);
            currentFormData.footSpacing = currentFootSpacing;
            
            // Calculate foot alignment
            Vector3 footLine = (rightFootPos - leftFootPos).normalized;
            Vector3 bodyForward = playerBody != null ? playerBody.forward : Vector3.forward;
            float alignment = Vector3.Dot(footLine, bodyForward);
            currentFormData.footAlignment = new Vector3(alignment, 0, 0);
            
            // Update stance based on foot positioning
            if (enableAutomaticStanceDetection)
            {
                DetectStanceFromFeet(leftFootPos, rightFootPos);
            }
            
            lastLeftFootPosition = leftFootPos;
            lastRightFootPosition = rightFootPos;
        }
        
        private void UpdateWeightDistribution()
        {
            if (!enableWeightTracking) return;
            
            // Estimate weight distribution based on hip position relative to feet
            if (leftFootReference != null && rightFootReference != null && hipReference != null)
            {
                Vector3 leftFoot = leftFootReference.position;
                Vector3 rightFoot = rightFootReference.position;
                Vector3 hip = hipReference.position;
                
                // Project hip position onto the line between feet
                Vector3 footLine = rightFoot - leftFoot;
                Vector3 hipProjected = Vector3.Project(hip - leftFoot, footLine) + leftFoot;
                
                // Calculate weight distribution (0 = all on left, 1 = all on right)
                float distanceToLeft = Vector3.Distance(hipProjected, leftFoot);
                float totalDistance = Vector3.Distance(leftFoot, rightFoot);
                
                if (totalDistance > 0)
                {
                    currentWeightDistribution = distanceToLeft / totalDistance;
                    currentFormData.weightDistribution = currentWeightDistribution;
                }
            }
        }
        
        private void DetectStanceChange()
        {
            if (Time.time - lastStanceChangeTime < stanceChangeDelay) return;
            
            // Analyze foot positioning and weight distribution
            BoxingStance detectedStance = AnalyzeStanceFromForm();
            
            if (detectedStance != currentStance && !isTransitioning)
            {
                StartStanceTransition(detectedStance);
            }
        }
        
        private BoxingStance AnalyzeStanceFromForm()
        {
            // Orthodox: Left foot forward, weight slightly forward
            // Southpaw: Right foot forward, weight slightly forward
            
            if (leftFootReference == null || rightFootReference == null) 
                return currentStance;
            
            Vector3 leftFoot = leftFootReference.position;
            Vector3 rightFoot = rightFootReference.position;
            Vector3 bodyForward = playerBody != null ? playerBody.forward : Vector3.forward;
            
            // Check which foot is more forward
            float leftForwardness = Vector3.Dot(leftFoot, bodyForward);
            float rightForwardness = Vector3.Dot(rightFoot, bodyForward);
            
            // Orthodox = left foot forward, Southpaw = right foot forward
            return leftForwardness > rightForwardness ? BoxingStance.Orthodox : BoxingStance.Southpaw;
        }
        
        private void DetectStanceFromFeet(Vector3 leftFoot, Vector3 rightFoot)
        {
            // Detect stance based on foot positioning relative to player facing direction
            Vector3 bodyForward = playerBody != null ? playerBody.forward : Vector3.forward;
            
            float leftForwardDistance = Vector3.Dot(leftFoot, bodyForward);
            float rightForwardDistance = Vector3.Dot(rightFoot, bodyForward);
            
            float forwardDifference = leftForwardDistance - rightForwardDistance;
            
            // Threshold for stance detection
            if (Mathf.Abs(forwardDifference) > 0.1f)
            {
                BoxingStance detectedStance = forwardDifference > 0 ? BoxingStance.Orthodox : BoxingStance.Southpaw;
                
                if (detectedStance != currentStance && !isTransitioning)
                {
                    StartStanceTransition(detectedStance);
                }
            }
        }
        
        private void StartStanceTransition(BoxingStance newStance)
        {
            activeTransition = new StanceTransition
            {
                fromStance = currentStance,
                toStance = newStance,
                transitionTime = Time.time,
                completionPercentage = 0f,
                isComplete = false
            };
            
            isTransitioning = true;
            StartCoroutine(StanceTransitionCoroutine());
            
            AdvancedLoggingSystem.LogDebug(AdvancedLoggingSystem.LogCategory.Boxing, "BoxingFormTracker", $"🥊 Stance transition started: {currentStance} → {newStance}");
        }
        
        private IEnumerator StanceTransitionCoroutine()
        {
            float transitionDuration = 1f; // 1 second transition
            float startTime = Time.time;
            
            while (Time.time - startTime < transitionDuration)
            {
                float progress = (Time.time - startTime) / transitionDuration;
                activeTransition.completionPercentage = progress;
                
                OnStanceTransition?.Invoke(activeTransition);
                
                yield return new WaitForEndOfFrame();
            }
            
            // Complete transition
            currentStance = activeTransition.toStance;
            activeTransition.isComplete = true;
            activeTransition.completionPercentage = 1f;
            
            OnStanceChanged?.Invoke(currentStance);
            OnStanceTransition?.Invoke(activeTransition);
            
            isTransitioning = false;
            lastStanceChangeTime = Time.time;
            
            AdvancedLoggingSystem.LogDebug(AdvancedLoggingSystem.LogCategory.Boxing, "BoxingFormTracker", $"✅ Stance transition complete: {currentStance}");
        }
        
        private void AnalyzeFormQuality()
        {
            float formScore = 0f;
            int criteriaCount = 0;
            
            // Foot spacing quality (0-1)
            if (enableFootTracking)
            {
                float spacingQuality = 1f - Mathf.Abs(currentFootSpacing - stanceWidthOptimal) / stanceWidthOptimal;
                formScore += Mathf.Clamp01(spacingQuality);
                criteriaCount++;
            }
            
            // Hip rotation quality (0-1)
            if (enableHipTracking)
            {
                float hipQuality = Mathf.Clamp01(Mathf.Abs(currentHipRotation) / hipRotationThreshold);
                formScore += hipQuality;
                criteriaCount++;
            }
            
            // Weight distribution quality (0-1)
            if (enableWeightTracking)
            {
                float optimalWeight = currentStance == BoxingStance.Orthodox ? 0.4f : 0.6f;
                float weightQuality = 1f - Mathf.Abs(currentWeightDistribution - optimalWeight);
                formScore += Mathf.Clamp01(weightQuality);
                criteriaCount++;
            }
            
            // Stance consistency (0-1)
            if (!isTransitioning)
            {
                formScore += 1f; // Bonus for stable stance
                criteriaCount++;
            }
            
            // Calculate average form score
            if (criteriaCount > 0)
            {
                formScore /= criteriaCount;
            }
            
            // Convert to form quality enum
            FormQuality quality = FormQuality.Poor;
            if (formScore >= 0.9f) quality = FormQuality.Perfect;
            else if (formScore >= 0.75f) quality = FormQuality.Excellent;
            else if (formScore >= 0.6f) quality = FormQuality.Good;
            else if (formScore >= 0.4f) quality = FormQuality.Fair;
            
            currentFormData.overallForm = quality;
            currentFormData.stanceQuality = formScore;
            currentFormData.isProperStance = formScore >= 0.6f;
        }
        
        private void CalculateFormBonuses()
        {
            if (!providePowerBonus) return;
            
            float baseMultiplier = 1f;
            float bonusMultiplier = 0f;
            
            // Hip power bonus
            if (enableHipTracking && Mathf.Abs(currentHipRotation) > hipRotationThreshold)
            {
                bonusMultiplier += 0.3f;
            }
            
            // Stance quality bonus
            bonusMultiplier += currentFormData.stanceQuality * 0.5f;
            
            // Weight transfer bonus
            if (enableWeightTracking)
            {
                float weightTransferBonus = weightTransferCurve.Evaluate(Mathf.Abs(currentWeightDistribution - 0.5f) * 2f);
                bonusMultiplier += weightTransferBonus * 0.4f;
            }
            
            // Form quality bonus
            switch (currentFormData.overallForm)
            {
                case FormQuality.Perfect: bonusMultiplier += 1f; break;
                case FormQuality.Excellent: bonusMultiplier += 0.7f; break;
                case FormQuality.Good: bonusMultiplier += 0.4f; break;
                case FormQuality.Fair: bonusMultiplier += 0.1f; break;
            }
            
            currentFormData.powerMultiplier = baseMultiplier + Mathf.Min(bonusMultiplier, maxPowerBonus);
            currentFormData.accuracyBonus = bonusMultiplier * 0.2f; // Accuracy bonus
        }
        
        // Public methods
        public void ManualStanceChange(BoxingStance newStance)
        {
            if (newStance != currentStance && !isTransitioning)
            {
                StartStanceTransition(newStance);
            }
        }
        
        public float GetCurrentPowerMultiplier()
        {
            return currentFormData.powerMultiplier;
        }
        
        public float GetCurrentAccuracyBonus()
        {
            return currentFormData.accuracyBonus;
        }
        
        public bool IsStanceOptimal()
        {
            return currentFormData.stanceQuality >= 0.8f && 
                   currentFormData.overallForm >= FormQuality.Good &&
                   !isTransitioning;
        }
        
        public Vector3 GetOptimalStancePosition()
        {
            // Return the optimal position for the current stance
            float forwardOffset = currentStance == BoxingStance.Orthodox ? 0.2f : -0.2f;
            return playerBody.position + playerBody.forward * forwardOffset;
        }
        
        [ContextMenu("Switch to Orthodox")]
        public void SwitchToOrthodox()
        {
            ManualStanceChange(BoxingStance.Orthodox);
        }
        
        [ContextMenu("Switch to Southpaw")]
        public void SwitchToSouthpaw()
        {
            ManualStanceChange(BoxingStance.Southpaw);
        }
        
        [ContextMenu("Analyze Current Form")]
        public void AnalyzeCurrentForm()
        {
            AnalyzeBoxingForm();
            AdvancedLoggingSystem.LogTrace(AdvancedLoggingSystem.LogCategory.Boxing, "BoxingFormTracker", $"Form Analysis: {currentFormData.overallForm}, Power: {currentFormData.powerMultiplier:F2}x");
        }
        
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            // Draw stance indicators
            if (leftFootReference != null && rightFootReference != null)
            {
                Gizmos.color = currentStance == BoxingStance.Orthodox ? Color.blue : Color.red;
                Gizmos.DrawWireSphere(leftFootReference.position, 0.1f);
                Gizmos.DrawWireSphere(rightFootReference.position, 0.1f);
                Gizmos.DrawLine(leftFootReference.position, rightFootReference.position);
            }
            
            // Draw hip tracking
            if (hipReference != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(hipReference.position, 0.05f);
                Gizmos.DrawRay(hipReference.position, hipVelocity * 0.5f);
            }
            
            // Draw form quality indicator
            Color formColor = Color.red;
            switch (currentFormData.overallForm)
            {
                case FormQuality.Perfect: formColor = Color.magenta; break;
                case FormQuality.Excellent: formColor = Color.green; break;
                case FormQuality.Good: formColor = Color.yellow; break;
                case FormQuality.Fair: formColor = Color.orange; break;
            }
            
            if (playerBody != null)
            {
                Gizmos.color = formColor;
                Gizmos.DrawWireSphere(playerBody.position + Vector3.up * 2f, 0.2f);
            }
        }
        
        // Public API methods
        public BoxingFormData GetCurrentFormData()
        {
            return currentFormData;
        }
        
        public BoxingStance GetCurrentStance()
        {
            return currentStance;
        }
    }
} 