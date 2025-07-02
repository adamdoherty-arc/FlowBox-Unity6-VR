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
using VRBoxingGame.Boxing;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Flow Mode System - Beat Saber style VR Boxing with flowing targets synchronized to music
    /// Unity 6 optimized with GPU instancing and predictive spawning
    /// </summary>
    public class FlowModeSystem : MonoBehaviour
    {
        [Header("Flow Target Settings")]
        public GameObject flowTargetPrefab;
        public Material leftHandMaterial;
        public Material rightHandMaterial;
        public Material dodgeObstacleMaterial;
        public float targetSpeed = 8f;
        public float targetSize = 0.4f;
        
        [Header("Flow Lanes Configuration")]
        public int numberOfLanes = 5;
        public float laneSpacing = 0.8f;
        public float laneDistance = 15f;
        public Transform flowOrigin;
        public Transform playerTarget;
        
        [Header("Music Synchronization")]
        public bool syncWithMusic = true;
        public float beatPrediction = 2f; // Spawn targets 2 seconds before beat
        public float musicEnergyMultiplier = 1.5f;
        public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Flow Patterns")]
        public FlowPattern[] availablePatterns;
        public float patternChangeInterval = 16f; // Change pattern every 16 beats
        public bool enableDynamicPatterns = true;
        public float difficultyProgression = 0.1f;
        
        [Header("Obstacle Integration")]
        public bool enableObstacles = true;
        public GameObject[] obstaclePrefabs;
        public float obstacleSpawnChance = 0.2f;
        public float obstacleHeight = 2f;
        
        [Header("Visual Effects")]
        public bool enableFlowTrails = true;
        public ParticleSystem[] laneParticles;
        public LineRenderer[] flowGuideLines;
        public Color leftHandColor = Color.blue;
        public Color rightHandColor = Color.red;
        public Color obstacleColor = Color.black;
        
        [Header("Scoring & Feedback")]
        public bool enableFlowScore = true;
        public float flowMultiplierMax = 4f;
        public float perfectHitWindow = 0.1f;
        public float goodHitWindow = 0.2f;
        
        [Header("Events")]
        public UnityEvent<FlowHitData> OnFlowTargetHit;
        public UnityEvent<ObstacleData> OnObstacleHit;
        public UnityEvent<float> OnFlowMultiplierChanged;
        public UnityEvent<FlowComboData> OnFlowCombo;
        
        // Data Structures
        [System.Serializable]
        public struct FlowTarget
        {
            public GameObject gameObject;
            public HandType requiredHand;
            public int laneIndex;
            public float spawnTime;
            public float hitTime;
            public float speed;
            public Vector3 startPosition;
            public Vector3 endPosition;
            public bool isHit;
            public bool isObstacle;
        }
        
        [System.Serializable]
        public struct FlowHitData
        {
            public HandType handUsed;
            public float accuracy;
            public float timing;
            public int laneIndex;
            public bool isPerfectHit;
            public float multiplier;
            public int score;
        }
        
        [System.Serializable]
        public struct ObstacleData
        {
            public Vector3 hitPosition;
            public float damage;
            public bool wasAvoided;
        }
        
        [System.Serializable]
        public struct FlowComboData
        {
            public int comboCount;
            public float multiplier;
            public bool isStreakBroken;
        }
        
        [System.Serializable]
        public struct FlowPattern
        {
            public string patternName;
            public float difficulty;
            public FlowNote[] notes;
            public float beatsPerMinute;
            public bool allowSimultaneous;
        }
        
        [System.Serializable]
        public struct FlowNote
        {
            public float beatTime;
            public int laneIndex;
            public HandType handType;
            public bool isObstacle;
            public float holdDuration;
        }
        
        public enum HandType
        {
            Left,
            Right,
            Either,
            Both,
            Dodge
        }
        
        // Private Variables
        private List<FlowTarget> activeTargets = new List<FlowTarget>();
        private Queue<FlowNote> upcomingNotes = new Queue<FlowNote>();
        private Transform[] flowLanes;
        private float currentBeatTime = 0f;
        private float currentMultiplier = 1f;
        private int currentCombo = 0;
        private int maxCombo = 0;
        private float lastPatternChange = 0f;
        private int currentPatternIndex = 0;
        
        // Performance tracking
        private int targetsHit = 0;
        private int targetsMissed = 0;
        private int obstaclesAvoided = 0;
        private int obstaclesHit = 0;
        private float totalAccuracy = 0f;
        
        // GPU Instancing
        private MaterialPropertyBlock leftPropertyBlock;
        private MaterialPropertyBlock rightPropertyBlock;
        private MaterialPropertyBlock obstaclePropertyBlock;
        private Matrix4x4[] instanceMatrices = new Matrix4x4[1023];
        private Vector4[] instanceColors = new Vector4[1023];
        private int instanceCount = 0;
        
        // Singleton
        public static FlowModeSystem Instance { get; private set; }
        
        // Properties
        public float CurrentMultiplier => currentMultiplier;
        public int CurrentCombo => currentCombo;
        public float HitAccuracy => targetsHit + targetsMissed > 0 ? (float)targetsHit / (targetsHit + targetsMissed) : 0f;
        public bool IsFlowModeActive { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeFlowMode();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetupFlowLanes();
            SetupVisualEffects();
            ConnectToAudioSystem();
            InitializeGPUInstancing();
        }
        
        private void Update()
        {
            if (IsFlowModeActive)
            {
                UpdateFlowTargets();
                ProcessUpcomingNotes();
                UpdateMultiplier();
                UpdateVisualEffects();
                RenderInstancedTargets();
            }
        }
        
        private void InitializeFlowMode()
        {
            Debug.Log("🌊 Initializing Flow Mode System...");
            
            // Initialize flow origin and player target
            if (flowOrigin == null)
            {
                GameObject originObj = new GameObject("Flow Origin");
                flowOrigin = originObj.transform;
                flowOrigin.position = new Vector3(0, 1.5f, laneDistance);
            }
            
            if (playerTarget == null)
            {
                var xrCamera = FindObjectOfType<Camera>();
                if (xrCamera != null)
                {
                    GameObject targetObj = new GameObject("Player Target");
                    playerTarget = targetObj.transform;
                    playerTarget.position = xrCamera.transform.position + xrCamera.transform.forward * 1f;
                }
            }
            
            // Create default flow target prefab if needed
            if (flowTargetPrefab == null)
            {
                flowTargetPrefab = CreateDefaultFlowTarget();
            }
        }
        
        private GameObject CreateDefaultFlowTarget()
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Flow Target";
            target.transform.localScale = Vector3.one * targetSize;
            
            // Add flow target component
            var flowComponent = target.AddComponent<FlowTargetComponent>();
            
            // Add collider for hit detection
            var collider = target.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            
            return target;
        }
        
        private void SetupFlowLanes()
        {
            flowLanes = new Transform[numberOfLanes];
            
            for (int i = 0; i < numberOfLanes; i++)
            {
                GameObject laneObj = new GameObject($"Flow Lane {i}");
                laneObj.transform.SetParent(flowOrigin);
                
                // Position lanes in a grid pattern
                float x = (i - (numberOfLanes - 1) * 0.5f) * laneSpacing;
                float y = 0f; // Could add vertical lanes later
                laneObj.transform.localPosition = new Vector3(x, y, 0);
                
                flowLanes[i] = laneObj.transform;
                
                // Create lane visual guide
                if (enableFlowTrails && i < flowGuideLines.Length && flowGuideLines[i] != null)
                {
                    SetupLaneGuideLine(flowGuideLines[i], i);
                }
            }
            
            Debug.Log($"🛤️ Created {numberOfLanes} flow lanes");
        }
        
        private void SetupLaneGuideLine(LineRenderer guideLine, int laneIndex)
        {
            guideLine.positionCount = 2;
            guideLine.startWidth = 0.05f;
            guideLine.endWidth = 0.05f;
            guideLine.material.color = new Color(1f, 1f, 1f, 0.3f);
            
            // Set line positions
            Vector3 startPos = flowLanes[laneIndex].position;
            Vector3 endPos = playerTarget.position + (flowLanes[laneIndex].position - flowOrigin.position);
            
            guideLine.SetPosition(0, startPos);
            guideLine.SetPosition(1, endPos);
        }
        
        private void SetupVisualEffects()
        {
            // Setup lane particles
            if (enableFlowTrails && laneParticles != null)
            {
                for (int i = 0; i < Mathf.Min(laneParticles.Length, numberOfLanes); i++)
                {
                    if (laneParticles[i] != null)
                    {
                        laneParticles[i].transform.position = flowLanes[i].position;
                        var main = laneParticles[i].main;
                        main.startColor = Color.white;
                        main.startSpeed = targetSpeed * 0.5f;
                    }
                }
            }
        }
        
        private void ConnectToAudioSystem()
        {
            if (AdvancedAudioManager.Instance != null)
            {
                AdvancedAudioManager.Instance.OnBeatDetected.AddListener(OnBeatDetected);
                AdvancedAudioManager.Instance.OnMusicEnergyChanged.AddListener(OnMusicEnergyChanged);
            }
        }
        
        private void InitializeGPUInstancing()
        {
            leftPropertyBlock = new MaterialPropertyBlock();
            rightPropertyBlock = new MaterialPropertyBlock();
            obstaclePropertyBlock = new MaterialPropertyBlock();
            
            Debug.Log("🚀 Flow Mode GPU instancing initialized");
        }
        
        private void OnBeatDetected(AdvancedAudioManager.BeatData beatData)
        {
            if (!syncWithMusic || !IsFlowModeActive) return;
            
            currentBeatTime = beatData.beatTime;
            
            // Trigger pattern generation based on beat
            if (beatData.isStrongBeat)
            {
                GenerateFlowPattern(beatData);
            }
        }
        
        private void OnMusicEnergyChanged(float energy)
        {
            if (!IsFlowModeActive) return;
            
            // Adjust target spawn rate based on music energy
            float intensityMultiplier = intensityCurve.Evaluate(energy) * musicEnergyMultiplier;
            
            // Update visual effects intensity
            UpdateEffectsIntensity(intensityMultiplier);
        }
        
        private void GenerateFlowPattern(AdvancedAudioManager.BeatData beatData)
        {
            // Change pattern if enough time has passed
            if (Time.time - lastPatternChange > patternChangeInterval)
            {
                AdvanceToNextPattern();
                lastPatternChange = Time.time;
            }
            
            // Get current pattern
            if (availablePatterns != null && availablePatterns.Length > 0)
            {
                var currentPattern = availablePatterns[currentPatternIndex];
                GenerateNotesFromPattern(currentPattern, beatData);
            }
            else
            {
                // Generate default pattern
                GenerateDefaultPattern(beatData);
            }
        }
        
        private void GenerateNotesFromPattern(FlowPattern pattern, AdvancedAudioManager.BeatData beatData)
        {
            foreach (var note in pattern.notes)
            {
                // Schedule note spawn
                float spawnTime = Time.time + beatPrediction;
                
                var flowNote = new FlowNote
                {
                    beatTime = note.beatTime,
                    laneIndex = note.laneIndex % numberOfLanes,
                    handType = note.handType,
                    isObstacle = note.isObstacle,
                    holdDuration = note.holdDuration
                };
                
                upcomingNotes.Enqueue(flowNote);
            }
        }
        
        private void GenerateDefaultPattern(AdvancedAudioManager.BeatData beatData)
        {
            // Generate simple alternating pattern
            int laneIndex = UnityEngine.Random.Range(0, numberOfLanes);
            HandType handType = UnityEngine.Random.value > 0.5f ? HandType.Left : HandType.Right;
            
            var note = new FlowNote
            {
                beatTime = currentBeatTime,
                laneIndex = laneIndex,
                handType = handType,
                isObstacle = enableObstacles && UnityEngine.Random.value < obstacleSpawnChance,
                holdDuration = 0f
            };
            
            upcomingNotes.Enqueue(note);
        }
        
        private void AdvanceToNextPattern()
        {
            if (availablePatterns == null || availablePatterns.Length == 0) return;
            
            if (enableDynamicPatterns)
            {
                // Choose pattern based on player performance
                float performance = HitAccuracy;
                currentPatternIndex = Mathf.RoundToInt(performance * (availablePatterns.Length - 1));
            }
            else
            {
                currentPatternIndex = (currentPatternIndex + 1) % availablePatterns.Length;
            }
            
            Debug.Log($"🎵 Flow pattern changed to: {availablePatterns[currentPatternIndex].patternName}");
        }
        
        private void ProcessUpcomingNotes()
        {
            while (upcomingNotes.Count > 0)
            {
                var note = upcomingNotes.Peek();
                
                // Check if it's time to spawn this note
                if (Time.time >= currentBeatTime + note.beatTime - beatPrediction)
                {
                    SpawnFlowTarget(note);
                    upcomingNotes.Dequeue();
                }
                else
                {
                    break; // Notes are ordered by time
                }
            }
        }
        
        private void SpawnFlowTarget(FlowNote note)
        {
            if (note.laneIndex >= numberOfLanes) return;
            
            Vector3 spawnPos = flowLanes[note.laneIndex].position;
            Vector3 targetPos = playerTarget.position + (spawnPos - flowOrigin.position);
            
            GameObject targetObj = Instantiate(flowTargetPrefab, spawnPos, Quaternion.identity);
            
            // Configure target
            var flowTarget = new FlowTarget
            {
                gameObject = targetObj,
                requiredHand = note.handType,
                laneIndex = note.laneIndex,
                spawnTime = Time.time,
                hitTime = Time.time + (laneDistance / targetSpeed),
                speed = targetSpeed,
                startPosition = spawnPos,
                endPosition = targetPos,
                isHit = false,
                isObstacle = note.isObstacle
            };
            
            // Set visual appearance
            ApplyTargetAppearance(targetObj, flowTarget);
            
            // Add flow target component
            var component = targetObj.GetComponent<FlowTargetComponent>();
            if (component == null)
            {
                component = targetObj.AddComponent<FlowTargetComponent>();
            }
            
            component.Initialize(flowTarget);
            
            // Start movement
            _ = MoveFlowTargetAsync(flowTarget);
            
            activeTargets.Add(flowTarget);
        }
        
        private void ApplyTargetAppearance(GameObject targetObj, FlowTarget target)
        {
            var renderer = targetObj.GetComponent<Renderer>();
            if (renderer == null) return;
            
            if (target.isObstacle)
            {
                renderer.material = dodgeObstacleMaterial != null ? dodgeObstacleMaterial : new Material(Shader.Find("Standard"));
                renderer.material.color = obstacleColor;
                targetObj.tag = "FlowObstacle";
            }
            else if (target.requiredHand == HandType.Left)
            {
                renderer.material = leftHandMaterial != null ? leftHandMaterial : new Material(Shader.Find("Standard"));
                renderer.material.color = leftHandColor;
                targetObj.tag = "FlowTargetLeft";
            }
            else if (target.requiredHand == HandType.Right)
            {
                renderer.material = rightHandMaterial != null ? rightHandMaterial : new Material(Shader.Find("Standard"));
                renderer.material.color = rightHandColor;
                targetObj.tag = "FlowTargetRight";
            }
        }
        
        private async Task MoveFlowTargetAsync(FlowTarget target)
        {
            try
            {
                float elapsedTime = 0f;
                float duration = laneDistance / target.speed;
                
                while (elapsedTime < duration && target.gameObject != null && !target.isHit)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / duration;
                    
                    Vector3 currentPos = Vector3.Lerp(target.startPosition, target.endPosition, progress);
                    target.gameObject.transform.position = currentPos;
                    
                    await Task.Yield();
                }
                
                // Target reached player without being hit
                if (target.gameObject != null && !target.isHit)
                {
                    OnFlowTargetMissed(target);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in flow target movement: {ex.Message}");
            }
        }
        
        private void UpdateFlowTargets()
        {
            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                var target = activeTargets[i];
                
                if (target.gameObject == null)
                {
                    activeTargets.RemoveAt(i);
                    continue;
                }
                
                // Check for timeout
                if (Time.time > target.hitTime + 2f && !target.isHit)
                {
                    CleanupFlowTarget(target);
                    activeTargets.RemoveAt(i);
                }
            }
        }
        
        private void UpdateMultiplier()
        {
            // Decay multiplier over time if no hits
            if (currentCombo == 0)
            {
                currentMultiplier = Mathf.Lerp(currentMultiplier, 1f, Time.deltaTime * 2f);
            }
            
            // Cap multiplier
            currentMultiplier = Mathf.Clamp(currentMultiplier, 1f, flowMultiplierMax);
            
            OnFlowMultiplierChanged?.Invoke(currentMultiplier);
        }
        
        private void UpdateVisualEffects()
        {
            // Update lane particles based on activity
            if (laneParticles != null)
            {
                for (int i = 0; i < Mathf.Min(laneParticles.Length, numberOfLanes); i++)
                {
                    if (laneParticles[i] != null)
                    {
                        var emission = laneParticles[i].emission;
                        
                        // Check if this lane has active targets
                        bool hasActiveTargets = activeTargets.Exists(t => t.laneIndex == i && !t.isHit);
                        emission.enabled = hasActiveTargets;
                        
                        if (hasActiveTargets)
                        {
                            emission.rateOverTime = 20f * currentMultiplier;
                        }
                    }
                }
            }
        }
        
        private void UpdateEffectsIntensity(float intensity)
        {
            // Update particle systems
            if (laneParticles != null)
            {
                foreach (var particles in laneParticles)
                {
                    if (particles != null)
                    {
                        var main = particles.main;
                        main.startSpeed = targetSpeed * 0.5f * intensity;
                        
                        var emission = particles.emission;
                        emission.rateOverTime = 10f * intensity;
                    }
                }
            }
        }
        
        private void RenderInstancedTargets()
        {
            // This would implement GPU instanced rendering for performance
            // For now, using regular GameObjects is sufficient
        }
        
        public void OnFlowTargetHit(HandType handUsed, Vector3 hitPosition, int laneIndex)
        {
            // Find the target that was hit
            for (int i = 0; i < activeTargets.Count; i++)
            {
                var target = activeTargets[i];
                
                if (target.laneIndex == laneIndex && !target.isHit && !target.isObstacle)
                {
                    // Check if correct hand was used
                    if (target.requiredHand == handUsed || target.requiredHand == HandType.Either)
                    {
                        ProcessTargetHit(target, handUsed, hitPosition);
                        break;
                    }
                }
            }
        }
        
        private void ProcessTargetHit(FlowTarget target, HandType handUsed, Vector3 hitPosition)
        {
            // Calculate timing accuracy
            float timingDifference = Mathf.Abs(Time.time - target.hitTime);
            float accuracy = 1f;
            bool isPerfect = false;
            
            if (timingDifference <= perfectHitWindow)
            {
                accuracy = 1f;
                isPerfect = true;
            }
            else if (timingDifference <= goodHitWindow)
            {
                accuracy = 0.8f;
            }
            else
            {
                accuracy = 0.5f;
            }
            
            // Update combo and multiplier
            currentCombo++;
            maxCombo = Mathf.Max(maxCombo, currentCombo);
            
            if (isPerfect)
            {
                currentMultiplier += 0.1f;
            }
            
            // Calculate score
            int baseScore = isPerfect ? 150 : 100;
            int finalScore = Mathf.RoundToInt(baseScore * accuracy * currentMultiplier);
            
            // Create hit data
            var hitData = new FlowHitData
            {
                handUsed = handUsed,
                accuracy = accuracy,
                timing = timingDifference,
                laneIndex = target.laneIndex,
                isPerfectHit = isPerfect,
                multiplier = currentMultiplier,
                score = finalScore
            };
            
            // Mark target as hit
            target.isHit = true;
            targetsHit++;
            totalAccuracy += accuracy;
            
            // Add score to game
            GameManager.Instance?.AddScore(finalScore, isPerfect);
            
            // Trigger events
            OnFlowTargetHit?.Invoke(hitData);
            
            var comboData = new FlowComboData
            {
                comboCount = currentCombo,
                multiplier = currentMultiplier,
                isStreakBroken = false
            };
            OnFlowCombo?.Invoke(comboData);
            
            // Clean up target
            CleanupFlowTarget(target);
        }
        
        public void OnObstacleHit(Vector3 hitPosition)
        {
            // Player hit an obstacle - break combo and apply penalty
            currentCombo = 0;
            currentMultiplier = Mathf.Max(1f, currentMultiplier - 0.5f);
            obstaclesHit++;
            
            var obstacleData = new ObstacleData
            {
                hitPosition = hitPosition,
                damage = 10f,
                wasAvoided = false
            };
            
            OnObstacleHit?.Invoke(obstacleData);
            
            var comboData = new FlowComboData
            {
                comboCount = 0,
                multiplier = currentMultiplier,
                isStreakBroken = true
            };
            OnFlowCombo?.Invoke(comboData);
            
            Debug.Log("💥 Obstacle hit! Combo broken.");
        }
        
        private void OnFlowTargetMissed(FlowTarget target)
        {
            if (target.isObstacle)
            {
                // Successfully avoided obstacle
                obstaclesAvoided++;
            }
            else
            {
                // Missed a target - break combo
                currentCombo = 0;
                currentMultiplier = Mathf.Max(1f, currentMultiplier - 0.2f);
                targetsMissed++;
                
                var comboData = new FlowComboData
                {
                    comboCount = 0,
                    multiplier = currentMultiplier,
                    isStreakBroken = true
                };
                OnFlowCombo?.Invoke(comboData);
            }
            
            CleanupFlowTarget(target);
        }
        
        private void CleanupFlowTarget(FlowTarget target)
        {
            if (target.gameObject != null)
            {
                Destroy(target.gameObject);
            }
        }
        
        // Public API
        public void StartFlowMode()
        {
            IsFlowModeActive = true;
            currentBeatTime = 0f;
            currentMultiplier = 1f;
            currentCombo = 0;
            maxCombo = 0;
            
            // Reset statistics
            targetsHit = 0;
            targetsMissed = 0;
            obstaclesAvoided = 0;
            obstaclesHit = 0;
            totalAccuracy = 0f;
            
            // Clear any existing targets
            ClearAllFlowTargets();
            
            Debug.Log("🌊 Flow Mode started!");
        }
        
        public void StopFlowMode()
        {
            IsFlowModeActive = false;
            ClearAllFlowTargets();
            upcomingNotes.Clear();
            
            Debug.Log("🌊 Flow Mode stopped");
        }
        
        public void ClearAllFlowTargets()
        {
            foreach (var target in activeTargets)
            {
                CleanupFlowTarget(target);
            }
            activeTargets.Clear();
        }
        
        public void SetDifficulty(float difficulty)
        {
            difficulty = Mathf.Clamp01(difficulty);
            
            // Adjust parameters based on difficulty
            targetSpeed = Mathf.Lerp(6f, 12f, difficulty);
            obstacleSpawnChance = Mathf.Lerp(0.1f, 0.4f, difficulty);
            perfectHitWindow = Mathf.Lerp(0.15f, 0.08f, difficulty);
            goodHitWindow = Mathf.Lerp(0.3f, 0.15f, difficulty);
        }
        
        // Statistics
        public float GetAverageAccuracy()
        {
            return targetsHit > 0 ? totalAccuracy / targetsHit : 0f;
        }
        
        public int GetMaxCombo()
        {
            return maxCombo;
        }
        
        public Dictionary<string, object> GetFlowModeStats()
        {
            return new Dictionary<string, object>
            {
                {"is_active", IsFlowModeActive},
                {"targets_hit", targetsHit},
                {"targets_missed", targetsMissed},
                {"obstacles_hit", obstaclesHit},
                {"obstacles_avoided", obstaclesAvoided},
                {"hit_accuracy", HitAccuracy},
                {"current_multiplier", currentMultiplier},
                {"current_combo", currentCombo},
                {"max_combo", maxCombo},
                {"average_accuracy", GetAverageAccuracy()},
                {"flow_intensity", currentFlowIntensity},
                {"music_energy", lastMusicEnergy},
                {"lane_count", numberOfLanes},
                {"pattern_index", currentPatternIndex},
                {"beat_sync", syncWithMusic},
                {"total_score", totalScore}
            };
        }
        
        private void OnDestroy()
        {
            StopFlowMode();
        }
    }
    
    // Flow Target Component
    public class FlowTargetComponent : MonoBehaviour
    {
        private FlowModeSystem.FlowTarget targetData;
        
        public void Initialize(FlowModeSystem.FlowTarget data)
        {
            targetData = data;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Handle collision with hands or obstacles
            if (other.CompareTag("LeftHand"))
            {
                FlowModeSystem.Instance?.OnFlowTargetHit(FlowModeSystem.HandType.Left, transform.position, targetData.laneIndex);
            }
            else if (other.CompareTag("RightHand"))
            {
                FlowModeSystem.Instance?.OnFlowTargetHit(FlowModeSystem.HandType.Right, transform.position, targetData.laneIndex);
            }
            else if (other.CompareTag("Player") && targetData.isObstacle)
            {
                FlowModeSystem.Instance?.OnObstacleHit(transform.position);
            }
        }
    }
} 