using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Core;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Scene Asset Manager using Unity 6 Addressable Asset System
    /// Manages 8 different scene environments as prefabs instead of separate scene files
    /// </summary>
    public class SceneAssetManager : MonoBehaviour
    {
        [Header("Scene Prefab Assets")]
        public GameObject defaultArenaPrefab;
        public GameObject rainStormPrefab;
        public GameObject neonCityPrefab;
        public GameObject spaceStationPrefab;
        public GameObject crystalCavePrefab;
        public GameObject underwaterWorldPrefab;
        public GameObject desertOasisPrefab;
        public GameObject forestGladePrefab;
        
        [Header("Addressable Asset Keys")]
        public string[] sceneAssetKeys = {
            "Scene_DefaultArena",
            "Scene_RainStorm", 
            "Scene_NeonCity",
            "Scene_SpaceStation",
            "Scene_CrystalCave",
            "Scene_UnderwaterWorld",
            "Scene_DesertOasis",
            "Scene_ForestGlade"
        };
        
        [Header("Scene Management")]
        public Transform sceneContainer;
        public bool useAddressableAssets = false;
        public bool enableScenePooling = true;
        
        // Scene instances
        private Dictionary<int, GameObject> loadedScenes = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> pooledScenes = new Dictionary<int, GameObject>();
        private GameObject currentActiveScene;
        private int currentSceneIndex = -1;
        
        // Addressable handles
        private Dictionary<string, AsyncOperationHandle<GameObject>> assetHandles = 
            new Dictionary<string, AsyncOperationHandle<GameObject>>();
        
        public static SceneAssetManager Instance { get; private set; }
        
        // Events
        public System.Action<int> OnSceneLoaded;
        public System.Action<int> OnSceneUnloaded;
        public System.Action<float> OnLoadingProgress;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSceneManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSceneManager()
        {
            if (sceneContainer == null)
            {
                GameObject containerObj = new GameObject("Scene Container");
                sceneContainer = containerObj.transform;
                sceneContainer.SetParent(transform);
            }
            
            Debug.Log("🏗️ Scene Asset Manager initialized");
        }
        
        /// <summary>
        /// Load scene by index
        /// </summary>
        public async Task<bool> LoadSceneAsync(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= 8)
            {
                Debug.LogError($"❌ Invalid scene index: {sceneIndex}");
                return false;
            }
            
            try
            {
                // Unload current scene
                if (currentActiveScene != null)
                {
                    await UnloadCurrentSceneAsync();
                }
                
                // Load new scene
                GameObject scenePrefab = await GetScenePrefabAsync(sceneIndex);
                if (scenePrefab == null)
                {
                    Debug.LogError($"❌ Failed to load scene prefab for index {sceneIndex}");
                    return false;
                }
                
                // Instantiate scene
                currentActiveScene = Instantiate(scenePrefab, sceneContainer);
                currentActiveScene.name = $"Scene_{sceneIndex}_{GetSceneName(sceneIndex)}";
                currentSceneIndex = sceneIndex;
                
                // Configure scene
                await ConfigureSceneAsync(sceneIndex);
                
                // Register in loaded scenes
                loadedScenes[sceneIndex] = currentActiveScene;
                
                OnSceneLoaded?.Invoke(sceneIndex);
                Debug.Log($"✅ Scene {sceneIndex} ({GetSceneName(sceneIndex)}) loaded successfully");
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error loading scene {sceneIndex}: {ex.Message}");
                return false;
            }
        }
        
        private async Task<GameObject> GetScenePrefabAsync(int sceneIndex)
        {
            OnLoadingProgress?.Invoke(0.2f);
            
            if (useAddressableAssets && sceneIndex < sceneAssetKeys.Length)
            {
                // Load from Addressable Assets
                return await LoadAddressableSceneAsync(sceneAssetKeys[sceneIndex]);
            }
            else
            {
                // Load from direct prefab references
                return GetDirectScenePrefab(sceneIndex);
            }
        }
        
        private async Task<GameObject> LoadAddressableSceneAsync(string assetKey)
        {
            try
            {
                if (assetHandles.ContainsKey(assetKey))
                {
                    return assetHandles[assetKey].Result;
                }
                
                var handle = Addressables.LoadAssetAsync<GameObject>(assetKey);
                assetHandles[assetKey] = handle;
                
                await handle.Task;
                
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    OnLoadingProgress?.Invoke(0.7f);
                    return handle.Result;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Failed to load addressable asset: {assetKey}");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error loading addressable asset {assetKey}: {ex.Message}");
                return null;
            }
        }
        
        private GameObject GetDirectScenePrefab(int sceneIndex)
        {
            OnLoadingProgress?.Invoke(0.5f);
            
            switch (sceneIndex)
            {
                case 0: return defaultArenaPrefab;
                case 1: return rainStormPrefab;
                case 2: return neonCityPrefab;
                case 3: return spaceStationPrefab;
                case 4: return crystalCavePrefab;
                case 5: return underwaterWorldPrefab;
                case 6: return desertOasisPrefab;
                case 7: return forestGladePrefab;
                default:
                    Debug.LogWarning($"⚠️ No prefab defined for scene {sceneIndex}, creating default");
                    return CreateDefaultScenePrefab();
            }
        }
        
        private GameObject CreateDefaultScenePrefab()
        {
            GameObject defaultScene = new GameObject("Default Scene");
            
            // Add basic ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(defaultScene.transform);
            ground.transform.localScale = Vector3.one * 10f;
            ground.name = "Ground";
            
            // Add basic lighting
            GameObject lightObj = new GameObject("Directional Light");
            lightObj.transform.SetParent(defaultScene.transform);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            
            return defaultScene;
        }
        
        private async Task ConfigureSceneAsync(int sceneIndex)
        {
            OnLoadingProgress?.Invoke(0.8f);
            
            // Apply scene-specific configurations
            switch (sceneIndex)
            {
                case 1: // Rain Storm
                    await ConfigureRainStormAsync();
                    break;
                case 2: // Neon City
                    await ConfigureNeonCityAsync();
                    break;
                case 5: // Underwater World
                    await ConfigureUnderwaterAsync();
                    break;
            }
            
            OnLoadingProgress?.Invoke(1f);
        }
        
        private async Task ConfigureRainStormAsync()
        {
            // Configure rain-specific settings
            if (Physics.gravity.y != -20f)
            {
                Physics.gravity = new Vector3(0, -20f, 0); // Stronger gravity for rain
            }
            
            await Task.Delay(100);
        }
        
        private async Task ConfigureNeonCityAsync()
        {
            // Configure neon city settings
            RenderSettings.ambientLight = Color.blue * 0.3f;
            await Task.Delay(100);
        }
        
        private async Task ConfigureUnderwaterAsync()
        {
            // Configure underwater physics
            Physics.gravity = new Vector3(0, -5f, 0); // Reduced gravity underwater
            await Task.Delay(100);
        }
        
        private async Task UnloadCurrentSceneAsync()
        {
            if (currentActiveScene != null)
            {
                int sceneIndex = currentSceneIndex;
                
                if (enableScenePooling)
                {
                    // Pool the scene instead of destroying
                    currentActiveScene.SetActive(false);
                    pooledScenes[sceneIndex] = currentActiveScene;
                }
                else
                {
                    // Destroy the scene
                    DestroyImmediate(currentActiveScene);
                }
                
                loadedScenes.Remove(sceneIndex);
                OnSceneUnloaded?.Invoke(sceneIndex);
                
                currentActiveScene = null;
                currentSceneIndex = -1;
            }
            
            // Reset physics
            Physics.gravity = new Vector3(0, -9.81f, 0);
            await Task.Delay(50);
        }
        
        /// <summary>
        /// Quick scene switch for testing
        /// </summary>
        public void QuickSwitchScene(int sceneIndex)
        {
            _ = LoadSceneAsync(sceneIndex);
        }
        
        /// <summary>
        /// Get scene name by index
        /// </summary>
        public string GetSceneName(int sceneIndex)
        {
            string[] sceneNames = {
                "Default Arena", "Rain Storm", "Neon City", "Space Station",
                "Crystal Cave", "Underwater World", "Desert Oasis", "Forest Glade"
            };
            
            if (sceneIndex >= 0 && sceneIndex < sceneNames.Length)
                return sceneNames[sceneIndex];
            
            return "Unknown Scene";
        }
        
        /// <summary>
        /// Check if scene is loaded
        /// </summary>
        public bool IsSceneLoaded(int sceneIndex)
        {
            return loadedScenes.ContainsKey(sceneIndex);
        }
        
        /// <summary>
        /// Get current scene index
        /// </summary>
        public int GetCurrentSceneIndex()
        {
            return currentSceneIndex;
        }
        
        /// <summary>
        /// Preload scene for faster switching
        /// </summary>
        public async Task PreloadSceneAsync(int sceneIndex)
        {
            if (pooledScenes.ContainsKey(sceneIndex)) return;
            
            GameObject scenePrefab = await GetScenePrefabAsync(sceneIndex);
            if (scenePrefab != null)
            {
                GameObject pooledScene = Instantiate(scenePrefab, sceneContainer);
                pooledScene.SetActive(false);
                pooledScenes[sceneIndex] = pooledScene;
                
                Debug.Log($"📦 Preloaded scene {sceneIndex} ({GetSceneName(sceneIndex)})");
            }
        }
        
        /// <summary>
        /// Clean up all loaded scenes
        /// </summary>
        public void CleanupAllScenes()
        {
            foreach (var scene in loadedScenes.Values)
            {
                if (scene != null) DestroyImmediate(scene);
            }
            
            foreach (var scene in pooledScenes.Values)
            {
                if (scene != null) DestroyImmediate(scene);
            }
            
            loadedScenes.Clear();
            pooledScenes.Clear();
            
            Debug.Log("🧹 All scenes cleaned up");
        }
        
        private void OnDestroy()
        {
            CleanupAllScenes();
            
            // Release addressable asset handles
            foreach (var handle in assetHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            assetHandles.Clear();
        }
    }
} 