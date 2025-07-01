using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using System.Collections;
using VRBoxingGame.Audio;
using VRBoxingGame.Boxing;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Advanced Immersive Environment System - Unity 6 optimized environments
    /// Implements cutting-edge VR environmental rendering with Job System optimization
    /// </summary>
    public class AdvancedImmersiveEnvironmentSystem : MonoBehaviour
    {
        [Header("Unity 6 Optimization")]
        public bool enableJobSystemOptimization = true;
        public bool enableGPUInstancing = true;
        public bool enableRenderGraph = true;
        public int particleJobBatchSize = 64;
        
        [Header("Environment Settings")]
        public EnvironmentType currentEnvironment = EnvironmentType.Underwater;
        public float environmentScale = 1f;
        public float effectsIntensity = 1f;
        public bool enableRealtimeLighting = true;
        
        [Header("Underwater Enhancements")]
        public GameObject[] underwaterFishPrefabs;
        public GameObject[] coralPrefabs;
        public GameObject seaweedPrefab;
        public int maxFishCount = 200;
        public int maxCoralCount = 50;
        public float oceanCurrentStrength = 1f;
        
        [Header("Particle Systems")]
        public GameObject bubbleParticlesPrefab;
        public GameObject bioluminescenceParticlesPrefab;
        public GameObject causticsProjectorPrefab;
        
        [Header("Audio Integration")]
        public AudioClip underwaterAmbientSound;
        public AudioClip bubblesSound;
        public AudioClip fishSwimmingSound;
        
        // Singleton
        public static AdvancedImmersiveEnvironmentSystem Instance { get; private set; }
        
        // Job System Data
        private NativeArray<float3> particlePositions;
        private NativeArray<float3> particleVelocities;
        private NativeArray<float> particleLifetimes;
        private JobHandle environmentJobHandle;
        
        // Environment Objects
        private List<GameObject> activeEnvironmentObjects = new List<GameObject>();
        private List<ParticleSystem> activeParticleSystems = new List<ParticleSystem>();
        private Dictionary<string, Material> optimizedMaterials = new Dictionary<string, Material>();
        
        // Performance Tracking
        private int frameCount = 0;
        private float performanceTimer = 0f;
        private float targetFrameRate = 90f;
        
        public enum EnvironmentType
        {
            Default,
            Underwater,
            CrystalCave,
            RainStorm,
            SpaceStation,
            DesertOasis,
            ForestGlade,
            NeonCity
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSystem()
        {
            // Initialize Job System
            if (enableJobSystemOptimization)
            {
                InitializeJobSystem();
            }
            
            // Initialize optimized materials
            InitializeOptimizedMaterials();
            
            // Set up render graph if enabled
            if (enableRenderGraph)
            {
                SetupRenderGraph();
            }
            
            Debug.Log("🌊 Advanced Immersive Environment System initialized with Unity 6 optimizations");
        }
        
        private void InitializeJobSystem()
        {
            int maxParticles = 1000;
            particlePositions = new NativeArray<float3>(maxParticles, Allocator.Persistent);
            particleVelocities = new NativeArray<float3>(maxParticles, Allocator.Persistent);
            particleLifetimes = new NativeArray<float>(maxParticles, Allocator.Persistent);
        }
        
        private void InitializeOptimizedMaterials()
        {
            // Create GPU instanced materials for better performance
            var underwaterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            underwaterMaterial.enableInstancing = true;
            underwaterMaterial.SetFloat("_Surface", 1); // Transparent
            underwaterMaterial.SetColor("_BaseColor", new Color(0.2f, 0.6f, 1f, 0.7f));
            optimizedMaterials["Underwater"] = underwaterMaterial;
            
            var bioluminescenceMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bioluminescenceMaterial.EnableKeyword("_EMISSION");
            bioluminescenceMaterial.SetColor("_EmissionColor", Color.cyan * 2f);
            bioluminescenceMaterial.enableInstancing = true;
            optimizedMaterials["Bioluminescence"] = bioluminescenceMaterial;
            
            var coralMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            coralMaterial.enableInstancing = true;
            coralMaterial.SetColor("_BaseColor", new Color(1f, 0.4f, 0.6f, 1f));
            optimizedMaterials["Coral"] = coralMaterial;
        }
        
        private void SetupRenderGraph()
        {
            // Unity 6 Render Graph integration
            var renderGraphSystem = FindObjectOfType<VRRenderGraphSystem>();
            if (renderGraphSystem != null)
            {
                renderGraphSystem.RegisterEnvironmentRenderer(this);
            }
        }
        
        private void Update()
        {
            if (enableJobSystemOptimization)
            {
                UpdateWithJobSystem();
            }
            else
            {
                UpdateTraditional();
            }
            
            UpdatePerformanceMonitoring();
        }
        
        private void UpdateWithJobSystem()
        {
            // Complete previous frame's job
            environmentJobHandle.Complete();
            
            // Schedule new environment update job
            var environmentJob = new EnvironmentUpdateJob
            {
                positions = particlePositions,
                velocities = particleVelocities,
                lifetimes = particleLifetimes,
                deltaTime = Time.deltaTime,
                oceanCurrentStrength = oceanCurrentStrength,
                playerPosition = GetPlayerPosition()
            };
            
            environmentJobHandle = environmentJob.Schedule(particlePositions.Length, particleJobBatchSize);
            
            // Apply results to visual elements
            StartCoroutine(ApplyJobResults());
        }
        
        private void UpdateTraditional()
        {
            // Traditional update for compatibility
            UpdateOceanCurrent();
            UpdateBioluminescence();
            UpdateAudioReactivity();
        }
        
        private IEnumerator ApplyJobResults()
        {
            yield return new WaitForEndOfFrame();
            environmentJobHandle.Complete();
            
            // Apply particle system updates
            for (int i = 0; i < activeParticleSystems.Count && i < particlePositions.Length; i++)
            {
                if (activeParticleSystems[i] != null)
                {
                    var particles = new ParticleSystem.Particle[1];
                    particles[0].position = particlePositions[i];
                    particles[0].velocity = particleVelocities[i];
                    particles[0].remainingLifetime = particleLifetimes[i];
                    
                    activeParticleSystems[i].SetParticles(particles, 1);
                }
            }
        }
        
        public void CreateUnderwaterEnvironment()
        {
            Debug.Log("🐟 Creating advanced underwater environment with Unity 6 optimizations");
            
            ClearCurrentEnvironment();
            currentEnvironment = EnvironmentType.Underwater;
            
            // Create fish schools with LOD system
            CreateOptimizedFishSchools();
            
            // Create coral reefs with GPU instancing
            CreateInstancedCoralReefs();
            
            // Create advanced particle systems
            CreateUnderwaterParticleSystems();
            
            // Set up caustics lighting
            SetupCausticsLighting();
            
            // Configure underwater audio
            ConfigureUnderwaterAudio();
            
            // Create seaweed with wind zones
            CreateDynamicSeaweed();
            
            Debug.Log("✅ Advanced underwater environment created successfully");
        }
        
        private void CreateOptimizedFishSchools()
        {
            if (underwaterFishPrefabs == null || underwaterFishPrefabs.Length == 0) return;
            
            int fishCount = Mathf.Min(maxFishCount, 200);
            
            for (int i = 0; i < fishCount; i++)
            {
                GameObject fishPrefab = underwaterFishPrefabs[Random.Range(0, underwaterFishPrefabs.Length)];
                if (fishPrefab == null) continue;
                
                Vector3 spawnPosition = Random.insideUnitSphere * 25f;
                spawnPosition.y = Mathf.Abs(spawnPosition.y) + 2f; // Keep above ground
                
                GameObject fish = Instantiate(fishPrefab, spawnPosition, Random.rotation);
                
                // Add LOD component for performance
                var lodGroup = fish.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = fish.AddComponent<LODGroup>();
                    SetupFishLOD(fish, lodGroup);
                }
                
                // Add optimized fish behavior
                var fishBehavior = fish.GetComponent<FishTargetBehavior>();
                if (fishBehavior == null)
                {
                    fishBehavior = fish.AddComponent<FishTargetBehavior>();
                }
                
                // Configure for performance
                var rigidbody = fish.GetComponent<Rigidbody>();
                if (rigidbody == null) rigidbody = fish.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.drag = 2f;
                
                activeEnvironmentObjects.Add(fish);
            }
        }
        
        private void SetupFishLOD(GameObject fish, LODGroup lodGroup)
        {
            var renderers = fish.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            
            LOD[] lods = new LOD[3];
            
            // LOD 0 - High quality (0-15m)
            lods[0] = new LOD(0.6f, renderers);
            
            // LOD 1 - Medium quality (15-30m)
            var mediumRenderers = new Renderer[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                mediumRenderers[i] = renderers[i];
                if (mediumRenderers[i].material != null)
                {
                    mediumRenderers[i].material.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }
            }
            lods[1] = new LOD(0.3f, mediumRenderers);
            
            // LOD 2 - Low quality (30m+)
            var lowRenderers = new Renderer[1];
            lowRenderers[0] = renderers[0]; // Only main renderer
            lods[2] = new LOD(0.1f, lowRenderers);
            
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }
        
        private void CreateInstancedCoralReefs()
        {
            if (coralPrefabs == null || coralPrefabs.Length == 0) return;
            
            // Use GPU instancing for coral reefs
            int coralCount = Mathf.Min(maxCoralCount, 50);
            var coralPositions = new List<Matrix4x4>();
            
            for (int i = 0; i < coralCount; i++)
            {
                Vector3 position = Random.insideUnitSphere * 30f;
                position.y = -2f; // On the ocean floor
                
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                Vector3 scale = Vector3.one * Random.Range(0.5f, 2f);
                
                coralPositions.Add(Matrix4x4.TRS(position, rotation, scale));
            }
            
            // Create instanced coral using Graphics.DrawMeshInstanced
            if (coralPrefabs.Length > 0 && coralPositions.Count > 0)
            {
                StartCoroutine(RenderInstancedCorals(coralPositions));
            }
        }
        
        private IEnumerator RenderInstancedCorals(List<Matrix4x4> coralPositions)
        {
            var coralMesh = coralPrefabs[0].GetComponent<MeshFilter>()?.sharedMesh;
            if (coralMesh == null || !optimizedMaterials.ContainsKey("Coral")) yield break;
            
            var coralMaterial = optimizedMaterials["Coral"];
            
            while (currentEnvironment == EnvironmentType.Underwater)
            {
                // Render corals using GPU instancing
                Graphics.DrawMeshInstanced(
                    coralMesh,
                    0,
                    coralMaterial,
                    coralPositions.ToArray(),
                    coralPositions.Count
                );
                
                yield return null; // Wait for next frame
            }
        }
        
        private void CreateUnderwaterParticleSystems()
        {
            // Create bubble system
            if (bubbleParticlesPrefab != null)
            {
                GameObject bubbles = Instantiate(bubbleParticlesPrefab, Vector3.zero, Quaternion.identity);
                var bubbleSystem = bubbles.GetComponent<ParticleSystem>();
                if (bubbleSystem != null)
                {
                    ConfigureBubbleSystem(bubbleSystem);
                    activeParticleSystems.Add(bubbleSystem);
                }
                activeEnvironmentObjects.Add(bubbles);
            }
            
            // Create bioluminescence system
            if (bioluminescenceParticlesPrefab != null)
            {
                GameObject bioluminescence = Instantiate(bioluminescenceParticlesPrefab, Vector3.zero, Quaternion.identity);
                var bioSystem = bioluminescence.GetComponent<ParticleSystem>();
                if (bioSystem != null)
                {
                    ConfigureBioluminescenceSystem(bioSystem);
                    activeParticleSystems.Add(bioSystem);
                }
                activeEnvironmentObjects.Add(bioluminescence);
            }
        }
        
        private void ConfigureBubbleSystem(ParticleSystem bubbleSystem)
        {
            var main = bubbleSystem.main;
            main.startLifetime = 5f;
            main.startSpeed = 1f;
            main.startSize = 0.1f;
            main.startColor = new Color(0.8f, 0.9f, 1f, 0.6f);
            main.maxParticles = 100;
            
            var emission = bubbleSystem.emission;
            emission.rateOverTime = 20f;
            
            var shape = bubbleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(50f, 1f, 50f);
            
            var velocityOverLifetime = bubbleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = 2f; // Bubbles rise
        }
        
        private void ConfigureBioluminescenceSystem(ParticleSystem bioSystem)
        {
            var main = bioSystem.main;
            main.startLifetime = 3f;
            main.startSpeed = 0.5f;
            main.startSize = 0.05f;
            main.startColor = Color.cyan;
            main.maxParticles = 500;
            
            var emission = bioSystem.emission;
            emission.rateOverTime = 50f;
            
            var shape = bioSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 25f;
            
            // Add music reactivity
            var audioManager = AdvancedAudioManager.Instance;
            if (audioManager != null)
            {
                StartCoroutine(ReactToMusic(bioSystem));
            }
        }
        
        private void SetupCausticsLighting()
        {
            if (causticsProjectorPrefab == null) return;
            
            GameObject caustics = Instantiate(causticsProjectorPrefab, new Vector3(0, 10f, 0), Quaternion.Euler(90, 0, 0));
            var projector = caustics.GetComponent<Light>();
            if (projector != null)
            {
                projector.type = LightType.Directional;
                projector.color = new Color(0.7f, 0.9f, 1f, 1f);
                projector.intensity = 0.5f;
                
                // Add caustics animation
                var causticsAnimator = caustics.AddComponent<CausticsAnimator>();
                causticsAnimator.Initialize();
            }
            
            activeEnvironmentObjects.Add(caustics);
        }
        
        private void ConfigureUnderwaterAudio()
        {
            var audioManager = AdvancedAudioManager.Instance;
            if (audioManager == null) return;
            
            // Set underwater audio mode
            audioManager.SetUnderwaterMode(true);
            
            // Play ambient underwater sounds
            if (underwaterAmbientSound != null)
            {
                audioManager.PlayEnvironmentalSound(underwaterAmbientSound, 0.3f, true);
            }
        }
        
        private void CreateDynamicSeaweed()
        {
            if (seaweedPrefab == null) return;
            
            for (int i = 0; i < 30; i++)
            {
                Vector3 position = Random.insideUnitSphere * 20f;
                position.y = -2f; // On ocean floor
                
                GameObject seaweed = Instantiate(seaweedPrefab, position, Random.rotation);
                
                // Add cloth component for realistic movement
                var cloth = seaweed.GetComponent<Cloth>();
                if (cloth == null)
                {
                    cloth = seaweed.AddComponent<Cloth>();
                }
                
                // Configure for underwater movement
                cloth.damping = 0.8f;
                cloth.stretchingStiffness = 0.5f;
                cloth.bendingStiffness = 0.3f;
                
                // Add wind zone for current effect
                var windZone = seaweed.AddComponent<WindZone>();
                windZone.mode = WindZoneMode.Spherical;
                windZone.radius = 2f;
                windZone.windMain = oceanCurrentStrength;
                windZone.windTurbulence = 0.1f;
                
                activeEnvironmentObjects.Add(seaweed);
            }
        }
        
        private IEnumerator ReactToMusic(ParticleSystem particleSystem)
        {
            var audioManager = AdvancedAudioManager.Instance;
            if (audioManager == null) yield break;
            
            while (currentEnvironment == EnvironmentType.Underwater)
            {
                float audioEnergy = audioManager.GetCurrentAudioEnergy();
                float beatIntensity = audioManager.GetBeatIntensity();
                
                var emission = particleSystem.emission;
                emission.rateOverTime = 50f + (audioEnergy * 100f);
                
                var main = particleSystem.main;
                main.startSize = 0.05f + (beatIntensity * 0.1f);
                
                yield return null;
            }
        }
        
        private void UpdateOceanCurrent()
        {
            // Simulate ocean current affecting all underwater objects
            Vector3 currentDirection = new Vector3(
                Mathf.Sin(Time.time * 0.3f),
                0,
                Mathf.Cos(Time.time * 0.2f)
            ) * oceanCurrentStrength;
            
            foreach (var obj in activeEnvironmentObjects)
            {
                if (obj == null) continue;
                
                var rigidbody = obj.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.AddForce(currentDirection, ForceMode.Acceleration);
                }
            }
        }
        
        private void UpdateBioluminescence()
        {
            float playerDistance = Vector3.Distance(transform.position, GetPlayerPosition());
            float glowIntensity = Mathf.Lerp(0.5f, 2f, 1f - (playerDistance / 10f));
            
            foreach (var obj in activeEnvironmentObjects)
            {
                if (obj == null) continue;
                
                var bioluminescence = obj.GetComponent<BioluminescenceEffect>();
                if (bioluminescence != null)
                {
                    bioluminescence.SetIntensity(glowIntensity);
                }
            }
        }
        
        private void UpdateAudioReactivity()
        {
            var audioManager = AdvancedAudioManager.Instance;
            if (audioManager == null) return;
            
            float audioEnergy = audioManager.GetCurrentAudioEnergy();
            float beatIntensity = audioManager.GetBeatIntensity();
            
            // Apply audio reactivity to particle systems
            foreach (var particleSystem in activeParticleSystems)
            {
                if (particleSystem == null) continue;
                
                var emission = particleSystem.emission;
                emission.rateOverTime = emission.rateOverTime.constant + (audioEnergy * 20f);
            }
        }
        
        private void UpdatePerformanceMonitoring()
        {
            frameCount++;
            performanceTimer += Time.deltaTime;
            
            if (performanceTimer >= 1f)
            {
                float currentFPS = frameCount / performanceTimer;
                
                // Adjust quality based on performance
                if (currentFPS < targetFrameRate * 0.8f)
                {
                    AdjustQualityForPerformance();
                }
                
                frameCount = 0;
                performanceTimer = 0f;
            }
        }
        
        private void AdjustQualityForPerformance()
        {
            // Reduce particle count
            foreach (var particleSystem in activeParticleSystems)
            {
                if (particleSystem != null)
                {
                    var main = particleSystem.main;
                    main.maxParticles = Mathf.Max(main.maxParticles - 20, 50);
                }
            }
            
            Debug.Log("🔧 Adjusted environment quality for better performance");
        }
        
        public void ClearCurrentEnvironment()
        {
            // Complete any running jobs
            if (environmentJobHandle.IsCreated)
            {
                environmentJobHandle.Complete();
            }
            
            // Clean up all environment objects
            foreach (var obj in activeEnvironmentObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            activeEnvironmentObjects.Clear();
            activeParticleSystems.Clear();
            
            // Reset audio
            var audioManager = AdvancedAudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.SetUnderwaterMode(false);
                audioManager.StopEnvironmentalSounds();
            }
        }
        
        private float3 GetPlayerPosition()
        {
            if (Camera.main != null)
            {
                return Camera.main.transform.position;
            }
            return float3.zero;
        }
        
        private void OnDestroy()
        {
            // Complete any running jobs
            if (environmentJobHandle.IsCreated)
            {
                environmentJobHandle.Complete();
            }
            
            // Dispose native arrays
            if (particlePositions.IsCreated) particlePositions.Dispose();
            if (particleVelocities.IsCreated) particleVelocities.Dispose();
            if (particleLifetimes.IsCreated) particleLifetimes.Dispose();
            
            ClearCurrentEnvironment();
        }
    }
    
    /// <summary>
    /// Unity 6 Job for environment particle updates
    /// </summary>
    [BurstCompile]
    public struct EnvironmentUpdateJob : IJobParallelFor
    {
        public NativeArray<float3> positions;
        public NativeArray<float3> velocities;
        public NativeArray<float> lifetimes;
        
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float oceanCurrentStrength;
        [ReadOnly] public float3 playerPosition;
        
        public void Execute(int index)
        {
            if (index >= positions.Length) return;
            
            float3 position = positions[index];
            float3 velocity = velocities[index];
            float lifetime = lifetimes[index];
            
            // Apply ocean current
            float3 current = new float3(
                math.sin(lifetime * 0.3f) * oceanCurrentStrength,
                0,
                math.cos(lifetime * 0.2f) * oceanCurrentStrength
            );
            
            // Update velocity
            velocity += current * deltaTime;
            velocity *= 0.99f; // Drag
            
            // Update position
            position += velocity * deltaTime;
            
            // Update lifetime
            lifetime -= deltaTime;
            if (lifetime <= 0)
            {
                // Reset particle
                position = playerPosition + (math.normalize(position - playerPosition) * 25f);
                lifetime = 5f;
            }
            
            // Write back
            positions[index] = position;
            velocities[index] = velocity;
            lifetimes[index] = lifetime;
        }
    }
    
    /// <summary>
    /// Caustics lighting animator for realistic underwater lighting
    /// </summary>
    public class CausticsAnimator : MonoBehaviour
    {
        private Light causticsLight;
        private float animationSpeed = 0.5f;
        private float intensityVariation = 0.3f;
        private float baseIntensity;
        
        public void Initialize()
        {
            causticsLight = GetComponent<Light>();
            if (causticsLight != null)
            {
                baseIntensity = causticsLight.intensity;
            }
        }
        
        private void Update()
        {
            if (causticsLight == null) return;
            
            // Animate caustics intensity
            float variation = Mathf.Sin(Time.time * animationSpeed) * intensityVariation;
            causticsLight.intensity = baseIntensity + variation;
            
            // Slightly rotate for moving caustics effect
            transform.Rotate(0, 0, Time.deltaTime * 5f);
        }
    }
} 