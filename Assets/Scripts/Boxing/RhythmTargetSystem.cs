using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using VRBoxingGame.Core;
using VRBoxingGame.Audio;
using VRBoxingGame.Environment;
using System.Threading.Tasks;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Rhythm-based target system for white/gray circles with spinning block mechanics
    /// Enhanced with GPU instancing for optimal VR performance
    /// </summary>
    public class RhythmTargetSystem : MonoBehaviour
    {
        [Header("Circle Target Settings")]
        public GameObject whiteCirclePrefab;
        public GameObject grayCirclePrefab;
        public GameObject combinedBlockPrefab;
        public float baseSpeed = 5f;
        public float maxSpeed = 15f;
        public float circleSize = 0.3f;
        
        [Header("Spawning")]
        public Transform leftSpawnPoint;
        public Transform rightSpawnPoint;
        public Transform centerPoint;
        public float spawnDistance = 10f;
        
        [Header("Rhythm Mechanics")]
        public bool syncWithMusic = true;
        public float beatAnticipation = 0.5f; // Spawn circles this many seconds before beat
        public float spinSpeedMultiplier = 2f;
        public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Block Mechanics")]
        public float blockCombineTime = 1f;
        public float blockSpinDuration = 2f;
        public float blockApproachTime = 1.5f;
        
        [Header("GPU Instancing Optimization")]
        public bool enableGPUInstancing = true;
        public int maxInstancesPerBatch = 1023; // GPU instancing limit
        public Material whiteCircleInstancedMaterial;
        public Material grayCircleInstancedMaterial;
        
        [Header("Events")]
        public UnityEvent<CircleHitData> OnCircleHit;
        public UnityEvent<BlockData> OnBlockSuccess;
        public UnityEvent<BlockData> OnBlockFailed;
        public UnityEvent<float> OnSpeedChange;
        
        public enum CircleType
        {
            White,
            Gray
        }
        
        public enum HandSide
        {
            Left,
            Right
        }
        
        [System.Serializable]
        public struct CircleHitData
        {
            public CircleType circleType;
            public HandSide requiredHand;
            public float accuracy;
            public float speed;
            public Vector3 hitPosition;
            public bool isPerfectTiming;
        }
        
        [System.Serializable]
        public struct BlockData
        {
            public float spinSpeed;
            public float approachSpeed;
            public bool wasBlocked;
            public float blockTiming;
            public Vector3 blockPosition;
        }
        
        [System.Serializable]
        public struct RhythmCircle
        {
            public GameObject gameObject;
            public CircleType type;
            public HandSide requiredHand;
            public float spawnTime;
            public float targetTime;
            public float speed;
            public bool isHit;
            public Vector3 targetPosition;
        }
        
        // Private variables
        private List<RhythmCircle> activeCircles = new List<RhythmCircle>();
        private Queue<float> upcomingBeats = new Queue<float>();
        private float currentDifficulty = 1f;
        private float lastBeatTime = 0f;
        
        // Block mechanics
        private bool blockInProgress = false;
        private GameObject activeBlockObject;
        private float blockStartTime;
        private float blockSpinSpeed;
        
        // Performance tracking
        private int circlesHit = 0;
        private int circlesMissed = 0;
        private int blocksSuccessful = 0;
        private int blocksFailed = 0;
        
        // GPU Instancing variables
        private MaterialPropertyBlock whitePropertyBlock;
        private MaterialPropertyBlock grayPropertyBlock;
        private Matrix4x4[] whiteInstanceMatrices = new Matrix4x4[1023];
        private Matrix4x4[] grayInstanceMatrices = new Matrix4x4[1023];
        private Vector4[] whiteInstanceColors = new Vector4[1023];
        private Vector4[] grayInstanceColors = new Vector4[1023];
        private int whiteInstanceCount = 0;
        private int grayInstanceCount = 0;
        private Mesh circleMesh;
        
        // Performance tracking
        private int totalInstancesRendered = 0;
        private float lastInstanceRenderTime = 0f;
        
        // Singleton
        public static RhythmTargetSystem Instance { get; private set; }
        
        // Properties
        public float HitAccuracy => circlesHit + circlesMissed > 0 ? (float)circlesHit / (circlesHit + circlesMissed) : 0f;
        public float BlockAccuracy => blocksSuccessful + blocksFailed > 0 ? (float)blocksSuccessful / (blocksSuccessful + blocksFailed) : 0f;
        public float CurrentDifficulty => currentDifficulty;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeRhythmSystem();
                InitializeGPUInstancing();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeRhythmSystem()
        {
            // Validate spawn points
            if (leftSpawnPoint == null || rightSpawnPoint == null || centerPoint == null)
            {
                Debug.LogError("Spawn points not assigned! Creating default spawn points.");
                CreateDefaultSpawnPoints();
            }
            
            // Initialize beat queue
            upcomingBeats.Clear();
            
            // Subscribe to audio events
            if (AdvancedAudioManager.Instance != null)
            {
                AdvancedAudioManager.Instance.OnBeatDetected.AddListener(OnBeatDetected);
            }
            
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);
            }
            
            Debug.Log("Rhythm Target System initialized");
        }
        
        private void Update()
        {
            UpdateActiveCircles();
            UpdateBlockMechanics();
            UpdateDifficulty();
            
            if (syncWithMusic)
            {
                ProcessUpcomingBeats();
            }
            
            // Render instanced targets for GPU optimization
            if (enableGPUInstancing)
            {
                RenderInstancedTargets();
            }
        }
        
        private void OnBeatDetected(AdvancedAudioManager.BeatData beatData)
        {
            if (beatData.isStrongBeat)
            {
                // Schedule circle spawn for this beat
                float spawnTime = Time.time + beatAnticipation;
                upcomingBeats.Enqueue(spawnTime);
                lastBeatTime = Time.time;
            }
        }
        
        private void ProcessUpcomingBeats()
        {
            while (upcomingBeats.Count > 0 && Time.time >= upcomingBeats.Peek())
            {
                upcomingBeats.Dequeue();
                SpawnRhythmCircles();
            }
        }
        
        private void SpawnRhythmCircles()
        {
            // Determine pattern based on difficulty and music intensity
            float musicEnergy = AdvancedAudioManager.Instance?.GetTotalEnergy() ?? 0.5f;
            bool spawnPair = currentDifficulty > 0.3f && (Random.value < 0.6f || musicEnergy > 0.7f);
            
            if (spawnPair)
            {
                // Spawn both circles for eventual block - WHITE = LEFT, GRAY = RIGHT
                SpawnCircle(CircleType.White, HandSide.Left);
                SpawnCircle(CircleType.Gray, HandSide.Right);
            }
            else
            {
                // Spawn single circle - maintain white=left, gray=right pattern
                if (Random.value < 0.5f)
                {
                    SpawnCircle(CircleType.White, HandSide.Left);
                }
                else
                {
                    SpawnCircle(CircleType.Gray, HandSide.Right);
                }
            }
        }
        
        private void SpawnCircle(CircleType type, HandSide requiredHand)
        {
            // Determine spawn position
            Vector3 spawnPos = (requiredHand == HandSide.Left) ? leftSpawnPoint.position : rightSpawnPoint.position;
            
            // Create circle data
            var rhythmData = new RhythmCircle
            {
                type = type,
                requiredHand = requiredHand,
                spawnTime = Time.time,
                targetTime = Time.time + beatAnticipation,
                speed = Mathf.Lerp(baseSpeed, maxSpeed, currentDifficulty),
                targetPosition = centerPoint.position
            };
            
            // Create prefab instance
            GameObject prefab = (type == CircleType.White) ? whiteCirclePrefab : grayCirclePrefab;
            if (prefab != null)
            {
                rhythmData.gameObject = Instantiate(prefab, spawnPos, Quaternion.identity);
                rhythmData.gameObject.transform.localScale = Vector3.one * circleSize;
                
                // Add circle component for hit detection
                var circleComponent = rhythmData.gameObject.GetComponent<RhythmCircleComponent>();
                if (circleComponent == null)
                {
                    circleComponent = rhythmData.gameObject.AddComponent<RhythmCircleComponent>();
                }
                circleComponent.circleType = type;
                circleComponent.requiredHand = requiredHand;
                
                // Apply GPU instancing optimization
                ApplyGPUInstancingOptimization(rhythmData.gameObject, type);
                
                // Start movement
                _ = MoveCircleToTargetAsync(rhythmData);
                
                activeCircles.Add(rhythmData);
                
                // Apply scene transformation if enabled
                if (SceneTransformationSystem.Instance != null && SceneTransformationSystem.Instance.enableTransformations)
                {
                    GameObject transformedTarget = SceneTransformationSystem.Instance.TransformTarget(rhythmData.gameObject, type);
                    if (transformedTarget != rhythmData.gameObject)
                    {
                        rhythmData.gameObject = transformedTarget;
                    }
                }
            }
        }
        
        private async Task MoveCircleToTargetAsync(RhythmCircle circle)
        {
            try
            {
                if (circle.gameObject == null) return;
                
                Vector3 startPos = circle.gameObject.transform.position;
                Vector3 endPos = circle.targetPosition;
                float journeyTime = beatAnticipation;
                float elapsedTime = 0f;
                
                while (elapsedTime < journeyTime && circle.gameObject != null && !circle.isHit)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = speedCurve.Evaluate(elapsedTime / journeyTime);
                    
                    circle.gameObject.transform.position = Vector3.Lerp(startPos, endPos, progress);
                    
                    await Task.Yield();
                }
                
                // Circle reached center without being hit
                if (circle.gameObject != null && !circle.isHit)
                {
                    OnCircleMissed(circle);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in circle movement: {ex.Message}");
            }
        }
        
        private bool ShouldCreateBlock()
        {
            // Count unhit circles near center
            int circlesNearCenter = 0;
            foreach (var circle in activeCircles)
            {
                if (!circle.isHit && Vector3.Distance(circle.gameObject.transform.position, centerPoint.position) < 1f)
                {
                    circlesNearCenter++;
                }
            }
            
            return circlesNearCenter >= 2 && !blockInProgress;
        }
        
        private void CreateBlockSequence()
        {
            if (blockInProgress || combinedBlockPrefab == null) return;
            
            // Calculate average speed of recent circles
            float avgSpeed = GetAverageCircleSpeed();
            
            // Create block at center
            activeBlockObject = Instantiate(combinedBlockPrefab, centerPoint.position, Quaternion.identity);
            activeBlockObject.transform.localScale = Vector3.one * circleSize * 1.5f;
            
            // Set block properties
            blockInProgress = true;
            blockStartTime = Time.time;
            blockSpinSpeed = avgSpeed * spinSpeedMultiplier;
            
            // Apply scene transformation
            if (SceneTransformationSystem.Instance != null && SceneTransformationSystem.Instance.enableTransformations)
            {
                GameObject transformedBlock = SceneTransformationSystem.Instance.TransformBlock(activeBlockObject, blockSpinSpeed);
                if (transformedBlock != activeBlockObject)
                {
                    activeBlockObject = transformedBlock;
                }
            }
            
            // Start block sequence
            _ = BlockSequenceAsync(avgSpeed);
            
            // Clear circles near center to make room for block
            ClearCirclesNearCenter();
        }
        
        private async Task BlockSequenceAsync(float approachSpeed)
        {
            try
            {
                // Phase 1: Spin at center - duration based on approach speed
                float spinDuration = Mathf.Lerp(blockSpinDuration * 1.5f, blockSpinDuration * 0.5f, approachSpeed / maxSpeed);
                float spinEndTime = Time.time + spinDuration;
                Vector3 spinPosition = centerPoint.position;
                
                // Visual feedback - scale up slightly during spin phase
                Vector3 originalScale = activeBlockObject.transform.localScale;
                Vector3 spinScale = originalScale * 1.2f;
                
                while (Time.time < spinEndTime && activeBlockObject != null)
                {
                    // Spin the object - faster spin = faster approach
                    activeBlockObject.transform.Rotate(0, 0, blockSpinSpeed * Time.deltaTime);
                    
                    // Pulse scale during spin to indicate danger
                    float pulseTime = (Time.time - blockStartTime) / spinDuration;
                    float scaleMultiplier = 1f + Mathf.Sin(pulseTime * Mathf.PI * 4f) * 0.1f;
                    activeBlockObject.transform.localScale = originalScale * scaleMultiplier;
                    
                    await Task.Yield();
                }
                
                // Phase 2: Approach player - speed correlates with spin speed
                Vector3 startPos = spinPosition;
                var xrCamera = FindObjectOfType<Camera>();
                if (xrCamera == null) xrCamera = Camera.main;
                Vector3 endPos = xrCamera.transform.position + xrCamera.transform.forward * 0.3f;
                
                // Faster spinning blocks approach faster
                float speedMultiplier = blockSpinSpeed / 360f; // Normalize spin speed
                float approachTime = blockApproachTime / Mathf.Max(speedMultiplier, 0.5f);
                float elapsedTime = 0f;
                
                // Reset scale for approach
                activeBlockObject.transform.localScale = originalScale;
                
                while (elapsedTime < approachTime && activeBlockObject != null)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / approachTime;
                    
                    // Move towards player
                    activeBlockObject.transform.position = Vector3.Lerp(startPos, endPos, progress);
                    
                    // Continue spinning
                    activeBlockObject.transform.Rotate(0, 0, blockSpinSpeed * Time.deltaTime);
                    
                    await Task.Yield();
                }
                
                // Block reached player without being blocked
                if (activeBlockObject != null)
                {
                    OnBlockFailed(approachSpeed);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in block sequence: {ex.Message}");
                
                // Clean up block on error
                if (activeBlockObject != null)
                {
                    Destroy(activeBlockObject);
                    activeBlockObject = null;
                }
                blockInProgress = false;
            }
        }
        
        private float GetAverageCircleSpeed()
        {
            float totalSpeed = 0f;
            int count = 0;
            
            foreach (var circle in activeCircles)
            {
                if (!circle.isHit)
                {
                    totalSpeed += circle.speed;
                    count++;
                }
            }
            
            return count > 0 ? totalSpeed / count : baseSpeed;
        }
        
        private void ClearCirclesNearCenter()
        {
            for (int i = activeCircles.Count - 1; i >= 0; i--)
            {
                var circle = activeCircles[i];
                if (Vector3.Distance(circle.gameObject.transform.position, centerPoint.position) < 1f)
                {
                    if (circle.gameObject != null)
                    {
                        Destroy(circle.gameObject);
                    }
                    activeCircles.RemoveAt(i);
                }
            }
        }
        
        private void UpdateActiveCircles()
        {
            for (int i = activeCircles.Count - 1; i >= 0; i--)
            {
                var circle = activeCircles[i];
                
                if (circle.gameObject == null)
                {
                    activeCircles.RemoveAt(i);
                    continue;
                }
                
                // Check for timeout
                if (Time.time > circle.targetTime + 1f && !circle.isHit)
                {
                    OnCircleMissed(circle);
                    activeCircles.RemoveAt(i);
                }
            }
        }
        
        private void UpdateBlockMechanics()
        {
            if (blockInProgress && activeBlockObject == null)
            {
                blockInProgress = false;
            }
        }
        
        private void UpdateDifficulty()
        {
            if (GameManager.Instance != null)
            {
                currentDifficulty = GameManager.Instance.GetDynamicDifficulty();
                OnSpeedChange?.Invoke(currentDifficulty);
            }
        }
        
        public void OnCircleHit(CircleType type, HandSide hand, Vector3 hitPosition)
        {
            // Find matching circle
            for (int i = 0; i < activeCircles.Count; i++)
            {
                var circle = activeCircles[i];
                
                if (!circle.isHit && circle.type == type && circle.requiredHand == hand)
                {
                    // Check if it's the closest circle of this type
                    float distance = Vector3.Distance(circle.gameObject.transform.position, hitPosition);
                    
                    if (distance < circleSize)
                    {
                        // Calculate accuracy
                        float timing = Mathf.Abs(Time.time - circle.targetTime);
                        float accuracy = Mathf.Clamp01(1f - (timing / 0.5f));
                        bool isPerfect = timing < 0.1f;
                        
                        CircleHitData hitData = new CircleHitData
                        {
                            circleType = type,
                            requiredHand = hand,
                            accuracy = accuracy,
                            speed = circle.speed,
                            hitPosition = hitPosition,
                            isPerfectTiming = isPerfect
                        };
                        
                        // Mark as hit
                        circle.isHit = true;
                        activeCircles[i] = circle;
                        
                        // Award points
                        int basePoints = 100;
                        int bonusPoints = isPerfect ? 50 : 0;
                        int totalPoints = Mathf.RoundToInt((basePoints + bonusPoints) * accuracy);
                        
                        GameManager.Instance?.AddScore(totalPoints, isPerfect);
                        
                        // Trigger events
                        OnCircleHit?.Invoke(hitData);
                        
                        // Destroy circle
                        if (circle.gameObject != null)
                        {
                            Destroy(circle.gameObject);
                        }
                        
                        circlesHit++;
                        break;
                    }
                }
            }
        }
        
        public void OnBlockAttempt(Vector3 blockPosition)
        {
            if (!blockInProgress || activeBlockObject == null) return;
            
            float distance = Vector3.Distance(activeBlockObject.transform.position, blockPosition);
            
            if (distance < 0.5f) // Block successful
            {
                float blockTiming = Time.time - blockStartTime;
                
                BlockData blockData = new BlockData
                {
                    spinSpeed = blockSpinSpeed,
                    approachSpeed = GetAverageCircleSpeed(),
                    wasBlocked = true,
                    blockTiming = blockTiming,
                    blockPosition = blockPosition
                };
                
                // Award block points
                int blockPoints = 200;
                GameManager.Instance?.AddScore(blockPoints, true);
                
                OnBlockSuccess?.Invoke(blockData);
                
                // Clean up
                Destroy(activeBlockObject);
                activeBlockObject = null;
                blockInProgress = false;
                blocksSuccessful++;
            }
        }
        
        private void OnCircleMissed(RhythmCircle circle)
        {
            if (circle.gameObject != null)
            {
                Destroy(circle.gameObject);
            }
            circlesMissed++;
            GameManager.Instance?.RegisterTargetMissed();
        }
        
        private void OnBlockFailed(float approachSpeed)
        {
            BlockData blockData = new BlockData
            {
                spinSpeed = blockSpinSpeed,
                approachSpeed = approachSpeed,
                wasBlocked = false,
                blockTiming = Time.time - blockStartTime,
                blockPosition = Vector3.zero
            };
            
            OnBlockFailed?.Invoke(blockData);
            
            if (activeBlockObject != null)
            {
                Destroy(activeBlockObject);
            }
            activeBlockObject = null;
            blockInProgress = false;
            blocksFailed++;
            
            GameManager.Instance?.RegisterTargetMissed();
        }
        
        // Public API
        public void ClearAllTargets()
        {
            foreach (var circle in activeCircles)
            {
                if (circle.gameObject != null)
                {
                    Destroy(circle.gameObject);
                }
            }
            activeCircles.Clear();
            
            if (activeBlockObject != null)
            {
                Destroy(activeBlockObject);
                activeBlockObject = null;
            }
            blockInProgress = false;
        }
        
        public float GetDynamicDifficulty()
        {
            return currentDifficulty;
        }
        
        public void SetDifficulty(float difficulty)
        {
            currentDifficulty = Mathf.Clamp01(difficulty);
        }
        
        private void OnDrawGizmos()
        {
            // Draw spawn points
            if (leftSpawnPoint != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(leftSpawnPoint.position, 0.2f);
            }
            
            if (rightSpawnPoint != null)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(rightSpawnPoint.position, 0.2f);
            }
            
            if (centerPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(centerPoint.position, 0.3f);
            }
        }
        
        private void InitializeGPUInstancing()
        {
            if (!enableGPUInstancing) return;
            
            // Initialize MaterialPropertyBlocks
            whitePropertyBlock = new MaterialPropertyBlock();
            grayPropertyBlock = new MaterialPropertyBlock();
            
            // Get circle mesh from prefab or create default
            if (whiteCirclePrefab != null)
            {
                var meshFilter = whiteCirclePrefab.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    circleMesh = meshFilter.sharedMesh;
                }
            }
            
            // Fallback to primitive mesh
            if (circleMesh == null)
            {
                circleMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
            }
            
            // Initialize instanced materials if not assigned
            if (whiteCircleInstancedMaterial == null)
            {
                whiteCircleInstancedMaterial = CreateInstancedMaterial(Color.white);
            }
            if (grayCircleInstancedMaterial == null)
            {
                grayCircleInstancedMaterial = CreateInstancedMaterial(Color.gray);
            }
            
            Debug.Log("GPU Instancing initialized for RhythmTargetSystem");
        }
        
        private Material CreateInstancedMaterial(Color baseColor)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.enableInstancing = true;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Smoothness", 0.8f);
            return material;
        }
        
        private Mesh GetPrimitiveMesh(PrimitiveType primitiveType)
        {
            GameObject temp = GameObject.CreatePrimitive(primitiveType);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return mesh;
        }
        
        private void ApplyGPUInstancingOptimization(GameObject circle, CircleType type)
        {
            if (!enableGPUInstancing) return;
            
            // Add to instancing batch
            Matrix4x4 matrix = circle.transform.localToWorldMatrix;
            Color color = type == CircleType.White ? Color.white : Color.gray;
            
            if (type == CircleType.White && whiteInstanceCount < maxInstancesPerBatch)
            {
                whiteInstanceMatrices[whiteInstanceCount] = matrix;
                whiteInstanceColors[whiteInstanceCount] = color;
                whiteInstanceCount++;
            }
            else if (type == CircleType.Gray && grayInstanceCount < maxInstancesPerBatch)
            {
                grayInstanceMatrices[grayInstanceCount] = matrix;
                grayInstanceColors[grayInstanceCount] = color;
                grayInstanceCount++;
            }
            
            // Disable individual renderer to avoid double rendering
            var renderer = circle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        
        private void RenderInstancedTargets()
        {
            if (!enableGPUInstancing || circleMesh == null) return;
            
            // Render white circles
            if (whiteInstanceCount > 0 && whiteCircleInstancedMaterial != null)
            {
                whitePropertyBlock.SetVectorArray("_Color", whiteInstanceColors);
                
                Graphics.DrawMeshInstanced(
                    circleMesh,
                    0,
                    whiteCircleInstancedMaterial,
                    whiteInstanceMatrices,
                    whiteInstanceCount,
                    whitePropertyBlock
                );
                
                totalInstancesRendered += whiteInstanceCount;
            }
            
            // Render gray circles
            if (grayInstanceCount > 0 && grayCircleInstancedMaterial != null)
            {
                grayPropertyBlock.SetVectorArray("_Color", grayInstanceColors);
                
                Graphics.DrawMeshInstanced(
                    circleMesh,
                    0,
                    grayCircleInstancedMaterial,
                    grayInstanceMatrices,
                    grayInstanceCount,
                    grayPropertyBlock
                );
                
                totalInstancesRendered += grayInstanceCount;
            }
            
            // Reset counters for next frame
            whiteInstanceCount = 0;
            grayInstanceCount = 0;
            lastInstanceRenderTime = Time.time;
        }
        
        public int GetTotalInstancesRendered()
        {
            return totalInstancesRendered;
        }
        
        public float GetInstanceRenderEfficiency()
        {
            if (totalInstancesRendered == 0) return 0f;
            return (float)totalInstancesRendered / (activeCircles.Count + 1);
        }
        
        private void CreateDefaultSpawnPoints()
        {
            // Setup spawn points if not configured
            if (leftSpawnPoint == null)
            {
                leftSpawnPoint = new GameObject("LeftSpawn").transform;
                leftSpawnPoint.position = new Vector3(-3f, 1.5f, spawnDistance);
                leftSpawnPoint.SetParent(transform);
            }
            
            if (rightSpawnPoint == null)
            {
                rightSpawnPoint = new GameObject("RightSpawn").transform;
                rightSpawnPoint.position = new Vector3(3f, 1.5f, spawnDistance);
                rightSpawnPoint.SetParent(transform);
            }
            
            if (centerPoint == null)
            {
                centerPoint = new GameObject("CenterPoint").transform;
                centerPoint.position = new Vector3(0f, 1.5f, 0.5f);
                centerPoint.SetParent(transform);
            }
            
            // Set default spawn distance
            spawnDistance = 8f;
        }
        
        private void OnGameStateChanged(GameManager.GameState newState)
        {
            if (newState == GameManager.GameState.Playing)
            {
                // Reset performance tracking
                totalInstancesRendered = 0;
                whiteInstanceCount = 0;
                grayInstanceCount = 0;
            }
            else if (newState == GameManager.GameState.Finished)
            {
                ClearAllTargets();
            }
        }
    }
    
    // Component for individual rhythm circles
    public class RhythmCircleComponent : MonoBehaviour
    {
        public RhythmTargetSystem.CircleType circleType;
        public RhythmTargetSystem.HandSide requiredHand;
        
        private void OnTriggerEnter(Collider other)
        {
            // Check for hand collision
            if (other.CompareTag("LeftHand") && requiredHand == RhythmTargetSystem.HandSide.Left)
            {
                RhythmTargetSystem.Instance?.OnCircleHit(circleType, requiredHand, transform.position);
            }
            else if (other.CompareTag("RightHand") && requiredHand == RhythmTargetSystem.HandSide.Right)
            {
                RhythmTargetSystem.Instance?.OnCircleHit(circleType, requiredHand, transform.position);
            }
        }
    }
} 