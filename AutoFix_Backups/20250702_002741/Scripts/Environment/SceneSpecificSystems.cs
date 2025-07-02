using UnityEngine;
using System.Collections.Generic;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;
using System.Collections;
using System.Threading.Tasks;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Crystal Resonance System - Implements harmonic crystal mechanics
    /// </summary>
    public class CrystalResonanceSystem : MonoBehaviour
    {
        [Header("Crystal Settings")]
        public int maxCrystals = 20;
        public float resonanceRadius = 5f;
        public float harmonicFrequency = 440f;
        
        [Header("Audio")]
        public AudioSource crystalAudioSource;
        
        private List<GameObject> activeCrystals = new List<GameObject>();
        private Dictionary<GameObject, float> crystalFrequencies = new Dictionary<GameObject, float>();
        
        public void OnCrystalHit(RhythmTargetSystem.CircleHitData hitData)
        {
            // Create crystal resonance effect
            CreateCrystalResonance(hitData.hitPosition);
            
            // Play harmonic note
            PlayCrystalNote(hitData.circleType);
        }
        
        public void OnCrystalBlocked(RhythmTargetSystem.BlockData blockData)
        {
            // Create crystal cluster shatter effect
            CreateCrystalShatterEffect(blockData.blockPosition);
        }
        
        private void CreateCrystalResonance(Vector3 position)
        {
            // Create resonance wave effect
            GameObject effect = new GameObject("CrystalResonance");
            effect.transform.position = position;
            
            // Add particle system for crystal sparkles
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.8f, 0.4f, 1f, 0.8f);
            main.maxParticles = 50;
            
            var emission = particles.emission;
            emission.rateOverTime = 25;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            
            Destroy(effect, 3f);
        }
        
        private void PlayCrystalNote(RhythmTargetSystem.CircleType circleType)
        {
            if (crystalAudioSource == null) return;
            
            float frequency = circleType == RhythmTargetSystem.CircleType.White ? 440f : 330f;
            // Play crystal chime sound at the specified frequency
            // This would ideally use a procedural audio generator
        }
        
        private void CreateCrystalShatterEffect(Vector3 position)
        {
            GameObject effect = new GameObject("CrystalShatter");
            effect.transform.position = position;
            
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 5f;
            main.startSize = 0.3f;
            main.startColor = new Color(1f, 0.8f, 1f, 1f);
            main.maxParticles = 100;
            
            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 50)
            });
            
            Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// Forest Spirit System - Implements magical forest mechanics
    /// </summary>
    public class ForestSpiritSystem : MonoBehaviour
    {
        [Header("Spirit Settings")]
        public int maxSpirits = 15;
        public float magicalRadius = 8f;
        public float seasonalCycleTime = 30f;
        
        [Header("Nature Response")]
        public float bloomThreshold = 0.8f;
        public GameObject[] flowerPrefabs;
        public GameObject[] treePrefabs;
        
        private List<GameObject> activeSpirits = new List<GameObject>();
        private float seasonalTimer = 0f;
        private int consecutivePerfectHits = 0;
        
        private void Update()
        {
            UpdateSeasonalCycle();
        }
        
        public void OnSpiritHit(RhythmTargetSystem.CircleHitData hitData)
        {
            // Create magical sparkle effect
            CreateMagicalSparkles(hitData.hitPosition);
            
            // Check for perfect hits
            if (hitData.isPerfectTiming)
            {
                consecutivePerfectHits++;
                if (consecutivePerfectHits >= 3)
                {
                    CreateNatureBloom(hitData.hitPosition);
                    consecutivePerfectHits = 0;
                }
            }
            else
            {
                consecutivePerfectHits = 0;
            }
        }
        
        public void OnVineBlocked(RhythmTargetSystem.BlockData blockData)
        {
            // Create vine clearing effect
            CreateVineClearingEffect(blockData.blockPosition);
        }
        
        private void UpdateSeasonalCycle()
        {
            seasonalTimer += Time.deltaTime;
            float cycle = seasonalTimer / seasonalCycleTime;
            
            // Adjust environment based on music tempo
            if (AdvancedAudioManager.Instance != null)
            {
                float bpm = AdvancedAudioManager.Instance.CurrentBPM;
                ApplySeasonalChanges(bpm);
            }
        }
        
        private void ApplySeasonalChanges(float bpm)
        {
            // Slow BPM = Winter, Fast BPM = Summer
            if (bpm < 100f)
            {
                // Winter effects
                SetWinterEnvironment();
            }
            else if (bpm > 140f)
            {
                // Summer effects
                SetSummerEnvironment();
            }
            else
            {
                // Spring/Autumn effects
                SetTransitionEnvironment();
            }
        }
        
        private void SetWinterEnvironment()
        {
            // Reduce particle effects, cooler colors
            RenderSettings.ambientLight = new Color(0.6f, 0.7f, 1f);
        }
        
        private void SetSummerEnvironment()
        {
            // Increase particle effects, warmer colors
            RenderSettings.ambientLight = new Color(1f, 0.9f, 0.6f);
        }
        
        private void SetTransitionEnvironment()
        {
            // Balanced colors
            RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f);
        }
        
        private void CreateMagicalSparkles(Vector3 position)
        {
            GameObject effect = new GameObject("MagicalSparkles");
            effect.transform.position = position;
            
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 2f;
            main.startSize = 0.1f;
            main.startColor = new Color(1f, 1f, 0.5f, 0.8f);
            main.maxParticles = 30;
            
            var emission = particles.emission;
            emission.rateOverTime = 15;
            
            Destroy(effect, 3f);
        }
        
        private void CreateNatureBloom(Vector3 position)
        {
            // Create blooming flowers
            if (flowerPrefabs != null && flowerPrefabs.Length > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 flowerPos = position + Random.insideUnitSphere * 2f;
                    flowerPos.y = Mathf.Max(0f, flowerPos.y);
                    
                    GameObject flower = Instantiate(flowerPrefabs[Random.Range(0, flowerPrefabs.Length)], flowerPos, Quaternion.identity);
                    
                    // Add glow effect
                    Light flowerLight = flower.AddComponent<Light>();
                    flowerLight.type = LightType.Point;
                    flowerLight.color = Color.yellow;
                    flowerLight.intensity = 1f;
                    flowerLight.range = 2f;
                    
                    // Auto-destroy after some time
                    Destroy(flower, 10f);
                }
            }
        }
        
        private void CreateVineClearingEffect(Vector3 position)
        {
            GameObject effect = new GameObject("VineClearing");
            effect.transform.position = position;
            
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1f;
            main.startSpeed = 3f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            main.maxParticles = 40;
            
            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 20)
            });
            
            Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// Desert Mirage System - Implements mirage and sandstorm mechanics
    /// </summary>
    public class DesertMirageSystem : MonoBehaviour
    {
        [Header("Mirage Settings")]
        public float mirageChance = 0.3f;
        public float heatWaveIntensity = 1f;
        public float sandstormRadius = 10f;
        
        [Header("Heat Effects")]
        public float baseTemperature = 1f;
        public float maxTemperature = 3f;
        
        private float currentHeatLevel = 1f;
        private List<GameObject> activeMirages = new List<GameObject>();
        
        public void OnSpiritHit(RhythmTargetSystem.CircleHitData hitData)
        {
            // Increase heat level on hits
            currentHeatLevel += 0.1f;
            currentHeatLevel = Mathf.Clamp(currentHeatLevel, baseTemperature, maxTemperature);
            
            // Create sand particle effect
            CreateSandParticles(hitData.hitPosition);
            
            // Apply heat wave distortion
            ApplyHeatWaveEffect(hitData.hitPosition);
        }
        
        public void OnSandstormBlocked(RhythmTargetSystem.BlockData blockData)
        {
            CreateSandstormDispersion(blockData.blockPosition);
            
            // Temporarily reduce visibility
            _ = TemporaryVisibilityReductionAsync(3f);
        }
        
        private void Update()
        {
            // Gradually cool down
            currentHeatLevel = Mathf.Lerp(currentHeatLevel, baseTemperature, Time.deltaTime * 0.2f);
            
            // Apply heat distortion to environment
            ApplyEnvironmentalHeatEffects();
        }
        
        private void CreateSandParticles(Vector3 position)
        {
            GameObject effect = new GameObject("SandParticles");
            effect.transform.position = position;
            
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.2f;
            main.startColor = new Color(1f, 0.8f, 0.4f, 0.6f);
            main.maxParticles = 30;
            
            var emission = particles.emission;
            emission.rateOverTime = 15;
            
            Destroy(effect, 3f);
        }
        
        private void ApplyHeatWaveEffect(Vector3 position)
        {
            // Create heat wave distortion
            GameObject heatWave = new GameObject("HeatWave");
            heatWave.transform.position = position;
            
            // Add visual distortion effect
            // This would typically use a custom shader for heat wave distortion
            
            Destroy(heatWave, 2f);
        }
        
        private void CreateSandstormDispersion(Vector3 position)
        {
            GameObject effect = new GameObject("SandstormDispersion");
            effect.transform.position = position;
            
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 3f;
            main.startSpeed = 5f;
            main.startSize = 1f;
            main.startColor = new Color(0.8f, 0.6f, 0.3f, 0.5f);
            main.maxParticles = 100;
            
            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 50)
            });
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = sandstormRadius;
            
            Destroy(effect, 4f);
        }
        
        private async Task TemporaryVisibilityReductionAsync(float duration)
        {
            try
            {
                // Reduce visibility by increasing fog density
                float originalFogDensity = RenderSettings.fogDensity;
                float targetFogDensity = originalFogDensity * 3f;
                
                // Fade in
                float timer = 0f;
                while (timer < 1f)
                {
                    timer += Time.deltaTime * 2f;
                    RenderSettings.fogDensity = Mathf.Lerp(originalFogDensity, targetFogDensity, timer);
                    await Task.Yield();
                }
                
                // Wait
                await Task.Delay((int)((duration - 2f) * 1000));
                
                // Fade out
                timer = 0f;
                while (timer < 1f)
                {
                    timer += Time.deltaTime * 2f;
                    RenderSettings.fogDensity = Mathf.Lerp(targetFogDensity, originalFogDensity, timer);
                    await Task.Yield();
                }
                
                RenderSettings.fogDensity = originalFogDensity;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in visibility reduction effect: {ex.Message}");
                
                // Ensure fog density is reset
                if (RenderSettings.fogDensity > 1f)
                {
                    RenderSettings.fogDensity = 0.01f; // Default fog density
                }
            }
        }
        
        private void ApplyEnvironmentalHeatEffects()
        {
            // Apply heat-based visual effects
            float heatIntensity = (currentHeatLevel - baseTemperature) / (maxTemperature - baseTemperature);
            
            // Adjust ambient light to be warmer
            Color heatColor = Color.Lerp(Color.white, new Color(1f, 0.8f, 0.4f), heatIntensity);
            RenderSettings.ambientLight = heatColor * 0.8f;
        }
        
        public GameObject CreateMirageTarget(Vector3 position, RhythmTargetSystem.CircleType circleType)
        {
            // Create a mirage target that can't be hit
            GameObject mirage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mirage.name = "MirageTarget";
            mirage.transform.position = position;
            mirage.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
            
            // Make it translucent
            Renderer renderer = mirage.GetComponent<Renderer>();
            Material mirageMaterial = new Material(renderer.material);
            Color mirageColor = circleType == RhythmTargetSystem.CircleType.White ? Color.white : Color.gray;
            mirageColor.a = 0.5f;
            mirageMaterial.color = mirageColor;
            renderer.material = mirageMaterial;
            
            // Add mirage effect component
            MirageEffect mirageEffect = mirage.AddComponent<MirageEffect>();
            mirageEffect.Initialize(1f); // 100% chance it's a mirage
            
            // Remove collider so it can't be hit
            Collider collider = mirage.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            
            activeMirages.Add(mirage);
            
            return mirage;
        }
    }
    
    /// <summary>
    /// Mirage Effect Component - Makes targets appear as illusions
    /// </summary>
    public class MirageEffect : MonoBehaviour
    {
        [Header("Mirage Settings")]
        public float mirageChance = 0.3f;
        public bool isMirage = false;
        public float shimmerSpeed = 2f;
        
        private Renderer targetRenderer;
        private Material originalMaterial;
        private float shimmerTimer = 0f;
        
        private void Start()
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                originalMaterial = targetRenderer.material;
            }
        }
        
        public void Initialize(float chance)
        {
            mirageChance = chance;
            isMirage = Random.value < mirageChance;
            
            if (isMirage)
            {
                ApplyMirageEffect();
            }
        }
        
        private void Update()
        {
            if (isMirage)
            {
                ApplyShimmerEffect();
            }
        }
        
        private void ApplyMirageEffect()
        {
            if (targetRenderer == null) return;
            
            // Make the target translucent and shimmering
            Material mirageMaterial = new Material(originalMaterial);
            Color color = mirageMaterial.color;
            color.a = 0.6f;
            mirageMaterial.color = color;
            targetRenderer.material = mirageMaterial;
        }
        
        private void ApplyShimmerEffect()
        {
            if (targetRenderer == null) return;
            
            shimmerTimer += Time.deltaTime * shimmerSpeed;
            float shimmer = 0.5f + Mathf.Sin(shimmerTimer) * 0.3f;
            
            Color color = targetRenderer.material.color;
            color.a = shimmer;
            targetRenderer.material.color = color;
        }
    }
    
    /// <summary>
    /// Harmonic Oscillator - Makes crystals oscillate to musical frequencies
    /// </summary>
    public class HarmonicOscillator : MonoBehaviour
    {
        [Header("Harmonic Settings")]
        public float frequency = 440f; // A4 note
        public float amplitude = 0.1f;
        public Vector3 oscillationAxis = Vector3.up;
        
        private Vector3 originalPosition;
        private float timer = 0f;
        
        private void Start()
        {
            originalPosition = transform.position;
        }
        
        private void Update()
        {
            timer += Time.deltaTime;
            
            // Oscillate based on frequency
            float oscillation = Mathf.Sin(timer * frequency * 2f * Mathf.PI / 100f) * amplitude;
            transform.position = originalPosition + oscillationAxis * oscillation;
        }
    }
}