using UnityEngine;
using VRBoxingGame.Core;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Quick Scene Fix - Generates basic scene prefabs to solve critical menu failure
    /// This fixes the immediate issue where menu scene selection crashes
    /// </summary>
    public class QuickSceneFix : MonoBehaviour
    {
        [Header("Quick Scene Generation")]
        public bool autoFixOnStart = true;
        public Transform sceneContainer;
        
        private SceneAssetManager sceneAssetManager;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                StartCoroutine(QuickFixScenes());
            }
        }
        
        private System.Collections.IEnumerator QuickFixScenes()
        {
            Debug.Log("🚨 APPLYING QUICK SCENE FIX...");
            
            yield return new WaitForSeconds(1f);
            
            // Get SceneAssetManager
            sceneAssetManager = CachedReferenceManager.Get<SceneAssetManager>();
            if (sceneAssetManager == null)
            {
                Debug.LogError("❌ SceneAssetManager not found!");
                yield break;
            }
            
            // Create container
            if (sceneContainer == null)
            {
                GameObject container = new GameObject("Quick Generated Scenes");
                sceneContainer = container.transform;
                sceneContainer.SetParent(transform);
            }
            
            // Create 8 basic scene prefabs
            CreateBasicScenePrefabs();
            
            Debug.Log("✅ Quick scene fix applied - Menu should work now!");
        }
        
        private void CreateBasicScenePrefabs()
        {
            string[] sceneNames = {
                "Default Arena", "Rain Storm", "Neon City", "Space Station",
                "Crystal Cave", "Underwater World", "Desert Oasis", "Forest Glade"
            };
            
            Color[] sceneColors = {
                Color.white, Color.blue, Color.cyan, Color.black,
                Color.magenta, Color.blue, Color.yellow, Color.green
            };
            
            for (int i = 0; i < 8; i++)
            {
                GameObject scenePrefab = CreateBasicScene(i, sceneNames[i], sceneColors[i]);
                AssignToSceneManager(i, scenePrefab);
            }
        }
        
        private GameObject CreateBasicScene(int index, string name, Color themeColor)
        {
            // Create scene root
            GameObject scene = new GameObject($"Scene_{index}_{name}");
            scene.transform.SetParent(sceneContainer);
            
            // Add floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(scene.transform);
            floor.transform.localScale = new Vector3(10, 1, 10);
            
            // Color the floor
            var renderer = floor.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = themeColor;
            renderer.material = material;
            
            // Add lighting
            GameObject light = new GameObject("Scene Light");
            light.transform.SetParent(scene.transform);
            light.transform.position = new Vector3(0, 8, 0);
            light.transform.rotation = Quaternion.Euler(50, 30, 0);
            
            Light lightComponent = light.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.color = themeColor;
            lightComponent.intensity = 1f;
            
            // Add basic spawn points
            CreateSpawnPoints(scene);
            
            // Deactivate initially
            scene.SetActive(false);
            
            Debug.Log($"Created basic scene: {name}");
            return scene;
        }
        
        private void CreateSpawnPoints(GameObject parent)
        {
            GameObject spawns = new GameObject("Spawn Points");
            spawns.transform.SetParent(parent.transform);
            
            // Create basic spawn zones for each game mode
            CreateSpawnZone(spawns, "Traditional", Vector3.zero);
            CreateSpawnZone(spawns, "Flow", Vector3.forward * 8);
            CreateSpawnZone(spawns, "Staff", Vector3.right * 4);
            CreateSpawnZone(spawns, "Dodging", Vector3.left * 4);
        }
        
        private void CreateSpawnZone(GameObject parent, string modeName, Vector3 offset)
        {
            GameObject zone = new GameObject($"{modeName} Spawn Zone");
            zone.transform.SetParent(parent.transform);
            zone.transform.localPosition = offset;
            
            BoxCollider collider = zone.AddComponent<BoxCollider>();
            collider.size = new Vector3(6, 6, 6);
            collider.isTrigger = true;
        }
        
        private void AssignToSceneManager(int index, GameObject prefab)
        {
            // Use reflection to assign prefabs to SceneAssetManager fields
            var type = typeof(SceneAssetManager);
            string[] fieldNames = {
                "defaultArenaPrefab", "rainStormPrefab", "neonCityPrefab", 
                "spaceStationPrefab", "crystalCavePrefab", "underwaterWorldPrefab",
                "desertOasisPrefab", "forestGladePrefab"
            };
            
            if (index < fieldNames.Length)
            {
                var field = type.GetField(fieldNames[index]);
                if (field != null)
                {
                    field.SetValue(sceneAssetManager, prefab);
                    Debug.Log($"✅ Assigned {fieldNames[index]} to SceneAssetManager");
                }
            }
        }
        
        [ContextMenu("Apply Quick Scene Fix")]
        public void ApplyQuickFix()
        {
            StartCoroutine(QuickFixScenes());
        }
    }
} 