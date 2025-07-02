using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Core;
using VRBoxingGame.Audio;
using VRBoxingGame.HandTracking;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Two-Handed Staff Mode System - Both hands control a single staff/stick for coordinated VR combat
    /// Unity 6 optimized with physics-based staff simulation and dual-hand tracking
    /// </summary>
    public class TwoHandedStaffSystem : MonoBehaviour
    {
        [Header("Staff Configuration")]
        public GameObject staffPrefab;
        public float staffLength = 1.5f;
        public float staffRadius = 0.05f;
        public Material staffMaterial;
        public bool enableStaffPhysics = true;
        
        [Header("Hand Tracking")]
        public Transform leftHandTransform;
        public Transform rightHandTransform;
        public float handGripStrength = 0.8f;
        public float maxGripDistance = 0.3f;
        public bool requireBothHandsGripped = true;
        
        [Header("Target Configuration")]
        public GameObject topTargetPrefab;
        public GameObject bottomTargetPrefab;
        public GameObject leftTargetPrefab;
        public GameObject rightTargetPrefab;
        public float targetSpawnDistance = 8f;
        public float targetSize = 0.4f;
        
        [Header("Spawn Zones")]
        public Transform topSpawnZone;
        public Transform bottomSpawnZone;
        public Transform leftSpawnZone;
        public Transform rightSpawnZone;
        public int spawnPointsPerZone = 5;
        public float zoneWidth = 4f;
        public float zoneHeight = 2f;
        
        [Header("Staff Combat Mechanics")]
        public float staffSwingThreshold = 2f;
        public float perfectSwingWindow = 0.1f;
        public float goodSwingWindow = 0.2f;
        public AnimationCurve swingPowerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool enableStaffCombo = true;
        public int maxComboLength = 8;
        
        [Header("Target Patterns")]
        public StaffPattern[] availablePatterns;
        public float patternDuration = 8f;
        public bool enableDynamicDifficulty = true;
        public float difficultyProgression = 0.1f;
        
        [Header("Visual Feedback")]
        public LineRenderer staffTrailRenderer;
        public ParticleSystem staffHitEffect;
        public Material leftGripMaterial;
        public Material rightGripMaterial;
        public Color staffActiveColor = Color.cyan;
        public Color staffInactiveColor = Color.gray;
        
        [Header("Audio")]
        public AudioSource staffAudioSource;
        public AudioClip[] staffSwingClips;
        public AudioClip[] staffHitClips;
        public AudioClip staffGripClip;
        public AudioClip staffReleaseClip;
        
        [Header("Events")]
        public UnityEvent<StaffHitData> OnStaffTargetHit;
        public UnityEvent<StaffComboData> OnStaffCombo;
        public UnityEvent<StaffGripData> OnStaffGripChanged;
        public UnityEvent<float> OnStaffSwingPower;
        
        // Data Structures
        [System.Serializable]
        public struct StaffTarget
        {
            public GameObject gameObject;
            public StaffZone spawnZone;
            public Vector3 spawnPosition;
            public Vector3 targetPosition;
            public float spawnTime;
            public float hitWindow;
            public float speed;
            public bool isHit;
            public bool requiresBothHands;
            public StaffSwingType requiredSwing;
        }
        
        [System.Serializable]
        public struct StaffHitData
        {
            public StaffZone hitZone;
            public float swingPower;
            public float accuracy;
            public bool isPerfectHit;
            public bool usedBothHands;
            public StaffSwingType swingType;
            public int score;
        }
        
        [System.Serializable]
        public struct StaffComboData
        {
            public int comboCount;
            public float comboMultiplier;
            public StaffSwingType[] comboSequence;
            public bool isComboComplete;
        }
        
        [System.Serializable]
        public struct StaffGripData
        {
            public bool leftHandGripped;
            public bool rightHandGripped;
            public bool bothHandsGripped;
            public float leftGripStrength;
            public float rightGripStrength;
            public Vector3 leftGripPosition;
            public Vector3 rightGripPosition;
        }
        
        [System.Serializable]
        public struct StaffPattern
        {
            public string patternName;
            public float difficulty;
            public StaffNote[] notes;
            public float beatsPerMinute;
            public bool requiresPrecision;
        }
        
        [System.Serializable]
        public struct StaffNote
        {
            public float beatTime;
            public StaffZone zone;
            public StaffSwingType swingType;
            public bool requiresBothHands;
            public float holdDuration;
        }
        
        public enum StaffZone
        {
            Top,
            Bottom,
            Left,
            Right,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }
        
        public enum StaffSwingType
        {
            Horizontal,
            Vertical,
            Diagonal,
            Thrust,
            Block,
            Spin
        }
        
        // Private Variables
        private GameObject activeStaff;
        private Rigidbody staffRigidbody;
        private ConfigurableJoint leftHandJoint;
        private ConfigurableJoint rightHandJoint;
        private List<StaffTarget> activeTargets = new List<StaffTarget>();
        private Queue<StaffNote> upcomingNotes = new Queue<StaffNote>();
        
        // Staff State
        private bool leftHandGripped = false;
        private bool rightHandGripped = false;
        private Vector3 leftGripLocalPosition;
        private Vector3 rightGripLocalPosition;
        private Vector3 lastStaffPosition;
        private Vector3 staffVelocity;
        private float lastSwingTime = 0f;
        
        // Combo System
        private List<StaffSwingType> currentCombo = new List<StaffSwingType>();
        private int comboCount = 0;
        private float comboMultiplier = 1f;
        private float lastHitTime = 0f;
        
        // Performance Tracking
        private int targetsHit = 0;
        private int targetsMissed = 0;
        private float totalSwingPower = 0f;
        private float averageAccuracy = 0f;
        
        // Spawn Points
        private Transform[][] spawnPoints;
        
        // Pattern Management
        private int currentPatternIndex = 0;
        private float patternStartTime = 0f;
        
        // Singleton
        public static TwoHandedStaffSystem Instance { get; private set; }
        
        // Properties
        public bool IsStaffModeActive { get; private set; }
        public bool IsBothHandsGripped => leftHandGripped && rightHandGripped;
        public float StaffLength => staffLength;
        public GameObject ActiveStaff => activeStaff;
        public int CurrentCombo => comboCount;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeStaffSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetupSpawnZones();
            SetupHandTracking();
            ConnectToAudioSystem();
        }
        
        private void Update()
        {
            if (IsStaffModeActive)
            {
                UpdateHandTracking();
                UpdateStaffPhysics();
                UpdateStaffTargets();
                ProcessUpcomingNotes();
                UpdateComboSystem();
                UpdateVisualEffects();
            }
        }
        
        private void FixedUpdate()
        {
            if (IsStaffModeActive && enableStaffPhysics)
            {
                UpdateStaffPhysicsFixed();
            }
        }
        
        private void InitializeStaffSystem()
        {
            Debug.Log("🥢 Initializing Two-Handed Staff System...");
            
            // Create default staff if none assigned
            if (staffPrefab == null)
            {
                staffPrefab = CreateDefaultStaff();
            }
            
            // Initialize spawn zones if not assigned
            InitializeDefaultSpawnZones();
        }
        
        private GameObject CreateDefaultStaff()
        {
            GameObject staff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            staff.name = "Two-Handed Staff";
            staff.transform.localScale = new Vector3(staffRadius * 2f, staffLength * 0.5f, staffRadius * 2f);
            
            // Add rigidbody for physics
            var rb = staff.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.drag = 1f;
            rb.angularDrag = 2f;
            rb.useGravity = false; // Controlled by hand positions
            
            // Add staff component
            var staffComponent = staff.AddComponent<StaffComponent>();
            
            return staff;
        }
        
        private void InitializeDefaultSpawnZones()
        {
            var playerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            
            if (topSpawnZone == null)
            {
                GameObject topZone = new GameObject("Top Spawn Zone");
                topSpawnZone = topZone.transform;
                topSpawnZone.position = playerPos + Vector3.up * 2f + Vector3.forward * targetSpawnDistance;
            }
            
            if (bottomSpawnZone == null)
            {
                GameObject bottomZone = new GameObject("Bottom Spawn Zone");
                bottomSpawnZone = bottomZone.transform;
                bottomSpawnZone.position = playerPos + Vector3.down * 0.5f + Vector3.forward * targetSpawnDistance;
            }
            
            if (leftSpawnZone == null)
            {
                GameObject leftZone = new GameObject("Left Spawn Zone");
                leftSpawnZone = leftZone.transform;
                leftSpawnZone.position = playerPos + Vector3.left * 3f + Vector3.forward * targetSpawnDistance;
            }
            
            if (rightSpawnZone == null)
            {
                GameObject rightZone = new GameObject("Right Spawn Zone");
                rightSpawnZone = rightZone.transform;
                rightSpawnZone.position = playerPos + Vector3.right * 3f + Vector3.forward * targetSpawnDistance;
            }
        }
        
        private void SetupSpawnZones()
        {
            spawnPoints = new Transform[4][];
            
            // Create spawn points for each zone
            spawnPoints[0] = CreateSpawnPoints(topSpawnZone, "Top");
            spawnPoints[1] = CreateSpawnPoints(bottomSpawnZone, "Bottom");
            spawnPoints[2] = CreateSpawnPoints(leftSpawnZone, "Left");
            spawnPoints[3] = CreateSpawnPoints(rightSpawnZone, "Right");
            
            Debug.Log("🎯 Staff spawn zones configured");
        }
        
        private Transform[] CreateSpawnPoints(Transform parentZone, string zoneName)
        {
            Transform[] points = new Transform[spawnPointsPerZone];
            
            for (int i = 0; i < spawnPointsPerZone; i++)
            {
                GameObject spawnPoint = new GameObject($"{zoneName} Spawn Point {i}");
                spawnPoint.transform.SetParent(parentZone);
                
                // Distribute points across zone
                float normalizedPos = (float)i / (spawnPointsPerZone - 1) - 0.5f;
                Vector3 offset = Vector3.zero;
                
                if (zoneName == "Top" || zoneName == "Bottom")
                {
                    offset = Vector3.right * normalizedPos * zoneWidth;
                }
                else
                {
                    offset = Vector3.up * normalizedPos * zoneHeight;
                }
                
                spawnPoint.transform.localPosition = offset;
                points[i] = spawnPoint.transform;
            }
            
            return points;
        }
        
        private void SetupHandTracking()
        {
            // Find hand transforms if not assigned
            if (leftHandTransform == null)
            {
                var handTrackingManager = CachedReferenceManager.Get<HandTrackingManager>();
                if (handTrackingManager != null)
                {
                    leftHandTransform = handTrackingManager.leftHandTransform;
                }
            }
            
            if (rightHandTransform == null)
            {
                var handTrackingManager = CachedReferenceManager.Get<HandTrackingManager>();
                if (handTrackingManager != null)
                {
                    rightHandTransform = handTrackingManager.rightHandTransform;
                }
            }
        }
        
        private void ConnectToAudioSystem()
        {
            if (staffAudioSource == null)
            {
                staffAudioSource = gameObject.AddComponent<AudioSource>();
                staffAudioSource.spatialBlend = 1f;
                staffAudioSource.volume = 0.7f;
            }
        }
        
        private void UpdateHandTracking()
        {
            if (activeStaff == null || leftHandTransform == null || rightHandTransform == null) return;
            
            // Check grip status
            bool newLeftGrip = IsHandGripping(leftHandTransform, activeStaff.transform);
            bool newRightGrip = IsHandGripping(rightHandTransform, activeStaff.transform);
            
            // Handle grip changes
            if (newLeftGrip != leftHandGripped)
            {
                OnGripChanged(true, newLeftGrip);
                leftHandGripped = newLeftGrip;
                
                if (newLeftGrip)
                {
                    leftGripLocalPosition = activeStaff.transform.InverseTransformPoint(leftHandTransform.position);
                }
            }
            
            if (newRightGrip != rightHandGripped)
            {
                OnGripChanged(false, newRightGrip);
                rightHandGripped = newRightGrip;
                
                if (newRightGrip)
                {
                    rightGripLocalPosition = activeStaff.transform.InverseTransformPoint(rightHandTransform.position);
                }
            }
            
            // Update grip data
            var gripData = new StaffGripData
            {
                leftHandGripped = leftHandGripped,
                rightHandGripped = rightHandGripped,
                bothHandsGripped = IsBothHandsGripped,
                leftGripStrength = CalculateGripStrength(leftHandTransform, activeStaff.transform),
                rightGripStrength = CalculateGripStrength(rightHandTransform, activeStaff.transform),
                leftGripPosition = leftHandGripped ? leftHandTransform.position : Vector3.zero,
                rightGripPosition = rightHandGripped ? rightHandTransform.position : Vector3.zero
            };
            
            OnStaffGripChanged?.Invoke(gripData);
        }
        
        private bool IsHandGripping(Transform hand, Transform staff)
        {
            if (hand == null || staff == null) return false;
            
            float distance = Vector3.Distance(hand.position, staff.position);
            return distance <= maxGripDistance;
        }
        
        private float CalculateGripStrength(Transform hand, Transform staff)
        {
            if (hand == null || staff == null) return 0f;
            
            float distance = Vector3.Distance(hand.position, staff.position);
            return Mathf.Clamp01(1f - (distance / maxGripDistance));
        }
        
        private void OnGripChanged(bool isLeftHand, bool isGripped)
        {
            if (isGripped)
            {
                PlayAudioClip(staffGripClip);
                Debug.Log($"🤲 {(isLeftHand ? "Left" : "Right")} hand gripped staff");
            }
            else
            {
                PlayAudioClip(staffReleaseClip);
                Debug.Log($"👋 {(isLeftHand ? "Left" : "Right")} hand released staff");
            }
        }
        
        private void UpdateStaffPhysics()
        {
            if (activeStaff == null || !enableStaffPhysics) return;
            
            // Calculate staff velocity
            Vector3 currentPos = activeStaff.transform.position;
            staffVelocity = (currentPos - lastStaffPosition) / Time.deltaTime;
            lastStaffPosition = currentPos;
            
            // Detect swings
            float swingMagnitude = staffVelocity.magnitude;
            if (swingMagnitude > staffSwingThreshold && Time.time - lastSwingTime > 0.2f)
            {
                OnStaffSwing(swingMagnitude);
                lastSwingTime = Time.time;
            }
        }
        
        private void UpdateStaffPhysicsFixed()
        {
            if (activeStaff == null || staffRigidbody == null) return;
            
            // Apply forces based on hand positions
            if (IsBothHandsGripped)
            {
                Vector3 targetPosition = (leftHandTransform.position + rightHandTransform.position) * 0.5f;
                Vector3 force = (targetPosition - activeStaff.transform.position) * 50f;
                staffRigidbody.AddForce(force);
                
                // Apply rotation based on hand orientations
                Vector3 handDirection = (rightHandTransform.position - leftHandTransform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(handDirection, Vector3.up);
                Vector3 torque = Vector3.Cross(activeStaff.transform.forward, targetRotation * Vector3.forward) * 20f;
                staffRigidbody.AddTorque(torque);
            }
            else if (leftHandGripped || rightHandGripped)
            {
                // Single hand control - more difficult
                Transform controllingHand = leftHandGripped ? leftHandTransform : rightHandTransform;
                Vector3 force = (controllingHand.position - activeStaff.transform.position) * 30f;
                staffRigidbody.AddForce(force);
            }
        }
        
        private void OnStaffSwing(float swingPower)
        {
            // Calculate swing type based on staff movement
            StaffSwingType swingType = DetermineSwingType(staffVelocity);
            
            // Play swing audio
            PlayRandomSwingClip();
            
            // Check for targets in swing path
            CheckForTargetHits(swingPower, swingType);
            
            // Trigger swing power event
            OnStaffSwingPower?.Invoke(swingPower);
            
            Debug.Log($"🥢 Staff swing detected: {swingType} with power {swingPower:F2}");
        }
        
        private StaffSwingType DetermineSwingType(Vector3 velocity)
        {
            Vector3 normalizedVel = velocity.normalized;
            
            // Analyze dominant movement direction
            if (Mathf.Abs(normalizedVel.x) > 0.7f)
                return StaffSwingType.Horizontal;
            else if (Mathf.Abs(normalizedVel.y) > 0.7f)
                return StaffSwingType.Vertical;
            else if (Mathf.Abs(normalizedVel.z) > 0.7f)
                return StaffSwingType.Thrust;
            else
                return StaffSwingType.Diagonal;
        }
        
        private void CheckForTargetHits(float swingPower, StaffSwingType swingType)
        {
            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                var target = activeTargets[i];
                
                if (target.isHit) continue;
                
                float distance = Vector3.Distance(activeStaff.transform.position, target.gameObject.transform.position);
                
                if (distance <= targetSize + staffLength * 0.5f)
                {
                    // Check if swing type matches requirement
                    if (target.requiredSwing == swingType || target.requiredSwing == StaffSwingType.Block)
                    {
                        ProcessTargetHit(target, swingPower, swingType);
                        activeTargets.RemoveAt(i);
                    }
                }
            }
        }
        
        private void ProcessTargetHit(StaffTarget target, float swingPower, StaffSwingType swingType)
        {
            // Calculate accuracy based on timing and positioning
            float timingAccuracy = CalculateTimingAccuracy(target);
            float positionAccuracy = CalculatePositionAccuracy(target);
            float overallAccuracy = (timingAccuracy + positionAccuracy) * 0.5f;
            
            // Determine if it's a perfect hit
            bool isPerfectHit = overallAccuracy >= 0.9f && swingPower >= staffSwingThreshold * 1.5f;
            
            // Calculate score
            int baseScore = isPerfectHit ? 200 : 150;
            float powerMultiplier = swingPowerCurve.Evaluate(swingPower / (staffSwingThreshold * 2f));
            int finalScore = Mathf.RoundToInt(baseScore * overallAccuracy * powerMultiplier * comboMultiplier);
            
            // Create hit data
            var hitData = new StaffHitData
            {
                hitZone = target.spawnZone,
                swingPower = swingPower,
                accuracy = overallAccuracy,
                isPerfectHit = isPerfectHit,
                usedBothHands = IsBothHandsGripped,
                swingType = swingType,
                score = finalScore
            };
            
            // Update statistics
            targetsHit++;
            totalSwingPower += swingPower;
            averageAccuracy = (averageAccuracy * (targetsHit - 1) + overallAccuracy) / targetsHit;
            
            // Update combo
            UpdateCombo(swingType);
            
            // Mark target as hit
            target.isHit = true;
            
            // Add score to game
            GameManager.Instance?.AddScore(finalScore, isPerfectHit);
            
            // Play hit effects
            PlayHitEffects(target.gameObject.transform.position, isPerfectHit);
            
            // Destroy target
            if (target.gameObject != null)
            {
                Destroy(target.gameObject);
            }
            
            // Trigger events
            OnStaffTargetHit?.Invoke(hitData);
            
            Debug.Log($"🎯 Staff target hit! Zone: {target.spawnZone}, Accuracy: {overallAccuracy:F2}, Score: {finalScore}");
        }
        
        private float CalculateTimingAccuracy(StaffTarget target)
        {
            float currentTime = Time.time;
            float targetTime = target.spawnTime + target.hitWindow;
            float timingDifference = Mathf.Abs(currentTime - targetTime);
            
            if (timingDifference <= perfectSwingWindow)
                return 1f;
            else if (timingDifference <= goodSwingWindow)
                return 0.8f;
            else
                return Mathf.Max(0.2f, 1f - (timingDifference / goodSwingWindow));
        }
        
        private float CalculatePositionAccuracy(StaffTarget target)
        {
            float distance = Vector3.Distance(activeStaff.transform.position, target.gameObject.transform.position);
            float maxDistance = targetSize + staffLength * 0.5f;
            return Mathf.Clamp01(1f - (distance / maxDistance));
        }
        
        private void UpdateCombo(StaffSwingType swingType)
        {
            // Add to current combo
            currentCombo.Add(swingType);
            comboCount++;
            
            // Update combo multiplier
            comboMultiplier = 1f + (comboCount * 0.1f);
            comboMultiplier = Mathf.Min(comboMultiplier, 3f);
            
            // Check for combo patterns
            if (enableStaffCombo && currentCombo.Count >= 3)
            {
                CheckForComboPatterns();
            }
            
            // Reset combo timer
            lastHitTime = Time.time;
            
            // Trigger combo event
            var comboData = new StaffComboData
            {
                comboCount = comboCount,
                comboMultiplier = comboMultiplier,
                comboSequence = currentCombo.ToArray(),
                isComboComplete = false
            };
            OnStaffCombo?.Invoke(comboData);
        }
        
        private void CheckForComboPatterns()
        {
            // Check for specific combo patterns (e.g., alternating, spinning)
            if (currentCombo.Count >= 4)
            {
                bool isAlternating = true;
                for (int i = 2; i < currentCombo.Count; i++)
                {
                    if (currentCombo[i] == currentCombo[i-2])
                    {
                        isAlternating = false;
                        break;
                    }
                }
                
                if (isAlternating)
                {
                    comboMultiplier *= 1.2f;
                    Debug.Log("🔄 Alternating combo bonus!");
                }
            }
        }
        
        private void UpdateStaffTargets()
        {
            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                var target = activeTargets[i];
                
                if (target.gameObject == null)
                {
                    activeTargets.RemoveAt(i);
                    continue;
                }
                
                // Move target towards player
                float progress = (Time.time - target.spawnTime) / (target.hitWindow);
                if (progress <= 1f)
                {
                    Vector3 currentPos = Vector3.Lerp(target.spawnPosition, target.targetPosition, progress);
                    target.gameObject.transform.position = currentPos;
                }
                else if (!target.isHit)
                {
                    // Target missed
                    OnTargetMissed(target);
                    activeTargets.RemoveAt(i);
                }
            }
        }
        
        private void ProcessUpcomingNotes()
        {
            // Process upcoming notes based on current pattern
            if (availablePatterns != null && availablePatterns.Length > 0)
            {
                ProcessPatternNotes();
            }
        }
        
        private void ProcessPatternNotes()
        {
            // Check if it's time to advance to next pattern
            if (Time.time - patternStartTime >= patternDuration)
            {
                AdvanceToNextPattern();
            }
            
            // Spawn targets based on current pattern
            var currentPattern = availablePatterns[currentPatternIndex];
            foreach (var note in currentPattern.notes)
            {
                float spawnTime = patternStartTime + note.beatTime;
                if (Time.time >= spawnTime && !HasNoteBeenSpawned(note, spawnTime))
                {
                    SpawnTargetFromNote(note);
                }
            }
        }
        
        private void AdvanceToNextPattern()
        {
            currentPatternIndex = (currentPatternIndex + 1) % availablePatterns.Length;
            patternStartTime = Time.time;
            
            Debug.Log($"🎼 Advanced to pattern: {availablePatterns[currentPatternIndex].patternName}");
        }
        
        private bool HasNoteBeenSpawned(StaffNote note, float spawnTime)
        {
            // Check if this note has already been spawned
            foreach (var target in activeTargets)
            {
                if (Mathf.Abs(target.spawnTime - spawnTime) < 0.1f)
                {
                    return true;
                }
            }
            return false;
        }
        
        private void SpawnTargetFromNote(StaffNote note)
        {
            SpawnTarget(note.zone, note.swingType, note.requiresBothHands);
        }
        
        private void UpdateComboSystem()
        {
            // Reset combo if too much time has passed
            if (Time.time - lastHitTime > 3f && comboCount > 0)
            {
                ResetCombo();
            }
        }
        
        private void ResetCombo()
        {
            currentCombo.Clear();
            comboCount = 0;
            comboMultiplier = 1f;
            
            var comboData = new StaffComboData
            {
                comboCount = 0,
                comboMultiplier = 1f,
                comboSequence = new StaffSwingType[0],
                isComboComplete = true
            };
            OnStaffCombo?.Invoke(comboData);
            
            Debug.Log("💥 Combo reset");
        }
        
        private void UpdateVisualEffects()
        {
            if (activeStaff == null) return;
            
            // Update staff trail
            if (staffTrailRenderer != null)
            {
                staffTrailRenderer.enabled = staffVelocity.magnitude > staffSwingThreshold * 0.5f;
                
                if (staffTrailRenderer.enabled)
                {
                    // Update trail color based on swing power
                    float intensity = Mathf.Clamp01(staffVelocity.magnitude / (staffSwingThreshold * 2f));
                    Color trailColor = Color.Lerp(Color.white, Color.red, intensity);
                    staffTrailRenderer.startColor = trailColor;
                    staffTrailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
                }
            }
            
            // Update staff color based on grip status
            var renderer = activeStaff.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color staffColor = IsBothHandsGripped ? staffActiveColor : staffInactiveColor;
                renderer.material.color = staffColor;
            }
        }
        
        private void SpawnTarget(StaffZone zone, StaffSwingType requiredSwing, bool requiresBothHands)
        {
            // Get spawn point for zone
            Transform spawnPoint = GetSpawnPointForZone(zone);
            if (spawnPoint == null) return;
            
            // Choose appropriate target prefab
            GameObject targetPrefab = GetTargetPrefabForZone(zone);
            if (targetPrefab == null) return;
            
            // Spawn target
            GameObject targetObj = Instantiate(targetPrefab, spawnPoint.position, spawnPoint.rotation);
            targetObj.transform.localScale = Vector3.one * targetSize;
            
            // Calculate target position (near player)
            Vector3 playerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            Vector3 targetPos = playerPos + GetTargetOffsetForZone(zone);
            
            // Create target data
            var target = new StaffTarget
            {
                gameObject = targetObj,
                spawnZone = zone,
                spawnPosition = spawnPoint.position,
                targetPosition = targetPos,
                spawnTime = Time.time,
                hitWindow = 3f,
                speed = 5f,
                isHit = false,
                requiresBothHands = requiresBothHands,
                requiredSwing = requiredSwing
            };
            
            // Add target component
            var targetComponent = targetObj.GetComponent<StaffTargetComponent>();
            if (targetComponent == null)
            {
                targetComponent = targetObj.AddComponent<StaffTargetComponent>();
            }
            targetComponent.Initialize(target);
            
            activeTargets.Add(target);
            
            Debug.Log($"🎯 Spawned staff target in {zone} zone, swing type: {requiredSwing}");
        }
        
        private Transform GetSpawnPointForZone(StaffZone zone)
        {
            int zoneIndex = (int)zone % 4; // Map to basic zones
            if (zoneIndex < spawnPoints.Length && spawnPoints[zoneIndex] != null)
            {
                int pointIndex = UnityEngine.Random.Range(0, spawnPoints[zoneIndex].Length);
                return spawnPoints[zoneIndex][pointIndex];
            }
            return null;
        }
        
        private GameObject GetTargetPrefabForZone(StaffZone zone)
        {
            switch (zone)
            {
                case StaffZone.Top:
                case StaffZone.TopLeft:
                case StaffZone.TopRight:
                    return topTargetPrefab;
                case StaffZone.Bottom:
                case StaffZone.BottomLeft:
                case StaffZone.BottomRight:
                    return bottomTargetPrefab;
                case StaffZone.Left:
                    return leftTargetPrefab;
                case StaffZone.Right:
                    return rightTargetPrefab;
                default:
                    return topTargetPrefab;
            }
        }
        
        private Vector3 GetTargetOffsetForZone(StaffZone zone)
        {
            switch (zone)
            {
                case StaffZone.Top:
                    return Vector3.up * 0.5f + Vector3.forward * 1f;
                case StaffZone.Bottom:
                    return Vector3.down * 0.5f + Vector3.forward * 1f;
                case StaffZone.Left:
                    return Vector3.left * 1f + Vector3.forward * 1f;
                case StaffZone.Right:
                    return Vector3.right * 1f + Vector3.forward * 1f;
                default:
                    return Vector3.forward * 1f;
            }
        }
        
        private void OnTargetMissed(StaffTarget target)
        {
            targetsMissed++;
            ResetCombo();
            
            if (target.gameObject != null)
            {
                Destroy(target.gameObject);
            }
            
            Debug.Log($"❌ Staff target missed in {target.spawnZone} zone");
        }
        
        private void PlayHitEffects(Vector3 position, bool isPerfectHit)
        {
            // Play hit particle effect
            if (staffHitEffect != null)
            {
                staffHitEffect.transform.position = position;
                staffHitEffect.Play();
                
                if (isPerfectHit)
                {
                    var main = staffHitEffect.main;
                    main.startColor = Color.gold;
                }
            }
            
            // Play hit audio
            PlayRandomHitClip();
        }
        
        private void PlayRandomSwingClip()
        {
            if (staffSwingClips != null && staffSwingClips.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, staffSwingClips.Length);
                PlayAudioClip(staffSwingClips[index]);
            }
        }
        
        private void PlayRandomHitClip()
        {
            if (staffHitClips != null && staffHitClips.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, staffHitClips.Length);
                PlayAudioClip(staffHitClips[index]);
            }
        }
        
        private void PlayAudioClip(AudioClip clip)
        {
            if (clip != null && staffAudioSource != null)
            {
                staffAudioSource.PlayOneShot(clip);
            }
        }
        
        // Public API
        public void StartStaffMode()
        {
            IsStaffModeActive = true;
            
            // Spawn staff
            if (activeStaff == null)
            {
                Vector3 spawnPos = Camera.main != null ? Camera.main.transform.position + Vector3.forward * 0.5f : Vector3.forward * 0.5f;
                activeStaff = Instantiate(staffPrefab, spawnPos, Quaternion.identity);
                staffRigidbody = activeStaff.GetComponent<Rigidbody>();
                lastStaffPosition = activeStaff.transform.position;
            }
            
            // Reset statistics
            targetsHit = 0;
            targetsMissed = 0;
            totalSwingPower = 0f;
            averageAccuracy = 0f;
            ResetCombo();
            
            // Start first pattern
            patternStartTime = Time.time;
            currentPatternIndex = 0;
            
            Debug.Log("🥢 Two-Handed Staff Mode started!");
        }
        
        public void StopStaffMode()
        {
            IsStaffModeActive = false;
            
            // Clean up staff
            if (activeStaff != null)
            {
                Destroy(activeStaff);
                activeStaff = null;
                staffRigidbody = null;
            }
            
            // Clear targets
            ClearAllTargets();
            
            Debug.Log("🥢 Two-Handed Staff Mode stopped");
        }
        
        public void ClearAllTargets()
        {
            foreach (var target in activeTargets)
            {
                if (target.gameObject != null)
                {
                    Destroy(target.gameObject);
                }
            }
            activeTargets.Clear();
        }
        
        public void SetDifficulty(float difficulty)
        {
            difficulty = Mathf.Clamp01(difficulty);
            
            // Adjust parameters based on difficulty
            staffSwingThreshold = Mathf.Lerp(1.5f, 3f, difficulty);
            perfectSwingWindow = Mathf.Lerp(0.15f, 0.08f, difficulty);
            goodSwingWindow = Mathf.Lerp(0.3f, 0.15f, difficulty);
            
            // Adjust target parameters
            targetSize = Mathf.Lerp(0.5f, 0.3f, difficulty);
        }
        
        // Statistics
        public Dictionary<string, object> GetStaffModeStats()
        {
            return new Dictionary<string, object>
            {
                {"is_active", IsStaffModeActive},
                {"targets_hit", targetsHit},
                {"targets_missed", targetsMissed},
                {"hit_accuracy", HitAccuracy},
                {"current_combo", currentCombo},
                {"max_combo", maxCombo},
                {"average_swing_power", targetsHit > 0 ? totalSwingPower / targetsHit : 0f},
                {"average_accuracy", averageAccuracy},
                {"left_hand_grip", isLeftHandGripping},
                {"right_hand_grip", isRightHandGripping},
                {"current_pattern", availablePatterns != null && availablePatterns.Length > 0 && currentPatternIndex < availablePatterns.Length ? availablePatterns[currentPatternIndex].patternName : "None"},
                {"pattern_progress", GetCurrentPatternProgress()},
                {"staff_position", activeStaff != null ? activeStaff.transform.position : Vector3.zero},
                {"staff_velocity", staffRigidbody != null ? staffRigidbody.velocity.magnitude : 0f}
            };
        }
        
        private float GetCurrentPatternProgress()
        {
            if (availablePatterns == null || availablePatterns.Length == 0) return 0f;
            if (currentPatternIndex >= availablePatterns.Length) return 0f;
            
            var currentPattern = availablePatterns[currentPatternIndex];
            float patternDuration = currentPattern.beatsPerMinute * (60f / 120f); // Assuming 120 BPM
            float elapsed = Time.time - patternStartTime;
            
            return Mathf.Clamp01(elapsed / patternDuration);
        }
        
        private void OnDestroy()
        {
            StopStaffMode();
        }        
    }
    
    // Staff Component
    public class StaffComponent : MonoBehaviour
    {
        public TwoHandedStaffSystem staffSystem;
        
        private void Start()
        {
            staffSystem = TwoHandedStaffSystem.Instance;
        }
    }
    
    // Staff Target Component
    public class StaffTargetComponent : MonoBehaviour
    {
        private TwoHandedStaffSystem.StaffTarget targetData;
        
        public void Initialize(TwoHandedStaffSystem.StaffTarget data)
        {
            targetData = data;
        }
        
        public TwoHandedStaffSystem.StaffTarget GetTargetData()
        {
            return targetData;
        }
    }
} 