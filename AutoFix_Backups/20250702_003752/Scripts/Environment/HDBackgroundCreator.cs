using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRBoxingGame.Environment;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Creates high-definition background environments for the VR rhythm game
    /// </summary>
    public class HDBackgroundCreator : MonoBehaviour
    {
        [Header("Background Creation")]
        public bool createOnStart = true;
        public DynamicBackgroundSystem.BackgroundType defaultBackground = DynamicBackgroundSystem.BackgroundType.Cyberpunk;
        
        [Header("Quality Settings")]
        public int textureResolution = 2048;
        public int particleCount = 500;
        public bool enableRealtimeReflections = true;
        
        private void Start()
        {
            if (createOnStart)
            {
                CreateAllBackgrounds();
            }
        }
        
        [ContextMenu("Create All HD Backgrounds")]
        public void CreateAllBackgrounds()
        {
            CreateCyberpunkEnvironment();
            CreateSpaceEnvironment();
            CreateAbstractEnvironment();
            CreateCrystalEnvironment();
            CreateAuroraEnvironment();
            CreateUnderwaterEnvironment();
            
            Debug.Log("All HD backgrounds created!");
        }
        
        private void CreateCyberpunkEnvironment()
        {
            GameObject cyberpunk = new GameObject("Cyberpunk_Environment");
            cyberpunk.transform.SetParent(transform);
            
            // Create neon city skyline
            CreateNeonBuildings(cyberpunk.transform);
            CreateNeonSigns(cyberpunk.transform);
            CreateCyberpunkSkybox(cyberpunk.transform);
            CreateRainEffect(cyberpunk.transform);
            CreateNeonLights(cyberpunk.transform);
            
            // Save as prefab
            SaveEnvironmentPrefab(cyberpunk, "Cyberpunk_HD");
        }
        
        private void CreateSpaceEnvironment()
        {
            GameObject space = new GameObject("Space_Environment");
            space.transform.SetParent(transform);
            
            // Create space elements
            CreateStarField(space.transform);
            CreatePlanets(space.transform);
            CreateNebula(space.transform);
            CreateSpaceStations(space.transform);
            CreateAsteroidField(space.transform);
            
            SaveEnvironmentPrefab(space, "Space_HD");
        }
        
        private void CreateAbstractEnvironment()
        {
            GameObject abstract = new GameObject("Abstract_Environment");
            abstract.transform.SetParent(transform);
            
            // Create geometric patterns
            CreateFloatingGeometry(abstract.transform);
            CreateEnergyFields(abstract.transform);
            CreateAbstractSkybox(abstract.transform);
            CreateMusicReactiveShapes(abstract.transform);
            
            SaveEnvironmentPrefab(abstract, "Abstract_HD");
        }
        
        private void CreateCrystalEnvironment()
        {
            GameObject crystal = new GameObject("Crystal_Environment");
            crystal.transform.SetParent(transform);
            
            // Create crystal cave
            CreateCrystalFormations(crystal.transform);
            CreateReflectiveSurfaces(crystal.transform);
            CreateCrystalLighting(crystal.transform);
            CreateGemstoneEffects(crystal.transform);
            
            SaveEnvironmentPrefab(crystal, "Crystal_HD");
        }
        
        private void CreateAuroraEnvironment()
        {
            GameObject aurora = new GameObject("Aurora_Environment");
            aurora.transform.SetParent(transform);
            
            // Create aurora effects
            CreateAuroraParticles(aurora.transform);
            CreateIcyTerrain(aurora.transform);
            CreateNorthernLights(aurora.transform);
            CreateSnowEffect(aurora.transform);
            
            SaveEnvironmentPrefab(aurora, "Aurora_HD");
        }
        
        private void CreateUnderwaterEnvironment()
        {
            GameObject underwater = new GameObject("Underwater_Environment");
            underwater.transform.SetParent(transform);
            
            // Create underwater scene
            CreateCoralReefs(underwater.transform);
            CreateBioluminescence(underwater.transform);
            CreateWaterCaustics(underwater.transform);
            CreateSeaLife(underwater.transform);
            CreateBubbleEffects(underwater.transform);
            
            SaveEnvironmentPrefab(underwater, "Underwater_HD");
        }
        
        // Cyberpunk Environment Creation
        private void CreateNeonBuildings(Transform parent)
        {
            for (int i = 0; i < 20; i++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = $"NeonBuilding_{i}";
                building.transform.SetParent(parent);
                
                // Position buildings in a circle around player
                float angle = (i / 20f) * 360f;
                float distance = Random.Range(50f, 100f);
                float height = Random.Range(20f, 80f);
                
                Vector3 position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    height / 2f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                building.transform.position = position;
                building.transform.localScale = new Vector3(
                    Random.Range(5f, 15f),
                    height,
                    Random.Range(5f, 15f)
                );
                
                // Add neon material
                Material neonMat = CreateNeonMaterial();
                building.GetComponent<Renderer>().material = neonMat;
                
                // Add music reactivity
                building.AddComponent<ReactiveEnvironmentObject>();
            }
        }
        
        private void CreateNeonSigns(Transform parent)
        {
            for (int i = 0; i < 50; i++)
            {
                GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sign.name = $"NeonSign_{i}";
                sign.transform.SetParent(parent);
                
                // Random placement on buildings
                float angle = Random.Range(0f, 360f);
                float distance = Random.Range(30f, 90f);
                float height = Random.Range(10f, 60f);
                
                sign.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    height,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                sign.transform.LookAt(Vector3.zero);
                sign.transform.localScale = Vector3.one * Random.Range(2f, 8f);
                
                // Glowing neon material
                Material signMat = CreateGlowingMaterial();
                sign.GetComponent<Renderer>().material = signMat;
            }
        }
        
        private void CreateCyberpunkSkybox(Transform parent)
        {
            // Create dark cyberpunk skybox with city glow
            Material skyboxMat = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetSkyboxMaterial(new Color(0.1f, 0.05f, 0.2f), new Color(0.8f, 0.2f, 0.4f)) :
                new Material(Shader.Find("Skybox/Gradient"));
            skyboxMat.SetColor("_Color1", new Color(0.1f, 0.05f, 0.2f)); // Dark purple
            skyboxMat.SetColor("_Color2", new Color(0.8f, 0.2f, 0.4f)); // Neon pink
            skyboxMat.SetFloat("_Exponent", 2f);
            
            RenderSettings.skybox = skyboxMat;
            RenderSettings.ambientLight = new Color(0.3f, 0.1f, 0.3f);
        }
        
        private void CreateRainEffect(Transform parent)
        {
            GameObject rainSystem = new GameObject("RainEffect");
            rainSystem.transform.SetParent(parent);
            
            ParticleSystem rain = rainSystem.AddComponent<ParticleSystem>();
            var main = rain.main;
            main.startLifetime = 2f;
            main.startSpeed = 10f;
            main.startSize = 0.1f;
            main.startColor = new Color(0.5f, 0.7f, 1f, 0.8f);
            main.maxParticles = 1000;
            
            var emission = rain.emission;
            emission.rateOverTime = 500;
            
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(200f, 1f, 200f);
            
            rainSystem.transform.position = new Vector3(0, 100f, 0);
        }
        
        private void CreateNeonLights(Transform parent)
        {
            Color[] neonColors = {
                Color.cyan, Color.magenta, Color.yellow, 
                new Color(0f, 1f, 0.5f), new Color(1f, 0.3f, 0f)
            };
            
            for (int i = 0; i < 30; i++)
            {
                GameObject lightObj = new GameObject($"NeonLight_{i}");
                lightObj.transform.SetParent(parent);
                
                Light neonLight = lightObj.AddComponent<Light>();
                neonLight.type = LightType.Point;
                neonLight.color = neonColors[Random.Range(0, neonColors.Length)];
                neonLight.intensity = Random.Range(2f, 8f);
                neonLight.range = Random.Range(10f, 30f);
                
                // Random positioning
                float angle = Random.Range(0f, 360f);
                float distance = Random.Range(20f, 80f);
                float height = Random.Range(5f, 50f);
                
                lightObj.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    height,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                // Add flicker effect
                lightObj.AddComponent<LightFlicker>();
            }
        }
        
        // Space Environment Creation
        private void CreateStarField(Transform parent)
        {
            GameObject starField = new GameObject("StarField");
            starField.transform.SetParent(parent);
            
            ParticleSystem stars = starField.AddComponent<ParticleSystem>();
            var main = stars.main;
            main.startLifetime = float.MaxValue;
            main.startSpeed = 0f;
            main.startSize = Random.Range(0.1f, 0.5f);
            main.startColor = Color.white;
            main.maxParticles = 2000;
            
            var emission = stars.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 2000)
            });
            
            var shape = stars.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 500f;
        }
        
        private void CreatePlanets(Transform parent)
        {
            string[] planetNames = { "Mars", "Jupiter", "Saturn", "Venus", "Neptune" };
            Color[] planetColors = { 
                Color.red, new Color(1f, 0.8f, 0.4f), new Color(1f, 1f, 0.8f), 
                new Color(1f, 0.9f, 0.7f), Color.blue 
            };
            
            for (int i = 0; i < planetNames.Length; i++)
            {
                GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                planet.name = planetNames[i];
                planet.transform.SetParent(parent);
                
                float distance = Random.Range(200f, 800f);
                float angle = (i / (float)planetNames.Length) * 360f;
                
                planet.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(-100f, 100f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                planet.transform.localScale = Vector3.one * Random.Range(20f, 100f);
                
                Material planetMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(planetColors[i]) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                planetMat.color = planetColors[i];
                planetMat.SetFloat("_Metallic", 0.3f);
                planetMat.SetFloat("_Smoothness", 0.8f);
                planet.GetComponent<Renderer>().material = planetMat;
                
                // Add slow rotation
                planet.AddComponent<PlanetRotation>();
            }
        }
        
        private void CreateNebula(Transform parent)
        {
            GameObject nebula = new GameObject("Nebula");
            nebula.transform.SetParent(parent);
            
            ParticleSystem nebulaEffect = nebula.AddComponent<ParticleSystem>();
            var main = nebulaEffect.main;
            main.startLifetime = float.MaxValue;
            main.startSpeed = 0.5f;
            main.startSize = Random.Range(50f, 200f);
            main.startColor = new Color(0.8f, 0.3f, 1f, 0.3f);
            main.maxParticles = 100;
            
            var shape = nebulaEffect.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1000f, 300f, 1000f);
            
            nebula.transform.position = new Vector3(0, 0, 300f);
        }
        
        // Helper Methods
        private Material CreateNeonMaterial()
        {
            Color neonColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            Material mat = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetURPLitMaterial(neonColor) :
                new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = neonColor;
            mat.SetFloat("_Metallic", 0.8f);
            mat.SetFloat("_Smoothness", 0.9f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", mat.color * 2f);
            return mat;
        }
        
        private Material CreateGlowingMaterial()
        {
            Color glowColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            Material mat = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetMaterial(Shader.Find("Sprites/Default"), glowColor) :
                new Material(Shader.Find("Sprites/Default"));
            mat.color = glowColor;
            return mat;
        }
        
        private void SaveEnvironmentPrefab(GameObject environment, string name)
        {
            // In a real implementation, this would save as a prefab asset
            environment.SetActive(false);
            Debug.Log($"Environment '{name}' created and ready");
        }
        
        // Additional helper components would be implemented here
        private void CreateSpaceStations(Transform parent) 
        { 
            for (int i = 0; i < 3; i++)
            {
                GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                station.name = $"SpaceStation_{i}";
                station.transform.SetParent(parent);
                
                float distance = Random.Range(150f, 300f);
                float angle = (i / 3f) * 360f;
                
                station.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(-50f, 50f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                station.transform.localScale = new Vector3(20f, 50f, 20f);
                
                Material stationMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(Color.gray) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                stationMat.color = Color.gray;
                stationMat.SetFloat("_Metallic", 0.9f);
                station.GetComponent<Renderer>().material = stationMat;
            }
        }
        
        private void CreateAsteroidField(Transform parent) 
        { 
            for (int i = 0; i < 50; i++)
            {
                GameObject asteroid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                asteroid.name = $"Asteroid_{i}";
                asteroid.transform.SetParent(parent);
                
                float distance = Random.Range(100f, 500f);
                float angle = Random.Range(0f, 360f);
                
                asteroid.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(-100f, 100f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                asteroid.transform.localScale = Vector3.one * Random.Range(2f, 15f);
                
                Material asteroidMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(new Color(0.3f, 0.2f, 0.1f)) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                asteroidMat.color = new Color(0.3f, 0.2f, 0.1f);
                asteroid.GetComponent<Renderer>().material = asteroidMat;
                
                asteroid.AddComponent<PlanetRotation>();
            }
        }
        
        private void CreateFloatingGeometry(Transform parent) 
        { 
            for (int i = 0; i < 20; i++)
            {
                PrimitiveType[] shapes = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Cylinder };
                GameObject shape = GameObject.CreatePrimitive(shapes[Random.Range(0, shapes.Length)]);
                shape.name = $"FloatingShape_{i}";
                shape.transform.SetParent(parent);
                
                float distance = Random.Range(20f, 80f);
                float angle = Random.Range(0f, 360f);
                
                shape.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(-20f, 20f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                shape.transform.localScale = Vector3.one * Random.Range(3f, 8f);
                
                Material shapeMat = CreateNeonMaterial();
                shape.GetComponent<Renderer>().material = shapeMat;
                
                shape.AddComponent<ReactiveEnvironmentObject>();
            }
        }
        
        private void CreateEnergyFields(Transform parent) 
        { 
            GameObject energyField = new GameObject("EnergyField");
            energyField.transform.SetParent(parent);
            
            ParticleSystem energy = energyField.AddComponent<ParticleSystem>();
            var main = energy.main;
            main.startLifetime = 5f;
            main.startSpeed = 2f;
            main.startSize = Random.Range(1f, 3f);
            main.startColor = new Color(0f, 1f, 1f, 0.5f);
            main.maxParticles = 500;
            
            var emission = energy.emission;
            emission.rateOverTime = 100;
            
            var shape = energy.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 50f;
        }
        
        private void CreateAbstractSkybox(Transform parent) 
        { 
            Material skyboxMat = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetSkyboxMaterial(new Color(0.2f, 0f, 0.8f), new Color(1f, 0.5f, 0f)) :
                new Material(Shader.Find("Skybox/Gradient"));
            skyboxMat.SetColor("_Color1", new Color(0.2f, 0f, 0.8f)); // Deep purple
            skyboxMat.SetColor("_Color2", new Color(1f, 0.5f, 0f)); // Orange
            skyboxMat.SetFloat("_Exponent", 1.5f);
            
            RenderSettings.skybox = skyboxMat;
            RenderSettings.ambientLight = new Color(0.3f, 0.1f, 0.5f);
        }
        
        private void CreateMusicReactiveShapes(Transform parent) 
        { 
            for (int i = 0; i < 15; i++)
            {
                GameObject shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shape.name = $"ReactiveShape_{i}";
                shape.transform.SetParent(parent);
                
                float distance = Random.Range(30f, 70f);
                float angle = (i / 15f) * 360f;
                
                shape.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(-10f, 10f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                shape.transform.localScale = Vector3.one * Random.Range(2f, 6f);
                
                Material reactMat = CreateNeonMaterial();
                shape.GetComponent<Renderer>().material = reactMat;
                
                var reactive = shape.AddComponent<ReactiveEnvironmentObject>();
                reactive.reactToScale = true;
                reactive.reactToRotation = true;
                reactive.reactToColor = true;
                reactive.frequencyBand = Random.Range(0, 8);
            }
        }
        
        private void CreateCrystalFormations(Transform parent)
        {
            for (int i = 0; i < 20; i++)
            {
                GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crystal.name = $"Crystal_{i}";
                crystal.transform.SetParent(parent);
                
                float distance = Random.Range(20f, 150f);
                float angle = Random.Range(0f, 360f);
                
                crystal.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(-10f, 30f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                crystal.transform.localScale = new Vector3(
                    Random.Range(2f, 8f),
                    Random.Range(10f, 25f),
                    Random.Range(2f, 8f)
                );
                
                crystal.transform.rotation = Quaternion.Euler(
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f)
                );
                
                Color crystalColor = Color.HSVToRGB(Random.Range(0.5f, 0.8f), 0.8f, 1f);
                Material crystalMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(crystalColor) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                crystalMat.color = crystalColor;
                crystalMat.SetFloat("_Metallic", 0.1f);
                crystalMat.SetFloat("_Smoothness", 0.9f);
                crystal.GetComponent<Renderer>().material = crystalMat;
            }
        }
        
        private void CreateReflectiveSurfaces(Transform parent)
        {
            GameObject reflectivePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            reflectivePlane.name = "ReflectivePlane";
            reflectivePlane.transform.SetParent(parent);
            reflectivePlane.transform.localScale = Vector3.one * 50f;
            reflectivePlane.transform.position = new Vector3(0, -5f, 0);
            
            Material reflectiveMat = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetURPLitMaterial(new Color(0.9f, 0.9f, 1f)) :
                new Material(Shader.Find("Universal Render Pipeline/Lit"));
            reflectiveMat.color = new Color(0.9f, 0.9f, 1f);
            reflectiveMat.SetFloat("_Metallic", 1f);
            reflectiveMat.SetFloat("_Smoothness", 1f);
            reflectivePlane.GetComponent<Renderer>().material = reflectiveMat;
        }
        
        private void CreateCrystalLighting(Transform parent)
        {
            // Create multiple colored lights for crystal ambiance
            Color[] crystalColors = { Color.cyan, Color.magenta, Color.blue, Color.white };
            
            for (int i = 0; i < 8; i++)
            {
                GameObject lightObj = new GameObject($"CrystalLight_{i}");
                lightObj.transform.SetParent(parent);
                
                float angle = (i / 8f) * 360f;
                lightObj.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * 30f,
                    Random.Range(10f, 30f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * 30f
                );
                
                Light crystalLight = lightObj.AddComponent<Light>();
                crystalLight.type = LightType.Point;
                crystalLight.color = crystalColors[Random.Range(0, crystalColors.Length)];
                crystalLight.intensity = Random.Range(1f, 3f);
                crystalLight.range = Random.Range(20f, 40f);
                crystalLight.shadows = LightShadows.Soft;
                
                // Add flicker effect
                lightObj.AddComponent<LightFlicker>();
            }
        }
        
        private void CreateGemstoneEffects(Transform parent)
        {
            // Create floating gemstone particles
            GameObject gemstoneSystem = new GameObject("GemstoneParticles");
            gemstoneSystem.transform.SetParent(parent);
            
            ParticleSystem particles = gemstoneSystem.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 10f;
            main.startSpeed = 1f;
            main.startSize = 0.3f;
            main.startColor = Color.cyan;
            main.maxParticles = 200;
            
            var emission = particles.emission;
            emission.rateOverTime = 20;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 50f;
        }
        
        private void CreateAuroraParticles(Transform parent)
        {
            // Create aurora-like particle effects in 360 degrees
            GameObject auroraSystem = new GameObject("AuroraParticles");
            auroraSystem.transform.SetParent(parent);
            auroraSystem.transform.position = new Vector3(0, 30f, 0);
            
            ParticleSystem aurora = auroraSystem.AddComponent<ParticleSystem>();
            var main = aurora.main;
            main.startLifetime = 15f;
            main.startSpeed = 2f;
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startColor = new Color(0.2f, 1f, 0.8f, 0.3f);
            main.maxParticles = 500;
            
            var emission = aurora.emission;
            emission.rateOverTime = 30;
            
            var shape = aurora.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 100f;
        }
        
        private void CreateIcyTerrain(Transform parent)
        {
            GameObject icePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            icePlane.name = "IcyTerrain";
            icePlane.transform.SetParent(parent);
            icePlane.transform.localScale = Vector3.one * 100f;
            icePlane.transform.position = new Vector3(0, -2f, 0);
            
            Material iceMat = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetURPLitMaterial(new Color(0.9f, 0.95f, 1f)) :
                new Material(Shader.Find("Universal Render Pipeline/Lit"));
            iceMat.color = new Color(0.9f, 0.95f, 1f);
            iceMat.SetFloat("_Metallic", 0.2f);
            iceMat.SetFloat("_Smoothness", 0.9f);
            icePlane.GetComponent<Renderer>().material = iceMat;
            
            // Add some ice chunks
            for (int i = 0; i < 30; i++)
            {
                GameObject iceChunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                iceChunk.name = $"IceChunk_{i}";
                iceChunk.transform.SetParent(parent);
                
                iceChunk.transform.position = new Vector3(
                    Random.Range(-100f, 100f),
                    Random.Range(-1f, 5f),
                    Random.Range(-100f, 100f)
                );
                
                iceChunk.transform.localScale = Vector3.one * Random.Range(2f, 8f);
                iceChunk.transform.rotation = Quaternion.Euler(
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f),
                    Random.Range(0f, 360f)
                );
                
                iceChunk.GetComponent<Renderer>().material = iceMat;
            }
        }
        
        private void CreateNorthernLights(Transform parent)
        {
            // Create multiple directional lights to simulate northern lights
            for (int i = 0; i < 4; i++)
            {
                GameObject lightObj = new GameObject($"NorthernLight_{i}");
                lightObj.transform.SetParent(parent);
                lightObj.transform.position = new Vector3(0, 50f, 0);
                lightObj.transform.rotation = Quaternion.Euler(Random.Range(-30f, -60f), i * 90f, 0);
                
                Light northernLight = lightObj.AddComponent<Light>();
                northernLight.type = LightType.Directional;
                northernLight.color = new Color(Random.Range(0f, 1f), 1f, Random.Range(0.5f, 1f), 0.5f);
                northernLight.intensity = Random.Range(0.3f, 0.8f);
                northernLight.shadows = LightShadows.None;
                
                // Add dynamic intensity changes
                lightObj.AddComponent<LightFlicker>();
            }
        }
        
        private void CreateSnowEffect(Transform parent)
        {
            // Create falling snow particles
            GameObject snowSystem = new GameObject("SnowParticles");
            snowSystem.transform.SetParent(parent);
            snowSystem.transform.position = new Vector3(0, 40f, 0);
            
            ParticleSystem snow = snowSystem.AddComponent<ParticleSystem>();
            var main = snow.main;
            main.startLifetime = 8f;
            main.startSpeed = 2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startColor = Color.white;
            main.maxParticles = 1000;
            
            var emission = snow.emission;
            emission.rateOverTime = 100;
            
            var shape = snow.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(100f, 1f, 100f);
            
            var velocityOverLifetime = snow.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-3f, -1f);
        }
        
        private void CreateCoralReefs(Transform parent)
        {
            for (int i = 0; i < 25; i++)
            {
                GameObject coral = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coral.name = $"Coral_{i}";
                coral.transform.SetParent(parent);
                
                float distance = Random.Range(15f, 100f);
                float angle = Random.Range(0f, 360f);
                
                coral.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(-10f, 5f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                coral.transform.localScale = Vector3.one * Random.Range(3f, 12f);
                
                Color coralColor = Color.HSVToRGB(Random.Range(0f, 0.15f), 0.8f, 1f);
                Material coralMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(coralColor) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                coralMat.color = coralColor;
                coralMat.SetFloat("_Metallic", 0f);
                coralMat.SetFloat("_Smoothness", 0.3f);
                coral.GetComponent<Renderer>().material = coralMat;
            }
        }
        
        private void CreateBioluminescence(Transform parent)
        {
            // Create glowing particles that react to movement
            GameObject bioSystem = new GameObject("BioluminescentParticles");
            bioSystem.transform.SetParent(parent);
            
            ParticleSystem bio = bioSystem.AddComponent<ParticleSystem>();
            var main = bio.main;
            main.startLifetime = 5f;
            main.startSpeed = 0.5f;
            main.startSize = 0.2f;
            main.startColor = new Color(0f, 1f, 0.8f, 0.7f);
            main.maxParticles = 300;
            
            var emission = bio.emission;
            emission.rateOverTime = 60;
            
            var shape = bio.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 30f;
        }
        
        private void CreateWaterCaustics(Transform parent)
        {
            // Create caustic light patterns on surfaces
            GameObject causticsObj = new GameObject("WaterCaustics");
            causticsObj.transform.SetParent(parent);
            
            Light causticLight = causticsObj.AddComponent<Light>();
            causticLight.type = LightType.Directional;
            causticLight.color = new Color(0.7f, 0.9f, 1f);
            causticLight.intensity = 1.5f;
            causticLight.shadows = LightShadows.Soft;
            
            // Add animated caustic patterns (simplified)
            causticsObj.AddComponent<LightFlicker>();
        }
        
        private void CreateSeaLife(Transform parent)
        {
            for (int i = 0; i < 15; i++)
            {
                GameObject fish = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fish.name = $"Fish_{i}";
                fish.transform.SetParent(parent);
                
                float distance = Random.Range(10f, 50f);
                float angle = Random.Range(0f, 360f);
                
                fish.transform.position = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    Random.Range(-5f, 15f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                );
                
                fish.transform.localScale = new Vector3(0.5f, 0.3f, 2f);
                fish.transform.rotation = Quaternion.LookRotation(Random.insideUnitSphere);
                
                Color fishColor = Color.HSVToRGB(Random.Range(0.15f, 0.65f), 0.8f, 1f);
                Material fishMat = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(fishColor) :
                    new Material(Shader.Find("Universal Render Pipeline/Lit"));
                fishMat.color = fishColor;
                fishMat.SetFloat("_Metallic", 0.1f);
                fishMat.SetFloat("_Smoothness", 0.7f);
                fish.GetComponent<Renderer>().material = fishMat;
                
                // Add simple swimming behavior
                fish.AddComponent<SimpleSwimming>();
            }
        }
        
        private void CreateBubbleEffects(Transform parent)
        {
            // Create rising bubble particles
            GameObject bubbleSystem = new GameObject("BubbleParticles");
            bubbleSystem.transform.SetParent(parent);
            bubbleSystem.transform.position = new Vector3(0, -10f, 0);
            
            ParticleSystem bubbles = bubbleSystem.AddComponent<ParticleSystem>();
            var main = bubbles.main;
            main.startLifetime = 6f;
            main.startSpeed = 3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 1f);
            main.startColor = new Color(1f, 1f, 1f, 0.3f);
            main.maxParticles = 200;
            
            var emission = bubbles.emission;
            emission.rateOverTime = 30;
            
            var shape = bubbles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 25f;
            
            var velocityOverLifetime = bubbles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(2f, 4f);
        }
    }
    
    // Helper components
    public class LightFlicker : MonoBehaviour
    {
        private Light lightComponent;
        private float baseIntensity;
        
        void Start()
        {
            lightComponent = GetComponent<Light>();
            baseIntensity = lightComponent.intensity;
        }
        
        void Update()
        {
            lightComponent.intensity = baseIntensity + Mathf.Sin(Time.time * Random.Range(5f, 15f)) * 0.5f;
        }
    }
    
    public class PlanetRotation : MonoBehaviour
    {
        private Vector3 rotationSpeed;
        
        void Start()
        {
            rotationSpeed = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            );
        }
        
        void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
} 