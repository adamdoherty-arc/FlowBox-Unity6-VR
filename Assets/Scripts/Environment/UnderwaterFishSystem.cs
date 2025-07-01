using UnityEngine;
using System.Collections.Generic;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using System.Collections;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Threading.Tasks;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Underwater Fish System - Implements detailed fish AI behaviors with Unity 6 optimization
    /// Small fish scatter, medium fish retreat and regroup, large fish become aggressive
    /// </summary>
    public class UnderwaterFishSystem : MonoBehaviour
    {
        [Header("Fish Spawning")]
        public int maxFishSchools = 5;
        public int fishPerSchool = 8;
        public float schoolRadius = 3f;
        public float spawnRadius = 15f;
        
        [Header("Fish Prefabs")]
        public GameObject smallFishPrefab;
        public GameObject mediumFishPrefab;
        public GameObject largeFishPrefab;
        public GameObject sharkBlockPrefab;
        
        [Header("Current Effects")]
        public Vector3 currentDirection = Vector3.right;
        public float currentStrength = 1f;
        public float currentVariation = 0.5f;
        
        [Header("Bioluminescence")]
        public float baseGlowIntensity = 0.5f;
        public float approachGlowMultiplier = 2f;
        public float hitGlowReduction = 0.3f;
        
        [Header("Unity 6 Optimization")]
        public bool enableJobSystemOptimization = true;
        public bool enableBurstCompilation = true;
        public int jobBatchSize = 32;
        
        // Singleton pattern - FIXED
        public static UnderwaterFishSystem Instance { get; private set; }
        
        // Active fish tracking
        private List<FishSchool> activeSchools = new List<FishSchool>();
        private List<GameObject> activeFish = new List<GameObject>();
        
        // Unity 6 Job System optimization
        private NativeArray<float3> fishPositions;
        private NativeArray<float3> fishVelocities;
        private NativeArray<float> fishTimers;
        private JobHandle fishUpdateJobHandle;
        
        // Current system
        private Vector3 currentVelocity;
        private float currentTimer = 0f;
        private float currentCycleDuration = 10f;
        
        private void Awake()
        {
            // Initialize singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            InitializeUnderwaterEnvironment();
            InitializeJobSystem();
        }
        
        private void InitializeJobSystem()
        {
            if (!enableJobSystemOptimization) return;
            
            int maxFish = maxFishSchools * fishPerSchool;
            fishPositions = new NativeArray<float3>(maxFish, Allocator.Persistent);
            fishVelocities = new NativeArray<float3>(maxFish, Allocator.Persistent);
            fishTimers = new NativeArray<float>(maxFish, Allocator.Persistent);
            
            Debug.Log("🐟 Fish Job System initialized for high-performance simulation");
        }
        
        private void Update()
        {
            if (enableJobSystemOptimization)
            {
                UpdateFishWithJobSystem();
            }
            else
            {
                UpdateCurrentSystem();
                UpdateFishSchools();
            }
            
            UpdateBioluminescence();
        }
        
        private void UpdateFishWithJobSystem()
        {
            // Complete previous job
            fishUpdateJobHandle.Complete();
            
            // Update fish data arrays
            UpdateFishDataArrays();
            
            // Schedule fish update job
            var fishUpdateJob = new FishSimulationJob
            {
                positions = fishPositions,
                velocities = fishVelocities,
                timers = fishTimers,
                deltaTime = Time.deltaTime,
                currentDirection = currentDirection,
                currentStrength = currentStrength,
                playerPosition = GetPlayerPosition()
            };
            
            fishUpdateJobHandle = fishUpdateJob.Schedule(activeFish.Count, jobBatchSize);
            
            // Apply results back to GameObjects
            fishUpdateJobHandle.Complete();
            ApplyJobResultsToFish();
        }
        
        private void UpdateFishDataArrays()
        {
            for (int i = 0; i < activeFish.Count && i < fishPositions.Length; i++)
            {
                if (activeFish[i] != null)
                {
                    fishPositions[i] = activeFish[i].transform.position;
                    var rb = activeFish[i].GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        fishVelocities[i] = rb.velocity;
                    }
                    fishTimers[i] += Time.deltaTime;
                }
            }
        }
        
        private void ApplyJobResultsToFish()
        {
            for (int i = 0; i < activeFish.Count && i < fishPositions.Length; i++)
            {
                if (activeFish[i] != null)
                {
                    activeFish[i].transform.position = fishPositions[i];
                    var rb = activeFish[i].GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = fishVelocities[i];
                    }
                }
            }
        }
        
        private float3 GetPlayerPosition()
        {
            return VRBoxingGame.Core.VRCameraHelper.PlayerPosition;
            return float3.zero;
        }
        
        private void InitializeUnderwaterEnvironment()
        {
            // Create initial fish schools
            for (int i = 0; i < maxFishSchools; i++)
            {
                CreateFishSchool();
            }
            
            Debug.Log($"🌊 Underwater Fish System initialized with {maxFishSchools} schools");
        }
        
        // ADDED: CreateFishTarget method that was referenced but missing
        public GameObject CreateFishTarget(RhythmTargetSystem.CircleType circleType, GameObject originalTarget)
        {
            // Determine fish size based on circle type
            FishTargetBehavior.FishSize fishSize = circleType == RhythmTargetSystem.CircleType.White ? 
                FishTargetBehavior.FishSize.Small : FishTargetBehavior.FishSize.Medium;
            
            // Get appropriate prefab
            GameObject fishPrefab = GetFishPrefab(fishSize);
            if (fishPrefab == null)
            {
                // Fallback: create basic fish
                return CreateBasicFish(circleType, originalTarget.transform.position);
            }
            
            // Create fish from prefab
            GameObject fish = Instantiate(fishPrefab, originalTarget.transform.position, originalTarget.transform.rotation);
            
            // Add fish behavior
            var fishBehavior = fish.GetComponent<FishTargetBehavior>();
            if (fishBehavior == null)
            {
                fishBehavior = fish.AddComponent<FishTargetBehavior>();
            }
            fishBehavior.Initialize(circleType, fishSize);
            
            // Add underwater physics
            var rigidbody = fish.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = fish.AddComponent<Rigidbody>();
            rigidbody.drag = 2.5f;
            rigidbody.useGravity = false;
            
            // Add bioluminescence
            var bioluminescence = fish.GetComponent<BioluminescenceEffect>();
            if (bioluminescence == null)
            {
                bioluminescence = fish.AddComponent<BioluminescenceEffect>();
            }
            bioluminescence.Initialize(circleType == RhythmTargetSystem.CircleType.White ? Color.white : Color.gray);
            
            // Add to active fish tracking
            activeFish.Add(fish);
            
            // Destroy original target
            if (originalTarget != fish)
            {
                Destroy(originalTarget);
            }
            
            return fish;
        }
        
        // ADDED: CreateSharkBlock method
        public GameObject CreateSharkBlock(GameObject originalBlock, float spinSpeed)
        {
            GameObject shark;
            
            if (sharkBlockPrefab != null)
            {
                shark = Instantiate(sharkBlockPrefab, originalBlock.transform.position, originalBlock.transform.rotation);
            }
            else
            {
                // Fallback: create basic shark block
                shark = CreateBasicSharkBlock(originalBlock.transform.position, spinSpeed);
            }
            
            // Add shark behavior
            var sharkBehavior = shark.GetComponent<SharkBlockBehavior>();
            if (sharkBehavior == null)
            {
                sharkBehavior = shark.AddComponent<SharkBlockBehavior>();
            }
            sharkBehavior.Initialize(spinSpeed);
            
            // Add underwater physics
            var rigidbody = shark.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = shark.AddComponent<Rigidbody>();
            rigidbody.drag = 1.5f;
            rigidbody.useGravity = false;
            
            // Destroy original block
            if (originalBlock != shark)
            {
                Destroy(originalBlock);
            }
            
            return shark;
        }
        
        private GameObject GetFishPrefab(FishTargetBehavior.FishSize fishSize)
        {
            return fishSize switch
            {
                FishTargetBehavior.FishSize.Small => smallFishPrefab,
                FishTargetBehavior.FishSize.Medium => mediumFishPrefab,
                FishTargetBehavior.FishSize.Large => largeFishPrefab,
                _ => smallFishPrefab
            };
        }
        
        private GameObject CreateBasicFish(RhythmTargetSystem.CircleType circleType, Vector3 position)
        {
            GameObject fish = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fish.name = $"Fish_{circleType}_{Random.Range(1000, 9999)}";
            fish.transform.position = position;
            
            // Set scale based on type
            float scale = circleType == RhythmTargetSystem.CircleType.White ? 0.5f : 0.8f;
            fish.transform.localScale = Vector3.one * scale;
            
            // Set color
            var renderer = fish.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material fishMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(GetFishColor(circleType)) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                fishMat.color = GetFishColor(circleType);
                fishMat.SetFloat("_Metallic", 0.2f);
                fishMat.SetFloat("_Smoothness", 0.8f);
                renderer.material = fishMat;
            }
            
            return fish;
        }
        
        private GameObject CreateBasicSharkBlock(Vector3 position, float spinSpeed)
        {
            GameObject shark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shark.name = $"SharkBlock_{Random.Range(1000, 9999)}";
            shark.transform.position = position;
            shark.transform.localScale = Vector3.one * 1.5f;
            
            // Set shark appearance
            var renderer = shark.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material sharkMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(new Color(0.3f, 0.3f, 0.5f)) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                sharkMat.color = new Color(0.3f, 0.3f, 0.5f);
                sharkMat.SetFloat("_Metallic", 0.1f);
                sharkMat.SetFloat("_Smoothness", 0.9f);
                renderer.material = sharkMat;
            }
            
            return shark;
        }
        
        private void CreateFishSchool()
        {
            Vector3 schoolCenter = Random.insideUnitSphere * spawnRadius;
            schoolCenter.y = Mathf.Abs(schoolCenter.y); // Keep above ground
            
            FishSchool school = new FishSchool
            {
                center = schoolCenter,
                fish = new List<GameObject>(),
                schoolType = (FishSchool.SchoolType)Random.Range(0, 3),
                isActive = true
            };
            
            // Create fish in the school
            for (int i = 0; i < fishPerSchool; i++)
            {
                GameObject fish = CreateFish(school.schoolType, schoolCenter);
                school.fish.Add(fish);
                activeFish.Add(fish);
            }
            
            activeSchools.Add(school);
        }
        
        private GameObject CreateFish(FishSchool.SchoolType schoolType, Vector3 centerPosition)
        {
            GameObject fishPrefab = GetFishPrefab(GetFishSize(schoolType));
            GameObject fish;
            
            if (fishPrefab != null)
            {
                fish = Instantiate(fishPrefab, centerPosition, Quaternion.identity);
            }
            else
            {
                // Create basic fish object
                fish = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            }
            
            fish.name = $"Fish_{schoolType}_{Random.Range(1000, 9999)}";
            
            // Position within school
            Vector3 offset = Random.insideUnitSphere * schoolRadius;
            fish.transform.position = centerPosition + offset;
            
            // Scale based on school type
            Vector3 scale = GetFishScale(schoolType);
            fish.transform.localScale = scale;
            
            // Add fish behavior
            var fishBehavior = fish.GetComponent<FishTargetBehavior>();
            if (fishBehavior == null)
            {
                fishBehavior = fish.AddComponent<FishTargetBehavior>();
            }
            fishBehavior.Initialize(RhythmTargetSystem.CircleType.White, GetFishSize(schoolType));
            
            // Add underwater physics
            var rigidbody = fish.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = fish.AddComponent<Rigidbody>();
            rigidbody.drag = 2f;
            rigidbody.useGravity = false;
            
            // Add bioluminescence
            var bioluminescence = fish.GetComponent<BioluminescenceEffect>();
            if (bioluminescence == null)
            {
                bioluminescence = fish.AddComponent<BioluminescenceEffect>();
            }
            Color fishColor = GetFishColor(schoolType);
            bioluminescence.Initialize(fishColor);
            
            // Add school behavior
            var schoolBehavior = fish.GetComponent<SchoolBehavior>();
            if (schoolBehavior == null)
            {
                schoolBehavior = fish.AddComponent<SchoolBehavior>();
            }
            schoolBehavior.Initialize(centerPosition, schoolRadius);
            
            // Add collider for interaction
            SphereCollider collider = fish.GetComponent<SphereCollider>();
            if (collider == null) collider = fish.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            
            return fish;
        }
        
        private Vector3 GetFishScale(FishSchool.SchoolType schoolType)
        {
            switch (schoolType)
            {
                case FishSchool.SchoolType.Small:
                    return Vector3.one * Random.Range(0.3f, 0.6f);
                case FishSchool.SchoolType.Medium:
                    return Vector3.one * Random.Range(0.8f, 1.2f);
                case FishSchool.SchoolType.Large:
                    return Vector3.one * Random.Range(1.5f, 2.5f);
                default:
                    return Vector3.one;
            }
        }
        
        private FishTargetBehavior.FishSize GetFishSize(FishSchool.SchoolType schoolType)
        {
            switch (schoolType)
            {
                case FishSchool.SchoolType.Small:
                    return FishTargetBehavior.FishSize.Small;
                case FishSchool.SchoolType.Medium:
                    return FishTargetBehavior.FishSize.Medium;
                case FishSchool.SchoolType.Large:
                    return FishTargetBehavior.FishSize.Large;
                default:
                    return FishTargetBehavior.FishSize.Small;
            }
        }
        
        private Color GetFishColor(RhythmTargetSystem.CircleType circleType)
        {
            return circleType == RhythmTargetSystem.CircleType.White ? 
                new Color(0.8f, 0.9f, 1f, 0.9f) : new Color(0.5f, 0.6f, 0.8f, 0.9f);
        }
        
        private Color GetFishColor(FishTargetBehavior.FishSize fishSize)
        {
            switch (fishSize)
            {
                case FishTargetBehavior.FishSize.Small:
                    return new Color(0.2f, 0.8f, 1f, 0.8f); // Bright blue
                case FishTargetBehavior.FishSize.Medium:
                    return new Color(1f, 0.6f, 0.2f, 0.8f); // Orange
                case FishTargetBehavior.FishSize.Large:
                    return new Color(0.8f, 0.2f, 0.2f, 0.9f); // Red
                default:
                    return new Color(0.5f, 0.5f, 0.8f, 0.8f);
            }
        }
        
        private void UpdateCurrentSystem()
        {
            currentTimer += Time.deltaTime;
            
            // Create flowing current effect
            float cycle = currentTimer / currentCycleDuration;
            currentVelocity = new Vector3(
                Mathf.Sin(cycle * 2 * Mathf.PI) * currentStrength,
                Mathf.Sin(cycle * 3 * Mathf.PI) * currentStrength * 0.3f,
                Mathf.Cos(cycle * 2 * Mathf.PI) * currentStrength * 0.5f
            );
            
            // Apply current to all fish
            foreach (var fish in activeFish)
            {
                if (fish != null)
                {
                    var rigidbody = fish.GetComponent<Rigidbody>();
                    if (rigidbody != null)
                    {
                        Vector3 currentForce = currentVelocity * (1f + Random.Range(-currentVariation, currentVariation));
                        rigidbody.AddForce(currentForce, ForceMode.Acceleration);
                    }
                }
            }
        }
        
        private void UpdateFishSchools()
        {
            foreach (var school in activeSchools)
            {
                if (!school.isActive) continue;
                
                UpdateSchoolBehavior(school);
            }
        }
        
        private void UpdateSchoolBehavior(FishSchool school)
        {
            Vector3 schoolCenter = CalculateSchoolCenter(school);
            
            foreach (var fish in school.fish)
            {
                if (fish == null) continue;
                
                var schoolBehavior = fish.GetComponent<SchoolBehavior>();
                if (schoolBehavior != null)
                {
                    schoolBehavior.UpdateSchoolPosition(schoolCenter);
                }
            }
        }
        
        private Vector3 CalculateSchoolCenter(FishSchool school)
        {
            Vector3 center = Vector3.zero;
            int validFish = 0;
            
            foreach (var fish in school.fish)
            {
                if (fish != null)
                {
                    center += fish.transform.position;
                    validFish++;
                }
            }
            
            return validFish > 0 ? center / validFish : school.center;
        }
        
        private void UpdateBioluminescence()
        {
            foreach (var fish in activeFish)
            {
                if (fish == null) continue;
                
                var bioluminescence = fish.GetComponent<BioluminescenceEffect>();
                if (bioluminescence != null)
                {
                    // Get distance to player
                    float distanceToPlayer = Vector3.Distance(fish.transform.position, VRBoxingGame.Core.VRCameraHelper.PlayerPosition);
                    
                    // Adjust glow based on proximity
                    float glowIntensity = baseGlowIntensity;
                    if (distanceToPlayer < 5f)
                    {
                        glowIntensity *= approachGlowMultiplier * (1f - distanceToPlayer / 5f);
                    }
                    
                    bioluminescence.SetIntensity(glowIntensity);
                }
            }
        }
        
        public void OnFishHit(RhythmTargetSystem.CircleHitData hitData)
        {
            // Find the fish that was hit
            GameObject hitFish = FindNearestFish(hitData.hitPosition);
            if (hitFish == null) return;
            
            var fishBehavior = hitFish.GetComponent<FishTargetBehavior>();
            if (fishBehavior == null) return;
            
            // Apply size-specific behavior
            switch (fishBehavior.fishSize)
            {
                case FishTargetBehavior.FishSize.Small:
                    HandleSmallFishHit(hitFish, hitData);
                    break;
                    
                case FishTargetBehavior.FishSize.Medium:
                    HandleMediumFishHit(hitFish, hitData);
                    break;
                    
                case FishTargetBehavior.FishSize.Large:
                    HandleLargeFishHit(hitFish, hitData);
                    break;
            }
        }
        
        private void HandleSmallFishHit(GameObject fish, RhythmTargetSystem.CircleHitData hitData)
        {
            // Small fish scatter immediately
            var fishBehavior = fish.GetComponent<FishTargetBehavior>();
            fishBehavior.ScatterFromHit(hitData.hitPosition);
            
            // Reduce bioluminescence
            var bioluminescence = fish.GetComponent<BioluminescenceEffect>();
            if (bioluminescence != null)
            {
                bioluminescence.SetIntensity(baseGlowIntensity * hitGlowReduction);
            }
            
            // Cause nearby small fish to scatter too
            ScatterNearbyFish(fish.transform.position, 3f, FishTargetBehavior.FishSize.Small);
        }
        
        private void HandleMediumFishHit(GameObject fish, RhythmTargetSystem.CircleHitData hitData)
        {
            var fishBehavior = fish.GetComponent<FishTargetBehavior>();
            if (fishBehavior != null)
            {
                fishBehavior.GetStunnedAndRetreat(hitData.hitPosition);
                
                // Schedule regroup after delay
                _ = RegroupMediumFishAsync(fish, 3f);
            }
        }
        
        private void HandleLargeFishHit(GameObject fish, RhythmTargetSystem.CircleHitData hitData)
        {
            var fishBehavior = fish.GetComponent<FishTargetBehavior>();
            if (fishBehavior != null)
            {
                fishBehavior.GetPushedBackAndReturnAggressive(hitData.hitPosition);
                
                // Make fish aggressive for a duration
                _ = MakeFishAggressiveAsync(fish, 5f);
            }
        }
        
        private void ScatterNearbyFish(Vector3 hitPosition, float radius, FishTargetBehavior.FishSize targetSize)
        {
            foreach (var fish in activeFish)
            {
                if (fish == null) continue;
                
                float distance = Vector3.Distance(fish.transform.position, hitPosition);
                if (distance <= radius)
                {
                    var fishBehavior = fish.GetComponent<FishTargetBehavior>();
                    if (fishBehavior != null && fishBehavior.fishSize == targetSize)
                    {
                        fishBehavior.ScatterFromHit(hitPosition);
                    }
                }
            }
        }
        
        private async Task RegroupMediumFishAsync(GameObject fish, float delay)
        {
            try
            {
                await Task.Delay((int)(delay * 1000));
                
                if (fish != null)
                {
                    var fishBehavior = fish.GetComponent<FishTargetBehavior>();
                    if (fishBehavior != null)
                    {
                        fishBehavior.RegroupWithSchool();
                        Debug.Log($"Medium fish {fish.name} regrouped with school");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in medium fish regroup: {ex.Message}");
            }
        }
        
        private async Task MakeFishAggressiveAsync(GameObject fish, float duration)
        {
            try
            {
                if (fish != null)
                {
                    var fishBehavior = fish.GetComponent<FishTargetBehavior>();
                    if (fishBehavior != null)
                    {
                        fishBehavior.SetAggressive(true);
                        Debug.Log($"Large fish {fish.name} became aggressive");
                        
                        await Task.Delay((int)(duration * 1000));
                        
                        if (fish != null && fishBehavior != null)
                        {
                            fishBehavior.SetAggressive(false);
                            Debug.Log($"Large fish {fish.name} calmed down");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in fish aggression effect: {ex.Message}");
            }
        }
        
        private GameObject FindNearestFish(Vector3 position)
        {
            GameObject nearestFish = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var fish in activeFish)
            {
                if (fish == null) continue;
                
                float distance = Vector3.Distance(fish.transform.position, position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestFish = fish;
                }
            }
            
            return nearestFish;
        }
        
        public void OnSharkBlocked(RhythmTargetSystem.BlockData blockData)
        {
            // Create shark block effect
            CreateSharkBlockEffect(blockData.blockPosition);
            
            // Cause all fish to scatter from the area
            ScatterAllFishFrom(blockData.blockPosition, 8f);
        }
        
        private void CreateSharkBlockEffect(Vector3 position)
        {
            // Create water disturbance effect
            GameObject effect = new GameObject("SharkBlockEffect");
            effect.transform.position = position;
            
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 5f;
            main.startSize = 1f;
            main.startColor = new Color(0.3f, 0.6f, 1f, 0.7f);
            main.maxParticles = 100;
            
            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 50)
            });
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2f;
            
            // Auto-destroy after effect
            Destroy(effect, 3f);
        }
        
        private void ScatterAllFishFrom(Vector3 position, float radius)
        {
            foreach (var fish in activeFish)
            {
                if (fish == null) continue;
                
                float distance = Vector3.Distance(fish.transform.position, position);
                if (distance <= radius)
                {
                    var fishBehavior = fish.GetComponent<FishTargetBehavior>();
                    if (fishBehavior != null)
                    {
                        fishBehavior.ScatterFromHit(position);
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            // Complete any running jobs
            if (fishUpdateJobHandle.IsCreated)
            {
                fishUpdateJobHandle.Complete();
            }
            
            // Dispose native arrays
            if (fishPositions.IsCreated) fishPositions.Dispose();
            if (fishVelocities.IsCreated) fishVelocities.Dispose();
            if (fishTimers.IsCreated) fishTimers.Dispose();
            
            // Clean up all fish
            foreach (var fish in activeFish)
            {
                if (fish != null)
                {
                    Destroy(fish);
                }
            }
            
            activeFish.Clear();
            activeSchools.Clear();
        }
        
        // Fish school data structure
        [System.Serializable]
        public class FishSchool
        {
            public Vector3 center;
            public List<GameObject> fish;
            public SchoolType schoolType;
            public bool isActive;
            
            public enum SchoolType
            {
                Small,
                Medium,
                Large
            }
        }
    }
    
    /// <summary>
    /// Unity 6 Job System for high-performance fish simulation
    /// </summary>
    [BurstCompile]
    public struct FishSimulationJob : IJobParallelFor
    {
        public NativeArray<float3> positions;
        public NativeArray<float3> velocities;
        public NativeArray<float> timers;
        
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float3 currentDirection;
        [ReadOnly] public float currentStrength;
        [ReadOnly] public float3 playerPosition;
        
        public void Execute(int index)
        {
            if (index >= positions.Length) return;
            
            // Current fish state
            float3 position = positions[index];
            float3 velocity = velocities[index];
            float timer = timers[index];
            
            // Apply ocean current
            float3 currentForce = currentDirection * currentStrength;
            
            // Apply swimming behavior
            float3 swimmingForce = new float3(
                math.sin(timer * 0.5f) * 0.3f,
                math.sin(timer * 0.7f) * 0.2f,
                math.cos(timer * 0.6f) * 0.3f
            );
            
            // Calculate distance to player for behavior modification
            float distanceToPlayer = math.distance(position, playerPosition);
            float playerInfluence = math.saturate(5f / (distanceToPlayer + 1f));
            
            // Apply forces
            velocity += (currentForce + swimmingForce) * deltaTime;
            
            // Apply drag
            velocity *= 0.98f;
            
            // Update position
            position += velocity * deltaTime;
            
            // Write back results
            positions[index] = position;
            velocities[index] = velocity;
            timers[index] = timer + deltaTime;
        }
    }
    
    /// <summary>
    /// Shark Block Behavior Component
    /// </summary>
    public class SharkBlockBehavior : MonoBehaviour
    {
        [Header("Shark Properties")]
        public float spinSpeed = 2f;
        public float approachSpeed = 3f;
        public float attackRange = 5f;
        
        private Vector3 targetPosition;
        private bool isAttacking = false;
        private float attackTimer = 0f;
        private Rigidbody sharkRigidbody;
        
        public void Initialize(float speed)
        {
            spinSpeed = speed;
            sharkRigidbody = GetComponent<Rigidbody>();
                            targetPosition = VRBoxingGame.Core.VRCameraHelper.PlayerPosition;
            
            // Start attack sequence
            isAttacking = true;
        }
        
        private void Update()
        {
            if (isAttacking)
            {
                UpdateSharkAttack();
            }
            
            // Spin the shark
            transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
        }
        
        private void UpdateSharkAttack()
        {
            attackTimer += Time.deltaTime;
            
            // Move toward player
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (sharkRigidbody != null)
            {
                sharkRigidbody.AddForce(direction * approachSpeed, ForceMode.Acceleration);
            }
            
            // Check if reached player or timeout
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget < attackRange || attackTimer > 10f)
            {
                // Attack complete
                isAttacking = false;
                Destroy(gameObject, 2f);
            }
        }
    }
} 