using UnityEngine;
using System.Collections.Generic;
using VRBoxingGame.Core;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Scene Prefab Creator - Generates the 8 missing scene prefabs that the menu system expects
    /// This solves the critical architecture issue where scenes don't exist
    /// </summary>
    public class ScenePrefabCreator : MonoBehaviour
    {
        [Header("Scene Prefab Generation")]
        public bool createPrefabsOnStart = true;
        public Transform prefabContainer;
        
        [Header("Scene Environment Assets")]
        public Material arenaMaterial;
        public Material stormMaterial; 
        public Material neonMaterial;
        public Material spaceMaterial;
        public Material crystalMaterial;
        public Material underwaterMaterial;
        public Material desertMaterial;
        public Material forestMaterial;
        
        [Header("Audio Assets")]
        public AudioClip[] ambientSounds;
        public AudioClip[] musicTracks;
        
        [Header("Lighting Presets")]
        public Color[] ambientColors;
        public float[] lightIntensities;
        
        private SceneAssetManager sceneAssetManager;
        private Dictionary<int, GameObject> createdPrefabs = new Dictionary<int, GameObject>();
        
        public static ScenePrefabCreator Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (createPrefabsOnStart)
            {
                StartCoroutine(CreateAllScenePrefabs());
            }
        }
        
        private System.Collections.IEnumerator CreateAllScenePrefabs()
        {
            Debug.Log("🏗️ Creating missing scene prefabs...");
            
            // Wait for other systems
            yield return new WaitForSeconds(1f);
            
            // Get SceneAssetManager reference
            sceneAssetManager = CachedReferenceManager.Get<SceneAssetManager>();
            if (sceneAssetManager == null)
            {
                Debug.LogError("❌ SceneAssetManager not found!");
                yield break;
            }
            
            // Create container if needed
            if (prefabContainer == null)
            {
                GameObject container = new GameObject("Generated Scene Prefabs");
                prefabContainer = container.transform;
                prefabContainer.SetParent(transform);
            }
            
            // Create all 8 scene prefabs
            for (int i = 0; i < 8; i++)
            {
                CreateScenePrefab(i);
                yield return null; // Spread across frames
            }
            
            // Assign prefabs to SceneAssetManager
            AssignPrefabsToSceneManager();
            
            Debug.Log("✅ All scene prefabs created successfully!");
        }
        
        private void CreateScenePrefab(int sceneIndex)
        {
            string sceneName = GetSceneName(sceneIndex);
            Debug.Log($"🏗️ Creating scene prefab: {sceneName}");
            
            // Create root object
            GameObject scenePrefab = new GameObject($"Scene_{sceneIndex}_{sceneName}");
            scenePrefab.transform.SetParent(prefabContainer);
            
            // Add basic components
            scenePrefab.AddComponent<SceneEnvironment>();
            
            // Create environment based on scene type
            switch (sceneIndex)
            {
                case 0: CreateDefaultArena(scenePrefab); break;
                case 1: CreateRainStorm(scenePrefab); break;
                case 2: CreateNeonCity(scenePrefab); break;
                case 3: CreateSpaceStation(scenePrefab); break;
                case 4: CreateCrystalCave(scenePrefab); break;
                case 5: CreateUnderwaterWorld(scenePrefab); break;
                case 6: CreateDesertOasis(scenePrefab); break;
                case 7: CreateForestGlade(scenePrefab); break;
            }
            
            // Add common scene elements
            AddCommonSceneElements(scenePrefab, sceneIndex);
            
            // Store created prefab
            createdPrefabs[sceneIndex] = scenePrefab;
            
            // Deactivate initially
            scenePrefab.SetActive(false);
        }
        
        private void CreateDefaultArena(GameObject parent)
        {
            // Create arena floor
            GameObject floor = CreatePrimitive(PrimitiveType.Plane, parent, "Arena Floor");
            floor.transform.localScale = new Vector3(10, 1, 10);
            ApplyMaterial(floor, arenaMaterial);
            
            // Create walls
            CreateArenaWalls(parent);
            
            // Add arena lighting
            CreateLighting(parent, Color.white, 1.2f);
            
            // Add crowd stands (simple geometry)
            CreateCrowdStands(parent);
        }
        
        private void CreateRainStorm(GameObject parent)
        {
            // Create platform
            GameObject platform = CreatePrimitive(PrimitiveType.Plane, parent, "Storm Platform");
            platform.transform.localScale = new Vector3(8, 1, 8);
            ApplyMaterial(platform, stormMaterial);
            
            // Add rain effects
            CreateRainEffect(parent);
            
            // Add storm lighting
            CreateLighting(parent, Color.blue, 0.6f);
            
            // Add storm sounds
            AddAmbientSound(parent, 1); // Storm sounds
        }
        
        private void CreateNeonCity(GameObject parent)
        {
            // Create neon platform
            GameObject platform = CreatePrimitive(PrimitiveType.Plane, parent, "Neon Platform");
            platform.transform.localScale = new Vector3(12, 1, 12);
            ApplyMaterial(platform, neonMaterial);
            
            // Add neon buildings (simple cubes)
            CreateNeonBuildings(parent);
            
            // Add neon lighting
            CreateLighting(parent, Color.cyan, 1.5f);
            
            // Add city ambiance
            AddAmbientSound(parent, 2);
        }
        
        private void CreateSpaceStation(GameObject parent)
        {
            // Create space platform
            GameObject platform = CreatePrimitive(PrimitiveType.Cylinder, parent, "Space Platform");
            platform.transform.localScale = new Vector3(15, 0.5f, 15);
            ApplyMaterial(platform, spaceMaterial);
            
            // Add space elements
            CreateSpaceElements(parent);
            
            // Add space lighting
            CreateLighting(parent, Color.black, 0.3f);
            
            // Add space ambiance
            AddAmbientSound(parent, 3);
        }
        
        private void CreateCrystalCave(GameObject parent)
        {
            // Create cave floor
            GameObject floor = CreatePrimitive(PrimitiveType.Plane, parent, "Cave Floor");
            floor.transform.localScale = new Vector3(8, 1, 8);
            ApplyMaterial(floor, crystalMaterial);
            
            // Add crystal formations
            CreateCrystalFormations(parent);
            
            // Add crystal lighting
            CreateLighting(parent, Color.magenta, 0.8f);
            
            // Add cave ambiance
            AddAmbientSound(parent, 4);
        }
        
        private void CreateUnderwaterWorld(GameObject parent)
        {
            // Create seafloor
            GameObject seafloor = CreatePrimitive(PrimitiveType.Plane, parent, "Seafloor");
            seafloor.transform.localScale = new Vector3(15, 1, 15);
            ApplyMaterial(seafloor, underwaterMaterial);
            
            // Add underwater elements
            CreateUnderwaterElements(parent);
            
            // Add underwater lighting
            CreateLighting(parent, Color.blue, 0.4f);
            
            // Add underwater sounds
            AddAmbientSound(parent, 5);
        }
        
        private void CreateDesertOasis(GameObject parent)
        {
            // Create desert floor
            GameObject floor = CreatePrimitive(PrimitiveType.Plane, parent, "Desert Floor");
            floor.transform.localScale = new Vector3(12, 1, 12);
            ApplyMaterial(floor, desertMaterial);
            
            // Add oasis elements
            CreateOasisElements(parent);
            
            // Add desert lighting
            CreateLighting(parent, Color.yellow, 1.8f);
            
            // Add desert ambiance
            AddAmbientSound(parent, 6);
        }
        
        private void CreateForestGlade(GameObject parent)
        {
            // Create forest floor
            GameObject floor = CreatePrimitive(PrimitiveType.Plane, parent, "Forest Floor");
            floor.transform.localScale = new Vector3(10, 1, 10);
            ApplyMaterial(floor, forestMaterial);
            
            // Add forest elements
            CreateForestElements(parent);
            
            // Add forest lighting
            CreateLighting(parent, Color.green, 1f);
            
            // Add forest sounds
            AddAmbientSound(parent, 7);
        }
        
        private void AddCommonSceneElements(GameObject parent, int sceneIndex)
        {
            // Add spawn points for different game modes
            CreateSpawnPoints(parent, sceneIndex);
            
            // Add boundary markers
            CreateBoundaryMarkers(parent);
            
            // Add game mode zones
            CreateGameModeZones(parent, sceneIndex);
        }
        
        private void CreateSpawnPoints(GameObject parent, int sceneIndex)
        {
            GameObject spawnContainer = new GameObject("Spawn Points");
            spawnContainer.transform.SetParent(parent.transform);
            
            // Create spawn points for different game modes
            var config = GetSceneConfig(sceneIndex);
            
            // Traditional mode spawns
            CreateSpawnZone(spawnContainer, "Traditional Spawns", Vector3.zero, new Vector3(6, 3, 6));
            
            // Flow mode spawns
            CreateSpawnZone(spawnContainer, "Flow Spawns", Vector3.forward * 8, config.flowModeSpawnArea);
            
            // Staff mode spawns
            CreateSpawnZone(spawnContainer, "Staff Spawns", Vector3.right * 4, config.staffModeSpawnArea);
            
            // Dodging spawns
            CreateSpawnZone(spawnContainer, "Dodging Spawns", Vector3.left * 4, config.dodgingModeSpawnArea);
        }
        
        private void CreateSpawnZone(GameObject parent, string name, Vector3 offset, Vector3 size)
        {
            GameObject zone = new GameObject(name);
            zone.transform.SetParent(parent.transform);
            zone.transform.localPosition = offset;
            
            // Add box collider to define zone
            BoxCollider collider = zone.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = true;
            
            // Add visual marker (wireframe)
            zone.AddComponent<SpawnZoneMarker>();
        }
        
        private void CreateBoundaryMarkers(GameObject parent)
        {
            GameObject boundaryContainer = new GameObject("Boundary Markers");
            boundaryContainer.transform.SetParent(parent.transform);
            
            // Create simple boundary markers
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 8f;
                
                GameObject marker = CreatePrimitive(PrimitiveType.Capsule, boundaryContainer, $"Boundary_{i}");
                marker.transform.localPosition = position;
                marker.transform.localScale = new Vector3(0.2f, 2f, 0.2f);
                
                // Make it a trigger for boundary detection
                marker.GetComponent<Collider>().isTrigger = true;
                marker.AddComponent<BoundaryMarker>();
            }
        }
        
        private void CreateGameModeZones(GameObject parent, int sceneIndex)
        {
            GameObject zonesContainer = new GameObject("Game Mode Zones");
            zonesContainer.transform.SetParent(parent.transform);
            
            // Add game mode zone manager
            var zoneManager = zonesContainer.AddComponent<GameModeZoneManager>();
            zoneManager.sceneIndex = sceneIndex;
        }
        
        // Helper methods for creating specific scene elements
        private void CreateArenaWalls(GameObject parent)
        {
            // Create 4 walls around the arena
            Vector3[] wallPositions = {
                new Vector3(0, 2, 5),   // North
                new Vector3(0, 2, -5),  // South  
                new Vector3(5, 2, 0),   // East
                new Vector3(-5, 2, 0)   // West
            };
            
            Vector3[] wallScales = {
                new Vector3(10, 4, 0.5f),
                new Vector3(10, 4, 0.5f),
                new Vector3(0.5f, 4, 10),
                new Vector3(0.5f, 4, 10)
            };
            
            for (int i = 0; i < 4; i++)
            {
                GameObject wall = CreatePrimitive(PrimitiveType.Cube, parent, $"Wall_{i}");
                wall.transform.localPosition = wallPositions[i];
                wall.transform.localScale = wallScales[i];
                ApplyMaterial(wall, arenaMaterial);
            }
        }
        
        private void CreateCrowdStands(GameObject parent)
        {
            // Simple crowd stands as cubes
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle), 1, Mathf.Sin(angle)) * 12f;
                
                GameObject stand = CreatePrimitive(PrimitiveType.Cube, parent, $"CrowdStand_{i}");
                stand.transform.localPosition = position;
                stand.transform.localScale = new Vector3(4, 2, 2);
                ApplyMaterial(stand, arenaMaterial);
            }
        }
        
        private void CreateRainEffect(GameObject parent)
        {
            GameObject rainEffect = new GameObject("Rain Effect");
            rainEffect.transform.SetParent(parent.transform);
            rainEffect.transform.localPosition = new Vector3(0, 8, 0);
            
            // Add particle system for rain
            ParticleSystem rain = rainEffect.AddComponent<ParticleSystem>();
            var main = rain.main;
            main.startLifetime = 2f;
            main.startSpeed = 10f;
            main.startColor = Color.blue;
            main.maxParticles = 1000;
            
            var emission = rain.emission;
            emission.rateOverTime = 500;
            
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20, 1, 20);
        }
        
        private void CreateNeonBuildings(GameObject parent)
        {
            // Create simple neon buildings as tall cubes
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle), 5, Mathf.Sin(angle)) * 15f;
                
                GameObject building = CreatePrimitive(PrimitiveType.Cube, parent, $"NeonBuilding_{i}");
                building.transform.localPosition = position;
                building.transform.localScale = new Vector3(2, 10, 2);
                ApplyMaterial(building, neonMaterial);
                
                // Add emissive material for neon effect
                var renderer = building.GetComponent<Renderer>();
                if (renderer.material != null)
                {
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", Color.cyan);
                }
            }
        }
        
        private void CreateSpaceElements(GameObject parent)
        {
            // Create space station elements
            GameObject spaceContainer = new GameObject("Space Elements");
            spaceContainer.transform.SetParent(parent.transform);
            
            // Add some floating space debris
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle), Random.Range(2f, 8f), Mathf.Sin(angle)) * Random.Range(20f, 30f);
                
                GameObject debris = CreatePrimitive(PrimitiveType.Cube, spaceContainer, $"SpaceDebris_{i}");
                debris.transform.localPosition = position;
                debris.transform.localScale = Vector3.one * Random.Range(0.5f, 2f);
                debris.transform.rotation = Random.rotation;
                ApplyMaterial(debris, spaceMaterial);
            }
        }
        
        private void CreateCrystalFormations(GameObject parent)
        {
            // Create crystal formations as scaled cubes
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle), Random.Range(1f, 4f), Mathf.Sin(angle)) * Random.Range(6f, 10f);
                
                GameObject crystal = CreatePrimitive(PrimitiveType.Cube, parent, $"Crystal_{i}");
                crystal.transform.localPosition = position;
                crystal.transform.localScale = new Vector3(1, Random.Range(2f, 6f), 1);
                crystal.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), Random.Range(-15f, 15f));
                ApplyMaterial(crystal, crystalMaterial);
                
                // Add emissive effect
                var renderer = crystal.GetComponent<Renderer>();
                if (renderer.material != null)
                {
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", Color.magenta * 0.5f);
                }
            }
        }
        
        private void CreateUnderwaterElements(GameObject parent)
        {
            // Create underwater plants and rocks
            GameObject underwaterContainer = new GameObject("Underwater Elements");
            underwaterContainer.transform.SetParent(parent.transform);
            
            // Add some seaweed-like objects
            for (int i = 0; i < 15; i++)
            {
                Vector3 position = new Vector3(Random.Range(-10f, 10f), Random.Range(0.5f, 3f), Random.Range(-10f, 10f));
                
                GameObject seaweed = CreatePrimitive(PrimitiveType.Capsule, underwaterContainer, $"Seaweed_{i}");
                seaweed.transform.localPosition = position;
                seaweed.transform.localScale = new Vector3(0.3f, Random.Range(2f, 5f), 0.3f);
                ApplyMaterial(seaweed, underwaterMaterial);
            }
        }
        
        private void CreateOasisElements(GameObject parent)
        {
            // Create palm trees and water
            GameObject oasisContainer = new GameObject("Oasis Elements");
            oasisContainer.transform.SetParent(parent.transform);
            
            // Add palm trees (cylinders for trunks)
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                Vector3 position = new Vector3(Mathf.Cos(angle), 3, Mathf.Sin(angle)) * 8f;
                
                GameObject palm = CreatePrimitive(PrimitiveType.Cylinder, oasisContainer, $"Palm_{i}");
                palm.transform.localPosition = position;
                palm.transform.localScale = new Vector3(0.5f, 6f, 0.5f);
                ApplyMaterial(palm, desertMaterial);
            }
            
            // Add oasis water
            GameObject water = CreatePrimitive(PrimitiveType.Cylinder, oasisContainer, "Oasis Water");
            water.transform.localPosition = Vector3.zero;
            water.transform.localScale = new Vector3(4, 0.1f, 4);
            ApplyMaterial(water, underwaterMaterial);
        }
        
        private void CreateForestElements(GameObject parent)
        {
            // Create trees
            GameObject forestContainer = new GameObject("Forest Elements");
            forestContainer.transform.SetParent(parent.transform);
            
            // Add trees
            for (int i = 0; i < 20; i++)
            {
                Vector3 position = new Vector3(Random.Range(-8f, 8f), 2.5f, Random.Range(-8f, 8f));
                
                GameObject tree = CreatePrimitive(PrimitiveType.Cylinder, forestContainer, $"Tree_{i}");
                tree.transform.localPosition = position;
                tree.transform.localScale = new Vector3(0.8f, 5f, 0.8f);
                ApplyMaterial(tree, forestMaterial);
            }
        }
        
        private void CreateLighting(GameObject parent, Color ambientColor, float intensity)
        {
            GameObject lightObj = new GameObject("Scene Light");
            lightObj.transform.SetParent(parent.transform);
            lightObj.transform.localPosition = new Vector3(0, 8, 0);
            lightObj.transform.localRotation = Quaternion.Euler(50, 30, 0);
            
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = ambientColor;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }
        
        private void AddAmbientSound(GameObject parent, int soundIndex)
        {
            if (ambientSounds != null && soundIndex < ambientSounds.Length && ambientSounds[soundIndex] != null)
            {
                GameObject audioObj = new GameObject("Ambient Audio");
                audioObj.transform.SetParent(parent.transform);
                
                AudioSource audioSource = audioObj.AddComponent<AudioSource>();
                audioSource.clip = ambientSounds[soundIndex];
                audioSource.loop = true;
                audioSource.playOnAwake = true;
                audioSource.volume = 0.3f;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }
        
        private GameObject CreatePrimitive(PrimitiveType type, GameObject parent, string name)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent.transform);
            return primitive;
        }
        
        private void ApplyMaterial(GameObject obj, Material material)
        {
            if (material != null)
            {
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = material;
                }
            }
        }
        
        private string GetSceneName(int sceneIndex)
        {
            string[] names = {
                "Default Arena", "Rain Storm", "Neon City", "Space Station",
                "Crystal Cave", "Underwater World", "Desert Oasis", "Forest Glade"
            };
            return sceneIndex < names.Length ? names[sceneIndex] : "Unknown";
        }
        
        private SceneGameModeIntegrator.SceneConfiguration GetSceneConfig(int sceneIndex)
        {
            var integrator = CachedReferenceManager.Get<SceneGameModeIntegrator>();
            if (integrator != null && sceneIndex < integrator.sceneConfigs.Length)
            {
                return integrator.sceneConfigs[sceneIndex];
            }
            
            // Return default config
            return new SceneGameModeIntegrator.SceneConfiguration
            {
                flowModeSpawnArea = new Vector3(8, 6, 12),
                staffModeSpawnArea = new Vector3(6, 8, 6),
                dodgingModeSpawnArea = new Vector3(10, 8, 10)
            };
        }
        
        private void AssignPrefabsToSceneManager()
        {
            if (sceneAssetManager == null) return;
            
            // Use reflection to assign the created prefabs to SceneAssetManager
            var fields = typeof(SceneAssetManager).GetFields();
            
            for (int i = 0; i < 8; i++)
            {
                if (createdPrefabs.ContainsKey(i))
                {
                    string fieldName = GetPrefabFieldName(i);
                    var field = System.Array.Find(fields, f => f.Name == fieldName);
                    
                    if (field != null)
                    {
                        field.SetValue(sceneAssetManager, createdPrefabs[i]);
                        Debug.Log($"✅ Assigned {fieldName} to SceneAssetManager");
                    }
                }
            }
        }
        
        private string GetPrefabFieldName(int sceneIndex)
        {
            string[] fieldNames = {
                "defaultArenaPrefab", "rainStormPrefab", "neonCityPrefab", "spaceStationPrefab",
                "crystalCavePrefab", "underwaterWorldPrefab", "desertOasisPrefab", "forestGladePrefab"
            };
            return sceneIndex < fieldNames.Length ? fieldNames[sceneIndex] : "";
        }
        
        [ContextMenu("Create All Scene Prefabs")]
        public void CreateAllScenePrefabsManual()
        {
            StartCoroutine(CreateAllScenePrefabs());
        }
        
        public GameObject GetCreatedPrefab(int sceneIndex)
        {
            return createdPrefabs.ContainsKey(sceneIndex) ? createdPrefabs[sceneIndex] : null;
        }
    }
    
    // Helper components
    public class SceneEnvironment : MonoBehaviour
    {
        public int sceneIndex;
        public bool isActive;
        
        public void ActivateScene()
        {
            isActive = true;
            gameObject.SetActive(true);
        }
        
        public void DeactivateScene()
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }
    
    public class SpawnZoneMarker : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
        }
    }
    
    public class BoundaryMarker : MonoBehaviour
    {
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("Player approaching boundary");
                // Send boundary warning event
            }
        }
    }
    
    public class GameModeZoneManager : MonoBehaviour
    {
        public int sceneIndex;
        
        private void Start()
        {
            // Configure zones based on scene compatibility
            ConfigureGameModeZones();
        }
        
        private void ConfigureGameModeZones()
        {
            var integrator = CachedReferenceManager.Get<SceneGameModeIntegrator>();
            if (integrator != null && sceneIndex < integrator.sceneConfigs.Length)
            {
                var config = integrator.sceneConfigs[sceneIndex];
                Debug.Log($"Configured game mode zones for {config.sceneName}");
            }
        }
    }
} 