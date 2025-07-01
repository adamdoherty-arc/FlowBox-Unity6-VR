using UnityEngine;
using System.Collections.Generic;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using System.Collections;
using Unity.Mathematics;
using System.Threading.Tasks;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Underwater Fish System - Implements detailed fish AI behaviors
    /// Small fish scatter, medium fish retreat and regroup, large fish become aggressive
    /// </summary>
    public class UnderwaterFishSystem : MonoBehaviour
    {
        [Header("Fish Spawning")]
        public int maxFishSchools = 5;
        public int fishPerSchool = 8;
        public float schoolRadius = 3f;
        public float spawnRadius = 15f;
        
        [Header("Current Effects")]
        public Vector3 currentDirection = Vector3.right;
        public float currentStrength = 1f;
        public float currentVariation = 0.5f;
        
        [Header("Bioluminescence")]
        public float baseGlowIntensity = 0.5f;
        public float approachGlowMultiplier = 2f;
        public float hitGlowReduction = 0.3f;
        
        // Active fish tracking
        private List<FishSchool> activeSchools = new List<FishSchool>();
        private List<GameObject> activeFish = new List<GameObject>();
        
        // Current system
        private Vector3 currentVelocity;
        private float currentTimer = 0f;
        private float currentCycleDuration = 10f;
        
        private void Start()
        {
            InitializeUnderwaterEnvironment();
        }
        
        private void Update()
        {
            UpdateCurrentSystem();
            UpdateFishSchools();
            UpdateBioluminescence();
        }
        
        private void InitializeUnderwaterEnvironment()
        {
            // Create initial fish schools
            for (int i = 0; i < maxFishSchools; i++)
            {
                CreateFishSchool();
            }
            
            Debug.Log($"Underwater Fish System initialized with {maxFishSchools} schools");
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
            // Create basic fish object
            GameObject fish = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fish.name = $"Fish_{schoolType}_{Random.Range(1000, 9999)}";
            
            // Position within school
            Vector3 offset = Random.insideUnitSphere * schoolRadius;
            fish.transform.position = centerPosition + offset;
            
            // Scale based on school type
            Vector3 scale = GetFishScale(schoolType);
            fish.transform.localScale = scale;
            
            // Add fish behavior
            var fishBehavior = fish.AddComponent<FishTargetBehavior>();
            fishBehavior.Initialize(RhythmTargetSystem.CircleType.White, GetFishSize(schoolType));
            
            // Add underwater physics
            var rigidbody = fish.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = fish.AddComponent<Rigidbody>();
            rigidbody.drag = 2f;
            rigidbody.useGravity = false;
            
            // Add bioluminescence
            var bioluminescence = fish.AddComponent<BioluminescenceEffect>();
            Color fishColor = GetFishColor(schoolType);
            bioluminescence.Initialize(fishColor);
            
            // Add school behavior
            var schoolBehavior = fish.AddComponent<SchoolBehavior>();
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
        
        private Color GetFishColor(FishSchool.SchoolType schoolType)
        {
            switch (schoolType)
            {
                case FishSchool.SchoolType.Small:
                    return new Color(0.2f, 0.8f, 1f, 0.8f); // Bright blue
                case FishSchool.SchoolType.Medium:
                    return new Color(1f, 0.6f, 0.2f, 0.8f); // Orange
                case FishSchool.SchoolType.Large:
                    return new Color(0.8f, 0.2f, 0.2f, 0.9f); // Red
                default:
                    return Color.cyan;
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
                    float distanceToPlayer = Vector3.Distance(fish.transform.position, Camera.main.transform.position);
                    
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
} 