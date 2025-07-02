using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Core;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;

namespace VRBoxingGame.AI
{
    /// <summary>
    /// Enhanced AI Coach Visual System with 3D Holographic Coach, Voice Guidance, and Real-time Form Corrections
    /// Unity 6 VR Optimized with Spatial Audio and Gesture Demonstrations
    /// </summary>
    public class AICoachVisualSystem : MonoBehaviour
    {
        [Header("Holographic Coach")]
        public GameObject holographicCoachPrefab;
        public Transform coachSpawnPoint;
        public float coachDistance = 3f;
        public bool enableHolographicCoach = true;
        public Material holographicMaterial;
        
        [Header("Voice Coaching")]
        public bool enableVoiceCoaching = true;
        public AudioSource coachVoiceSource;
        public AudioClip[] formCorrectionClips;
        public AudioClip[] motivationalClips;
        public AudioClip[] techniqueTips;
        public float voiceVolume = 0.8f;
        
        [Header("Visual Feedback")]
        public Canvas coachingCanvas;
        public TextMeshProUGUI coachingText;
        public Image formMeter;
        public Image powerMeter;
        public Image accuracyMeter;
        public GameObject formOverlayPrefab;
        
        [Header("Gesture Demonstrations")]
        public bool enableGestureDemos = true;
        public GameObject[] punchDemoPrefabs;
        public Transform demoSpawnPoint;
        public float demoScale = 0.7f;
        
        [Header("Real-time Analysis")]
        public bool enableRealTimeFormOverlay = true;
        public LineRenderer[] formGuideLines;
        public GameObject[] positionMarkers;
        public Color correctFormColor = Color.green;
        public Color incorrectFormColor = Color.red;
        
        [Header("Personalized Training")]
        public bool enablePersonalizedPlans = true;
        public int maxTrainingPlan = 10;
        public float adaptiveLearningRate = 0.1f;
        
        // Private variables
        private GameObject activeHolographicCoach;
        private Animator coachAnimator;
        private AdvancedGameStateManager gameStateManager;
        private BoxingFormTracker formTracker;
        private Queue<CoachingInstruction> pendingInstructions = new Queue<CoachingInstruction>();
        private Dictionary<string, float> playerWeaknesses = new Dictionary<string, float>();
        private List<PersonalizedDrill> currentTrainingPlan = new List<PersonalizedDrill>();
        
        [System.Serializable]
        public struct CoachingInstruction
        {
            public CoachingType type;
            public string message;
            public string voiceKey;
            public float urgency;
            public Vector3 targetPosition;
            public Color visualColor;
            public float duration;
            public bool requiresGestureDemo;
            public string demoAnimation;
        }
        
        [System.Serializable]
        public struct PersonalizedDrill
        {
            public string drillName;
            public SkillArea targetSkill;
            public float difficulty;
            public int repetitions;
            public string instructions;
            public bool completed;
        }
        
        public enum CoachingType
        {
            StanceCorrection,
            HipRotation,
            FootPositioning,
            PowerGeneration,
            AccuracyImprovement,
            EnduranceGuidance,
            BreathingTechnique,
            ComboTraining,
            DefensiveForm,
            Motivation
        }
        
        public enum SkillArea
        {
            Stance,
            Power,
            Accuracy,
            Speed,
            Endurance,
            Form,
            Defense,
            Combinations
        }
        
        // Singleton
        public static AICoachVisualSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeAICoach();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetupHolographicCoach();
            SetupVoiceSystem();
            SetupVisualFeedback();
            ConnectToGameSystems();
        }
        
        private void Update()
        {
            ProcessPendingInstructions();
            UpdateRealTimeFormOverlay();
            UpdateVisualMeters();
            UpdatePersonalizedTraining();
        }
        
