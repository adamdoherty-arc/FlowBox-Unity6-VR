using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Threading.Tasks;
using VRBoxingGame.Performance;
using VRBoxingGame.Core;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Creates stunning HD rain scene with dynamic weather and lightning effects
    /// </summary>
    public class RainSceneCreator : MonoBehaviour
    {
        [Header("Rain Scene Settings")]
        public bool createOnStart = true;
        public WeatherIntensity defaultIntensity = WeatherIntensity.Medium;
        
        [Header("Rain Effects")]
        public int maxRainParticles = 5000;
        public float rainSpeed = 15f;
        public float rainArea = 100f;
        public Material rainMaterial;
        
        [Header("Lightning & Thunder")]
        public float lightningFrequency = 8f; // seconds between strikes
        public float lightningDuration = 0.3f;
        public AnimationCurve lightningIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AudioClip[] thunderSounds;
        public AudioClip[] rainSounds;
        
        [Header("HD Environment")]
        public Material skyboxMaterial;
        public Gradient fogColorGradient;
        public AnimationCurve fogDensityCurve;
        public float environmentScale = 200f;
        
        [Header("Performance")]
        public bool enableJobSystemOptimization = true;
        public bool enableVROptimizations = true;
        public int targetFrameRate = 90;
        
        public enum WeatherIntensity
        {
            Light,
            Medium, 
            Heavy
        }
        
        [System.Serializable]
        public struct WeatherConfig
        {
            public string name;
            public int rainParticleCount;
            public float rainIntensity;
            public float windStrength;
            public float lightningFrequency;
            public float thunderVolume;
            public Color ambientColor;
            public float fogDensity;
            public float visibilityRange;
        }
        
        // Weather configurations
        private WeatherConfig[] weatherConfigs = new WeatherConfig[]
        {
            new WeatherConfig // Light
            {
                name = "Light Rain",
                rainParticleCount = 1500,
                rainIntensity = 0.3f,
                windStrength = 2f,
                lightningFrequency = 15f,
                thunderVolume = 0.4f,
                ambientColor = new Color(0.6f, 0.7f, 0.8f),
                fogDensity = 0.01f,
                visibilityRange = 150f
            },
            new WeatherConfig // Medium
            {
                name = "Medium Rain",
                rainParticleCount = 3000,
                rainIntensity = 0.6f,
                windStrength = 5f,
                lightningFrequency = 8f,
                thunderVolume = 0.7f,
                ambientColor = new Color(0.4f, 0.5f, 0.6f),
                fogDensity = 0.02f,
                visibilityRange = 100f
            },
            new WeatherConfig // Heavy
            {
                name = "Heavy Storm",
                rainParticleCount = 5000,
                rainIntensity = 1f,
                windStrength = 10f,
                lightningFrequency = 4f,
                thunderVolume = 1f,
                ambientColor = new Color(0.2f, 0.3f, 0.4f),
                fogDensity = 0.04f,
                visibilityRange = 50f
            }
        };
        
        // Scene objects
        private ParticleSystem rainSystem;
        private ParticleSystem mistSystem;
        private ParticleSystem splashSystem;
        private Light lightningLight;
        private AudioSource rainAudioSource;
        private AudioSource thunderAudioSource;
        private Camera mainCamera;
        private Volume postProcessVolume;
        
        // Lightning system - modernized
        private bool isLightningLoopRunning = false;
        private float nextLightningTime;
        private bool isLightningActive = false;
        
        // Current state
        private WeatherIntensity currentIntensity;
        private WeatherConfig currentConfig;
        private bool isRainEnvironmentActive = false;
        
        private void Start()
        {
            if (createOnStart)
            {
                CreateRainScene();
            }
        }
        
        [ContextMenu("Create Rain Scene")]
        public void CreateRainScene()
        {
            Debug.Log("🌧️ Creating HD Rain Scene...");
            
            // Setup scene foundation
            SetupEnvironment();
            SetupLighting();
            SetupAudio();
            
            // Create weather effects
            CreateRainSystem();
            CreateMistSystem();
            CreateSplashSystem();
            CreateLightningSystem();
            
            // Setup post-processing
            SetupPostProcessing();
            
            // Apply default intensity
            SetWeatherIntensity(defaultIntensity);
            
            // Start lightning system
            StartLightningSystem();
            
            Debug.Log("✅ HD Rain Scene created successfully!");
        }
        
        private void SetupEnvironment()
        {
            // Create HD skybox
            if (skyboxMaterial == null)
            {
                skyboxMaterial = CreateStormSkybox();
            }
            RenderSettings.skybox = skyboxMaterial;
            
            // Setup fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            
            // Create environment geometry
            CreateEnvironmentGeometry();
            
            Debug.Log("Environment setup complete");
        }
        
        private Material CreateStormSkybox()
        {
            Material skybox = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetSkyboxMaterial(new Color(0.2f, 0.2f, 0.3f), new Color(0.6f, 0.6f, 0.7f)) :
                new Material(Shader.Find("Skybox/Gradient"));
            skybox.SetColor("_Color1", new Color(0.2f, 0.2f, 0.3f)); // Dark storm clouds
            skybox.SetColor("_Color2", new Color(0.6f, 0.6f, 0.7f)); // Lighter gray
            skybox.SetFloat("_Exponent", 2f);
            
            RenderSettings.skybox = skybox;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.6f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.02f;
            
            return skybox;
        }
        
        private void CreateEnvironmentGeometry()
        {
            // Create distant mountains/hills in full 360 degrees for immersive experience
            CreateMountains();
            CreateGround();
            
            Debug.Log("Environment setup complete");
        }
        
        private void CreateMountains()
        {
            for (int i = 0; i < 16; i++)
            {
                GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mountain.name = $"Mountain_{i}";
                mountain.transform.SetParent(transform);
                
                float angle = (i / 16f) * 360f;
                float distance = Random.Range(150f, 300f);
                
                mountain.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(20f, 80f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                mountain.transform.localScale = new Vector3(
                    Random.Range(30f, 60f),
                    Random.Range(40f, 120f),
                    Random.Range(30f, 60f)
                );
                
                Material mountainMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(new Color(0.3f, 0.4f, 0.3f)) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mountainMat.color = new Color(0.3f, 0.4f, 0.3f);
                mountainMat.SetFloat("_Metallic", 0.1f);
                mountainMat.SetFloat("_Smoothness", 0.2f);
                mountain.GetComponent<Renderer>().material = mountainMat;
                
                // Add reactive component for music response
                mountain.AddComponent<ReactiveEnvironmentObject>();
            }
        }
        
        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "RainGround";
            ground.transform.SetParent(transform);
            ground.transform.position = new Vector3(0, -2f, 0);
            ground.transform.localScale = Vector3.one * 50f;
            
            Material groundMat = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetURPLitMaterial(new Color(0.2f, 0.3f, 0.2f)) :
                new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.2f, 0.3f, 0.2f);
            groundMat.SetFloat("_Metallic", 0f);
            groundMat.SetFloat("_Smoothness", 0.1f);
            ground.GetComponent<Renderer>().material = groundMat;
        }
        
        private void SetupLighting()
        {
            // Main directional light (dim for storm)
            Light mainLight = RenderSettings.sun;
            if (mainLight == null)
            {
                GameObject lightObj = new GameObject("Main Light");
                lightObj.transform.SetParent(transform);
                mainLight = lightObj.AddComponent<Light>();
                mainLight.type = LightType.Directional;
            }
            
            mainLight.intensity = 0.3f;
            mainLight.color = new Color(0.7f, 0.8f, 0.9f);
            mainLight.shadows = LightShadows.Soft;
            
            // Create lightning light
            GameObject lightningObj = new GameObject("Lightning Light");
            lightningObj.transform.SetParent(transform);
            lightningLight = lightningObj.AddComponent<Light>();
            lightningLight.type = LightType.Directional;
            lightningLight.intensity = 0f;
            lightningLight.color = new Color(0.9f, 0.95f, 1f);
            lightningLight.shadows = LightShadows.None;
            
            Debug.Log("Lighting setup complete");
        }
        
        private void SetupAudio()
        {
            // Rain audio source
            GameObject rainAudioObj = new GameObject("Rain Audio");
            rainAudioObj.transform.SetParent(transform);
            rainAudioSource = rainAudioObj.AddComponent<AudioSource>();
            rainAudioSource.loop = true;
            rainAudioSource.volume = 0.6f;
            rainAudioSource.spatialBlend = 0f; // 2D audio
            
            // Thunder audio source
            GameObject thunderAudioObj = new GameObject("Thunder Audio");
            thunderAudioObj.transform.SetParent(transform);
            thunderAudioSource = thunderAudioObj.AddComponent<AudioSource>();
            thunderAudioSource.loop = false;
            thunderAudioSource.volume = 0.8f;
            thunderAudioSource.spatialBlend = 0f; // 2D audio
            
            // Load audio clips if not assigned
            LoadAudioClips();
            
            Debug.Log("Audio setup complete");
        }
        
        private void LoadAudioClips()
        {
            // Create procedural rain sound if needed
            if (rainSounds == null || rainSounds.Length == 0)
            {
                rainSounds = new AudioClip[] { CreateProceduralRainSound() };
            }
            
            // Create procedural thunder sounds if needed
            if (thunderSounds == null || thunderSounds.Length == 0)
            {
                thunderSounds = new AudioClip[] { 
                    CreateProceduralThunderSound(1f),
                    CreateProceduralThunderSound(1.5f),
                    CreateProceduralThunderSound(2f)
                };
            }
        }
        
        private AudioClip CreateProceduralRainSound()
        {
            int sampleRate = 44100;
            float duration = 5f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            
            AudioClip clip = AudioClip.Create("ProceduralRain", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            
            // Generate white noise for rain
            for (int i = 0; i < samples; i++)
            {
                data[i] = (Random.value - 0.5f) * 0.3f;
                
                // Add some low-frequency rumble
                float time = i / (float)sampleRate;
                data[i] += Mathf.Sin(time * 20f) * 0.1f;
            }
            
            clip.SetData(data, 0);
            return clip;
        }
        
        private AudioClip CreateProceduralThunderSound(float intensity)
        {
            int sampleRate = 44100;
            float duration = 3f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            
            AudioClip clip = AudioClip.Create($"ProceduralThunder_{intensity}", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            
            for (int i = 0; i < samples; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Exp(-time * 2f); // Decay envelope
                
                // Low-frequency rumble
                float rumble = Mathf.Sin(time * 40f) * envelope * intensity;
                
                // Sharp crack at the beginning
                if (time < 0.1f)
                {
                    rumble += (Random.value - 0.5f) * envelope * intensity * 2f;
                }
                
                data[i] = rumble * 0.5f;
            }
            
            clip.SetData(data, 0);
            return clip;
        }
        
        private void CreateRainSystem()
        {
            GameObject rainObj = new GameObject("Rain System");
            rainObj.transform.SetParent(transform);
            rainSystem = rainObj.AddComponent<ParticleSystem>();
            
            var main = rainSystem.main;
            main.startLifetime = 2f;
            main.startSpeed = rainSpeed;
            main.startSize = 0.05f;
            main.startColor = new Color(0.7f, 0.8f, 1f, 0.8f);
            main.maxParticles = maxRainParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = rainSystem.emission;
            emission.rateOverTime = 2000;
            
            var shape = rainSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(rainArea, 1f, rainArea);
            
            var velocityOverLifetime = rainSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-rainSpeed);
            
            // Wind effect
            var forceOverLifetime = rainSystem.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.space = ParticleSystemSimulationSpace.World;
            forceOverLifetime.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
            
            // Collision with ground
            var collision = rainSystem.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision2D;
            
            rainObj.transform.position = new Vector3(0, 50f, 0);
            
            Debug.Log("Rain particle system created");
        }
        
        private void CreateMistSystem()
        {
            GameObject mistObj = new GameObject("Mist System");
            mistObj.transform.SetParent(transform);
            mistSystem = mistObj.AddComponent<ParticleSystem>();
            
            var main = mistSystem.main;
            main.startLifetime = 8f;
            main.startSpeed = 1f;
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startColor = new Color(0.8f, 0.9f, 1f, 0.1f);
            main.maxParticles = 200;
            
            var emission = mistSystem.emission;
            emission.rateOverTime = 25;
            
            var shape = mistSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(rainArea * 0.8f, 20f, rainArea * 0.8f);
            
            var velocityOverLifetime = mistSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
            
            mistObj.transform.position = new Vector3(0, 5f, 0);
            
            Debug.Log("Mist system created");
        }
        
        private void CreateSplashSystem()
        {
            GameObject splashObj = new GameObject("Splash System");
            splashObj.transform.SetParent(transform);
            splashSystem = splashObj.AddComponent<ParticleSystem>();
            
            var main = splashSystem.main;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new Color(0.9f, 0.95f, 1f, 0.6f);
            main.maxParticles = 1000;
            
            var emission = splashSystem.emission;
            emission.rateOverTime = 100;
            
            var shape = splashSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(rainArea * 0.5f, 1f, rainArea * 0.5f);
            
            var velocityOverLifetime = splashSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, 3f);
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(1f, 2f);
            
            splashObj.transform.position = new Vector3(0, -4.5f, 0);
            
            Debug.Log("Splash system created");
        }
        
        private void CreateLightningSystem()
        {
            // Lightning is handled by the lightning light and coroutine
            nextLightningTime = Time.time + Random.Range(2f, 5f);
            Debug.Log("Lightning system ready");
        }
        
        private void SetupPostProcessing()
        {
            // Find or create post-process volume
            postProcessVolume = CachedReferenceManager.Get<Volume>();
            if (postProcessVolume == null)
            {
                GameObject ppObj = new GameObject("Post Process Volume");
                ppObj.transform.SetParent(transform);
                postProcessVolume = ppObj.AddComponent<Volume>();
                postProcessVolume.isGlobal = true;
            }
            
            Debug.Log("Post-processing setup complete");
        }
        
        public void SetWeatherIntensity(WeatherIntensity intensity)
        {
            currentIntensity = intensity;
            currentConfig = weatherConfigs[(int)intensity];
            
            ApplyWeatherConfig();
            
            Debug.Log($"Weather intensity set to: {currentConfig.name}");
        }
        
        private void ApplyWeatherConfig()
        {
            // Update rain system
            if (rainSystem != null)
            {
                var main = rainSystem.main;
                main.maxParticles = currentConfig.rainParticleCount;
                
                var emission = rainSystem.emission;
                emission.rateOverTime = currentConfig.rainParticleCount * currentConfig.rainIntensity;
                
                var forceOverLifetime = rainSystem.forceOverLifetime;
                forceOverLifetime.x = new ParticleSystem.MinMaxCurve(
                    -currentConfig.windStrength, 
                    currentConfig.windStrength
                );
            }
            
            // Update lighting
            RenderSettings.ambientLight = currentConfig.ambientColor;
            
            // Update fog
            RenderSettings.fogColor = currentConfig.ambientColor;
            RenderSettings.fogDensity = currentConfig.fogDensity;
            
            // Update audio
            if (rainAudioSource != null && rainSounds.Length > 0)
            {
                rainAudioSource.clip = rainSounds[0];
                rainAudioSource.volume = currentConfig.rainIntensity * 0.6f;
                if (!rainAudioSource.isPlaying)
                {
                    rainAudioSource.Play();
                }
            }
            
            // Update lightning frequency
            lightningFrequency = currentConfig.lightningFrequency;
        }
        
        private void StartLightningSystem()
        {
            if (isLightningLoopRunning)
            {
                StopLightningLoop();
            }
            isLightningLoopRunning = true;
            _ = LightningLoopAsync();
        }
        
        private void StopLightningLoop()
        {
            isLightningLoopRunning = false;
        }
        
        private async Task LightningLoopAsync()
        {
            while (isLightningLoopRunning)
            {
                float waitTime = Random.Range(lightningFrequency * 0.5f, lightningFrequency * 1.5f);
                await Task.Delay((int)(waitTime * 1000));
                
                if (!isLightningActive && isLightningLoopRunning)
                {
                    _ = TriggerLightningAsync();
                }
            }
        }
        
        private async Task TriggerLightningAsync()
        {
            isLightningActive = true;
            
            try
            {
                // Lightning flash
                float flashDuration = lightningDuration;
                float elapsedTime = 0f;
                
                while (elapsedTime < flashDuration && isLightningLoopRunning)
                {
                    float normalizedTime = elapsedTime / flashDuration;
                    float intensity = lightningIntensityCurve.Evaluate(normalizedTime) * 8f;
                    
                    if (lightningLight != null)
                    {
                        lightningLight.intensity = intensity;
                        
                        // Random flicker
                        if (Random.value < 0.3f)
                        {
                            lightningLight.intensity *= Random.Range(0.5f, 1.5f);
                        }
                    }
                    
                    elapsedTime += Time.deltaTime;
                    await Task.Yield();
                }
                
                if (lightningLight != null)
                {
                    lightningLight.intensity = 0f;
                }
                
                // Thunder delay (sound travels slower than light)
                float thunderDelay = Random.Range(0.5f, 2f);
                await Task.Delay((int)(thunderDelay * 1000));
                
                // Play thunder
                if (thunderAudioSource != null && thunderSounds.Length > 0)
                {
                    AudioClip thunderClip = thunderSounds[Random.Range(0, thunderSounds.Length)];
                    thunderAudioSource.clip = thunderClip;
                    thunderAudioSource.volume = currentConfig.thunderVolume;
                    thunderAudioSource.Play();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Lightning trigger error: {e.Message}");
            }
            finally
            {
                isLightningActive = false;
            }
        }
        
        // Integration with rhythm game
        public void OnBeatDetected(float intensity)
        {
            // Make rain react to music beats
            if (rainSystem != null)
            {
                var emission = rainSystem.emission;
                emission.rateOverTime = currentConfig.rainParticleCount * (currentConfig.rainIntensity + intensity * 0.5f);
            }
            
            // Chance for lightning on strong beats
            if (intensity > 0.8f && !isLightningActive && Random.value < 0.3f)
            {
                _ = TriggerLightningAsync();
            }
        }
        
        // Public API
        [ContextMenu("Set Light Rain")]
        public void SetLightRain() => SetWeatherIntensity(WeatherIntensity.Light);
        
        [ContextMenu("Set Medium Rain")]
        public void SetMediumRain() => SetWeatherIntensity(WeatherIntensity.Medium);
        
        [ContextMenu("Set Heavy Rain")]
        public void SetHeavyRain() => SetWeatherIntensity(WeatherIntensity.Heavy);
        
        [ContextMenu("Trigger Lightning")]
        public void TriggerLightningManual()
        {
            if (!isLightningActive)
            {
                _ = TriggerLightningAsync();
            }
        }
        
        private void OnDestroy()
        {
            if (isLightningLoopRunning)
            {
                StopLightningLoop();
            }
        }
        
        private void OnDrawGizmos()
        {
            // Draw rain area
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 50f, new Vector3(rainArea, 1f, rainArea));
            
            // Draw visibility range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, currentConfig.visibilityRange);
        }

        /// <summary>
        /// Creates complete rain environment - called by SceneLoadingManager
        /// </summary>
        public void CreateCompleteRainEnvironment()
        {
            if (isRainEnvironmentActive) return;
            
            CreateRainScene();
            isRainEnvironmentActive = true;
            
            Debug.Log("✅ Complete rain environment created");
        }

        /// <summary>
        /// Destroys rain environment - called by SceneLoadingManager
        /// </summary>
        public void DestroyRainEnvironment()
        {
            if (!isRainEnvironmentActive) return;
            
            // Stop lightning system
            isLightningLoopRunning = false;
            
            // Destroy particle systems
            if (rainSystem != null) DestroyImmediate(rainSystem.gameObject);
            if (mistSystem != null) DestroyImmediate(mistSystem.gameObject);
            if (splashSystem != null) DestroyImmediate(splashSystem.gameObject);
            
            // Destroy audio sources
            if (rainAudioSource != null) DestroyImmediate(rainAudioSource.gameObject);
            if (thunderAudioSource != null) DestroyImmediate(thunderAudioSource.gameObject);
            
            // Destroy lightning light
            if (lightningLight != null) DestroyImmediate(lightningLight.gameObject);
            
            // Reset render settings
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            
            isRainEnvironmentActive = false;
            
            Debug.Log("🗑️ Rain environment destroyed");
        }
    }
} 