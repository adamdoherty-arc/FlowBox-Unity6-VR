using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Rendering;
using UnityEngine;
using System.Collections.Generic;

namespace VRBoxingGame.Boxing.ECS
{
    /// <summary>
    /// High-performance ECS Target System for Unity 6
    /// Manages thousands of targets with optimal performance
    /// </summary>
    public class ECSTargetSystem : MonoBehaviour
    {
        [Header("ECS Settings")]
        public int maxTargets = 1000;
        public bool enableBurstCompilation = true;
        public float targetLifetime = 5f;
        
        [Header("Target Types")]
        public GameObject whiteTargetPrefab;
        public GameObject grayTargetPrefab;
        public GameObject blockTargetPrefab;
        
        [Header("Spawning")]
        public float spawnRadius = 2.5f;
        public int spawnPoints = 8;
        public float spawnHeight = 1.5f;
        
        // ECS World reference
        private World targetWorld;
        private EntityManager entityManager;
        private EntityQuery targetQuery;
        private EntityQuery movementQuery;
        
        // Entity prefabs
        private Entity whiteTargetEntity;
        private Entity grayTargetEntity;
        private Entity blockTargetEntity;
        
        // Job handles
        private JobHandle targetMovementJobHandle;
        private JobHandle targetLifetimeJobHandle;
        private JobHandle targetCollisionJobHandle;
        
        // Performance tracking
        private int activeTargetCount = 0;
        private float lastSpawnTime = 0f;
        private NativeArray<float3> spawnPositions;
        
        // Singleton
        public static ECSTargetSystem Instance { get; private set; }
        
        // Public properties
        public int ActiveTargetCount => activeTargetCount;
        public World TargetWorld => targetWorld;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeECSWorld();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeECSWorld()
        {
            // Create dedicated ECS world for targets
            targetWorld = new World("VR Boxing Targets");
            entityManager = targetWorld.EntityManager;
            
            // Setup entity queries
            SetupEntityQueries();
            
            // Create entity prefabs
            CreateEntityPrefabs();
            
            // Initialize spawn positions
            InitializeSpawnPositions();
            
            Debug.Log($"🎯 ECS Target System initialized - Max Targets: {maxTargets}");
        }
        
        private void SetupEntityQueries()
        {
            // Query for all targets
            targetQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<TargetComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
            
            // Query for moving targets
            movementQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<LocalTransform>(),
                ComponentType.ReadOnly<TargetMovementComponent>(),
                ComponentType.ReadOnly<TargetComponent>()
            );
        }
        