        private void InitializeAICoach()
        {
            Debug.Log("🤖 Initializing Enhanced AI Coach Visual System...");
            
            // Initialize player weakness tracking
            playerWeaknesses["stance"] = 0f;
            playerWeaknesses["power"] = 0f;
            playerWeaknesses["accuracy"] = 0f;
            playerWeaknesses["endurance"] = 0f;
            playerWeaknesses["form"] = 0f;
            
            // Setup canvas if not assigned
            if (coachingCanvas == null)
            {
                coachingCanvas = FindObjectOfType<Canvas>();
            }
        }
        
        private void SetupHolographicCoach()
        {
            if (!enableHolographicCoach || holographicCoachPrefab == null) return;
            
            // Find coach spawn point or create one
            if (coachSpawnPoint == null)
            {
                GameObject spawnObj = new GameObject("AI Coach Spawn Point");
                coachSpawnPoint = spawnObj.transform;
                
                // Position coach in front and to the side of player
                var xrCamera = FindObjectOfType<Camera>();
                if (xrCamera != null)
                {
                    Vector3 playerPos = xrCamera.transform.position;
                    Vector3 coachPos = playerPos + (xrCamera.transform.right * 2f) + (xrCamera.transform.forward * coachDistance);
                    coachSpawnPoint.position = coachPos;
                    coachSpawnPoint.LookAt(playerPos);
                }
            }
            
            // Instantiate holographic coach
            if (activeHolographicCoach == null)
            {
                activeHolographicCoach = Instantiate(holographicCoachPrefab, coachSpawnPoint.position, coachSpawnPoint.rotation);
                coachAnimator = activeHolographicCoach.GetComponent<Animator>();
                
                // Apply holographic effect
                ApplyHolographicEffect(activeHolographicCoach);
                
                Debug.Log("🏃‍♂️ Holographic AI Coach spawned successfully!");
            }
        }
        
        private void ApplyHolographicEffect(GameObject coach)
        {
            if (holographicMaterial == null)
            {
                // Create holographic material
                holographicMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                holographicMaterial.SetFloat("_Mode", 3); // Transparent mode
                holographicMaterial.SetColor("_BaseColor", new Color(0.3f, 0.8f, 1f, 0.7f));
                holographicMaterial.SetFloat("_Metallic", 0.2f);
                holographicMaterial.SetFloat("_Smoothness", 0.8f);
                holographicMaterial.EnableKeyword("_ALPHABLEND_ON");
            }
            
            // Apply to all renderers
            Renderer[] renderers = coach.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = holographicMaterial;
            }
            
