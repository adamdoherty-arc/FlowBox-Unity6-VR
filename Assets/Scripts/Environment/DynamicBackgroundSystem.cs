using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using VRBoxingGame.Audio;
using System.Collections;
using System.Threading.Tasks;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Dynamic HD Background System that reacts to music and gameplay
    /// </summary>
    public class DynamicBackgroundSystem : MonoBehaviour
    {
        [Header("Background Themes")]
        public BackgroundTheme[] availableThemes = new BackgroundTheme[]
        {
            new BackgroundTheme 
            { 
                themeName = "Cyberpunk City",
                type = BackgroundType.Cyberpunk,
                ambientColor = new Color(0.2f, 0.1f, 0.3f),
                fogColor = new Color(0.8f, 0.2f, 0.4f),
                fogDensity = 0.02f
            },
            new BackgroundTheme 
            { 
                themeName = "Space Station",
                type = BackgroundType.Space,
                ambientColor = new Color(0.05f, 0.05f, 0.2f),
                fogColor = new Color(0.1f, 0.1f, 0.3f),
                fogDensity = 0.001f
            },
            new BackgroundTheme 
            { 
                themeName = "Abstract Geometry",
                type = BackgroundType.Abstract,
                ambientColor = new Color(0.3f, 0.3f, 0.3f),
                fogColor = new Color(0.5f, 0.5f, 0.8f),
                fogDensity = 0.01f
            },
            new BackgroundTheme 
            { 
                themeName = "Crystal Cave",
                type = BackgroundType.Crystal,
                ambientColor = new Color(0.4f, 0.3f, 0.5f),
                fogColor = new Color(0.6f, 0.4f, 0.8f),
                fogDensity = 0.015f
            },
            new BackgroundTheme 
            { 
                themeName = "Aurora Fields",
                type = BackgroundType.Aurora,
                ambientColor = new Color(0.2f, 0.4f, 0.3f),
                fogColor = new Color(0.3f, 0.8f, 0.5f),
                fogDensity = 0.008f
            },
            new BackgroundTheme 
            { 
                themeName = "Underwater Realm",
                type = BackgroundType.Underwater,
                ambientColor = new Color(0.1f, 0.3f, 0.4f),
                fogColor = new Color(0.2f, 0.6f, 0.8f),
                fogDensity = 0.025f
            }
        };
        public int currentThemeIndex = 0;
        public bool autoSwitchThemes = false;
        public float themeSwitchInterval = 60f;
        
        [Header("Music Reactivity")]
        public bool reactToMusic = true;
        public Light mainLight;
        public float musicReactivityStrength = 1f;
        public float bassReactivity = 2f;
        public float trebleReactivity = 1.5f;
        
        [Header("Performance")]
        public bool enableDynamicLOD = true;
        public float lodDistance = 20f;
        public bool enableJobSystemEffects = true;
        
        [Header("Lighting")]
        public Light mainLight;
        public Light[] accentLights;
        public AnimationCurve lightIntensityCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 2f);
        
        [System.Serializable]
        public struct BackgroundTheme
        {
            public string themeName;
            public GameObject environmentPrefab;
            public Material skyboxMaterial;
            public Color ambientColor;
            public Color fogColor;
            public float fogDensity;
            public ParticleSystem[] particleEffects;
            public AudioClip ambientSound;
            public BackgroundType type;
        }
        
        public enum BackgroundType
        {
            Cyberpunk,
            Space,
            Abstract,
            Crystal,
            Aurora,
            Underwater,
            Energy,
            Minimal
        }
        
        // Private variables
        private GameObject currentEnvironment;
        private BackgroundTheme currentTheme;
        private float lastThemeSwitchTime;
        
        // Music reaction data
        private float[] frequencyBands = new float[8];
        private float bassEnergy = 0f;
        private float midEnergy = 0f;
        private float trebleEnergy = 0f;
        
        // Job System data for effects
        private NativeArray<float3> particlePositions;
        private NativeArray<float3> particleVelocities;
        private NativeArray<float> particleLifetimes;
        private JobHandle currentJobHandle;
        
        // Dynamic effects
        private List<ParticleSystem> activeParticles = new List<ParticleSystem>();
        private List<Transform> reactiveObjects = new List<Transform>();
        
        // Singleton
        public static DynamicBackgroundSystem Instance { get; private set; }
        
        // Properties
        public BackgroundTheme CurrentTheme => currentTheme;
        public int ThemeCount => availableThemes.Length;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeBackgroundSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeBackgroundSystem()
        {
            // Initialize Job System arrays
            if (enableJobSystemEffects)
            {
                int maxParticles = 1000;
                particlePositions = new NativeArray<float3>(maxParticles, Allocator.Persistent);
                particleVelocities = new NativeArray<float3>(maxParticles, Allocator.Persistent);
                particleLifetimes = new NativeArray<float>(maxParticles, Allocator.Persistent);
            }
            
            // Subscribe to audio events
            if (AdvancedAudioManager.Instance != null)
            {
                AdvancedAudioManager.Instance.OnAudioAnalysis.AddListener(OnAudioAnalysis);
                AdvancedAudioManager.Instance.OnBeatDetected.AddListener(OnBeatDetected);
            }
            
            // Load initial theme
            if (availableThemes.Length > 0)
            {
                LoadTheme(currentThemeIndex);
            }
            
            Debug.Log("Dynamic Background System initialized");
        }
        
        private void Update()
        {
            if (autoSwitchThemes && Time.time - lastThemeSwitchTime > themeSwitchInterval)
            {
                SwitchToNextTheme();
            }
            
            if (reactToMusic)
            {
                UpdateMusicReactivity();
            }
            
            if (enableJobSystemEffects)
            {
                UpdateEffectsWithJobs();
            }
        }
        
        private void OnAudioAnalysis(AdvancedAudioManager.AudioAnalysisData audioData)
        {
            if (!reactToMusic) return;
            
            frequencyBands = audioData.frequencyBands;
            bassEnergy = audioData.bassEnergy;
            midEnergy = audioData.midEnergy;
            trebleEnergy = audioData.trebleEnergy;
        }
        
        private void OnBeatDetected(AdvancedAudioManager.BeatData beatData)
        {
            if (!reactToMusic) return;
            
            // Trigger beat-reactive effects
            if (beatData.isStrongBeat)
            {
                TriggerBeatEffect(beatData.intensity);
            }
        }
        
        private void UpdateMusicReactivity()
        {
            // Update lighting based on music
            UpdateReactiveLighting();
            
            // Update particle effects
            UpdateReactiveParticles();
            
            // Update environment objects
            UpdateReactiveObjects();
        }
        
        private void UpdateReactiveLighting()
        {
            if (mainLight == null) return;
            
            // Modulate main light intensity based on total energy
            float totalEnergy = (bassEnergy + midEnergy + trebleEnergy) / 3f;
            float targetIntensity = lightIntensityCurve.Evaluate(totalEnergy * musicReactivityStrength);
            mainLight.intensity = Mathf.Lerp(mainLight.intensity, targetIntensity, Time.deltaTime * 5f);
            
            // Update accent lights with frequency bands
            for (int i = 0; i < accentLights.Length && i < frequencyBands.Length; i++)
            {
                if (accentLights[i] != null)
                {
                    float bandEnergy = frequencyBands[i] * musicReactivityStrength;
                    accentLights[i].intensity = Mathf.Lerp(accentLights[i].intensity, bandEnergy, Time.deltaTime * 10f);
                    
                    // Color shift based on frequency
                    Color targetColor = Color.HSVToRGB((float)i / frequencyBands.Length, 0.8f, 1f);
                    accentLights[i].color = Color.Lerp(accentLights[i].color, targetColor, Time.deltaTime * 2f);
                }
            }
        }
        
        private void UpdateReactiveParticles()
        {
            foreach (var particles in activeParticles)
            {
                if (particles == null) continue;
                
                var emission = particles.emission;
                var main = particles.main;
                
                // Modulate emission rate based on bass energy
                float targetRate = bassEnergy * bassReactivity * 50f;
                emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, targetRate, Time.deltaTime * 5f);
                
                // Modulate particle speed based on treble energy
                float targetSpeed = trebleEnergy * trebleReactivity * 5f;
                main.startSpeed = Mathf.Lerp(main.startSpeed.constant, targetSpeed, Time.deltaTime * 3f);
            }
        }
        
        private void UpdateReactiveObjects()
        {
            for (int i = 0; i < reactiveObjects.Count; i++)
            {
                if (reactiveObjects[i] == null) continue;
                
                // Scale objects based on frequency bands
                int bandIndex = i % frequencyBands.Length;
                float energy = frequencyBands[bandIndex];
                float targetScale = 1f + (energy * musicReactivityStrength * 0.5f);
                
                Vector3 currentScale = reactiveObjects[i].localScale;
                Vector3 targetScaleVector = Vector3.one * targetScale;
                reactiveObjects[i].localScale = Vector3.Lerp(currentScale, targetScaleVector, Time.deltaTime * 8f);
                
                // Rotate objects based on music
                float rotationSpeed = energy * musicReactivityStrength * 50f;
                reactiveObjects[i].Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
        }
        
        private void TriggerBeatEffect(float intensity)
        {
            // Pulse main light
            if (mainLight != null)
            {
                _ = PulseLightEffectAsync(intensity);
            }
            
            // Trigger particle burst
            foreach (var particle in activeParticles)
            {
                if (particle != null)
                {
                    var emission = particle.emission;
                    emission.SetBursts(new ParticleSystem.Burst[]
                    {
                        new ParticleSystem.Burst(0.0f, (short)(50 * intensity))
                    });
                }
            }
        }
        
        private async Task PulseLightEffectAsync(float intensity)
        {
            try
            {
                if (mainLight == null) return;
                
                float originalIntensity = mainLight.intensity;
                float targetIntensity = originalIntensity * lightIntensityCurve.Evaluate(intensity);
                float duration = 0.3f;
                float elapsedTime = 0f;
                
                // Pulse up
                while (elapsedTime < duration * 0.5f)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / (duration * 0.5f);
                    mainLight.intensity = Mathf.Lerp(originalIntensity, targetIntensity, progress);
                    await Task.Yield();
                }
                
                // Pulse down
                elapsedTime = 0f;
                while (elapsedTime < duration * 0.5f)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / (duration * 0.5f);
                    mainLight.intensity = Mathf.Lerp(targetIntensity, originalIntensity, progress);
                    await Task.Yield();
                }
                
                mainLight.intensity = originalIntensity;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in light pulse effect: {ex.Message}");
            }
        }
        
        // Unity 6 Job System for particle effects
        [BurstCompile]
        public struct ParticleUpdateJob : IJobParallelFor
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float musicEnergy;
            
            public NativeArray<float3> positions;
            public NativeArray<float3> velocities;
            public NativeArray<float> lifetimes;
            
            public void Execute(int index)
            {
                if (lifetimes[index] <= 0) return;
                
                // Update position
                positions[index] += velocities[index] * deltaTime;
                
                // Update velocity with music influence
                velocities[index] *= (1f + musicEnergy * 0.1f);
                
                // Update lifetime
                lifetimes[index] -= deltaTime;
            }
        }
        
        private void UpdateEffectsWithJobs()
        {
            // Complete previous job
            currentJobHandle.Complete();
            
            // Schedule particle update job
            var particleJob = new ParticleUpdateJob
            {
                deltaTime = Time.deltaTime,
                musicEnergy = (bassEnergy + midEnergy + trebleEnergy) / 3f,
                positions = particlePositions,
                velocities = particleVelocities,
                lifetimes = particleLifetimes
            };
            
            currentJobHandle = particleJob.Schedule(particlePositions.Length, 64);
        }
        
        public void LoadTheme(int themeIndex)
        {
            if (themeIndex < 0 || themeIndex >= availableThemes.Length) return;
            
            // Unload current theme
            if (currentEnvironment != null)
            {
                DestroyImmediate(currentEnvironment);
            }
            
            currentTheme = availableThemes[themeIndex];
            currentThemeIndex = themeIndex;
            
            // Load new environment
            if (currentTheme.environmentPrefab != null)
            {
                currentEnvironment = Instantiate(currentTheme.environmentPrefab, transform);
                CollectReactiveObjects();
            }
            
            // Update skybox
            if (currentTheme.skyboxMaterial != null)
            {
                RenderSettings.skybox = currentTheme.skyboxMaterial;
            }
            
            // Update lighting
            RenderSettings.ambientLight = currentTheme.ambientColor;
            RenderSettings.fog = currentTheme.fogDensity > 0;
            RenderSettings.fogColor = currentTheme.fogColor;
            RenderSettings.fogDensity = currentTheme.fogDensity;
            
            // Setup particle effects
            SetupParticleEffects();
            
            // Play ambient sound
            if (currentTheme.ambientSound != null && AdvancedAudioManager.Instance != null)
            {
                AdvancedAudioManager.Instance.PlaySFX(currentTheme.ambientSound, Vector3.zero, 0.3f);
            }
            
            lastThemeSwitchTime = Time.time;
            
            Debug.Log($"Loaded background theme: {currentTheme.themeName}");
        }
        
        private void CollectReactiveObjects()
        {
            reactiveObjects.Clear();
            
            if (currentEnvironment == null) return;
            
            // Find objects tagged as "Reactive"
            GameObject[] reactiveGameObjects = GameObject.FindGameObjectsWithTag("Reactive");
            foreach (var obj in reactiveGameObjects)
            {
                reactiveObjects.Add(obj.transform);
            }
            
            // Also collect objects with specific components
            var reactiveComponents = currentEnvironment.GetComponentsInChildren<ReactiveEnvironmentObject>();
            foreach (var component in reactiveComponents)
            {
                reactiveObjects.Add(component.transform);
            }
        }
        
        private void SetupParticleEffects()
        {
            activeParticles.Clear();
            
            // Add theme-specific particles
            if (currentTheme.particleEffects != null)
            {
                foreach (var particleSystem in currentTheme.particleEffects)
                {
                    if (particleSystem != null)
                    {
                        var instance = Instantiate(particleSystem, transform);
                        activeParticles.Add(instance);
                    }
                }
            }
            
            // Add particles from current environment
            if (currentEnvironment != null)
            {
                var environmentParticles = currentEnvironment.GetComponentsInChildren<ParticleSystem>();
                activeParticles.AddRange(environmentParticles);
            }
        }
        
        public void SwitchToNextTheme()
        {
            int nextIndex = (currentThemeIndex + 1) % availableThemes.Length;
            LoadTheme(nextIndex);
        }
        
        public void SwitchToPreviousTheme()
        {
            int prevIndex = currentThemeIndex - 1;
            if (prevIndex < 0) prevIndex = availableThemes.Length - 1;
            LoadTheme(prevIndex);
        }
        
        public void SetMusicReactivity(bool enabled)
        {
            reactToMusic = enabled;
        }
        
        public void SetReactivityStrength(float strength)
        {
            musicReactivityStrength = Mathf.Clamp01(strength);
        }
        
        // Create preset themes
        [ContextMenu("Create Cyberpunk Theme")]
        public void CreateCyberpunkTheme()
        {
            // This would be called in editor to set up preset themes
            Debug.Log("Create cyberpunk theme setup");
        }
        
        private void OnDestroy()
        {
            // Clean up Job System resources
            if (enableJobSystemEffects)
            {
                currentJobHandle.Complete();
                
                if (particlePositions.IsCreated) particlePositions.Dispose();
                if (particleVelocities.IsCreated) particleVelocities.Dispose();
                if (particleLifetimes.IsCreated) particleLifetimes.Dispose();
            }
        }
        
        private void OnDrawGizmos()
        {
            // Draw LOD distance
            if (enableDynamicLOD)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, lodDistance);
            }
        }
    }
} 