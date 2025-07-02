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
    /// Comprehensive Dodging System with squats, spins, rotations, and clever integration
    /// Unity 6 optimized with full-body tracking and dynamic obstacle patterns
    /// </summary>
    public class ComprehensiveDodgingSystem : MonoBehaviour
    {
        [Header("Dodging Configuration")]
        public bool enableDodging = true;
        public bool enableSquatDodging = true;
        public bool enableSpinDodging = true;
        public bool enableLeanDodging = true;
        public bool enableDuckDodging = true;
        
        [Header("Obstacle Settings")]
        public GameObject[] highObstaclePrefabs;   // Require ducking/squatting
        public GameObject[] lowObstaclePrefabs;    // Require jumping/leaning up
        public GameObject[] leftObstaclePrefabs;   // Require lean right
        public GameObject[] rightObstaclePrefabs;  // Require lean left
        public GameObject[] spinObstaclePrefabs;   // Require full 360° spin
        
        [Header("Movement Thresholds")]
        public float squatThreshold = 0.4f;        // How low player must go
        public float spinThreshold = 270f;         // Degrees to complete spin
        public float leanThreshold = 0.3f;         // How far to lean
        public float duckDuration = 0.5f;          // How long to stay ducked
        
        [Header("Obstacle Spawning")]
        public Transform[] spawnPoints;
        public float obstacleSpeed = 6f;
        public float spawnDistance = 12f;
        public float obstacleLifetime = 8f;
        public float difficultyMultiplier = 1f;
        
        [Header("Intensive Dodging Mode")]
        public bool intensiveDodgingMode = false;
        public float intensiveSpawnRate = 2f;      // Obstacles per second
        public float intensiveSquatFrequency = 0.7f; // 70% require squats
        public float intensiveSpinFrequency = 0.3f;  // 30% require spins
        
        [Header("Player Tracking")]
        public Transform playerHead;
        public Transform playerLeftHand;
        public Transform playerRightHand;
        public Transform playerHips;
        public float playerHeight = 1.8f;
        
        [Header("Visual Feedback")]
        public ParticleSystem dodgeSuccessEffect;
        public ParticleSystem dodgeFailEffect;
        public LineRenderer warningIndicator;
        public Material dangerMaterial;
        public Color warningColor = Color.red;
        public Color safeColor = Color.green;
        
        [Header("Audio")]
        public AudioSource dodgeAudioSource;
        public AudioClip[] dodgeSuccessClips;
        public AudioClip[] dodgeFailClips;
        public AudioClip[] warningClips;
        public AudioClip spinCompleteClip;
        
        [Header("Events")]
        public UnityEvent<DodgeData> OnDodgeSuccess;
        public UnityEvent<DodgeData> OnDodgeFail;
        public UnityEvent<ObstacleData> OnObstacleSpawned;
        public UnityEvent<float> OnIntensityChanged;
        
        // Data Structures
        [System.Serializable]
        public struct DodgeObstacle
        {
            public GameObject gameObject;
            public DodgeType requiredDodge;
            public Vector3 spawnPosition;
            public Vector3 targetPosition;
            public float spawnTime;
            public float speed;
            public bool isActive;
            public bool wasAvoided;
            public float dangerRadius;
        }
        
        [System.Serializable]
        public struct DodgeData
        {
            public DodgeType dodgeType;
            public float accuracy;
            public float responseTime;
            public bool isPerfectDodge;
            public Vector3 obstaclePosition;
            public int score;
        }
        
        [System.Serializable]
        public struct ObstacleData
        {
            public DodgeType dodgeType;
            public Vector3 position;
            public float speed;
            public float dangerLevel;
        }
        
        public enum DodgeType
        {
            Squat,      // Lower body down
            Duck,       // Quick head duck
            LeanLeft,   // Lean body left
            LeanRight,  // Lean body right
            Spin360,    // Full 360° rotation
            Jump,       // Jump up (rare)
            Matrix      // Extreme backward lean
        }
        
        // Private Variables
        private List<DodgeObstacle> activeObstacles = new List<DodgeObstacle>();
        private float lastSpawnTime = 0f;
        private float playerBaseHeight = 0f;
        private float currentPlayerHeight = 0f;
        private float playerRotation = 0f;
        private float lastPlayerRotation = 0f;
        private Vector3 playerBasePosition = Vector3.zero;
        
        // Dodging State
        private bool isSquatting = false;
        private bool isDucking = false;
        private bool isLeaning = false;
        private bool isSpinning = false;
        private float spinStartRotation = 0f;
        private float spinProgress = 0f;
        private float lastDodgeTime = 0f;
        
        // Statistics
        private int dodgesSuccessful = 0;
        private int dodgesFailed = 0;
        private int obstaclesSpawned = 0;
        private float totalDodgeAccuracy = 0f;
        private float averageResponseTime = 0f;
        
        // Integration with other modes
        private bool integratedWithFlow = false;
        private bool integratedWithStaff = false;
        private FlowModeSystem flowSystem;
        private TwoHandedStaffSystem staffSystem;
        
        // Singleton
        public static ComprehensiveDodgingSystem Instance { get; private set; }
        
        // Properties
        public bool IsDodgingModeActive { get; private set; }
        public float DodgeAccuracy => dodgesSuccessful + dodgesFailed > 0 ? (float)dodgesSuccessful / (dodgesSuccessful + dodgesFailed) : 0f;
        public bool IsPlayerDodging => isSquatting || isDucking || isLeaning || isSpinning;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeDodgingSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetupPlayerTracking();
            SetupVisualEffects();
            ConnectToOtherSystems();
            InitializeSpawnPoints();
        }
        
        private void Update()
        {
            if (IsDodgingModeActive || integratedWithFlow || integratedWithStaff)
            {
                UpdatePlayerTracking();
                UpdateObstacles();
                ProcessDodging();
                CheckObstacleCollisions();
                
                if (IsDodgingModeActive)
                {
                    UpdateObstacleSpawning();
                }
            }
        }
        
        private void InitializeDodgingSystem()
        {
            Debug.Log("🤸 Initializing Comprehensive Dodging System...");
            
            // Find player tracking components
            if (playerHead == null)
            {
                var xrCamera = CachedReferenceManager.Get<Camera>();
                if (xrCamera != null)
                {
                    playerHead = xrCamera.transform;
                }
            }
            
            // Initialize audio source
            if (dodgeAudioSource == null)
            {
                dodgeAudioSource = gameObject.AddComponent<AudioSource>();
                dodgeAudioSource.spatialBlend = 0f; // 2D audio for feedback
                dodgeAudioSource.volume = 0.7f;
            }
        }
        
        private void SetupPlayerTracking()
        {
            if (playerHead != null)
            {
                playerBaseHeight = playerHead.position.y;
                playerBasePosition = playerHead.position;
                lastPlayerRotation = playerHead.eulerAngles.y;
            }
            
            Debug.Log("🎯 Player tracking initialized for dodging system");
        }
        
        private void SetupVisualEffects()
        {
            // Setup warning indicator
            if (warningIndicator == null)
            {
                GameObject indicatorObj = new GameObject("Dodge Warning Indicator");
                warningIndicator = indicatorObj.AddComponent<LineRenderer>();
                warningIndicator.material = new Material(Shader.Find("Sprites/Default"));
                warningIndicator.color = warningColor;
                warningIndicator.startWidth = 0.1f;
                warningIndicator.endWidth = 0.1f;
                warningIndicator.positionCount = 2;
                warningIndicator.enabled = false;
            }
        }
        
        private void ConnectToOtherSystems()
        {
            // Connect to Flow Mode System
            flowSystem = FlowModeSystem.Instance;
            if (flowSystem != null)
            {
                Debug.Log("🌊 Connected to Flow Mode System");
            }
            
            // Connect to Staff System
            staffSystem = TwoHandedStaffSystem.Instance;
            if (staffSystem != null)
            {
                Debug.Log("🥢 Connected to Staff System");
            }
        }
        
        private void InitializeSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                CreateDefaultSpawnPoints();
            }
        }
        
        private void CreateDefaultSpawnPoints()
        {
            spawnPoints = new Transform[8];
            Vector3 playerPos = playerHead != null ? playerHead.position : Vector3.zero;
            
            // Create spawn points around player
            for (int i = 0; i < 8; i++)
            {
                GameObject spawnPoint = new GameObject($"Dodge Spawn Point {i}");
                spawnPoint.transform.SetParent(transform);
                
                float angle = i * 45f; // 8 points around player
                float radian = angle * Mathf.Deg2Rad;
                
                Vector3 spawnPos = playerPos + new Vector3(
                    Mathf.Sin(radian) * spawnDistance,
                    UnityEngine.Random.Range(-1f, 2f), // Varying heights
                    Mathf.Cos(radian) * spawnDistance
                );
                
                spawnPoint.transform.position = spawnPos;
                spawnPoints[i] = spawnPoint.transform;
            }
            
            Debug.Log("🎯 Created 8 dodge spawn points");
        }
        
        private void UpdatePlayerTracking()
        {
            if (playerHead == null) return;
            
            // Track height changes
            currentPlayerHeight = playerHead.position.y;
            float heightDifference = currentPlayerHeight - playerBaseHeight;
            
            // Track rotation changes
            float currentRotation = playerHead.eulerAngles.y;
            float rotationDelta = Mathf.DeltaAngle(lastPlayerRotation, currentRotation);
            playerRotation += rotationDelta;
            lastPlayerRotation = currentRotation;
            
            // Determine current dodge state
            UpdateDodgeStates(heightDifference);
            UpdateSpinTracking();
        }
        
        private void UpdateDodgeStates(float heightDifference)
        {
            // Squatting detection
            bool wasSquatting = isSquatting;
            isSquatting = heightDifference < -squatThreshold;
            
            // Ducking detection (quick, temporary movement)
            bool wasDucking = isDucking;
            isDucking = heightDifference < -0.2f && heightDifference > -squatThreshold;
            
            // Leaning detection
            Vector3 headPosition = playerHead.position;
            Vector3 leanVector = headPosition - playerBasePosition;
            float leanDistance = Vector3.Distance(new Vector3(headPosition.x, 0, headPosition.z), 
                                                  new Vector3(playerBasePosition.x, 0, playerBasePosition.z));
            
            isLeaning = leanDistance > leanThreshold;
            
            // Log dodge state changes
            if (isSquatting && !wasSquatting)
            {
                Debug.Log("🤸 Player started squatting");
            }
            else if (isDucking && !wasDucking)
            {
                Debug.Log("🦆 Player started ducking");
            }
        }
        
        private void UpdateSpinTracking()
        {
            if (!isSpinning && Mathf.Abs(playerRotation - spinStartRotation) > 45f)
            {
                // Player started spinning
                isSpinning = true;
                spinStartRotation = playerRotation;
                spinProgress = 0f;
                Debug.Log("🌪️ Player started spinning");
            }
            
            if (isSpinning)
            {
                float totalRotation = Mathf.Abs(playerRotation - spinStartRotation);
                spinProgress = totalRotation / 360f;
                
                if (totalRotation >= spinThreshold)
                {
                    // Spin completed
                    OnSpinCompleted();
                }
            }
        }
        
        private void OnSpinCompleted()
        {
            isSpinning = false;
            spinProgress = 0f;
            
            PlayAudioClip(spinCompleteClip);
            Debug.Log("🌪️ 360° spin completed!");
            
            // Check if this was needed for dodging
            CheckSpinDodge();
        }
        
        private void UpdateObstacleSpawning()
        {
            if (!IsDodgingModeActive) return;
            
            float spawnRate = intensiveDodgingMode ? intensiveSpawnRate : 1f;
            float spawnInterval = 1f / spawnRate;
            
            if (Time.time - lastSpawnTime >= spawnInterval)
            {
                SpawnRandomObstacle();
                lastSpawnTime = Time.time;
            }
        }
        
        private void SpawnRandomObstacle()
        {
            // Choose obstacle type based on settings
            DodgeType dodgeType = ChooseRandomDodgeType();
            SpawnObstacle(dodgeType);
        }
        
        private DodgeType ChooseRandomDodgeType()
        {
            if (intensiveDodgingMode)
            {
                // Intensive mode favors squats and spins
                float random = UnityEngine.Random.value;
                
                if (random < intensiveSquatFrequency)
                    return DodgeType.Squat;
                else if (random < intensiveSquatFrequency + intensiveSpinFrequency)
                    return DodgeType.Spin360;
                else
                    return (DodgeType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(DodgeType)).Length);
            }
            else
            {
                // Normal mode - balanced distribution
                return (DodgeType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(DodgeType)).Length);
            }
        }
        
        private void SpawnObstacle(DodgeType dodgeType)
        {
            // Choose spawn point
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            
            // Choose appropriate prefab
            GameObject obstaclePrefab = GetObstaclePrefab(dodgeType);
            if (obstaclePrefab == null) return;
            
            // Spawn obstacle
            GameObject obstacleObj = Instantiate(obstaclePrefab, spawnPoint.position, spawnPoint.rotation);
            
            // Calculate target position (near player)
            Vector3 playerPos = playerHead != null ? playerHead.position : Vector3.zero;
            Vector3 targetPos = playerPos + UnityEngine.Random.insideUnitSphere * 0.5f;
            
            // Create obstacle data
            var obstacle = new DodgeObstacle
            {
                gameObject = obstacleObj,
                requiredDodge = dodgeType,
                spawnPosition = spawnPoint.position,
                targetPosition = targetPos,
                spawnTime = Time.time,
                speed = obstacleSpeed * difficultyMultiplier,
                isActive = true,
                wasAvoided = false,
                dangerRadius = GetDangerRadius(dodgeType)
            };
            
            // Add obstacle component
            var component = obstacleObj.GetComponent<DodgeObstacleComponent>();
            if (component == null)
            {
                component = obstacleObj.AddComponent<DodgeObstacleComponent>();
            }
            component.Initialize(obstacle);
            
            // Apply visual styling
            ApplyObstacleVisuals(obstacleObj, dodgeType);
            
            // Start movement
            _ = MoveObstacleAsync(obstacle);
            
            activeObstacles.Add(obstacle);
            obstaclesSpawned++;
            
            // Trigger events
            var obstacleData = new ObstacleData
            {
                dodgeType = dodgeType,
                position = spawnPoint.position,
                speed = obstacle.speed,
                dangerLevel = GetDangerLevel(dodgeType)
            };
            OnObstacleSpawned?.Invoke(obstacleData);
            
            Debug.Log($"🚧 Spawned {dodgeType} obstacle");
        }
        
        private GameObject GetObstaclePrefab(DodgeType dodgeType)
        {
            switch (dodgeType)
            {
                case DodgeType.Squat:
                case DodgeType.Duck:
                    return GetRandomPrefab(highObstaclePrefabs);
                case DodgeType.Jump:
                    return GetRandomPrefab(lowObstaclePrefabs);
                case DodgeType.LeanLeft:
                    return GetRandomPrefab(rightObstaclePrefabs);
                case DodgeType.LeanRight:
                    return GetRandomPrefab(leftObstaclePrefabs);
                case DodgeType.Spin360:
                case DodgeType.Matrix:
                    return GetRandomPrefab(spinObstaclePrefabs);
                default:
                    return GetRandomPrefab(highObstaclePrefabs);
            }
        }
        
        private GameObject GetRandomPrefab(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return CreateDefaultObstacle();
            }
            
            return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        }
        
        private GameObject CreateDefaultObstacle()
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Dodge Obstacle";
            obstacle.transform.localScale = Vector3.one * 0.5f;
            
            var renderer = obstacle.GetComponent<Renderer>();
            if (dangerMaterial != null)
            {
                renderer.material = dangerMaterial;
            }
            else
            {
                renderer.material.color = warningColor;
            }
            
            return obstacle;
        }
        
        private void ApplyObstacleVisuals(GameObject obstacle, DodgeType dodgeType)
        {
            var renderer = obstacle.GetComponent<Renderer>();
            if (renderer == null) return;
            
            // Color coding by dodge type
            Color obstacleColor = GetObstacleColor(dodgeType);
            renderer.material.color = obstacleColor;
            
            // Add warning effect
            if (warningIndicator != null)
            {
                ShowWarningIndicator(obstacle.transform.position, dodgeType);
            }
        }
        
        private Color GetObstacleColor(DodgeType dodgeType)
        {
            switch (dodgeType)
            {
                case DodgeType.Squat: return Color.red;
                case DodgeType.Duck: return Color.yellow;
                case DodgeType.LeanLeft: return Color.blue;
                case DodgeType.LeanRight: return Color.cyan;
                case DodgeType.Spin360: return Color.magenta;
                case DodgeType.Jump: return Color.green;
                case DodgeType.Matrix: return Color.black;
                default: return Color.white;
            }
        }
        
        private void ShowWarningIndicator(Vector3 obstaclePosition, DodgeType dodgeType)
        {
            if (warningIndicator == null || playerHead == null) return;
            
            warningIndicator.enabled = true;
            warningIndicator.SetPosition(0, obstaclePosition);
            warningIndicator.SetPosition(1, playerHead.position);
            warningIndicator.color = GetObstacleColor(dodgeType);
            
            // Auto-hide after 1 second
            Invoke(nameof(HideWarningIndicator), 1f);
        }
        
        private void HideWarningIndicator()
        {
            if (warningIndicator != null)
            {
                warningIndicator.enabled = false;
            }
        }
        
        private float GetDangerRadius(DodgeType dodgeType)
        {
            // Different dodge types have different danger zones
            switch (dodgeType)
            {
                case DodgeType.Squat: return 1.2f;
                case DodgeType.Duck: return 0.8f;
                case DodgeType.Spin360: return 1.5f;
                case DodgeType.Matrix: return 1.0f;
                default: return 1.0f;
            }
        }
        
        private float GetDangerLevel(DodgeType dodgeType)
        {
            // Difficulty rating for different dodge types
            switch (dodgeType)
            {
                case DodgeType.Squat: return 0.3f;
                case DodgeType.Duck: return 0.2f;
                case DodgeType.LeanLeft:
                case DodgeType.LeanRight: return 0.4f;
                case DodgeType.Spin360: return 0.8f;
                case DodgeType.Matrix: return 0.9f;
                case DodgeType.Jump: return 0.5f;
                default: return 0.5f;
            }
        }
        
        private async Task MoveObstacleAsync(DodgeObstacle obstacle)
        {
            try
            {
                float duration = Vector3.Distance(obstacle.spawnPosition, obstacle.targetPosition) / obstacle.speed;
                float elapsedTime = 0f;
                
                while (elapsedTime < duration && obstacle.gameObject != null && obstacle.isActive)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / duration;
                    
                    Vector3 currentPos = Vector3.Lerp(obstacle.spawnPosition, obstacle.targetPosition, progress);
                    obstacle.gameObject.transform.position = currentPos;
                    
                    await Task.Yield();
                }
                
                // Obstacle reached player area
                if (obstacle.gameObject != null && obstacle.isActive && !obstacle.wasAvoided)
                {
                    OnObstacleReachedPlayer(obstacle);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in obstacle movement: {ex.Message}");
            }
        }
        
        private void UpdateObstacles()
        {
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = activeObstacles[i];
                
                if (obstacle.gameObject == null)
                {
                    activeObstacles.RemoveAt(i);
                    continue;
                }
                
                // Check for timeout
                if (Time.time - obstacle.spawnTime > obstacleLifetime)
                {
                    CleanupObstacle(obstacle);
                    activeObstacles.RemoveAt(i);
                }
            }
        }
        
        private void ProcessDodging()
        {
            // Check each active obstacle for successful dodges
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle.wasAvoided || !obstacle.isActive) continue;
                
                if (IsObstacleInDangerZone(obstacle))
                {
                    CheckDodgeSuccess(obstacle);
                }
            }
        }
        
        private bool IsObstacleInDangerZone(DodgeObstacle obstacle)
        {
            if (playerHead == null || obstacle.gameObject == null) return false;
            
            float distance = Vector3.Distance(playerHead.position, obstacle.gameObject.transform.position);
            return distance <= obstacle.dangerRadius;
        }
        
        private void CheckDodgeSuccess(DodgeObstacle obstacle)
        {
            bool dodgeSuccessful = false;
            float accuracy = 0f;
            
            switch (obstacle.requiredDodge)
            {
                case DodgeType.Squat:
                    dodgeSuccessful = isSquatting;
                    accuracy = isSquatting ? 1f : 0f;
                    break;
                    
                case DodgeType.Duck:
                    dodgeSuccessful = isDucking || isSquatting;
                    accuracy = (isDucking || isSquatting) ? 1f : 0f;
                    break;
                    
                case DodgeType.LeanLeft:
                case DodgeType.LeanRight:
                    dodgeSuccessful = isLeaning;
                    accuracy = isLeaning ? 1f : 0f;
                    break;
                    
                case DodgeType.Spin360:
                    dodgeSuccessful = spinProgress >= 0.75f; // 75% of spin completed
                    accuracy = spinProgress;
                    break;
                    
                case DodgeType.Matrix:
                    dodgeSuccessful = isLeaning || isDucking;
                    accuracy = (isLeaning || isDucking) ? 1f : 0f;
                    break;
            }
            
            if (dodgeSuccessful)
            {
                OnDodgeSuccessful(obstacle, accuracy);
            }
        }
        
        private void CheckObstacleCollisions()
        {
            // Check for failed dodges (obstacles hitting player)
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle.wasAvoided || !obstacle.isActive) continue;
                
                if (IsObstacleCollidingWithPlayer(obstacle))
                {
                    OnDodgeFailed(obstacle);
                }
            }
        }
        
        private bool IsObstacleCollidingWithPlayer(DodgeObstacle obstacle)
        {
            if (playerHead == null || obstacle.gameObject == null) return false;
            
            float distance = Vector3.Distance(playerHead.position, obstacle.gameObject.transform.position);
            return distance <= 0.3f; // Very close collision detection
        }
        
        private void OnDodgeSuccessful(DodgeObstacle obstacle, float accuracy)
        {
            obstacle.wasAvoided = true;
            dodgesSuccessful++;
            totalDodgeAccuracy += accuracy;
            
            // Calculate response time
            float responseTime = Time.time - obstacle.spawnTime;
            averageResponseTime = (averageResponseTime * (dodgesSuccessful - 1) + responseTime) / dodgesSuccessful;
            
            // Calculate score
            bool isPerfect = accuracy >= 0.9f;
            int baseScore = isPerfect ? 300 : 200;
            int finalScore = Mathf.RoundToInt(baseScore * accuracy);
            
            // Create dodge data
            var dodgeData = new DodgeData
            {
                dodgeType = obstacle.requiredDodge,
                accuracy = accuracy,
                responseTime = responseTime,
                isPerfectDodge = isPerfect,
                obstaclePosition = obstacle.gameObject.transform.position,
                score = finalScore
            };
            
            // Add score to game
            GameManager.Instance?.AddScore(finalScore, isPerfect);
            
            // Play success effects
            PlaySuccessEffects(obstacle.gameObject.transform.position, isPerfect);
            
            // Clean up obstacle
            CleanupObstacle(obstacle);
            
            // Trigger events
            OnDodgeSuccess?.Invoke(dodgeData);
            
            Debug.Log($"✅ Dodge successful! Type: {obstacle.requiredDodge}, Accuracy: {accuracy:F2}, Score: {finalScore}");
        }
        
        private void OnDodgeFailed(DodgeObstacle obstacle)
        {
            obstacle.wasAvoided = false;
            dodgesFailed++;
            
            // Create dodge data
            var dodgeData = new DodgeData
            {
                dodgeType = obstacle.requiredDodge,
                accuracy = 0f,
                responseTime = Time.time - obstacle.spawnTime,
                isPerfectDodge = false,
                obstaclePosition = obstacle.gameObject.transform.position,
                score = 0
            };
            
            // Play fail effects
            PlayFailEffects(obstacle.gameObject.transform.position);
            
            // Clean up obstacle
            CleanupObstacle(obstacle);
            
            // Trigger events
            OnDodgeFail?.Invoke(dodgeData);
            
            Debug.Log($"❌ Dodge failed! Type: {obstacle.requiredDodge}");
        }
        
        private void OnObstacleReachedPlayer(DodgeObstacle obstacle)
        {
            // Obstacle reached player without being dodged
            OnDodgeFailed(obstacle);
        }
        
        private void PlaySuccessEffects(Vector3 position, bool isPerfect)
        {
            // Play particle effect
            if (dodgeSuccessEffect != null)
            {
                dodgeSuccessEffect.transform.position = position;
                dodgeSuccessEffect.Play();
                
                if (isPerfect)
                {
                    var main = dodgeSuccessEffect.main;
                    main.startColor = Color.gold;
                }
            }
            
            // Play success audio
            PlayRandomAudioClip(dodgeSuccessClips);
        }
        
        private void PlayFailEffects(Vector3 position)
        {
            // Play particle effect
            if (dodgeFailEffect != null)
            {
                dodgeFailEffect.transform.position = position;
                dodgeFailEffect.Play();
            }
            
            // Play fail audio
            PlayRandomAudioClip(dodgeFailClips);
        }
        
        private void PlayRandomAudioClip(AudioClip[] clips)
        {
            if (clips != null && clips.Length > 0 && dodgeAudioSource != null)
            {
                int index = UnityEngine.Random.Range(0, clips.Length);
                dodgeAudioSource.PlayOneShot(clips[index]);
            }
        }
        
        private void CleanupObstacle(DodgeObstacle obstacle)
        {
            if (obstacle.gameObject != null)
            {
                Destroy(obstacle.gameObject);
            }
        }
        
        private void CheckSpinDodge()
        {
            // Check if there are any spin obstacles that this dodge applies to
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle.requiredDodge == DodgeType.Spin360 && IsObstacleInDangerZone(obstacle))
                {
                    OnDodgeSuccessful(obstacle, 1f);
                    break;
                }
            }
        }
        
        // Public API
        public void StartDodgingMode()
        {
            IsDodgingModeActive = true;
            
            // Reset statistics
            dodgesSuccessful = 0;
            dodgesFailed = 0;
            obstaclesSpawned = 0;
            totalDodgeAccuracy = 0f;
            averageResponseTime = 0f;
            
            // Clear existing obstacles
            ClearAllObstacles();
            
            // Reset player tracking
            if (playerHead != null)
            {
                playerBaseHeight = playerHead.position.y;
                playerBasePosition = playerHead.position;
            }
            
            Debug.Log("🤸 Comprehensive Dodging Mode started!");
        }
        
        public void StopDodgingMode()
        {
            IsDodgingModeActive = false;
            ClearAllObstacles();
            
            Debug.Log("🤸 Comprehensive Dodging Mode stopped");
        }
        
        public void SetIntensiveDodgingMode(bool intensive)
        {
            intensiveDodgingMode = intensive;
            OnIntensityChanged?.Invoke(intensive ? 1f : 0f);
            
            Debug.Log($"🔥 Intensive dodging mode: {(intensive ? "ON" : "OFF")}");
        }
        
        public void IntegrateWithFlowMode(bool integrate)
        {
            integratedWithFlow = integrate;
            
            if (integrate && flowSystem != null)
            {
                Debug.Log("🌊 Dodging integrated with Flow Mode");
            }
        }
        
        public void IntegrateWithStaffMode(bool integrate)
        {
            integratedWithStaff = integrate;
            
            if (integrate && staffSystem != null)
            {
                Debug.Log("🥢 Dodging integrated with Staff Mode");
            }
        }
        
        public void ClearAllObstacles()
        {
            foreach (var obstacle in activeObstacles)
            {
                CleanupObstacle(obstacle);
            }
            activeObstacles.Clear();
        }
        
        public void SetDifficulty(float difficulty)
        {
            difficultyMultiplier = Mathf.Clamp(difficulty, 0.5f, 3f);
            
            // Adjust thresholds based on difficulty
            squatThreshold = Mathf.Lerp(0.5f, 0.3f, difficulty);
            spinThreshold = Mathf.Lerp(180f, 360f, difficulty);
            leanThreshold = Mathf.Lerp(0.2f, 0.4f, difficulty);
        }
        
        // Integration Methods (called by other systems)
        public void SpawnDodgeObstacleForFlow(Vector3 position, DodgeType dodgeType)
        {
            if (!integratedWithFlow) return;
            
            // Spawn dodge obstacle at specific position for flow mode integration
            SpawnObstacleAtPosition(position, dodgeType);
        }
        
        public void SpawnDodgeObstacleForStaff(Vector3 position, DodgeType dodgeType)
        {
            if (!integratedWithStaff) return;
            
            // Spawn dodge obstacle at specific position for staff mode integration
            SpawnObstacleAtPosition(position, dodgeType);
        }
        
        private void SpawnObstacleAtPosition(Vector3 position, DodgeType dodgeType)
        {
            GameObject obstaclePrefab = GetObstaclePrefab(dodgeType);
            if (obstaclePrefab == null) return;
            
            GameObject obstacleObj = Instantiate(obstaclePrefab, position, Quaternion.identity);
            
            var obstacle = new DodgeObstacle
            {
                gameObject = obstacleObj,
                requiredDodge = dodgeType,
                spawnPosition = position,
                targetPosition = playerHead != null ? playerHead.position : Vector3.zero,
                spawnTime = Time.time,
                speed = obstacleSpeed,
                isActive = true,
                wasAvoided = false,
                dangerRadius = GetDangerRadius(dodgeType)
            };
            
            ApplyObstacleVisuals(obstacleObj, dodgeType);
            activeObstacles.Add(obstacle);
        }
        
        // Statistics
        public Dictionary<string, object> GetDodgingStats()
        {
            return new Dictionary<string, object>
            {
                {"dodges_successful", dodgesSuccessful},
                {"dodges_failed", dodgesFailed},
                {"dodge_accuracy", DodgeAccuracy},
                {"obstacles_spawned", obstaclesSpawned},
                {"average_response_time", averageResponseTime},
                {"intensive_mode", intensiveDodgingMode},
                {"integrated_with_flow", integratedWithFlow},
                {"integrated_with_staff", integratedWithStaff}
            };
        }
        
        private void OnDestroy()
        {
            StopDodgingMode();
        }
    }
    
    // Dodge Obstacle Component
    public class DodgeObstacleComponent : MonoBehaviour
    {
        private ComprehensiveDodgingSystem.DodgeObstacle obstacleData;
        
        public void Initialize(ComprehensiveDodgingSystem.DodgeObstacle data)
        {
            obstacleData = data;
        }
        
        public ComprehensiveDodgingSystem.DodgeObstacle GetObstacleData()
        {
            return obstacleData;
        }
    }
} 