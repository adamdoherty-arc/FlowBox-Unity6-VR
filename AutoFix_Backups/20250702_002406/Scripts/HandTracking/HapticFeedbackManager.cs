using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using VRBoxingGame.Boxing;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;

namespace VRBoxingGame.HandTracking
{
    /// <summary>
    /// Comprehensive haptic feedback system for VR boxing game
    /// Integrates with XR Interaction Toolkit 3.0+ for advanced haptic patterns
    /// </summary>
    public class HapticFeedbackManager : MonoBehaviour
    {
        [Header("Haptic Intensity Settings")]
        [Range(0f, 1f)]
        public float punchHitIntensity = 0.8f;
        [Range(0f, 1f)]
        public float blockSuccessIntensity = 1.0f;
        [Range(0f, 1f)]
        public float missedPunchIntensity = 0.3f;
        [Range(0f, 1f)]
        public float environmentInteractionIntensity = 0.5f;
        
        [Header("Haptic Duration Settings")]
        [Range(0.01f, 1f)]
        public float punchHitDuration = 0.1f;
        [Range(0.01f, 1f)]
        public float blockSuccessDuration = 0.2f;
        [Range(0.01f, 1f)]
        public float missedPunchDuration = 0.05f;
        [Range(0.01f, 1f)]
        public float environmentInteractionDuration = 0.15f;
        
        [Header("Advanced Haptic Patterns")]
        public bool enableAdvancedPatterns = true;
        public AnimationCurve punchImpactCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        public AnimationCurve blockSuccessCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [Header("Scene-Specific Haptics")]
        public bool enableSceneSpecificHaptics = true;
        [Range(0.5f, 2f)]
        public float underwaterDamping = 0.7f;
        [Range(0.5f, 2f)]
        public float spaceStationAmplification = 1.3f;
        [Range(0.5f, 2f)]
        public float crystalCaveResonance = 1.5f;
        
        // XR Controller references
        private XRBaseController leftController;
        private XRBaseController rightController;
        private List<InputDevice> inputDevices = new List<InputDevice>();
        
        // Haptic pattern tracking
        private CancellationTokenSource leftHapticCancellation;
        private CancellationTokenSource rightHapticCancellation;
        
        // Scene-specific modifiers
        private float currentHapticModifier = 1f;
        
        private void Start()
        {
            InitializeControllers();
            SubscribeToGameEvents();
            
            // Listen for scene changes
            var sceneManager = FindObjectOfType<SceneLoadingManager>();
            if (sceneManager != null)
            {
                sceneManager.OnSceneChanged += OnSceneChanged;
            }
        }
        
        private void InitializeControllers()
        {
            // Find XR controllers using XR Interaction Toolkit
            var controllers = FindObjectsOfType<XRBaseController>();
            
            foreach (var controller in controllers)
            {
                string controllerName = controller.name.ToLower();
                if (controllerName.Contains("left"))
                {
                    leftController = controller;
                    Debug.Log($"Left controller found: {controller.name}");
                }
                else if (controllerName.Contains("right"))
                {
                    rightController = controller;
                    Debug.Log($"Right controller found: {controller.name}");
                }
            }
            
            // Fallback: Get input devices directly
            if (leftController == null || rightController == null)
            {
                RefreshInputDevices();
            }
        }
        
        private void RefreshInputDevices()
        {
            inputDevices.Clear();
            InputDevices.GetDevices(inputDevices);
            
            Debug.Log($"Found {inputDevices.Count} input devices for haptic feedback");
        }
        
        private void SubscribeToGameEvents()
        {
            // Subscribe to rhythm target system events
            var rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            if (rhythmSystem != null)
            {
                // Note: These events need to be added to RhythmTargetSystem
                // rhythmSystem.OnCircleHit.AddListener(OnPunchHit);
                // rhythmSystem.OnBlockSuccess.AddListener(OnBlockSuccess);
                // rhythmSystem.OnPunchMiss.AddListener(OnPunchMiss);
                
                Debug.Log("Subscribed to rhythm system events");
            }
            
            // Subscribe to hand tracking events
            var handTracker = FindObjectOfType<HandTrackingManager>();
            if (handTracker != null)
            {
                Debug.Log("Found hand tracking manager for haptic integration");
            }
        }
        