        private void CreateEntityPrefabs()
        {
            // Convert prefabs to entities using Unity 6 approach
            if (whiteTargetPrefab != null)
            {
                // Create entity archetype
                var archetype = entityManager.CreateArchetype(
                    typeof(TargetComponent),
                    typeof(LocalTransform),
                    typeof(TargetMovementComponent)
                );
                
                whiteTargetEntity = entityManager.CreateEntity(archetype);
                
                // Add ECS components
                entityManager.SetComponentData(whiteTargetEntity, new TargetComponent
                {
                    targetType = TargetType.White,
                    spawnTime = 0f,
                    lifetime = targetLifetime,
                    isHit = false,
                    score = 100
                });
                
                entityManager.SetComponentData(whiteTargetEntity, new LocalTransform
                {
                    Position = float3.zero,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
            }
            
            if (grayTargetPrefab != null)
            {
                var archetype = entityManager.CreateArchetype(
                    typeof(TargetComponent),
                    typeof(LocalTransform),
                    typeof(TargetMovementComponent)
                );
                
                grayTargetEntity = entityManager.CreateEntity(archetype);
                
                entityManager.SetComponentData(grayTargetEntity, new TargetComponent
                {
                    targetType = TargetType.Gray,
                    spawnTime = 0f,
                    lifetime = targetLifetime,
                    isHit = false,
                    score = 100
                });
                
                entityManager.SetComponentData(grayTargetEntity, new LocalTransform
                {
                    Position = float3.zero,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
            }
            
            if (blockTargetPrefab != null)
            {
                var archetype = entityManager.CreateArchetype(
                    typeof(TargetComponent),
                    typeof(LocalTransform),
                    typeof(TargetMovementComponent)
                );
                
                blockTargetEntity = entityManager.CreateEntity(archetype);
                
                entityManager.SetComponentData(blockTargetEntity, new TargetComponent
                {
                    targetType = TargetType.Block,
                    spawnTime = 0f,
                    lifetime = targetLifetime * 1.5f,
                    isHit = false,
                    score = 200
                });
                
                entityManager.SetComponentData(blockTargetEntity, new LocalTransform
                {
                    Position = float3.zero,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
            }
        }
        
        private void InitializeSpawnPositions()
        {
            spawnPositions = new NativeArray<float3>(spawnPoints, Allocator.Persistent);
            
            for (int i = 0; i < spawnPoints; i++)
            {
                float angle = (i * 360f / spawnPoints) * Mathf.Deg2Rad;
                float3 position = new float3(
                    math.cos(angle) * spawnRadius,
                    spawnHeight,
                    math.sin(angle) * spawnRadius
                );
                spawnPositions[i] = position;
            }
        }
        
        private void Update()
        {
            if (targetWorld == null || !targetWorld.IsCreated) return;
            
            // Update active target count
            activeTargetCount = targetQuery.CalculateEntityCount();
            
            // Schedule ECS jobs
            ScheduleTargetJobs();
            
            // Update the ECS world
            targetWorld.Update();
        }
        
        private void ScheduleTargetJobs()
        {
            // Complete previous jobs
            targetMovementJobHandle.Complete();
            targetLifetimeJobHandle.Complete();
            targetCollisionJobHandle.Complete();
            
            // Schedule target movement job
            var movementJob = new TargetMovementJob
            {
                deltaTime = Time.deltaTime,
                playerPosition = GetPlayerPosition()
            };
            targetMovementJobHandle = movementJob.ScheduleParallel(movementQuery);
            
            // Schedule lifetime management job
            var lifetimeJob = new TargetLifetimeJob
            {
                currentTime = Time.time,
                commandBuffer = new EntityCommandBuffer(Allocator.TempJob)
            };
            targetLifetimeJobHandle = lifetimeJob.Schedule(targetQuery, targetMovementJobHandle);
            
            // Schedule collision detection job
            var collisionJob = new TargetCollisionJob
            {
                playerPosition = GetPlayerPosition(),
                collisionRadius = 0.3f,
                commandBuffer = new EntityCommandBuffer(Allocator.TempJob)
            };
            targetCollisionJobHandle = collisionJob.Schedule(targetQuery, targetLifetimeJobHandle);
        }
        
        private float3 GetPlayerPosition()
        {
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                return xrOrigin.Camera.transform.position;
            }
            return float3.zero;
        }
        
        // Public API
        public void SpawnTarget(TargetType type, int spawnIndex = -1)
        {
            if (activeTargetCount >= maxTargets) return;
            
            // Select spawn position
            if (spawnIndex < 0) spawnIndex = UnityEngine.Random.Range(0, spawnPoints);
            spawnIndex = math.clamp(spawnIndex, 0, spawnPoints - 1);
            
            float3 spawnPos = spawnPositions[spawnIndex];
            
            // Select entity prefab
            Entity prefabEntity = type switch
            {
                TargetType.White => whiteTargetEntity,
                TargetType.Gray => grayTargetEntity,
                TargetType.Block => blockTargetEntity,
                _ => whiteTargetEntity
            };
            
            // Instantiate entity
            Entity targetEntity = entityManager.Instantiate(prefabEntity);
            
            // Set position
            entityManager.SetComponentData(targetEntity, new LocalTransform
            {
                Position = spawnPos,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            
            // Update target data
            var targetData = entityManager.GetComponentData<TargetComponent>(targetEntity);
            targetData.spawnTime = Time.time;
            targetData.targetType = type;
            entityManager.SetComponentData(targetEntity, targetData);
            
            // Add movement component
            entityManager.AddComponentData(targetEntity, new TargetMovementComponent
            {
                startPosition = spawnPos,
                targetPosition = GetPlayerPosition(),
                speed = GetTargetSpeed(type),
                startTime = Time.time
            });
            
            lastSpawnTime = Time.time;
        }
        
        public void SpawnTargetAt(TargetType type, float3 position)
        {
            if (activeTargetCount >= maxTargets) return;
            
            Entity prefabEntity = type switch
            {
                TargetType.White => whiteTargetEntity,
                TargetType.Gray => grayTargetEntity,
                TargetType.Block => blockTargetEntity,
                _ => whiteTargetEntity
            };
            
            Entity targetEntity = entityManager.Instantiate(prefabEntity);
            
            entityManager.SetComponentData(targetEntity, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            
            var targetData = entityManager.GetComponentData<TargetComponent>(targetEntity);
            targetData.spawnTime = Time.time;
            targetData.targetType = type;
            entityManager.SetComponentData(targetEntity, targetData);
            
            entityManager.AddComponentData(targetEntity, new TargetMovementComponent
            {
                startPosition = position,
                targetPosition = GetPlayerPosition(),
                speed = GetTargetSpeed(type),
                startTime = Time.time
            });
        }
        
        public void ClearAllTargets()
        {
            if (targetWorld == null || !targetWorld.IsCreated) return;
            
            entityManager.DestroyEntity(targetQuery);
        }
        
        public int GetTargetCount(TargetType type)
        {
            if (targetWorld == null || !targetWorld.IsCreated) return 0;
            
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<TargetComponent>()
            );
            
            int count = 0;
            var entities = query.ToEntityArray(Allocator.Temp);
            
            foreach (var entity in entities)
            {
                var targetData = entityManager.GetComponentData<TargetComponent>(entity);
                if (targetData.targetType == type)
                {
                    count++;
                }
            }
            
            entities.Dispose();
            return count;
        }
        
        private float GetTargetSpeed(TargetType type)
        {
            return type switch
            {
                TargetType.White => 3f,
                TargetType.Gray => 3f,
                TargetType.Block => 2f,
                _ => 3f
            };
        }
        
        private void OnDestroy()
        {
            // Complete all jobs
            if (targetMovementJobHandle.IsCreated) targetMovementJobHandle.Complete();
            if (targetLifetimeJobHandle.IsCreated) targetLifetimeJobHandle.Complete();
            if (targetCollisionJobHandle.IsCreated) targetCollisionJobHandle.Complete();
            
            // Dispose native arrays
            if (spawnPositions.IsCreated) spawnPositions.Dispose();
            
            // Dispose ECS world
            if (targetWorld != null && targetWorld.IsCreated)
            {
                targetWorld.Dispose();
            }
        }
    }
    
    // ECS Component definitions
    public struct TargetComponent : IComponentData
    {
        public TargetType targetType;
        public float spawnTime;
        public float lifetime;
        public bool isHit;
        public int score;
    }
    
    public struct TargetMovementComponent : IComponentData
    {
        public float3 startPosition;
        public float3 targetPosition;
        public float speed;
        public float startTime;
    }
    
    public enum TargetType
    {
        White,
        Gray,
        Block
    }
    
    // High-performance ECS Jobs
    
    [BurstCompile]
    public struct TargetMovementJob : IJobChunk
    {
        public float deltaTime;
        public float3 playerPosition;
        
        public ComponentTypeHandle<LocalTransform> transformHandle;
        [ReadOnly] public ComponentTypeHandle<TargetMovementComponent> movementHandle;
        
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var transforms = chunk.GetNativeArray(ref transformHandle);
            var movements = chunk.GetNativeArray(ref movementHandle);
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var transform = transforms[i];
                var movement = movements[i];
                
                // Calculate movement progress
                float elapsedTime = Time.time - movement.startTime;
                float progress = elapsedTime * movement.speed;
                
                // Move towards target
                float3 direction = math.normalize(movement.targetPosition - movement.startPosition);
                float3 newPosition = movement.startPosition + direction * progress;
                
                transforms[i] = new LocalTransform
                {
                    Position = newPosition,
                    Rotation = transform.Rotation,
                    Scale = transform.Scale
                };
            }
        }
    }
    
    [BurstCompile]
    public struct TargetLifetimeJob : IJobChunk
    {
        public float currentTime;
        public EntityCommandBuffer commandBuffer;
        
        [ReadOnly] public EntityTypeHandle entityHandle;
        [ReadOnly] public ComponentTypeHandle<TargetComponent> targetHandle;
        
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityHandle);
            var targets = chunk.GetNativeArray(ref targetHandle);
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var target = targets[i];
                
                // Check if target has expired
                if (currentTime - target.spawnTime > target.lifetime)
                {
                    commandBuffer.DestroyEntity(entities[i]);
                }
            }
        }
    }
    
    [BurstCompile]
    public struct TargetCollisionJob : IJobChunk
    {
        public float3 playerPosition;
        public float collisionRadius;
        public EntityCommandBuffer commandBuffer;
        
        [ReadOnly] public EntityTypeHandle entityHandle;
        [ReadOnly] public ComponentTypeHandle<LocalTransform> transformHandle;
        [ReadOnly] public ComponentTypeHandle<TargetComponent> targetHandle;
        
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityHandle);
            var transforms = chunk.GetNativeArray(ref transformHandle);
            var targets = chunk.GetNativeArray(ref targetHandle);
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var transform = transforms[i];
                var target = targets[i];
                
                // Check collision with player
                float distance = math.distance(transform.Position, playerPosition);
                if (distance <= collisionRadius && !target.isHit)
                {
                    // Mark as hit and schedule for removal
                    var updatedTarget = target;
                    updatedTarget.isHit = true;
                    commandBuffer.SetComponent(entities[i], updatedTarget);
                    commandBuffer.DestroyEntity(entities[i]);
                }
            }
        }
    }
} 