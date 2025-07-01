using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using VRBoxingGame.Core;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Advanced Target System for Unity 6 with enhanced gameplay mechanics and optimizations
    /// </summary>
    public class AdvancedTargetSystem : MonoBehaviour
    {
        [Header("Target Types")]
        public TargetConfig[] targetConfigs;
        public GameObject[] powerUpPrefabs;
        public GameObject[] obstaclePrefabs;
        
        [Header("Spawning Settings")]
        public Transform[] spawnZones;
        public float spawnRadius = 3f;
        public int maxActiveTargets = 8;
        public float difficultyScaling = 1.2f;
        
        [Header("Advanced Features")]
        public bool enableTargetPrediction = true;
        public bool enableAdaptiveSpawning = true;
        public bool enableComboTargets = true;
        public bool enablePowerUps = true;
        
        [Header("Unity 6 Optimizations")]
        public bool enableJobSystemSpawning = true;
        public bool enableGPUInstancing = true;
        public int batchSize = 32;
        
        [Header("Events")]
        public UnityEvent<TargetHitData> OnTargetHit;
        public UnityEvent<ComboData> OnComboCompleted;
        public UnityEvent<PowerUpData> OnPowerUpCollected;
        public UnityEvent<float> OnDifficultyChanged;
        
        // Target types
        public enum TargetType
        {
            Basic,
            Speed,
            Power,
            Precision,
            Combo,
            Block,
            Duck,
            Dodge,
            PowerUp,
            Obstacle
        }
        
        public enum ComboType
        {
            LeftRight,
            HighLow,
            Cross,
            Uppercut,
            Hook,
            Custom
        }
        
        [System.Serializable]
        public struct TargetConfig
        {
            public TargetType type;
            public GameObject prefab;
            public float spawnWeight;
            public float lifetime;
            public int scoreValue;
            public float difficultyRequirement;
            public bool requiresSpecificHand;
            public bool isLeftHand;
        }
        
        [System.Serializable]
        public struct TargetHitData
        {
            public TargetType targetType;
            public float accuracy;
            public float power;
            public Vector3 hitPosition;
            public float timingScore;
            public bool isPerfectHit;
            public int scoreAwarded;
        }
        
        [System.Serializable]
        public struct ComboData
        {
            public ComboType comboType;
            public int comboLength;
            public float comboTime;
            public float averageAccuracy;
            public int bonusScore;
            public bool isPerfectCombo;
        }
        
        [System.Serializable]
        public struct PowerUpData
        {
            public PowerUpType powerUpType;
            public float duration;
            public float multiplier;
            public Vector3 position;
        }
        
        public enum PowerUpType
        {
            ScoreMultiplier,
            SlowMotion,
            DoublePoints,
            ShieldBreaker,
            ComboExtender,
            PerfectAim
        }
        
        // Private variables
        private List<GameObject> activeTargets = new List<GameObject>();
        private List<ComboTarget> activeCombo = new List<ComboTarget>();
        private Queue<SpawnRequest> spawnQueue = new Queue<SpawnRequest>();
        
        // Performance tracking
        private float currentDifficulty = 1f;
        private float playerPerformance = 0f;
        private int consecutiveHits = 0;
        private int consecutiveMisses = 0;
        
        // Job System data
        private NativeArray<float3> spawnPositions;
        private NativeArray<float> spawnTimes;
        private NativeArray<int> targetTypes;
        private JobHandle currentJobHandle;
        
        // Adaptive spawning
        private float lastSpawnTime;
        private float adaptiveSpawnRate = 1f;
        
        // Combo tracking
        private struct ComboTarget
        {
            public GameObject target;
            public float spawnTime;
            public TargetType type;
            public bool isHit;
        }
        
        private struct SpawnRequest
        {
            public TargetType type;
            public Vector3 position;
            public float delay;
        }
        
        // Singleton
        public static AdvancedTargetSystem Instance { get; private set; }
        
        // Properties
        public float CurrentDifficulty => currentDifficulty;
        public int ActiveTargetCount => activeTargets.Count;
        public float PlayerPerformance => playerPerformance;
        public int ConsecutiveHits => consecutiveHits;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeTargetSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeTargetSystem()
        {
            // Initialize Job System arrays
            if (enableJobSystemSpawning)
            {
                spawnPositions = new NativeArray<float3>(batchSize, Allocator.Persistent);
                spawnTimes = new NativeArray<float>(batchSize, Allocator.Persistent);
                targetTypes = new NativeArray<int>(batchSize, Allocator.Persistent);
            }
            
            // Setup spawn zones if not configured
            if (spawnZones == null || spawnZones.Length == 0)
            {
                CreateDefaultSpawnZones();
            }
            
            Debug.Log("Advanced Target System initialized with Unity 6 optimizations");
        }
        
        private void CreateDefaultSpawnZones()
        {
            GameObject spawnParent = new GameObject("Spawn Zones");
            spawnParent.transform.SetParent(transform);
            
            // Create 8 spawn zones around the player
            spawnZones = new Transform[8];
            for (int i = 0; i < 8; i++)
            {
                GameObject zone = new GameObject($"SpawnZone_{i}");
                zone.transform.SetParent(spawnParent.transform);
                
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(
                    Mathf.Sin(angle) * spawnRadius,
                    Random.Range(0.5f, 2.5f),
                    Mathf.Cos(angle) * spawnRadius
                );
                
                zone.transform.localPosition = position;
                spawnZones[i] = zone.transform;
            }
        }
        
        private void Update()
        {
            UpdateDifficulty();
            ProcessSpawnQueue();
            UpdateActiveTargets();
            UpdateComboSystem();
            
            if (enableAdaptiveSpawning)
            {
                UpdateAdaptiveSpawning();
            }
        }
        
        private void UpdateDifficulty()
        {
            if (!GameManager.Instance || !GameManager.Instance.IsGameActive) return;
            
            // Calculate difficulty based on time and performance
            float timeFactor = (GameManager.Instance.gameSessionDuration - GameManager.Instance.TimeRemaining) / 60f;
            float performanceFactor = playerPerformance;
            
            float newDifficulty = 1f + (timeFactor * 0.5f) + (performanceFactor * 0.3f);
            newDifficulty = Mathf.Clamp(newDifficulty, 0.5f, 3f);
            
            if (Mathf.Abs(newDifficulty - currentDifficulty) > 0.1f)
            {
                currentDifficulty = newDifficulty;
                OnDifficultyChanged?.Invoke(currentDifficulty);
            }
        }
        
        private void UpdateAdaptiveSpawning()
        {
            // Adjust spawn rate based on player performance
            if (consecutiveHits > 5)
            {
                adaptiveSpawnRate = Mathf.Min(adaptiveSpawnRate * 1.1f, 2f);
            }
            else if (consecutiveMisses > 3)
            {
                adaptiveSpawnRate = Mathf.Max(adaptiveSpawnRate * 0.9f, 0.5f);
            }
            
            // Calculate performance score
            float hitRate = GameManager.Instance ? GameManager.Instance.HitAccuracy : 0f;
            playerPerformance = Mathf.Lerp(playerPerformance, hitRate, Time.deltaTime * 0.5f);
        }
        
        private void ProcessSpawnQueue()
        {
            while (spawnQueue.Count > 0 && activeTargets.Count < maxActiveTargets)
            {
                SpawnRequest request = spawnQueue.Dequeue();
                
                if (Time.time >= request.delay)
                {
                    SpawnTarget(request.type, request.position);
                }
                else
                {
                    spawnQueue.Enqueue(request); // Put back if not ready
                    break;
                }
            }
        }
        
        private void UpdateActiveTargets()
        {
            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                if (activeTargets[i] == null)
                {
                    activeTargets.RemoveAt(i);
                }
            }
        }
        
        private void UpdateComboSystem()
        {
            if (!enableComboTargets) return;
            
            // Check for expired combo targets
            for (int i = activeCombo.Count - 1; i >= 0; i--)
            {
                if (Time.time - activeCombo[i].spawnTime > 5f) // 5 second combo window
                {
                    if (!activeCombo[i].isHit)
                    {
                        // Combo broken
                        ResetCombo();
                        break;
                    }
                    activeCombo.RemoveAt(i);
                }
            }
            
            // Check for completed combos
            CheckForCompletedCombos();
        }
        
        public void SpawnTarget(TargetType type, Vector3 position)
        {
            TargetConfig config = GetTargetConfig(type);
            if (config.prefab == null) return;
            
            // Use object pooling for performance
            GameObject target = ObjectPoolManager.Instance?.SpawnObject(
                config.prefab.name, position, Quaternion.identity);
            
            if (target == null)
            {
                target = Instantiate(config.prefab, position, Quaternion.identity);
            }
            
            // Configure target
            ConfigureTarget(target, config);
            
            activeTargets.Add(target);
            
            // Add to combo if applicable
            if (enableComboTargets && IsComboTarget(type))
            {
                activeCombo.Add(new ComboTarget
                {
                    target = target,
                    spawnTime = Time.time,
                    type = type,
                    isHit = false
                });
            }
        }
        
        private void ConfigureTarget(GameObject target, TargetConfig config)
        {
            // Configure target based on difficulty
            var targetComponent = target.GetComponent<BoxingTarget>();
            if (targetComponent != null)
            {
                targetComponent.lifetime = config.lifetime / currentDifficulty;
                targetComponent.baseScore = Mathf.RoundToInt(config.scoreValue * currentDifficulty);
                
                if (config.requiresSpecificHand)
                {
                    targetComponent.requiredHand = config.isLeftHand ? 
                        BoxingTarget.HandType.Left : BoxingTarget.HandType.Right;
                }
            }
            
            // Add enhanced target component
            var enhancedTarget = target.GetComponent<EnhancedTarget>();
            if (enhancedTarget == null)
            {
                enhancedTarget = target.AddComponent<EnhancedTarget>();
            }
            
            enhancedTarget.Initialize(config.type, currentDifficulty);
        }
        
        private TargetConfig GetTargetConfig(TargetType type)
        {
            foreach (var config in targetConfigs)
            {
                if (config.type == type)
                    return config;
            }
            
            return targetConfigs[0]; // Default to first config
        }
        
        private bool IsComboTarget(TargetType type)
        {
            return type == TargetType.Combo || type == TargetType.Basic || type == TargetType.Speed;
        }
        
        private void CheckForCompletedCombos()
        {
            if (activeCombo.Count < 2) return;
            
            // Check for specific combo patterns
            ComboType detectedCombo = DetectComboPattern();
            
            if (detectedCombo != ComboType.Custom)
            {
                CompleteCombo(detectedCombo);
            }
        }
        
        private ComboType DetectComboPattern()
        {
            if (activeCombo.Count < 2) return ComboType.Custom;
            
            // Simple left-right combo detection
            if (activeCombo.Count == 2)
            {
                var first = activeCombo[0];
                var second = activeCombo[1];
                
                if (first.isHit && second.isHit)
                {
                    return ComboType.LeftRight;
                }
            }
            
            return ComboType.Custom;
        }
        
        private void CompleteCombo(ComboType comboType)
        {
            float comboTime = Time.time - activeCombo[0].spawnTime;
            float averageAccuracy = CalculateComboAccuracy();
            int bonusScore = CalculateComboBonus(comboType, comboTime);
            
            ComboData comboData = new ComboData
            {
                comboType = comboType,
                comboLength = activeCombo.Count,
                comboTime = comboTime,
                averageAccuracy = averageAccuracy,
                bonusScore = bonusScore,
                isPerfectCombo = averageAccuracy > 0.9f && comboTime < 2f
            };
            
            OnComboCompleted?.Invoke(comboData);
            GameManager.Instance?.AddScore(bonusScore, comboData.isPerfectCombo);
            
            ResetCombo();
        }
        
        private float CalculateComboAccuracy()
        {
            // Calculate based on timing and precision
            return 0.85f; // Simplified for now
        }
        
        private int CalculateComboBonus(ComboType comboType, float comboTime)
        {
            int baseBonus = 200;
            float timeMultiplier = Mathf.Clamp(3f - comboTime, 1f, 3f);
            return Mathf.RoundToInt(baseBonus * timeMultiplier * currentDifficulty);
        }
        
        private void ResetCombo()
        {
            activeCombo.Clear();
        }
        
        public void OnTargetHitEvent(TargetHitData hitData)
        {
            consecutiveHits++;
            consecutiveMisses = 0;
            
            // Update combo system
            UpdateComboHit(hitData);
            
            OnTargetHit?.Invoke(hitData);
        }
        
        public void OnTargetMissedEvent()
        {
            consecutiveMisses++;
            consecutiveHits = 0;
            
            ResetCombo();
        }
        
        private void UpdateComboHit(TargetHitData hitData)
        {
            // Find and mark combo target as hit
            for (int i = 0; i < activeCombo.Count; i++)
            {
                var combo = activeCombo[i];
                if (Vector3.Distance(combo.target.transform.position, hitData.hitPosition) < 0.5f)
                {
                    combo.isHit = true;
                    activeCombo[i] = combo;
                    break;
                }
            }
        }
        
        public void SpawnPowerUp(Vector3 position)
        {
            if (!enablePowerUps || powerUpPrefabs.Length == 0) return;
            
            GameObject powerUpPrefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
            GameObject powerUp = Instantiate(powerUpPrefab, position, Quaternion.identity);
            
            var powerUpComponent = powerUp.GetComponent<PowerUp>();
            if (powerUpComponent == null)
            {
                powerUpComponent = powerUp.AddComponent<PowerUp>();
            }
            
            powerUpComponent.Initialize(GetRandomPowerUpType());
        }
        
        private PowerUpType GetRandomPowerUpType()
        {
            var values = System.Enum.GetValues(typeof(PowerUpType));
            return (PowerUpType)values.GetValue(Random.Range(0, values.Length));
        }
        
        // Unity 6 Job System for batch spawning
        [BurstCompile]
        public struct BatchSpawnJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<float> spawnTimes;
            [ReadOnly] public float currentTime;
            
            [WriteOnly] public NativeArray<int> spawnResults;
            
            public void Execute(int index)
            {
                if (currentTime >= spawnTimes[index])
                {
                    spawnResults[index] = 1; // Ready to spawn
                }
                else
                {
                    spawnResults[index] = 0; // Not ready
                }
            }
        }
        
        public void ScheduleBatchSpawn(Vector3[] positions, float[] delays, TargetType[] types)
        {
            if (!enableJobSystemSpawning) return;
            
            // Complete previous job
            currentJobHandle.Complete();
            
            int count = Mathf.Min(positions.Length, batchSize);
            
            // Copy data to native arrays
            for (int i = 0; i < count; i++)
            {
                spawnPositions[i] = positions[i];
                spawnTimes[i] = Time.time + delays[i];
                targetTypes[i] = (int)types[i];
            }
            
            // Schedule job
            var spawnJob = new BatchSpawnJob
            {
                positions = spawnPositions,
                spawnTimes = spawnTimes,
                currentTime = Time.time,
                spawnResults = new NativeArray<int>(count, Allocator.TempJob)
            };
            
            currentJobHandle = spawnJob.Schedule(count, 8);
        }
        
        public Vector3 GetRandomSpawnPosition()
        {
            if (spawnZones.Length == 0) return Vector3.zero;
            
            Transform zone = spawnZones[Random.Range(0, spawnZones.Length)];
            Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
            randomOffset.y = Mathf.Abs(randomOffset.y); // Keep above ground
            
            return zone.position + randomOffset;
        }
        
        public void ClearAllTargets()
        {
            foreach (GameObject target in activeTargets)
            {
                if (target != null)
                {
                    ObjectPoolManager.Instance?.ReturnObject(target);
                }
            }
            
            activeTargets.Clear();
            ResetCombo();
        }
        
        private void OnDestroy()
        {
            // Clean up Job System resources
            if (enableJobSystemSpawning)
            {
                currentJobHandle.Complete();
                
                if (spawnPositions.IsCreated) spawnPositions.Dispose();
                if (spawnTimes.IsCreated) spawnTimes.Dispose();
                if (targetTypes.IsCreated) targetTypes.Dispose();
            }
        }
        
        // Gizmos for debugging
        private void OnDrawGizmos()
        {
            if (spawnZones != null)
            {
                Gizmos.color = Color.yellow;
                foreach (Transform zone in spawnZones)
                {
                    if (zone != null)
                    {
                        Gizmos.DrawWireSphere(zone.position, 0.3f);
                    }
                }
            }
        }
    }
    
    // Enhanced target component
    public class EnhancedTarget : MonoBehaviour
    {
        private AdvancedTargetSystem.TargetType targetType;
        private float difficulty;
        
        public void Initialize(AdvancedTargetSystem.TargetType type, float diff)
        {
            targetType = type;
            difficulty = diff;
            
            // Apply type-specific behaviors
            ApplyTargetBehavior();
        }
        
        private void ApplyTargetBehavior()
        {
            switch (targetType)
            {
                case AdvancedTargetSystem.TargetType.Speed:
                    // Faster movement
                    break;
                case AdvancedTargetSystem.TargetType.Power:
                    // Requires more force
                    break;
                case AdvancedTargetSystem.TargetType.Precision:
                    // Smaller hit area
                    break;
            }
        }
    }
    
    // Power-up component
    public class PowerUp : MonoBehaviour
    {
        private AdvancedTargetSystem.PowerUpType powerUpType;
        
        public void Initialize(AdvancedTargetSystem.PowerUpType type)
        {
            powerUpType = type;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                CollectPowerUp();
            }
        }
        
        private void CollectPowerUp()
        {
            var powerUpData = new AdvancedTargetSystem.PowerUpData
            {
                powerUpType = powerUpType,
                duration = 10f,
                multiplier = 2f,
                position = transform.position
            };
            
            AdvancedTargetSystem.Instance?.OnPowerUpCollected?.Invoke(powerUpData);
            Destroy(gameObject);
        }
    }
}