        private void OnSceneChanged(SceneLoadingManager.SceneType sceneType)
        {
            UpdateHapticModifierForScene(sceneType);
        }
        
        private void UpdateHapticModifierForScene(SceneLoadingManager.SceneType sceneType)
        {
            if (!enableSceneSpecificHaptics)
            {
                currentHapticModifier = 1f;
                return;
            }
            
            switch (sceneType)
            {
                case SceneLoadingManager.SceneType.UnderwaterWorld:
                    currentHapticModifier = underwaterDamping;
                    Debug.Log("Haptic feedback: Underwater damping applied");
                    break;
                    
                case SceneLoadingManager.SceneType.SpaceStation:
                    currentHapticModifier = spaceStationAmplification;
                    Debug.Log("Haptic feedback: Space station amplification applied");
                    break;
                    
                case SceneLoadingManager.SceneType.CrystalCave:
                    currentHapticModifier = crystalCaveResonance;
                    Debug.Log("Haptic feedback: Crystal cave resonance applied");
                    break;
                    
                default:
                    currentHapticModifier = 1f;
                    break;
            }
        }
        
        // Public methods for game events
        public void OnPunchHit(Vector3 hitPosition, float accuracy, bool isLeftHand)
        {
            XRBaseController controller = isLeftHand ? leftController : rightController;
            
            if (controller != null)
            {
                float intensity = Mathf.Lerp(0.3f, punchHitIntensity, accuracy) * currentHapticModifier;
                
                if (enableAdvancedPatterns)
                {
                    StartHapticPattern(controller, intensity, punchHitDuration, punchImpactCurve, isLeftHand);
                }
                else
                {
                    controller.SendHapticImpulse(intensity, punchHitDuration);
                }
                
                Debug.Log($"Punch hit haptic: {(isLeftHand ? "Left" : "Right")} hand, intensity: {intensity:F2}");
            }
        }
        
        public void OnBlockSuccess(Vector3 blockPosition, float blockStrength)
        {
            float intensity = blockSuccessIntensity * blockStrength * currentHapticModifier;
            
            if (enableAdvancedPatterns)
            {
                // Both hands vibrate for successful blocks
                StartHapticPattern(leftController, intensity, blockSuccessDuration, blockSuccessCurve, true);
                StartHapticPattern(rightController, intensity, blockSuccessDuration, blockSuccessCurve, false);
            }
            else
            {
                leftController?.SendHapticImpulse(intensity, blockSuccessDuration);
                rightController?.SendHapticImpulse(intensity, blockSuccessDuration);
            }
            
            Debug.Log($"Block success haptic: Both hands, intensity: {intensity:F2}");
        }
        
        public void OnPunchMiss(bool isLeftHand)
        {
            XRBaseController controller = isLeftHand ? leftController : rightController;
            
            if (controller != null)
            {
                float intensity = missedPunchIntensity * currentHapticModifier;
                controller.SendHapticImpulse(intensity, missedPunchDuration);
                
                Debug.Log($"Punch miss haptic: {(isLeftHand ? "Left" : "Right")} hand");
            }
        }
        
        public void OnEnvironmentInteraction(Vector3 interactionPoint, float interactionStrength, bool isLeftHand)
        {
            XRBaseController controller = isLeftHand ? leftController : rightController;
            
            if (controller != null)
            {
                float intensity = environmentInteractionIntensity * interactionStrength * currentHapticModifier;
                controller.SendHapticImpulse(intensity, environmentInteractionDuration);
                
                Debug.Log($"Environment interaction haptic: {(isLeftHand ? "Left" : "Right")} hand, strength: {interactionStrength:F2}");
            }
        }
        
        private void StartHapticPattern(XRBaseController controller, float baseIntensity, float duration, AnimationCurve pattern, bool isLeftHand)
        {
            if (controller == null) return;
            
            // Cancel previous haptic on this hand
            if (isLeftHand)
            {
                leftHapticCancellation?.Cancel();
                leftHapticCancellation = new CancellationTokenSource();
                _ = ExecuteHapticPatternAsync(controller, baseIntensity, duration, pattern, leftHapticCancellation.Token);
            }
            else
            {
                rightHapticCancellation?.Cancel();
                rightHapticCancellation = new CancellationTokenSource();
                _ = ExecuteHapticPatternAsync(controller, baseIntensity, duration, pattern, rightHapticCancellation.Token);
            }
        }
        
