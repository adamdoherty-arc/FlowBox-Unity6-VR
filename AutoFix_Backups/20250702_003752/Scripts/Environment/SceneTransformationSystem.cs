using UnityEngine;
using System.Collections.Generic;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using VRBoxingGame.Core;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Comprehensive Scene Transformation System
    /// Transforms core game elements based on scene themes as described in scene descriptions
    /// </summary>
    public class SceneTransformationSystem : MonoBehaviour
    {
        [Header("Scene Configuration")]
        public SceneType currentSceneType = SceneType.DefaultArena;
        public bool enableTransformations = true;
        
        [Header("Target Transformation")]
        public GameObject[] fishPrefabs; // For underwater scene
        public GameObject[] crystalPrefabs; // For crystal cave
        public GameObject[] spiritPrefabs; // For forest glade
        public GameObject[] sandSpiritPrefabs; // For desert oasis
        
        [Header("Block Transformation")]
        public GameObject sharkBlockPrefab; // Underwater
        public GameObject crystalClusterPrefab; // Crystal cave
        public GameObject thornVinePrefab; // Forest glade
        public GameObject sandstormPrefab; // Desert oasis
        
        [Header("Physics Modifiers")]
        public float spaceGravity = 0.3f;
        public float crystalHarmonic = 1.5f;
        public float underwaterDrag = 2.5f;
        
        [Header("Traditional Mode Override")]
        public bool useTraditionalTargets = false;
        public Material traditionalWhiteMaterial;
        public Material traditionalGrayMaterial;
        public Material traditionalBlockMaterial;
        
        public enum SceneType
        {
            DefaultArena = 0,
            RainStorm = 1,
            NeonCity = 2,
            SpaceStation = 3,
            CrystalCave = 4,
            UnderwaterWorld = 5,
            DesertOasis = 6,
            ForestGlade = 7
        }
        
        // Singleton
        public static SceneTransformationSystem Instance { get; private set; }
        
        // Active transformations
        private Dictionary<GameObject, SceneTransformedObject> transformedObjects = new Dictionary<GameObject, SceneTransformedObject>();
        private List<GameObject> activeSceneObjects = new List<GameObject>();
        
        // Scene-specific systems
        private UnderwaterFishSystem fishSystem;
        private CrystalResonanceSystem crystalSystem;
        private ForestSpiritSystem spiritSystem;
        private DesertMirageSystem mirageSystem;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeTransformationSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeTransformationSystem()
        {
            // Initialize scene-specific systems
            fishSystem = gameObject.AddComponent<UnderwaterFishSystem>();
            crystalSystem = gameObject.AddComponent<CrystalResonanceSystem>();
            spiritSystem = gameObject.AddComponent<ForestSpiritSystem>();
            mirageSystem = gameObject.AddComponent<DesertMirageSystem>();
            
            // Subscribe to rhythm system events
            if (RhythmTargetSystem.Instance != null)
            {
                RhythmTargetSystem.Instance.OnCircleHit.AddListener(OnTargetHit);
                RhythmTargetSystem.Instance.OnBlockSuccess.AddListener(OnBlockSuccess);
            }
            
            Debug.Log("Scene Transformation System initialized");
        }
        
        public void SetSceneType(SceneType newSceneType)
        {
            if (currentSceneType == newSceneType) return;
            
            // Clean up current scene
            CleanupCurrentScene();
            
            // Set new scene
            currentSceneType = newSceneType;
            
            // Apply transformations
            ApplySceneTransformations();
            
            Debug.Log($"Scene transformed to: {newSceneType}");
        }
        
        public void SetUseTraditionalTargets(bool useTraditional)
        {
            useTraditionalTargets = useTraditional;
            
            // Handle immersive environment toggling
            var advancedEnvironment = AdvancedImmersiveEnvironmentSystem.Instance;
            if (advancedEnvironment != null)
            {
                if (useTraditional)
                {
                    // Switch to traditional mode - clear immersive environment
                    advancedEnvironment.ClearCurrentEnvironment();
                    Debug.Log("🎯 Switched to traditional targets - immersive environment disabled");
                }
                else
                {
                    // Switch to immersive mode - enable environment for current scene
                    switch (currentSceneType)
                    {
                        case SceneType.UnderwaterWorld:
                            advancedEnvironment.CreateUnderwaterEnvironment();
                            Debug.Log("🐟 Immersive underwater environment enabled");
                            break;
                            
                        case SceneType.CrystalCave:
                            // Could add crystal cave environment
                            Debug.Log("💎 Crystal cave immersive mode enabled");
                            break;
                            
                        // Add other immersive environments as needed
                    }
                }
            }
            
            // Refresh current scene transformation
            if (RhythmTargetSystem.Instance != null)
            {
                // Trigger scene refresh
                SetSceneType(currentSceneType);
            }
        }
        
        public GameObject TransformTarget(GameObject originalTarget, RhythmTargetSystem.CircleType circleType)
        {
            // If traditional mode is enabled, return original target with traditional materials
            if (useTraditionalTargets)
            {
                return ApplyTraditionalTargetStyle(originalTarget, circleType);
            }
            
            // Otherwise use scene-specific transformation
            return ApplySceneTransformation(originalTarget, circleType);
        }
        
        private GameObject ApplyTraditionalTargetStyle(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Apply traditional materials
                switch (circleType)
                {
                    case RhythmTargetSystem.CircleType.White:
                        if (traditionalWhiteMaterial != null)
                            renderer.material = traditionalWhiteMaterial;
                        break;
                    case RhythmTargetSystem.CircleType.Gray:
                        if (traditionalGrayMaterial != null)
                            renderer.material = traditionalGrayMaterial;
                        break;
                }
                
                // Ensure traditional circular shape
                target.name = circleType == RhythmTargetSystem.CircleType.White ? "WhiteCircle_Traditional" : "GrayCircle_Traditional";
                
                // Remove any scene-specific components that might have been added
                RemoveSceneSpecificComponents(target);
            }
            
            return target;
        }
        
        private GameObject ApplySceneTransformation(GameObject originalTarget, RhythmTargetSystem.CircleType circleType)
        {
            switch (currentSceneType)
            {
                case SceneType.DefaultArena:
                    return TransformToArenaTarget(originalTarget, circleType);
                case SceneType.RainStorm:
                    return TransformToRainTarget(originalTarget, circleType);
                case SceneType.NeonCity:
                    return TransformToNeonTarget(originalTarget, circleType);
                case SceneType.SpaceStation:
                    return TransformToSpaceTarget(originalTarget, circleType);
                case SceneType.CrystalCave:
                    return TransformToCrystalTarget(originalTarget, circleType);
                case SceneType.UnderwaterWorld:
                    return TransformToFishTarget(originalTarget, circleType);
                case SceneType.DesertOasis:
                    return TransformToDesertTarget(originalTarget, circleType);
                case SceneType.ForestGlade:
                    return TransformToForestTarget(originalTarget, circleType);
                default:
                    return originalTarget;
            }
        }
        
        public GameObject TransformBlock(GameObject originalBlock, float spinSpeed)
        {
            // If traditional mode is enabled, return original block with traditional material
            if (useTraditionalTargets)
            {
                return ApplyTraditionalBlockStyle(originalBlock);
            }
            
            // Otherwise use scene-specific transformation
            return ApplySceneBlockTransformation(originalBlock, spinSpeed);
        }
        
        private GameObject ApplyTraditionalBlockStyle(GameObject block)
        {
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null && traditionalBlockMaterial != null)
            {
                renderer.material = traditionalBlockMaterial;
            }
            
            block.name = "CombinedBlock_Traditional";
            
            // Remove any scene-specific components
            RemoveSceneSpecificComponents(block);
            
            return block;
        }
        
        private GameObject ApplySceneBlockTransformation(GameObject originalBlock, float spinSpeed)
        {
            switch (currentSceneType)
            {
                case SceneType.DefaultArena:
                    return TransformToArenaBlock(originalBlock, spinSpeed);
                case SceneType.RainStorm:
                    return TransformToRainBlock(originalBlock, spinSpeed);
                case SceneType.NeonCity:
                    return TransformToNeonBlock(originalBlock, spinSpeed);
                case SceneType.SpaceStation:
                    return TransformToSpaceBlock(originalBlock, spinSpeed);
                case SceneType.CrystalCave:
                    return TransformToCrystalBlock(originalBlock, spinSpeed);
                case SceneType.UnderwaterWorld:
                    return TransformToUnderwaterBlock(originalBlock, spinSpeed);
                case SceneType.DesertOasis:
                    return TransformToDesertBlock(originalBlock, spinSpeed);
                case SceneType.ForestGlade:
                    return TransformToForestBlock(originalBlock, spinSpeed);
                default:
                    return originalBlock;
            }
        }
        
        private void RemoveSceneSpecificComponents(GameObject target)
        {
            // Remove fish behavior components
            var fishBehavior = target.GetComponent<FishTargetBehavior>();
            if (fishBehavior != null) DestroyImmediate(fishBehavior);
            
            var schoolBehavior = target.GetComponent<SchoolBehavior>();
            if (schoolBehavior != null) DestroyImmediate(schoolBehavior);
            
            var bioluminescence = target.GetComponent<BioluminescenceEffect>();
            if (bioluminescence != null) DestroyImmediate(bioluminescence);
            
            // Remove crystal components
            var harmonicOscillator = target.GetComponent<HarmonicOscillator>();
            if (harmonicOscillator != null) DestroyImmediate(harmonicOscillator);
            
            // Remove other scene-specific components
            var mirageEffect = target.GetComponent<MirageEffect>();
            if (mirageEffect != null) DestroyImmediate(mirageEffect);
            
            // Reset any modified physics properties to defaults
            var rigidbody = target.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.drag = 0f;
                rigidbody.angularDrag = 0.05f;
                rigidbody.useGravity = false;
            }
        }
        
        // Underwater World Implementations
        private GameObject CreateFishTarget(RhythmTargetSystem.CircleType circleType, GameObject originalTarget)
        {
            GameObject fishPrefab = GetRandomFishPrefab(circleType);
            if (fishPrefab == null) return originalTarget;
            
            GameObject fish = Instantiate(fishPrefab, originalTarget.transform.position, originalTarget.transform.rotation);
            
            // Add fish behavior
            var fishBehavior = fish.AddComponent<FishTargetBehavior>();
            fishBehavior.Initialize(circleType, GetFishSize(circleType));
            
            // Add underwater physics
            var rigidbody = fish.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = fish.AddComponent<Rigidbody>();
            rigidbody.drag = underwaterDrag;
            rigidbody.useGravity = false;
            
            // Add bioluminescence
            var bioluminescence = fish.AddComponent<BioluminescenceEffect>();
            bioluminescence.Initialize(circleType == RhythmTargetSystem.CircleType.White ? Color.white : Color.gray);
            
            return fish;
        }
        
        private GameObject CreateSharkBlock(GameObject originalBlock, float spinSpeed)
        {
            if (sharkBlockPrefab == null) return originalBlock;
            
            GameObject shark = Instantiate(sharkBlockPrefab, originalBlock.transform.position, originalBlock.transform.rotation);
            
            // Add shark behavior
            var sharkBehavior = shark.AddComponent<SharkBlockBehavior>();
            sharkBehavior.Initialize(spinSpeed);
            
            return shark;
        }
        
        // Crystal Cave Implementations
        private GameObject CreateCrystalTarget(RhythmTargetSystem.CircleType circleType, GameObject originalTarget)
        {
            GameObject crystalPrefab = GetRandomCrystalPrefab(circleType);
            if (crystalPrefab == null) return originalTarget;
            
            GameObject crystal = Instantiate(crystalPrefab, originalTarget.transform.position, originalTarget.transform.rotation);
            
            // Add crystal behavior
            var crystalBehavior = crystal.AddComponent<CrystalTargetBehavior>();
            crystalBehavior.Initialize(circleType);
            
            // Add harmonic oscillation
            var oscillator = crystal.AddComponent<HarmonicOscillator>();
            oscillator.frequency = GetCrystalFrequency(circleType);
            
            return crystal;
        }
        
        private GameObject CreateCrystalClusterBlock(GameObject originalBlock, float spinSpeed)
        {
            if (crystalClusterPrefab == null) return originalBlock;
            
            GameObject cluster = Instantiate(crystalClusterPrefab, originalBlock.transform.position, originalBlock.transform.rotation);
            
            // Add cluster behavior
            var clusterBehavior = cluster.AddComponent<CrystalClusterBehavior>();
            clusterBehavior.Initialize(spinSpeed);
            
            return cluster;
        }
        
        // Forest Glade Implementations
        private GameObject CreateSpiritTarget(RhythmTargetSystem.CircleType circleType, GameObject originalTarget)
        {
            GameObject spiritPrefab = GetRandomSpiritPrefab(circleType);
            if (spiritPrefab == null) return originalTarget;
            
            GameObject spirit = Instantiate(spiritPrefab, originalTarget.transform.position, originalTarget.transform.rotation);
            
            // Add spirit behavior
            var spiritBehavior = spirit.AddComponent<SpiritTargetBehavior>();
            spiritBehavior.Initialize(circleType);
            
            // Add magical effects
            var magicalAura = spirit.AddComponent<MagicalAuraEffect>();
            magicalAura.Initialize(circleType);
            
            return spirit;
        }
        
        private GameObject CreateThornVineBlock(GameObject originalBlock, float spinSpeed)
        {
            if (thornVinePrefab == null) return originalBlock;
            
            GameObject vine = Instantiate(thornVinePrefab, originalBlock.transform.position, originalBlock.transform.rotation);
            
            // Add vine behavior
            var vineBehavior = vine.AddComponent<ThornVineBehavior>();
            vineBehavior.Initialize(spinSpeed);
            
            return vine;
        }
        
        // Desert Oasis Implementations
        private GameObject CreateSandSpiritTarget(RhythmTargetSystem.CircleType circleType, GameObject originalTarget)
        {
            GameObject sandSpiritPrefab = GetRandomSandSpiritPrefab(circleType);
            if (sandSpiritPrefab == null) return originalTarget;
            
            GameObject sandSpirit = Instantiate(sandSpiritPrefab, originalTarget.transform.position, originalTarget.transform.rotation);
            
            // Add sand spirit behavior
            var spiritBehavior = sandSpirit.AddComponent<SandSpiritBehavior>();
            spiritBehavior.Initialize(circleType);
            
            // Add mirage effect
            var mirageEffect = sandSpirit.AddComponent<MirageEffect>();
            mirageEffect.Initialize(0.3f); // 30% chance of being a mirage
            
            return sandSpirit;
        }
        
        private GameObject CreateSandstormBlock(GameObject originalBlock, float spinSpeed)
        {
            if (sandstormPrefab == null) return originalBlock;
            
            GameObject sandstorm = Instantiate(sandstormPrefab, originalBlock.transform.position, originalBlock.transform.rotation);
            
            // Add sandstorm behavior
            var sandstormBehavior = sandstorm.AddComponent<SandstormBehavior>();
            sandstormBehavior.Initialize(spinSpeed);
            
            return sandstorm;
        }
        
        // Space Station Implementations
        private GameObject CreateEnergyTarget(RhythmTargetSystem.CircleType circleType, GameObject originalTarget)
        {
            GameObject energyTarget = Instantiate(originalTarget, originalTarget.transform.position, originalTarget.transform.rotation);
            
            // Add energy effects
            var energyEffect = energyTarget.AddComponent<EnergyOrbEffect>();
            energyEffect.Initialize(circleType);
            
            // Modify physics for zero gravity
            var rigidbody = energyTarget.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = energyTarget.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            
            return energyTarget;
        }
        
        // Neon City Implementations
        private GameObject CreateHologramTarget(RhythmTargetSystem.CircleType circleType, GameObject originalTarget)
        {
            GameObject hologram = Instantiate(originalTarget, originalTarget.transform.position, originalTarget.transform.rotation);
            
            // Add hologram effects
            var hologramEffect = hologram.AddComponent<HologramEffect>();
            hologramEffect.Initialize(circleType);
            
            return hologram;
        }
        
        // Rain Storm Implementations
        private GameObject CreateRainTarget(RhythmTargetSystem.CircleType circleType, GameObject originalTarget)
        {
            GameObject rainTarget = Instantiate(originalTarget, originalTarget.transform.position, originalTarget.transform.rotation);
            
            // Add rain effects
            var rainEffect = rainTarget.AddComponent<RainDropletEffect>();
            rainEffect.Initialize(circleType);
            
            return rainTarget;
        }
        
        // Helper methods
        private GameObject GetRandomFishPrefab(RhythmTargetSystem.CircleType circleType)
        {
            if (fishPrefabs == null || fishPrefabs.Length == 0) return null;
            return fishPrefabs[Random.Range(0, fishPrefabs.Length)];
        }
        
        private FishTargetBehavior.FishSize GetFishSize(RhythmTargetSystem.CircleType circleType)
        {
            // Vary fish sizes for different mechanics
            float random = Random.value;
            if (random < 0.7f) return FishTargetBehavior.FishSize.Small;
            if (random < 0.9f) return FishTargetBehavior.FishSize.Medium;
            return FishTargetBehavior.FishSize.Large;
        }
        
        private GameObject GetRandomCrystalPrefab(RhythmTargetSystem.CircleType circleType)
        {
            if (crystalPrefabs == null || crystalPrefabs.Length == 0) return null;
            return crystalPrefabs[Random.Range(0, crystalPrefabs.Length)];
        }
        
        private float GetCrystalFrequency(RhythmTargetSystem.CircleType circleType)
        {
            // Different frequencies for different crystal types
            return circleType == RhythmTargetSystem.CircleType.White ? 440f : 330f; // A4 vs E4
        }
        
        private GameObject GetRandomSpiritPrefab(RhythmTargetSystem.CircleType circleType)
        {
            if (spiritPrefabs == null || spiritPrefabs.Length == 0) return null;
            return spiritPrefabs[Random.Range(0, spiritPrefabs.Length)];
        }
        
        private GameObject GetRandomSandSpiritPrefab(RhythmTargetSystem.CircleType circleType)
        {
            if (sandSpiritPrefabs == null || sandSpiritPrefabs.Length == 0) return null;
            return sandSpiritPrefabs[Random.Range(0, sandSpiritPrefabs.Length)];
        }
        
        private void ApplySceneTransformations()
        {
            // Apply scene-specific physics
            ApplyScenePhysics();
            
            // Enable appropriate scene system
            EnableSceneSystem();
        }
        
        private void ApplyScenePhysics()
        {
            switch (currentSceneType)
            {
                case SceneType.UnderwaterWorld:
                    Physics.gravity = new Vector3(0, -9.81f * 0.3f, 0); // Reduced gravity underwater
                    break;
                    
                case SceneType.SpaceStation:
                    Physics.gravity = new Vector3(0, -9.81f * spaceGravity, 0);
                    break;
                    
                default:
                    Physics.gravity = new Vector3(0, -9.81f, 0); // Normal gravity
                    break;
            }
        }
        
        private void EnableSceneSystem()
        {
            // Disable all systems first
            fishSystem.enabled = false;
            crystalSystem.enabled = false;
            spiritSystem.enabled = false;
            mirageSystem.enabled = false;
            
            // Enable appropriate system
            switch (currentSceneType)
            {
                case SceneType.UnderwaterWorld:
                    fishSystem.enabled = true;
                    
                    // Enable advanced immersive environment if not using traditional targets
                    if (!useTraditionalTargets)
                    {
                        var advancedEnvironment = AdvancedImmersiveEnvironmentSystem.Instance;
                        if (advancedEnvironment != null)
                        {
                            advancedEnvironment.CreateUnderwaterEnvironment();
                            Debug.Log("🌊 Advanced underwater environment activated");
                        }
                    }
                    break;
                    
                case SceneType.CrystalCave:
                    crystalSystem.enabled = true;
                    break;
                    
                case SceneType.ForestGlade:
                    spiritSystem.enabled = true;
                    break;
                    
                case SceneType.DesertOasis:
                    mirageSystem.enabled = true;
                    break;
            }
        }
        
        private void CleanupCurrentScene()
        {
            // Clean up all transformed objects
            foreach (var obj in activeSceneObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            activeSceneObjects.Clear();
            transformedObjects.Clear();
        }
        
        private void OnTargetHit(RhythmTargetSystem.CircleHitData hitData)
        {
            // Handle scene-specific hit effects
            switch (currentSceneType)
            {
                case SceneType.UnderwaterWorld:
                    fishSystem.OnFishHit(hitData);
                    break;
                    
                case SceneType.CrystalCave:
                    crystalSystem.OnCrystalHit(hitData);
                    break;
                    
                case SceneType.ForestGlade:
                    spiritSystem.OnSpiritHit(hitData);
                    break;
                    
                case SceneType.DesertOasis:
                    mirageSystem.OnSpiritHit(hitData);
                    break;
            }
        }
        
        private void OnBlockSuccess(RhythmTargetSystem.BlockData blockData)
        {
            // Handle scene-specific block effects
            switch (currentSceneType)
            {
                case SceneType.UnderwaterWorld:
                    fishSystem.OnSharkBlocked(blockData);
                    break;
                    
                case SceneType.CrystalCave:
                    crystalSystem.OnCrystalBlocked(blockData);
                    break;
                    
                case SceneType.ForestGlade:
                    spiritSystem.OnVineBlocked(blockData);
                    break;
                    
                case SceneType.DesertOasis:
                    mirageSystem.OnSandstormBlocked(blockData);
                    break;
            }
        }
        
        private void OnDestroy()
        {
            CleanupCurrentScene();
        }
        
        private void Start()
        {
            // Direct asset references - assign in inspector
            if (traditionalWhiteMaterial == null)
                Debug.LogWarning("SceneTransformationSystem: WhiteCircleMaterial not assigned in inspector");
            if (traditionalGrayMaterial == null)
                Debug.LogWarning("SceneTransformationSystem: GrayCircleMaterial not assigned in inspector");
            if (traditionalBlockMaterial == null)
                Debug.LogWarning("SceneTransformationSystem: RedBlockMaterial not assigned in inspector");
            
            Debug.Log($"Scene Transformation System initialized. Traditional mode: {useTraditionalTargets}");
        }

        // Arena transformations (traditional with enhanced materials)
        private GameObject TransformToArenaTarget(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            // Arena uses enhanced traditional targets with better materials
            return ApplyTraditionalTargetStyle(target, circleType);
        }

        private GameObject TransformToArenaBlock(GameObject block, float spinSpeed)
        {
            return ApplyTraditionalBlockStyle(block);
        }

        // Rain transformations
        private GameObject TransformToRainTarget(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Rain targets have water droplet effect
                renderer.material.color = circleType == RhythmTargetSystem.CircleType.White ? 
                    new Color(0.8f, 0.9f, 1f, 0.8f) : new Color(0.4f, 0.5f, 0.6f, 0.8f);
                renderer.material.SetFloat("_Metallic", 0.2f);
                renderer.material.SetFloat("_Smoothness", 0.9f);
            }
            
            // Add ripple effect
            target.name = circleType == RhythmTargetSystem.CircleType.White ? "RainDrop_White" : "RainDrop_Gray";
            return target;
        }

        private GameObject TransformToRainBlock(GameObject block, float spinSpeed)
        {
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.6f, 0.9f, 0.7f);
                renderer.material.SetFloat("_Metallic", 0.3f);
                renderer.material.SetFloat("_Smoothness", 0.8f);
            }
            
            block.name = "LightningOrb";
            return block;
        }

        // Neon transformations
        private GameObject TransformToNeonTarget(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Neon glowing effect
                Color neonColor = circleType == RhythmTargetSystem.CircleType.White ? 
                    new Color(1f, 1f, 1f, 1f) : new Color(0.8f, 0.2f, 0.8f, 1f);
                renderer.material.color = neonColor;
                renderer.material.SetFloat("_Emission", 2f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", neonColor * 2f);
            }
            
            target.name = circleType == RhythmTargetSystem.CircleType.White ? "NeonHolo_White" : "NeonHolo_Gray";
            return target;
        }

        private GameObject TransformToNeonBlock(GameObject block, float spinSpeed)
        {
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color neonRed = new Color(1f, 0.2f, 0.2f, 1f);
                renderer.material.color = neonRed;
                renderer.material.SetFloat("_Emission", 3f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", neonRed * 3f);
            }
            
            block.name = "DataCube";
            return block;
        }

        // Space transformations
        private GameObject TransformToSpaceTarget(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Energy orb effect
                Color energyColor = circleType == RhythmTargetSystem.CircleType.White ? 
                    new Color(0.9f, 0.9f, 1f, 0.8f) : new Color(0.6f, 0.6f, 0.8f, 0.8f);
                renderer.material.color = energyColor;
                renderer.material.SetFloat("_Metallic", 0f);
                renderer.material.SetFloat("_Smoothness", 0.1f);
            }
            
            // Reduce gravity effect
            var rigidbody = target.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = target.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.drag = spaceGravity;
            
            target.name = circleType == RhythmTargetSystem.CircleType.White ? "EnergyOrb_White" : "EnergyOrb_Gray";
            return target;
        }

        private GameObject TransformToSpaceBlock(GameObject block, float spinSpeed)
        {
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.3f, 0f, 0.9f);
                renderer.material.SetFloat("_Emission", 1.5f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f, 1f));
            }
            
            // Zero gravity spinning
            var rigidbody = block.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = block.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.angularDrag = 0f;
            
            block.name = "PlasmaCore";
            return block;
        }

        // Crystal transformations
        private GameObject TransformToCrystalTarget(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Crystal effect
                Color crystalColor = circleType == RhythmTargetSystem.CircleType.White ? 
                    new Color(0.9f, 0.9f, 1f, 0.7f) : new Color(0.7f, 0.7f, 0.9f, 0.7f);
                renderer.material.color = crystalColor;
                renderer.material.SetFloat("_Metallic", 0.1f);
                renderer.material.SetFloat("_Smoothness", 0.9f);
            }
            
            // Add harmonic oscillator
            var oscillator = target.GetComponent<HarmonicOscillator>();
            if (oscillator == null) oscillator = target.AddComponent<HarmonicOscillator>();
            oscillator.frequency = crystalHarmonic;
            oscillator.amplitude = 0.1f;
            
            target.name = circleType == RhythmTargetSystem.CircleType.White ? "Crystal_White" : "Crystal_Gray";
            return target;
        }

        private GameObject TransformToCrystalBlock(GameObject block, float spinSpeed)
        {
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.8f, 0.3f, 0.8f, 0.8f);
                renderer.material.SetFloat("_Metallic", 0.2f);
                renderer.material.SetFloat("_Smoothness", 0.95f);
            }
            
            // Add resonance effect
            var oscillator = block.GetComponent<HarmonicOscillator>();
            if (oscillator == null) oscillator = block.AddComponent<HarmonicOscillator>();
            oscillator.frequency = crystalHarmonic * 2f;
            oscillator.amplitude = 0.2f;
            
            block.name = "CrystalCluster";
            return block;
        }

        // Fish transformations (using existing UnderwaterFishSystem)
        private GameObject TransformToFishTarget(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            if (UnderwaterFishSystem.Instance != null)
            {
                return UnderwaterFishSystem.Instance.CreateFishTarget(circleType, target);
            }
            
            // Fallback if fish system not available
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color fishColor = circleType == RhythmTargetSystem.CircleType.White ? 
                    new Color(0.8f, 0.9f, 1f, 0.9f) : new Color(0.5f, 0.6f, 0.8f, 0.9f);
                renderer.material.color = fishColor;
            }
            
            // Add underwater physics
            var rigidbody = target.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = target.AddComponent<Rigidbody>();
            rigidbody.drag = underwaterDrag;
            rigidbody.useGravity = false;
            
            target.name = circleType == RhythmTargetSystem.CircleType.White ? "Fish_Small" : "Fish_Medium";
            return target;
        }

        private GameObject TransformToUnderwaterBlock(GameObject block, float spinSpeed)
        {
            if (UnderwaterFishSystem.Instance != null)
            {
                return UnderwaterFishSystem.Instance.CreateSharkBlock(block, spinSpeed);
            }
            
            // Fallback shark block
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.3f, 0.5f, 0.9f);
            }
            
            var rigidbody = block.GetComponent<Rigidbody>();
            if (rigidbody == null) rigidbody = block.AddComponent<Rigidbody>();
            rigidbody.drag = underwaterDrag * 0.5f;
            rigidbody.useGravity = false;
            
            block.name = "Shark";
            return block;
        }

        // Desert transformations
        private GameObject TransformToDesertTarget(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Sand spirit effect
                Color sandColor = circleType == RhythmTargetSystem.CircleType.White ? 
                    new Color(1f, 0.9f, 0.7f, 0.8f) : new Color(0.8f, 0.6f, 0.4f, 0.8f);
                renderer.material.color = sandColor;
                renderer.material.SetFloat("_Metallic", 0f);
                renderer.material.SetFloat("_Smoothness", 0.2f);
            }
            
            // Add mirage effect (30% chance to be illusion)
            if (Random.Range(0f, 1f) < 0.3f)
            {
                var mirageEffect = target.GetComponent<MirageEffect>();
                if (mirageEffect == null) mirageEffect = target.AddComponent<MirageEffect>();
                mirageEffect.isIllusion = true;
            }
            
            target.name = circleType == RhythmTargetSystem.CircleType.White ? "SandSpirit_White" : "SandSpirit_Gray";
            return target;
        }

        private GameObject TransformToDesertBlock(GameObject block, float spinSpeed)
        {
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.9f, 0.6f, 0.2f, 0.7f);
                renderer.material.SetFloat("_Metallic", 0f);
                renderer.material.SetFloat("_Smoothness", 0.1f);
            }
            
            block.name = "Sandstorm";
            return block;
        }

        // Forest transformations
        private GameObject TransformToForestTarget(GameObject target, RhythmTargetSystem.CircleType circleType)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Magical spirit effect
                Color spiritColor = circleType == RhythmTargetSystem.CircleType.White ? 
                    new Color(0.9f, 1f, 0.9f, 0.8f) : new Color(0.6f, 0.8f, 0.6f, 0.8f);
                renderer.material.color = spiritColor;
                renderer.material.SetFloat("_Emission", 0.5f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", spiritColor * 0.5f);
            }
            
            // Add seasonal adaptation based on music tempo
            float bpm = AdvancedAudioManager.Instance?.CurrentBPM ?? 120f;
            Color seasonalTint = GetSeasonalColor(bpm);
            if (renderer != null)
            {
                renderer.material.color = Color.Lerp(renderer.material.color, seasonalTint, 0.3f);
            }
            
            target.name = circleType == RhythmTargetSystem.CircleType.White ? "ForestSpirit_White" : "ForestSpirit_Gray";
            return target;
        }

        private GameObject TransformToForestBlock(GameObject block, float spinSpeed)
        {
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.4f, 0.6f, 0.2f, 0.9f);
                renderer.material.SetFloat("_Emission", 0.3f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.1f, 1f));
            }
            
            block.name = "ThornVine";
            return block;
        }

        private Color GetSeasonalColor(float bpm)
        {
            // Seasonal adaptation based on music tempo
            if (bpm < 80) return new Color(0.8f, 0.9f, 1f, 1f); // Winter (slow)
            else if (bpm < 120) return new Color(0.9f, 1f, 0.8f, 1f); // Spring (moderate)
            else if (bpm < 160) return new Color(1f, 0.9f, 0.7f, 1f); // Summer (fast)
            else return new Color(1f, 0.7f, 0.5f, 1f); // Autumn (very fast)
        }
    }
    
    // Base class for transformed objects
    public class SceneTransformedObject : MonoBehaviour
    {
        public SceneTransformationSystem.SceneType sceneType;
        public RhythmTargetSystem.CircleType originalType;
        public GameObject originalObject;
        
        public void Initialize(SceneTransformationSystem.SceneType scene, RhythmTargetSystem.CircleType type, GameObject original)
        {
            sceneType = scene;
            originalType = type;
            originalObject = original;
        }
    }
}