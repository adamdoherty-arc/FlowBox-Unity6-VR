using UnityEngine;
using VRBoxingGame.Boxing;
using VRBoxingGame.Core;
using System.Collections.Generic;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Integrates game modes with scene environments
    /// Ensures each game mode works properly in each scene
    /// </summary>
    public class SceneGameModeIntegrator : MonoBehaviour
    {
        [Header("Game Mode Systems")]
        public FlowModeSystem flowModeSystem;
        public TwoHandedStaffSystem staffSystem;
        public ComprehensiveDodgingSystem dodgingSystem;
        public RhythmTargetSystem rhythmSystem;
        public AICoachVisualSystem aiCoach;
        
        [Header("Scene Configurations")]
        public SceneConfiguration[] sceneConfigs = new SceneConfiguration[8];
        
        public static SceneGameModeIntegrator Instance { get; private set; }
        
        private int currentSceneIndex = 0;
        private GameMode currentGameMode = GameMode.Traditional;
        
        public enum GameMode
        {
            Traditional,
            Flow,
            Staff,
            Dodging,
            AICoach
        }
        
        [System.Serializable]
        public struct SceneConfiguration
        {
            public string sceneName;
            public bool supportsFlowMode;
            public bool supportsStaffMode;
            public bool supportsDodgingMode;
            public bool supportsAICoach;
            
            public Vector3 flowModeSpawnArea;
            public Vector3 staffModeSpawnArea;
            public Vector3 dodgingModeSpawnArea;
            
            public float gravityMultiplier;
            public float atmosphericDensity;
            public Color ambientColor;
            public float musicEchoFactor;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSceneConfigurations();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Get cached references
            flowModeSystem = CachedReferenceManager.Get<FlowModeSystem>();
            staffSystem = CachedReferenceManager.Get<TwoHandedStaffSystem>();
            dodgingSystem = CachedReferenceManager.Get<ComprehensiveDodgingSystem>();
            rhythmSystem = CachedReferenceManager.Get<RhythmTargetSystem>();
            aiCoach = CachedReferenceManager.Get<AICoachVisualSystem>();
            
            // Subscribe to scene changes
            var sceneManager = CachedReferenceManager.Get<SceneLoadingManager>();
            if (sceneManager != null)
            {
                sceneManager.OnSceneChanged += OnSceneChanged;
            }
        }
        
        private void InitializeSceneConfigurations()
        {
            // Default Arena (0)
            sceneConfigs[0] = new SceneConfiguration
            {
                sceneName = "Default Arena",
                supportsFlowMode = true,
                supportsStaffMode = true,
                supportsDodgingMode = true,
                supportsAICoach = true,
                flowModeSpawnArea = new Vector3(8f, 6f, 12f),
                staffModeSpawnArea = new Vector3(6f, 8f, 6f),
                dodgingModeSpawnArea = new Vector3(10f, 8f, 10f),
                gravityMultiplier = 1f,
                atmosphericDensity = 1f,
                ambientColor = Color.white,
                musicEchoFactor = 1f
            };
            
            // Rain Storm (1)
            sceneConfigs[1] = new SceneConfiguration
            {
                sceneName = "Rain Storm",
                supportsFlowMode = true,
                supportsStaffMode = false, // Too chaotic for staff
                supportsDodgingMode = true,
                supportsAICoach = true,
                flowModeSpawnArea = new Vector3(6f, 8f, 10f),
                dodgingModeSpawnArea = new Vector3(8f, 6f, 8f),
                gravityMultiplier = 1.2f, // Heavier feel in storm
                atmosphericDensity = 1.3f,
                ambientColor = Color.blue,
                musicEchoFactor = 1.5f
            };
            
            // Neon City (2)
            sceneConfigs[2] = new SceneConfiguration
            {
                sceneName = "Neon City",
                supportsFlowMode = true,
                supportsStaffMode = true,
                supportsDodgingMode = true,
                supportsAICoach = true,
                flowModeSpawnArea = new Vector3(10f, 8f, 12f),
                staffModeSpawnArea = new Vector3(8f, 6f, 8f),
                dodgingModeSpawnArea = new Vector3(12f, 8f, 12f),
                gravityMultiplier = 1f,
                atmosphericDensity = 0.9f,
                ambientColor = Color.cyan,
                musicEchoFactor = 0.8f
            };
            
            // Space Station (3)
            sceneConfigs[3] = new SceneConfiguration
            {
                sceneName = "Space Station",
                supportsFlowMode = true,
                supportsStaffMode = true,
                supportsDodgingMode = false, // No gravity makes dodging weird
                supportsAICoach = true,
                flowModeSpawnArea = new Vector3(12f, 10f, 12f),
                staffModeSpawnArea = new Vector3(8f, 10f, 8f),
                gravityMultiplier = 0.3f, // Low gravity
                atmosphericDensity = 0.7f,
                ambientColor = Color.black,
                musicEchoFactor = 0.5f
            };
            
            // Crystal Cave (4)
            sceneConfigs[4] = new SceneConfiguration
            {
                sceneName = "Crystal Cave",
                supportsFlowMode = true,
                supportsStaffMode = true,
                supportsDodgingMode = true,
                supportsAICoach = true,
                flowModeSpawnArea = new Vector3(8f, 6f, 10f),
                staffModeSpawnArea = new Vector3(6f, 8f, 6f),
                dodgingModeSpawnArea = new Vector3(8f, 6f, 8f),
                gravityMultiplier = 1f,
                atmosphericDensity = 1.1f,
                ambientColor = Color.magenta,
                musicEchoFactor = 2f // Lots of echo in caves
            };
            
            // Underwater World (5)
            sceneConfigs[5] = new SceneConfiguration
            {
                sceneName = "Underwater World",
                supportsFlowMode = true,
                supportsStaffMode = false, // Water resistance
                supportsDodgingMode = true,
                supportsAICoach = true,
                flowModeSpawnArea = new Vector3(10f, 12f, 10f),
                dodgingModeSpawnArea = new Vector3(12f, 10f, 12f),
                gravityMultiplier = 0.5f, // Buoyancy
                atmosphericDensity = 2f, // Water resistance
                ambientColor = Color.blue,
                musicEchoFactor = 1.8f
            };
            
            // Desert Oasis (6)
            sceneConfigs[6] = new SceneConfiguration
            {
                sceneName = "Desert Oasis",
                supportsFlowMode = true,
                supportsStaffMode = true,
                supportsDodgingMode = true,
                supportsAICoach = true,
                flowModeSpawnArea = new Vector3(12f, 8f, 12f),
                staffModeSpawnArea = new Vector3(8f, 8f, 8f),
                dodgingModeSpawnArea = new Vector3(10f, 8f, 10f),
                gravityMultiplier = 1f,
                atmosphericDensity = 0.8f, // Dry air
                ambientColor = Color.yellow,
                musicEchoFactor = 0.6f
            };
            
            // Forest Glade (7)
            sceneConfigs[7] = new SceneConfiguration
            {
                sceneName = "Forest Glade",
                supportsFlowMode = true,
                supportsStaffMode = true,
                supportsDodgingMode = true,
                supportsAICoach = true,
                flowModeSpawnArea = new Vector3(10f, 8f, 10f),
                staffModeSpawnArea = new Vector3(8f, 8f, 8f),
                dodgingModeSpawnArea = new Vector3(12f, 8f, 12f),
                gravityMultiplier = 1f,
                atmosphericDensity = 1f,
                ambientColor = Color.green,
                musicEchoFactor = 1.2f
            };
            
            Debug.Log("🎮 Scene-Game Mode configurations initialized");
        }
        
        private void OnSceneChanged(SceneLoadingManager.SceneType sceneType)
        {
            currentSceneIndex = (int)sceneType;
            ApplySceneConfiguration(currentSceneIndex);
            ReconfigureGameModeForScene(currentGameMode, currentSceneIndex);
        }
        
        private void ApplySceneConfiguration(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= sceneConfigs.Length) return;
            
            var config = sceneConfigs[sceneIndex];
            
            // Apply physics settings
            Physics.gravity = Vector3.up * (-9.81f * config.gravityMultiplier);
            
            // Apply visual settings
            RenderSettings.ambientLight = config.ambientColor * 0.5f;
            
            // Apply audio settings
            var audioManager = CachedReferenceManager.Get<AdvancedAudioManager>();
            if (audioManager != null)
            {
                // audioManager.SetEchoFactor(config.musicEchoFactor);
            }
            
            Debug.Log($"🎯 Applied scene configuration for {config.sceneName}");
        }
        
        public void SetGameMode(GameMode gameMode)
        {
            currentGameMode = gameMode;
            ReconfigureGameModeForScene(gameMode, currentSceneIndex);
        }
        
        private void ReconfigureGameModeForScene(GameMode gameMode, int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= sceneConfigs.Length) return;
            
            var config = sceneConfigs[sceneIndex];
            
            // Disable all game modes first
            DisableAllGameModes();
            
            switch (gameMode)
            {
                case GameMode.Flow:
                    if (config.supportsFlowMode)
                    {
                        ConfigureFlowModeForScene(config);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Flow Mode not supported in {config.sceneName}");
                        SetGameMode(GameMode.Traditional);
                    }
                    break;
                    
                case GameMode.Staff:
                    if (config.supportsStaffMode)
                    {
                        ConfigureStaffModeForScene(config);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Staff Mode not supported in {config.sceneName}");
                        SetGameMode(GameMode.Traditional);
                    }
                    break;
                    
                case GameMode.Dodging:
                    if (config.supportsDodgingMode)
                    {
                        ConfigureDodgingModeForScene(config);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Dodging Mode not supported in {config.sceneName}");
                        SetGameMode(GameMode.Traditional);
                    }
                    break;
                    
                case GameMode.AICoach:
                    if (config.supportsAICoach)
                    {
                        ConfigureAICoachForScene(config);
                    }
                    break;
                    
                case GameMode.Traditional:
                default:
                    ConfigureTraditionalModeForScene(config);
                    break;
            }
        }
        
        private void DisableAllGameModes()
        {
            if (flowModeSystem != null) flowModeSystem.enabled = false;
            if (staffSystem != null) staffSystem.enabled = false;
            if (dodgingSystem != null) dodgingSystem.enabled = false;
        }
        
        private void ConfigureFlowModeForScene(SceneConfiguration config)
        {
            if (flowModeSystem != null)
            {
                flowModeSystem.enabled = true;
                
                // Configure spawn area
                if (flowModeSystem.flowOrigin != null)
                {
                    // Adjust spawn bounds based on scene
                    Vector3 spawnArea = config.flowModeSpawnArea;
                    // flowModeSystem.SetSpawnBounds(spawnArea);
                }
                
                Debug.Log($"🌊 Flow Mode configured for {config.sceneName}");
            }
        }
        
        private void ConfigureStaffModeForScene(SceneConfiguration config)
        {
            if (staffSystem != null)
            {
                staffSystem.enabled = true;
                
                // Configure physics based on scene
                if (config.atmosphericDensity > 1f)
                {
                    // Increase drag for underwater/thick atmosphere
                    // staffSystem.SetAtmosphericDrag(config.atmosphericDensity);
                }
                
                Debug.Log($"🥢 Staff Mode configured for {config.sceneName}");
            }
        }
        
        private void ConfigureDodgingModeForScene(SceneConfiguration config)
        {
            if (dodgingSystem != null)
            {
                dodgingSystem.enabled = true;
                
                // Adjust dodge mechanics based on gravity
                if (config.gravityMultiplier != 1f)
                {
                    // Adjust dodge timing for different gravity
                    // dodgingSystem.SetGravityMultiplier(config.gravityMultiplier);
                }
                
                Debug.Log($"🤸 Dodging Mode configured for {config.sceneName}");
            }
        }
        
        private void ConfigureAICoachForScene(SceneConfiguration config)
        {
            if (aiCoach != null)
            {
                aiCoach.enabled = true;
                
                // Adjust coach behavior based on scene
                // aiCoach.SetSceneContext(config.sceneName);
                
                Debug.Log($"🤖 AI Coach configured for {config.sceneName}");
            }
        }
        
        private void ConfigureTraditionalModeForScene(SceneConfiguration config)
        {
            if (rhythmSystem != null)
            {
                rhythmSystem.enabled = true;
                Debug.Log($"🥊 Traditional Mode configured for {config.sceneName}");
            }
        }
        
        public bool IsGameModeSupported(GameMode gameMode, int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= sceneConfigs.Length) return false;
            
            var config = sceneConfigs[sceneIndex];
            
            return gameMode switch
            {
                GameMode.Flow => config.supportsFlowMode,
                GameMode.Staff => config.supportsStaffMode,
                GameMode.Dodging => config.supportsDodgingMode,
                GameMode.AICoach => config.supportsAICoach,
                GameMode.Traditional => true,
                _ => false
            };
        }
        
        public SceneConfiguration GetCurrentSceneConfig()
        {
            if (currentSceneIndex >= 0 && currentSceneIndex < sceneConfigs.Length)
            {
                return sceneConfigs[currentSceneIndex];
            }
            return new SceneConfiguration();
        }
        
        public GameMode GetCurrentGameMode()
        {
            return currentGameMode;
        }
        
        public string[] GetSupportedGameModes(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= sceneConfigs.Length) 
                return new string[] { "Traditional" };
            
            var config = sceneConfigs[sceneIndex];
            var supported = new List<string> { "Traditional" };
            
            if (config.supportsFlowMode) supported.Add("Flow");
            if (config.supportsStaffMode) supported.Add("Staff");
            if (config.supportsDodgingMode) supported.Add("Dodging");
            if (config.supportsAICoach) supported.Add("AI Coach");
            
            return supported.ToArray();
        }
    }
} 