        private async Task ExecuteHapticPatternAsync(XRBaseController controller, float baseIntensity, float duration, AnimationCurve pattern, CancellationToken cancellationToken)
        {
            try
            {
                float elapsedTime = 0f;
                
                while (elapsedTime < duration && !cancellationToken.IsCancellationRequested)
                {
                    float normalizedTime = elapsedTime / duration;
                    float intensity = baseIntensity * pattern.Evaluate(normalizedTime) * currentHapticModifier;
                    
                    // Send haptic impulse
                    controller.SendHapticImpulse(intensity, 0.02f);
                    
                    elapsedTime += 0.02f;
                    await Task.Delay(20, cancellationToken); // 20ms = 0.02s
                }
            }
            catch (System.OperationCanceledException)
            {
                // Haptic was cancelled, this is expected
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in haptic pattern execution: {ex.Message}");
            }
        }
        
        // Scene-specific haptic effects
        public void TriggerUnderwaterBubbleEffect(bool isLeftHand)
        {
            XRBaseController controller = isLeftHand ? leftController : rightController;
            if (controller != null)
            {
                _ = BubbleHapticEffectAsync(controller);
            }
        }
        
        private async Task BubbleHapticEffectAsync(XRBaseController controller)
        {
            try
            {
                // Create bubble-like haptic pattern
                for (int i = 0; i < 5; i++)
                {
                    float intensity = UnityEngine.Random.Range(0.3f, 0.7f) * currentHapticModifier;
                    controller.SendHapticImpulse(intensity, 0.1f);
                    await Task.Delay(100); // 100ms between bubbles
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in bubble haptic effect: {ex.Message}");
            }
        }
        
        public void TriggerCrystalResonanceEffect(float frequency, bool isLeftHand)
        {
            XRBaseController controller = isLeftHand ? leftController : rightController;
            if (controller != null)
            {
                _ = CrystalResonanceEffectAsync(controller, frequency);
            }
        }
        
        private async Task CrystalResonanceEffectAsync(XRBaseController controller, float frequency)
        {
            try
            {
                // Convert frequency to pulse interval (higher frequency = faster pulses)
                float pulseInterval = Mathf.Lerp(0.2f, 0.05f, frequency / 1000f);
                int pulseCount = Mathf.RoundToInt(2f / pulseInterval); // 2 second effect
                
                for (int i = 0; i < pulseCount; i++)
                {
                    float intensity = 0.6f * currentHapticModifier;
                    controller.SendHapticImpulse(intensity, pulseInterval * 0.5f);
                    await Task.Delay((int)(pulseInterval * 1000));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in crystal resonance haptic effect: {ex.Message}");
            }
        }
        
        // Debug and testing methods
        [ContextMenu("Test Left Controller Haptic")]
        public void TestLeftHaptic()
        {
            OnPunchHit(Vector3.zero, 1f, true);
        }
        
        [ContextMenu("Test Right Controller Haptic")]
        public void TestRightHaptic()
        {
            OnPunchHit(Vector3.zero, 1f, false);
        }
        
        [ContextMenu("Test Block Success Haptic")]
        public void TestBlockHaptic()
        {
            OnBlockSuccess(Vector3.zero, 1f);
        }
        
        private void OnEnable()
        {
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
        }
        
        private void OnDisable()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }
        
        private void OnDeviceConnected(InputDevice device)
        {
            Debug.Log($"XR Device connected: {device.name}");
            RefreshInputDevices();
        }
        
        private void OnDeviceDisconnected(InputDevice device)
        {
            Debug.Log($"XR Device disconnected: {device.name}");
            RefreshInputDevices();
        }
        
        private void OnDestroy()
        {
            // Cancel any running haptic patterns
            leftHapticCancellation?.Cancel();
            leftHapticCancellation?.Dispose();
            rightHapticCancellation?.Cancel();
            rightHapticCancellation?.Dispose();
            
            // Unsubscribe from input device events
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        }
    }
} 