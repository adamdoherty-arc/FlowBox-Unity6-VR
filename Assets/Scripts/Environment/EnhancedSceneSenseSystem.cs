using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using VRBoxingGame.Audio;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Enhanced SceneSense System for Unity 6 VR Environmental Storytelling
    /// Features: Atmospheric coherence, environmental narrative, progressive revelation
    /// Enhances player immersion through intelligent scene adaptation
    /// </summary>
    public class EnhancedSceneSenseSystem : MonoBehaviour
    {
        [Header("Environmental Storytelling")]
        public SceneNarrativeType currentNarrative = SceneNarrativeType.ProfessionalArena;
        public bool enableEnvironmentalStorytelling = true;
        public bool enableProgressiveRevealation = true;
        public float narrativeIntensity = 1.0f;
        
        [Header("Atmosphere Control")]
        public bool enableDynamicAtmosphere = true;
        public bool enablePerformanceReactivity = true;
        public float atmosphereTransitionSpeed = 2.0f;
        public AnimationCurve atmosphereResponseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Unity 6 Lighting System")]
        public Light primaryDirectionalLight;
        public Volume globalVolume;
        public bool enableVolumetricFog = true;
        public bool enableDynamicLighting = true;
        public bool enableRealtimeReflections = true;
        
        [Header("Particle & Effects")]
        public ParticleSystem[] atmosphericParticles;
        public bool enableAdvancedParticles = true;
        public int maxParticleCount = 10000;
        public bool enableGPUParticles = true;
        
        [Header("Audio Integration")]
        public bool enableSpatialAudio = true;
        public bool enableEnvironmentalReverb = true;
        public float audioReactivitySensitivity = 1.0f;
        public AudioReverbPreset[] sceneReverbPresets;
        
        [Header("Performance Monitoring")]
        public bool enablePerformanceAdaptation = true;
        public int targetFrameRate = 90;
        public float qualityAdjustmentThreshold = 0.8f;
        
        // Scene Narrative Types
        public enum SceneNarrativeType
        {
            ProfessionalArena,
            ElementalStorm,
            CyberpunkMetropolis,
            CosmicObservatory,
            ResonantCrystalCaverns,
            AbyssalSymphony,
            MirageOasis,
            EnchantedGrove
        }
        
        // Environmental Atmosphere Data
        [System.Serializable]
        public struct AtmosphereProfile
        {
            public string profileName;
            public Color ambientColor;
            public Color fogColor;
            public float fogDensity;
            public float lightIntensity;
            public float particleDensity;
            public AudioReverbPreset reverbPreset;
            public Gradient colorProgression;
        }
        
        // Performance tracking
        private struct PerformanceMetrics
        {
            public float averageFrameTime;
            public float currentFPS;
            public int visibleObjects;
            public float memoryUsage;
            public float gpuMemoryUsage;
        }
        
        // Private Variables
        private AtmosphereProfile currentAtmosphere;
        private AtmosphereProfile targetAtmosphere;
        private PerformanceMetrics performanceMetrics;
        private float playerPerformanceScore = 0.5f;
        private float environmentalComplexity = 1.0f;
        private bool isTransitioning = false;
        
        // Component References
        private SceneLoadingManager sceneManager;
        private AdvancedAudioManager audioManager;
        private VolumetricFog volumetricFogComponent;
        private HDAdditionalLightData hdLightData;
        
        // Atmosphere Profiles for Each Scene
        private Dictionary<SceneNarrativeType, AtmosphereProfile> atmosphereProfiles;
        
        // Native Arrays for Performance
        private NativeArray<float> performanceHistory;
        private NativeArray<float> atmosphereValues;
        
        // Singleton
        public static EnhancedSceneSenseSystem Instance { get; private set; }
        
        // Properties
        public float PlayerPerformanceScore => playerPerformanceScore;
        public float EnvironmentalComplexity => environmentalComplexity;
        public SceneNarrativeType CurrentNarrative => currentNarrative;
        public bool IsAtmosphereTransitioning => isTransitioning;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSceneSenseSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSceneSenseSystem()
        {
            Debug.Log("🎭 Initializing Enhanced SceneSense System...");
            
            // Find component references
            sceneManager = FindObjectOfType<SceneLoadingManager>();
            audioManager = FindObjectOfType<AdvancedAudioManager>();
            
            // Initialize lighting system
            InitializeLightingSystem();
            
            // Initialize atmosphere profiles
            InitializeAtmosphereProfiles();
            
            // Initialize native arrays
            performanceHistory = new NativeArray<float>(60, Allocator.Persistent); // 1 second at 60fps
            atmosphereValues = new NativeArray<float>(10, Allocator.Persistent);
            
            // Set initial atmosphere
            SetSceneNarrative(currentNarrative);
            
            Debug.Log("✅ Enhanced SceneSense System initialized!");
        }
        
        private void InitializeLightingSystem()
        {
            // Setup primary directional light
            if (primaryDirectionalLight == null)
            {
                primaryDirectionalLight = FindObjectOfType<Light>();
            }
            
            if (primaryDirectionalLight != null)
            {
                hdLightData = primaryDirectionalLight.GetComponent<HDAdditionalLightData>();
                if (hdLightData == null)
                {
                    hdLightData = primaryDirectionalLight.gameObject.AddComponent<HDAdditionalLightData>();
                }
            }
            
            // Setup volumetric fog
            if (globalVolume != null)
            {
                VolumetricFog volumetricFog;
                if (globalVolume.profile.TryGet(out volumetricFog))
                {
                    volumetricFogComponent = volumetricFog;
                }
            }
            
            Debug.Log("🌅 Lighting system initialized with HDRP features");
        }
        
        private void InitializeAtmosphereProfiles()
        {
            atmosphereProfiles = new Dictionary<SceneNarrativeType, AtmosphereProfile>();
            
            // Professional Arena
            atmosphereProfiles[SceneNarrativeType.ProfessionalArena] = new AtmosphereProfile
            {
                profileName = "Championship Arena",
                ambientColor = new Color(1.0f, 0.95f, 0.8f, 1.0f),
                fogColor = new Color(0.8f, 0.9f, 1.0f, 1.0f),
                fogDensity = 0.1f,
                lightIntensity = 1.2f,
                particleDensity = 0.3f,
                reverbPreset = AudioReverbPreset.Arena
            };
            
            // Elemental Storm
            atmosphereProfiles[SceneNarrativeType.ElementalStorm] = new AtmosphereProfile
            {
                profileName = "Elemental Fury",
                ambientColor = new Color(0.4f, 0.5f, 0.8f, 1.0f),
                fogColor = new Color(0.3f, 0.4f, 0.6f, 1.0f),
                fogDensity = 0.8f,
                lightIntensity = 2.0f,
                particleDensity = 1.5f,
                reverbPreset = AudioReverbPreset.Cave
            };
            
            // Cyberpunk Metropolis
            atmosphereProfiles[SceneNarrativeType.CyberpunkMetropolis] = new AtmosphereProfile
            {
                profileName = "Digital Underground",
                ambientColor = new Color(0.8f, 0.2f, 1.0f, 1.0f),
                fogColor = new Color(0.6f, 0.8f, 1.0f, 1.0f),
                fogDensity = 0.4f,
                lightIntensity = 0.8f,
                particleDensity = 0.7f,
                reverbPreset = AudioReverbPreset.City
            };
            
            // Cosmic Observatory
            atmosphereProfiles[SceneNarrativeType.CosmicObservatory] = new AtmosphereProfile
            {
                profileName = "Cosmic Symphony",
                ambientColor = new Color(0.1f, 0.1f, 0.3f, 1.0f),
                fogColor = new Color(0.2f, 0.1f, 0.4f, 1.0f),
                fogDensity = 0.05f,
                lightIntensity = 0.5f,
                particleDensity = 0.2f,
                reverbPreset = AudioReverbPreset.Off
            };
            
            // Resonant Crystal Caverns
            atmosphereProfiles[SceneNarrativeType.ResonantCrystalCaverns] = new AtmosphereProfile
            {
                profileName = "Harmonic Resonance",
                ambientColor = new Color(0.8f, 0.9f, 1.0f, 1.0f),
                fogColor = new Color(0.9f, 0.8f, 1.0f, 1.0f),
                fogDensity = 0.3f,
                lightIntensity = 1.5f,
                particleDensity = 0.8f,
                reverbPreset = AudioReverbPreset.Cave
            };
            
            // Abyssal Symphony
            atmosphereProfiles[SceneNarrativeType.AbyssalSymphony] = new AtmosphereProfile
            {
                profileName = "Ocean's Memory",
                ambientColor = new Color(0.2f, 0.4f, 0.8f, 1.0f),
                fogColor = new Color(0.1f, 0.3f, 0.6f, 1.0f),
                fogDensity = 1.2f,
                lightIntensity = 0.6f,
                particleDensity = 1.0f,
                reverbPreset = AudioReverbPreset.Underwater
            };
            
            // Mirage Oasis
            atmosphereProfiles[SceneNarrativeType.MirageOasis] = new AtmosphereProfile
            {
                profileName = "Desert Dreams",
                ambientColor = new Color(1.0f, 0.8f, 0.6f, 1.0f),
                fogColor = new Color(1.0f, 0.9f, 0.7f, 1.0f),
                fogDensity = 0.2f,
                lightIntensity = 1.8f,
                particleDensity = 0.5f,
                reverbPreset = AudioReverbPreset.Plain
            };
            
            // Enchanted Grove
            atmosphereProfiles[SceneNarrativeType.EnchantedGrove] = new AtmosphereProfile
            {
                profileName = "Living Symphony",
                ambientColor = new Color(0.6f, 0.9f, 0.4f, 1.0f),
                fogColor = new Color(0.7f, 1.0f, 0.6f, 1.0f),
                fogDensity = 0.4f,
                lightIntensity = 1.0f,
                particleDensity = 1.2f,
                reverbPreset = AudioReverbPreset.Forest
            };
            
            Debug.Log($"🎨 Initialized {atmosphereProfiles.Count} atmosphere profiles");
        }
        
        private void Update()
        {
            if (!enableDynamicAtmosphere) return;
            
            // Update performance metrics
            UpdatePerformanceMetrics();
            
            // Update player performance tracking
            UpdatePlayerPerformance();
            
            // Update environmental complexity
            UpdateEnvironmentalComplexity();
            
            // Process atmosphere transitions
            ProcessAtmosphereTransitions();
            
            // Update reactive environment elements
            if (enablePerformanceReactivity)
            {
                UpdateReactiveEnvironment();
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            // Track frame time and FPS
            performanceMetrics.averageFrameTime = Time.smoothDeltaTime;
            performanceMetrics.currentFPS = 1.0f / Time.smoothDeltaTime;
            
            // Estimate memory usage
            performanceMetrics.memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(0) / (1024f * 1024f);
            
            // Update performance history
            for (int i = 0; i < performanceHistory.Length - 1; i++)
            {
                performanceHistory[i] = performanceHistory[i + 1];
            }
            performanceHistory[performanceHistory.Length - 1] = performanceMetrics.currentFPS;
        }
        
        private void UpdatePlayerPerformance()
        {
            // Get performance data from boxing systems
            if (VRBoxingGame.Boxing.RhythmTargetSystem.Instance != null)
            {
                float hitAccuracy = VRBoxingGame.Boxing.RhythmTargetSystem.Instance.HitAccuracy;
                float combo = VRBoxingGame.Boxing.RhythmTargetSystem.Instance.CurrentCombo;
                
                // Calculate performance score (0.0 to 1.0)
                playerPerformanceScore = Mathf.Lerp(playerPerformanceScore, 
                    (hitAccuracy * 0.7f + Mathf.Clamp01(combo / 10f) * 0.3f), 
                    Time.deltaTime * 2.0f);
            }
        }
        
        private void UpdateEnvironmentalComplexity()
        {
            if (!enableProgressiveRevealation) return;
            
            // Calculate target complexity based on performance
            float targetComplexity = Mathf.Lerp(0.5f, 2.0f, playerPerformanceScore);
            
            // Apply performance adaptation
            if (enablePerformanceAdaptation && performanceMetrics.currentFPS < targetFrameRate * qualityAdjustmentThreshold)
            {
                targetComplexity *= 0.8f; // Reduce complexity if performance is poor
            }
            
            // Smooth complexity transition
            environmentalComplexity = Mathf.Lerp(environmentalComplexity, targetComplexity, Time.deltaTime * 0.5f);
        }
        
        private void ProcessAtmosphereTransitions()
        {
            if (!isTransitioning) return;
            
            // Lerp current atmosphere toward target
            float transitionSpeed = atmosphereTransitionSpeed * Time.deltaTime;
            float t = atmosphereResponseCurve.Evaluate(transitionSpeed);
            
            // Interpolate atmosphere values
            currentAtmosphere.ambientColor = Color.Lerp(currentAtmosphere.ambientColor, targetAtmosphere.ambientColor, t);
            currentAtmosphere.fogColor = Color.Lerp(currentAtmosphere.fogColor, targetAtmosphere.fogColor, t);
            currentAtmosphere.fogDensity = Mathf.Lerp(currentAtmosphere.fogDensity, targetAtmosphere.fogDensity, t);
            currentAtmosphere.lightIntensity = Mathf.Lerp(currentAtmosphere.lightIntensity, targetAtmosphere.lightIntensity, t);
            currentAtmosphere.particleDensity = Mathf.Lerp(currentAtmosphere.particleDensity, targetAtmosphere.particleDensity, t);
            
            // Apply atmosphere to scene
            ApplyAtmosphereToScene();
            
            // Check if transition is complete
            if (Vector3.Distance(currentAtmosphere.ambientColor, targetAtmosphere.ambientColor) < 0.01f)
            {
                currentAtmosphere = targetAtmosphere;
                isTransitioning = false;
                Debug.Log($"🎭 Atmosphere transition to {targetAtmosphere.profileName} complete");
            }
        }
        
        private void UpdateReactiveEnvironment()
        {
            // React to music beat strength
            if (audioManager != null)
            {
                float beatStrength = audioManager.GetCurrentBeatStrength();
                float musicEnergy = audioManager.GetCurrentEnergyLevel();
                
                // Modify lighting intensity based on music
                if (enableDynamicLighting && hdLightData != null)
                {
                    float baseLightIntensity = currentAtmosphere.lightIntensity;
                    float reactiveIntensity = baseLightIntensity * (1.0f + beatStrength * 0.3f * narrativeIntensity);
                    hdLightData.intensity = reactiveIntensity;
                }
                
                // Modify particle density based on music energy
                if (enableAdvancedParticles)
                {
                    UpdateParticleReactivity(musicEnergy, beatStrength);
                }
                
                // Modify fog density based on performance
                if (volumetricFogComponent != null)
                {
                    float performanceModifier = Mathf.Lerp(0.8f, 1.2f, playerPerformanceScore);
                    volumetricFogComponent.meanFreePath.value = currentAtmosphere.fogDensity * performanceModifier;
                }
            }
        }
        
        private void UpdateParticleReactivity(float musicEnergy, float beatStrength)
        {
            foreach (var particleSystem in atmosphericParticles)
            {
                if (particleSystem == null) continue;
                
                var emission = particleSystem.emission;
                var velocityOverLifetime = particleSystem.velocityOverLifetime;
                
                // Adjust emission rate based on music energy
                float baseEmissionRate = currentAtmosphere.particleDensity * 100f * environmentalComplexity;
                float reactiveEmissionRate = baseEmissionRate * (1.0f + musicEnergy * 0.5f);
                emission.rateOverTime = reactiveEmissionRate;
                
                // Adjust particle velocity based on beat strength
                if (velocityOverLifetime.enabled)
                {
                    float velocityMultiplier = 1.0f + beatStrength * 0.3f;
                    velocityOverLifetime.speedModifier = velocityMultiplier;
                }
            }
        }
        
        private void ApplyAtmosphereToScene()
        {
            // Apply ambient lighting
            RenderSettings.ambientLight = currentAtmosphere.ambientColor;
            
            // Apply fog settings
            RenderSettings.fog = true;
            RenderSettings.fogColor = currentAtmosphere.fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = currentAtmosphere.fogDensity * environmentalComplexity;
            
            // Apply lighting intensity
            if (primaryDirectionalLight != null)
            {
                primaryDirectionalLight.intensity = currentAtmosphere.lightIntensity * environmentalComplexity;
            }
            
            // Apply audio reverb
            if (enableEnvironmentalReverb && audioManager != null)
            {
                audioManager.SetGlobalReverb(currentAtmosphere.reverbPreset);
            }
        }
        
        // Public API
        public void SetSceneNarrative(SceneNarrativeType narrative)
        {
            if (atmosphereProfiles.ContainsKey(narrative))
            {
                currentNarrative = narrative;
                targetAtmosphere = atmosphereProfiles[narrative];
                isTransitioning = true;
                
                Debug.Log($"🎭 Setting scene narrative to: {targetAtmosphere.profileName}");
            }
        }
        
        public void SetNarrativeIntensity(float intensity)
        {
            narrativeIntensity = Mathf.Clamp01(intensity);
        }
        
        // **BUG FIX**: Add missing SetImmersiveMode method called by EnhancedMainMenuSystem
        public void SetImmersiveMode(bool immersive)
        {
            // Adjust narrative intensity based on immersive mode
            if (immersive)
            {
                narrativeIntensity = Mathf.Max(narrativeIntensity, 0.8f); // Ensure high intensity for immersive mode
                enableEnvironmentalStorytelling = true;
                enableProgressiveRevealation = true;
                
                Debug.Log("🎭 Enhanced Scene Sense: IMMERSIVE MODE activated - Full narrative storytelling enabled");
            }
            else
            {
                narrativeIntensity = Mathf.Min(narrativeIntensity, 0.3f); // Lower intensity for normal mode
                enableEnvironmentalStorytelling = false;
                enableProgressiveRevealation = false;
                
                Debug.Log("🎯 Enhanced Scene Sense: NORMAL MODE activated - Traditional experience enabled");
            }
            
            // Update atmosphere to reflect mode change
            if (atmosphereProfiles.ContainsKey(currentNarrative))
            {
                targetAtmosphere = atmosphereProfiles[currentNarrative];
                isTransitioning = true;
            }
        }
        
        public void TriggerEnvironmentalEvent(EnvironmentalEventType eventType, float intensity = 1.0f)
        {
            switch (eventType)
            {
                case EnvironmentalEventType.LightningStrike:
                    StartCoroutine(LightningStrikeEffect(intensity));
                    break;
                case EnvironmentalEventType.CrystalResonance:
                    StartCoroutine(CrystalResonanceEffect(intensity));
                    break;
                case EnvironmentalEventType.HologramFlicker:
                    StartCoroutine(HologramFlickerEffect(intensity));
                    break;
                case EnvironmentalEventType.BioluminescenceFlash:
                    StartCoroutine(BioluminescenceFlashEffect(intensity));
                    break;
            }
        }
        
        public SceneSenseMetrics GetSceneSenseMetrics()
        {
            return new SceneSenseMetrics
            {
                currentNarrative = currentNarrative.ToString(),
                playerPerformanceScore = playerPerformanceScore,
                environmentalComplexity = environmentalComplexity,
                currentFPS = performanceMetrics.currentFPS,
                memoryUsage = performanceMetrics.memoryUsage,
                atmosphereTransitionActive = isTransitioning,
                narrativeIntensity = narrativeIntensity
            };
        }
        
        // Environmental Events
        public enum EnvironmentalEventType
        {
            LightningStrike,
            CrystalResonance,
            HologramFlicker,
            BioluminescenceFlash,
            StarFormation,
            DesertMirage,
            ForestBloom
        }
        
        private System.Collections.IEnumerator LightningStrikeEffect(float intensity)
        {
            if (hdLightData != null)
            {
                float originalIntensity = hdLightData.intensity;
                hdLightData.intensity = originalIntensity * (1.0f + intensity * 3.0f);
                yield return new WaitForSeconds(0.1f);
                hdLightData.intensity = originalIntensity;
            }
        }
        
        private System.Collections.IEnumerator CrystalResonanceEffect(float intensity)
        {
            // Create expanding wave of crystal resonance
            foreach (var particleSystem in atmosphericParticles)
            {
                if (particleSystem != null)
                {
                    var emission = particleSystem.emission;
                    float originalRate = emission.rateOverTime.constant;
                    emission.rateOverTime = originalRate * (1.0f + intensity * 2.0f);
                    yield return new WaitForSeconds(0.5f);
                    emission.rateOverTime = originalRate;
                }
            }
        }
        
        private System.Collections.IEnumerator HologramFlickerEffect(float intensity)
        {
            // Flicker effect for cyberpunk scenes
            for (int i = 0; i < 5; i++)
            {
                if (hdLightData != null)
                {
                    hdLightData.intensity *= 0.5f;
                    yield return new WaitForSeconds(0.05f * intensity);
                    hdLightData.intensity *= 2.0f;
                    yield return new WaitForSeconds(0.1f * intensity);
                }
            }
        }
        
        private System.Collections.IEnumerator BioluminescenceFlashEffect(float intensity)
        {
            // Underwater bioluminescence flash
            Color originalAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = Color.Lerp(originalAmbient, Color.cyan, intensity * 0.5f);
            yield return new WaitForSeconds(0.3f);
            RenderSettings.ambientLight = originalAmbient;
        }
        
        private void OnDestroy()
        {
            // Dispose native arrays
            if (performanceHistory.IsCreated) performanceHistory.Dispose();
            if (atmosphereValues.IsCreated) atmosphereValues.Dispose();
        }
        
        [System.Serializable]
        public struct SceneSenseMetrics
        {
            public string currentNarrative;
            public float playerPerformanceScore;
            public float environmentalComplexity;
            public float currentFPS;
            public float memoryUsage;
            public bool atmosphereTransitionActive;
            public float narrativeIntensity;
        }
    }
} 