            // Add holographic animation
            var hologramEffect = coach.AddComponent<HolographicEffect>();
            hologramEffect.Initialize();
        }
        
        private void SetupVoiceSystem()
        {
            if (!enableVoiceCoaching) return;
            
            if (coachVoiceSource == null)
            {
                GameObject voiceObj = new GameObject("AI Coach Voice");
                coachVoiceSource = voiceObj.AddComponent<AudioSource>();
                coachVoiceSource.spatialBlend = 1f; // 3D spatial audio
                coachVoiceSource.volume = voiceVolume;
                coachVoiceSource.rolloffMode = AudioRolloffMode.Linear;
                coachVoiceSource.maxDistance = 10f;
                
                if (coachSpawnPoint != null)
                {
                    voiceObj.transform.position = coachSpawnPoint.position;
                }
            }
            
            Debug.Log("🎤 AI Coach voice system initialized");
        }
        
        private void SetupVisualFeedback()
        {
            if (coachingCanvas == null) return;
            
            // Create coaching UI elements if not assigned
            if (coachingText == null)
            {
                GameObject textObj = new GameObject("Coaching Text");
                textObj.transform.SetParent(coachingCanvas.transform);
                coachingText = textObj.AddComponent<TextMeshProUGUI>();
                coachingText.text = "AI Coach Ready";
                coachingText.fontSize = 24;
                coachingText.color = Color.white;
                coachingText.alignment = TextAlignmentOptions.Center;
                
                RectTransform rectTransform = textObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(400, 100);
                rectTransform.anchoredPosition = new Vector2(0, 200);
            }
            
            // Setup meters
            SetupProgressMeters();
            
            Debug.Log("👁️ AI Coach visual feedback system ready");
        }
        
        private void SetupProgressMeters()
        {
            if (coachingCanvas == null) return;
            
            // Form meter
            if (formMeter == null)
            {
                formMeter = CreateProgressMeter("Form", new Vector2(-200, -200), correctFormColor);
            }
            
            // Power meter
            if (powerMeter == null)
            {
                powerMeter = CreateProgressMeter("Power", new Vector2(0, -200), Color.yellow);
            }
            
            // Accuracy meter
            if (accuracyMeter == null)
            {
                accuracyMeter = CreateProgressMeter("Accuracy", new Vector2(200, -200), Color.blue);
            }
        }
        
        private Image CreateProgressMeter(string name, Vector2 position, Color color)
        {
            GameObject meterObj = new GameObject($"{name} Meter");
            meterObj.transform.SetParent(coachingCanvas.transform);
            
            RectTransform rect = meterObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 20);
            rect.anchoredPosition = position;
            
            Image meter = meterObj.AddComponent<Image>();
            meter.color = color;
            meter.type = Image.Type.Filled;
            meter.fillMethod = Image.FillMethod.Horizontal;
            
            // Add label
            GameObject labelObj = new GameObject($"{name} Label");
            labelObj.transform.SetParent(meterObj.transform);
            
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 20);
            labelRect.anchoredPosition = new Vector2(0, -25);
            
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = name;
            label.fontSize = 16;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            
            return meter;
        }
        
        private void ConnectToGameSystems()
        {
            // Connect to game state manager
            gameStateManager = AdvancedGameStateManager.Instance;
            if (gameStateManager != null)
            {
                gameStateManager.OnCoachingInstruction.AddListener(OnCoachingInstructionReceived);
                gameStateManager.OnPerformanceAnalysis.AddListener(OnPerformanceAnalyzed);
            }
            
            // Connect to form tracker
            formTracker = BoxingFormTracker.Instance;
            if (formTracker != null)
            {
                formTracker.OnFormAnalyzed.AddListener(OnFormAnalyzed);
                formTracker.OnStanceChanged.AddListener(OnStanceChanged);
            }
        }
        
        private void ProcessPendingInstructions()
        {
            if (pendingInstructions.Count == 0) return;
            
            var instruction = pendingInstructions.Dequeue();
            DisplayCoachingInstruction(instruction);
        }
        
        private async Task DisplayCoachingInstructionAsync(CoachingInstruction instruction)
        {
            // Update text display
            if (coachingText != null)
            {
                coachingText.text = instruction.message;
                coachingText.color = instruction.visualColor;
            }
            
            // Play voice coaching
            if (enableVoiceCoaching && !string.IsNullOrEmpty(instruction.voiceKey))
            {
                PlayVoiceInstruction(instruction.voiceKey);
            }
            
            // Animate holographic coach
            if (coachAnimator != null && !string.IsNullOrEmpty(instruction.demoAnimation))
            {
                coachAnimator.SetTrigger(instruction.demoAnimation);
            }
            
            // Show gesture demonstration
            if (instruction.requiresGestureDemo && enableGestureDemos)
            {
                await ShowGestureDemonstration(instruction.type);
            }
            
            // Display position markers
            if (instruction.targetPosition != Vector3.zero)
            {
                ShowPositionMarker(instruction.targetPosition, instruction.visualColor);
            }
            
            // Wait for instruction duration
            await Task.Delay((int)(instruction.duration * 1000));
            
            // Clear instruction
            if (coachingText != null)
            {
                coachingText.text = "";
            }
        }
        
        private void PlayVoiceInstruction(string voiceKey)
        {
            if (coachVoiceSource == null) return;
            
            AudioClip clipToPlay = null;
            
            // Select appropriate voice clip based on key
            switch (voiceKey)
            {
                case "form_correction":
                    if (formCorrectionClips != null && formCorrectionClips.Length > 0)
                        clipToPlay = formCorrectionClips[UnityEngine.Random.Range(0, formCorrectionClips.Length)];
                    break;
                    
                case "motivation":
                    if (motivationalClips != null && motivationalClips.Length > 0)
                        clipToPlay = motivationalClips[UnityEngine.Random.Range(0, motivationalClips.Length)];
                    break;
                    
                case "technique":
                    if (techniqueTips != null && techniqueTips.Length > 0)
                        clipToPlay = techniqueTips[UnityEngine.Random.Range(0, techniqueTips.Length)];
                    break;
            }
            
            if (clipToPlay != null)
            {
                coachVoiceSource.PlayOneShot(clipToPlay);
            }
        }
        
        private async Task ShowGestureDemonstration(CoachingType coachingType)
        {
            if (punchDemoPrefabs == null || punchDemoPrefabs.Length == 0) return;
            
            GameObject demoPrefab = null;
            
            // Select appropriate demo based on coaching type
            switch (coachingType)
            {
                case CoachingType.StanceCorrection:
                    demoPrefab = punchDemoPrefabs[0];
                    break;
                case CoachingType.PowerGeneration:
                    demoPrefab = punchDemoPrefabs[1];
                    break;
                case CoachingType.AccuracyImprovement:
                    demoPrefab = punchDemoPrefabs[2];
                    break;
            }
            
            if (demoPrefab != null && demoSpawnPoint != null)
            {
                GameObject demo = Instantiate(demoPrefab, demoSpawnPoint.position, demoSpawnPoint.rotation);
                demo.transform.localScale = Vector3.one * demoScale;
                
                // Make demo semi-transparent
                ApplyDemoEffect(demo);
                
                // Animate demo
                var demoAnimator = demo.GetComponent<Animator>();
                if (demoAnimator != null)
                {
                    demoAnimator.SetTrigger("PlayDemo");
                }
                
                // Wait for demo duration
                await Task.Delay(3000);
                
                // Clean up demo
                if (demo != null)
                {
                    Destroy(demo);
                }
            }
        }
        
        private void ApplyDemoEffect(GameObject demo)
        {
            Renderer[] renderers = demo.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material demoMaterial = new Material(renderer.material);
                Color color = demoMaterial.color;
                color.a = 0.6f;
                demoMaterial.color = color;
                renderer.material = demoMaterial;
            }
        }
        
        private void ShowPositionMarker(Vector3 position, Color color)
        {
            // Find or create position marker
            GameObject marker = null;
            if (positionMarkers != null && positionMarkers.Length > 0)
            {
                marker = positionMarkers[0];
            }
            else
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.localScale = Vector3.one * 0.2f;
                marker.GetComponent<Collider>().enabled = false;
            }
            
            marker.transform.position = position;
            marker.GetComponent<Renderer>().material.color = color;
            marker.SetActive(true);
            
            // Auto-hide after 5 seconds
            Invoke(nameof(HidePositionMarker), 5f);
        }
        
        private void HidePositionMarker()
        {
            if (positionMarkers != null && positionMarkers.Length > 0)
            {
                positionMarkers[0].SetActive(false);
            }
        }
        
        private void UpdateRealTimeFormOverlay()
        {
            if (!enableRealTimeFormOverlay || formTracker == null) return;
            
            var formData = formTracker.CurrentFormData;
            
            // Update form guide lines
            if (formGuideLines != null && formGuideLines.Length > 0)
            {
                // Hip rotation guide
                if (formGuideLines.Length > 0)
                {
                    UpdateHipRotationGuide(formGuideLines[0], formData.hipRotation);
                }
                
                // Stance guide
                if (formGuideLines.Length > 1)
                {
                    UpdateStanceGuide(formGuideLines[1], formData.stanceQuality);
                }
            }
        }
        
        private void UpdateHipRotationGuide(LineRenderer guideLine, float hipRotation)
        {
            if (guideLine == null) return;
            
            // Change color based on hip rotation quality
            Color guideColor = hipRotation >= 15f && hipRotation <= 45f ? correctFormColor : incorrectFormColor;
            guideLine.startColor = guideColor;
            guideLine.endColor = guideColor;
            
            // Update guide line position based on player's hip
            if (formTracker.hipReference != null)
            {
                Vector3 hipPos = formTracker.hipReference.position;
                Vector3 targetRotation = Quaternion.Euler(0, hipRotation, 0) * Vector3.forward;
                
                guideLine.SetPosition(0, hipPos);
                guideLine.SetPosition(1, hipPos + targetRotation * 0.5f);
            }
        }
        
        private void UpdateStanceGuide(LineRenderer guideLine, float stanceQuality)
        {
            if (guideLine == null || formTracker.leftFootReference == null || formTracker.rightFootReference == null) return;
            
            Color guideColor = stanceQuality > 0.7f ? correctFormColor : incorrectFormColor;
            guideLine.startColor = guideColor;
            guideLine.endColor = guideColor;
            
            // Draw line between feet
            guideLine.SetPosition(0, formTracker.leftFootReference.position);
            guideLine.SetPosition(1, formTracker.rightFootReference.position);
        }
        
        private void UpdateVisualMeters()
        {
            if (formTracker == null) return;
            
            var formData = formTracker.CurrentFormData;
            
            // Update form meter
            if (formMeter != null)
            {
                formMeter.fillAmount = formData.stanceQuality;
            }
            
            // Update power meter
            if (powerMeter != null)
            {
                powerMeter.fillAmount = Mathf.Clamp01(formData.powerMultiplier - 1f);
            }
            
            // Update accuracy meter (from game manager)
            if (accuracyMeter != null && gameStateManager != null)
            {
                // This would need to be connected to the actual accuracy system
                accuracyMeter.fillAmount = UnityEngine.Random.Range(0.6f, 1f); // Placeholder
            }
        }
        
        private void UpdatePersonalizedTraining()
        {
            if (!enablePersonalizedPlans || currentTrainingPlan.Count == 0) return;
            
            // Check if current drill is completed
            var currentDrill = currentTrainingPlan[0];
            if (ShouldAdvanceDrill(currentDrill))
            {
                CompleteDrill(currentDrill);
            }
        }
        
        private bool ShouldAdvanceDrill(PersonalizedDrill drill)
        {
            // Check if drill completion criteria are met
            if (formTracker == null) return false;
            
            var formData = formTracker.CurrentFormData;
            
            switch (drill.targetSkill)
            {
                case SkillArea.Stance:
                    return formData.stanceQuality > 0.8f;
                case SkillArea.Power:
                    return formData.powerMultiplier > 1.5f;
                case SkillArea.Form:
                    return formData.overallForm >= BoxingFormTracker.FormQuality.Excellent;
                default:
                    return false;
            }
        }
        
        private void CompleteDrill(PersonalizedDrill drill)
        {
            currentTrainingPlan.RemoveAt(0);
            
            // Generate coaching instruction for completion
            var instruction = new CoachingInstruction
            {
                type = CoachingType.Motivation,
                message = $"Great! You've mastered {drill.drillName}. Moving to next drill.",
                voiceKey = "motivation",
                urgency = 0.3f,
                visualColor = Color.green,
                duration = 3f
            };
            
            pendingInstructions.Enqueue(instruction);
            
            // Generate next drill if plan is empty
            if (currentTrainingPlan.Count == 0)
            {
                GeneratePersonalizedTrainingPlan();
            }
        }
        
        private void GeneratePersonalizedTrainingPlan()
        {
            currentTrainingPlan.Clear();
            
            // Analyze player weaknesses and create targeted drills
            foreach (var weakness in playerWeaknesses)
            {
                if (weakness.Value > 0.3f) // Needs improvement
                {
                    var drill = CreateDrillForWeakness(weakness.Key, weakness.Value);
                    currentTrainingPlan.Add(drill);
                }
            }
            
            // Ensure at least one drill
            if (currentTrainingPlan.Count == 0)
            {
                currentTrainingPlan.Add(CreateGeneralImprovementDrill());
            }
        }
        
        private PersonalizedDrill CreateDrillForWeakness(string weaknessType, float severity)
        {
            var drill = new PersonalizedDrill();
            
            switch (weaknessType)
            {
                case "stance":
                    drill.drillName = "Stance Stability Practice";
                    drill.targetSkill = SkillArea.Stance;
                    drill.instructions = "Hold perfect boxing stance for 30 seconds";
                    drill.repetitions = 3;
                    break;
                    
                case "power":
                    drill.drillName = "Hip Rotation Power Drill";
                    drill.targetSkill = SkillArea.Power;
                    drill.instructions = "Practice 20 power punches with hip rotation";
                    drill.repetitions = 20;
                    break;
                    
                case "accuracy":
                    drill.drillName = "Precision Target Practice";
                    drill.targetSkill = SkillArea.Accuracy;
                    drill.instructions = "Hit 15 consecutive targets with 90%+ accuracy";
                    drill.repetitions = 15;
                    break;
            }
            
            drill.difficulty = severity;
            drill.completed = false;
            
            return drill;
        }
        
        private PersonalizedDrill CreateGeneralImprovementDrill()
        {
            return new PersonalizedDrill
            {
                drillName = "General Form Practice",
                targetSkill = SkillArea.Form,
                instructions = "Maintain good form for 2 minutes",
                repetitions = 1,
                difficulty = 0.5f,
                completed = false
            };
        }
        
        // Event handlers
        private void OnCoachingInstructionReceived(AdvancedGameStateManager.CoachingInstruction gameInstruction)
        {
            var visualInstruction = ConvertToVisualInstruction(gameInstruction);
            pendingInstructions.Enqueue(visualInstruction);
        }
        
        private CoachingInstruction ConvertToVisualInstruction(AdvancedGameStateManager.CoachingInstruction gameInstruction)
        {
            return new CoachingInstruction
            {
                type = (CoachingType)gameInstruction.instructionType,
                message = gameInstruction.instruction,
                voiceKey = GetVoiceKeyForType((CoachingType)gameInstruction.instructionType),
                urgency = gameInstruction.urgency,
                targetPosition = gameInstruction.visualCuePosition,
                visualColor = gameInstruction.instructionColor,
                duration = gameInstruction.duration,
                requiresGestureDemo = gameInstruction.urgency > 0.7f,
                demoAnimation = GetAnimationForType((CoachingType)gameInstruction.instructionType)
            };
        }
        
        private string GetVoiceKeyForType(CoachingType type)
        {
            switch (type)
            {
                case CoachingType.StanceCorrection:
                case CoachingType.FootPositioning:
                case CoachingType.HipRotation:
                    return "form_correction";
                case CoachingType.PowerGeneration:
                case CoachingType.AccuracyImprovement:
                    return "technique";
                case CoachingType.Motivation:
                    return "motivation";
                default:
                    return "technique";
            }
        }
        
        private string GetAnimationForType(CoachingType type)
        {
            switch (type)
            {
                case CoachingType.StanceCorrection:
                    return "DemoStance";
                case CoachingType.PowerGeneration:
                    return "DemoPowerPunch";
                case CoachingType.AccuracyImprovement:
                    return "DemoAccuracy";
                default:
                    return "DemoGeneral";
            }
        }
        
        private void OnPerformanceAnalyzed(AdvancedGameStateManager.PlayerPerformanceData performance)
        {
            // Update player weaknesses based on performance
            playerWeaknesses["stance"] = Mathf.Lerp(playerWeaknesses["stance"], 
                1f - performance.formConsistency, adaptiveLearningRate);
            playerWeaknesses["power"] = Mathf.Lerp(playerWeaknesses["power"], 
                1f - Mathf.Clamp01(performance.powerGeneration - 1f), adaptiveLearningRate);
            playerWeaknesses["accuracy"] = Mathf.Lerp(playerWeaknesses["accuracy"], 
                1f - performance.accuracy, adaptiveLearningRate);
        }
        
        private void OnFormAnalyzed(BoxingFormTracker.BoxingFormData formData)
        {
            // Real-time form feedback could trigger immediate visual cues
            if (formData.stanceQuality < 0.5f)
            {
                var instruction = new CoachingInstruction
                {
                    type = CoachingType.StanceCorrection,
                    message = "Adjust your stance - wider base needed",
                    voiceKey = "form_correction",
                    urgency = 0.8f,
                    visualColor = incorrectFormColor,
                    duration = 2f,
                    requiresGestureDemo = true
                };
                
                pendingInstructions.Enqueue(instruction);
            }
        }
        
        private void OnStanceChanged(BoxingFormTracker.BoxingStance newStance)
        {
            var instruction = new CoachingInstruction
            {
                type = CoachingType.StanceCorrection,
                message = $"Stance changed to {newStance}. Adjust your form accordingly.",
                voiceKey = "form_correction",
                urgency = 0.5f,
                visualColor = Color.yellow,
                duration = 2f
            };
            
            pendingInstructions.Enqueue(instruction);
        }
        
        // Public API
        public void ActivateAICoach()
        {
            if (activeHolographicCoach != null)
            {
                activeHolographicCoach.SetActive(true);
            }
            
            if (coachingText != null)
            {
                coachingText.text = "AI Coach Activated! Let's improve your form.";
            }
        }
        
        public void DeactivateAICoach()
        {
            if (activeHolographicCoach != null)
            {
                activeHolographicCoach.SetActive(false);
            }
            
            if (coachingText != null)
            {
                coachingText.text = "";
            }
        }
        
        public void StartPersonalizedTraining()
        {
            GeneratePersonalizedTrainingPlan();
            
            var instruction = new CoachingInstruction
            {
                type = CoachingType.Motivation,
                message = "Starting personalized training session!",
                voiceKey = "motivation",
                urgency = 0.6f,
                visualColor = Color.green,
                duration = 3f
            };
            
            pendingInstructions.Enqueue(instruction);
        }
        
        private void OnDestroy()
        {
            // Clean up any resources
            if (activeHolographicCoach != null)
            {
                Destroy(activeHolographicCoach);
            }
        }
    }
    
    // Holographic effect component
    public class HolographicEffect : MonoBehaviour
    {
        private Renderer[] renderers;
        private Material[] originalMaterials;
        private float pulseSpeed = 2f;
        private float glitchFrequency = 0.1f;
        
        public void Initialize()
        {
            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
            
            InvokeRepeating(nameof(RandomGlitch), 1f, glitchFrequency);
        }
        
        private void Update()
        {
            // Pulse effect
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed);
            
            foreach (var renderer in renderers)
            {
                Color color = renderer.material.color;
                color.a = pulse;
                renderer.material.color = color;
            }
        }
        
        private void RandomGlitch()
        {
            if (UnityEngine.Random.value < 0.1f)
            {
                StartCoroutine(GlitchEffect());
            }
        }
        
        private System.Collections.IEnumerator GlitchEffect()
        {
            // Brief glitch effect
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }
            
            yield return new WaitForSeconds(0.05f);
            
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
        }
    }
} 