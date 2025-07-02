using System.Collections.Generic;
using UnityEngine;

namespace VRBoxingGame.Performance
{
    /// <summary>
    /// Material pooling system to optimize runtime material creation
    /// Reduces memory allocation and improves performance
    /// </summary>
    public class MaterialPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        public int maxPoolSize = 100;
        public bool enablePooling = true;
        public bool logPoolStats = false;

        // Material pools organized by shader
        private Dictionary<Shader, Queue<Material>> materialPools = new Dictionary<Shader, Queue<Material>>();
        private Dictionary<Material, Shader> materialToShader = new Dictionary<Material, Shader>();
        private HashSet<Material> pooledMaterials = new HashSet<Material>();

        // Statistics
        private int materialsCreated = 0;
        private int materialsReused = 0;
        private int materialsReturned = 0;

        public static MaterialPool Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("MaterialPool initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Gets a material from the pool or creates a new one
        /// </summary>
        public Material GetMaterial(Shader shader)
        {
            if (!enablePooling || shader == null)
            {
                return CreateNewMaterial(shader);
            }

            // Check if we have this shader in our pools
            if (!materialPools.ContainsKey(shader))
            {
                materialPools[shader] = new Queue<Material>();
            }

            Queue<Material> pool = materialPools[shader];

            // Try to reuse an existing material
            if (pool.Count > 0)
            {
                Material material = pool.Dequeue();
                if (material != null)
                {
                    materialsReused++;
                    pooledMaterials.Remove(material);
                    
                    if (logPoolStats)
                        Debug.Log($"MaterialPool: Reused material with shader {shader.name}");
                    
                    return material;
                }
            }

            // Create new material if pool is empty
            return CreateNewMaterial(shader);
        }

        /// <summary>
        /// Gets a material with specific properties
        /// </summary>
        public Material GetMaterial(Shader shader, Color color)
        {
            Material material = GetMaterial(shader);
            if (material != null)
            {
                material.color = color;
            }
            return material;
        }

        /// <summary>
        /// Gets a Universal Render Pipeline Lit material with color
        /// </summary>
        public Material GetURPLitMaterial(Color color)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            return GetMaterial(urpShader, color);
        }

        /// <summary>
        /// Gets a skybox gradient material
        /// </summary>
        public Material GetSkyboxMaterial(Color color1, Color color2)
        {
            Shader skyboxShader = Shader.Find("Skybox/Gradient");
            Material material = GetMaterial(skyboxShader);
            if (material != null)
            {
                material.SetColor("_Color1", color1);
                material.SetColor("_Color2", color2);
            }
            return material;
        }

        /// <summary>
        /// Gets an unlit color material
        /// </summary>
        public Material GetUnlitMaterial(Color color)
        {
            Shader unlitShader = Shader.Find("Unlit/Color");
            return GetMaterial(unlitShader, color);
        }

        /// <summary>
        /// Returns a material to the pool for reuse
        /// </summary>
        public void ReturnMaterial(Material material)
        {
            if (!enablePooling || material == null || pooledMaterials.Contains(material))
                return;

            // Find the shader for this material
            Shader shader = material.shader;
            if (shader == null || !materialToShader.ContainsKey(material))
            {
                // If we don't know the shader, try to add it
                materialToShader[material] = shader;
            }

            // Make sure we have a pool for this shader
            if (!materialPools.ContainsKey(shader))
            {
                materialPools[shader] = new Queue<Material>();
            }

            Queue<Material> pool = materialPools[shader];

            // Only return to pool if we haven't exceeded max size
            if (pool.Count < maxPoolSize)
            {
                // Reset material properties to defaults
                ResetMaterial(material);
                
                pool.Enqueue(material);
                pooledMaterials.Add(material);
                materialsReturned++;

                if (logPoolStats)
                    Debug.Log($"MaterialPool: Returned material with shader {shader.name}");
            }
            else
            {
                // Pool is full, destroy the material
                if (Application.isPlaying)
                {
                    Destroy(material);
                }
            }
        }

        /// <summary>
        /// Creates a new material and tracks it
        /// </summary>
        private Material CreateNewMaterial(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("MaterialPool: Cannot create material with null shader");
                return null;
            }

            Material material = new Material(shader);
            materialToShader[material] = shader;
            materialsCreated++;

            if (logPoolStats)
                Debug.Log($"MaterialPool: Created new material with shader {shader.name}");

            return material;
        }

        /// <summary>
        /// Resets a material to default state for reuse
        /// </summary>
        private void ResetMaterial(Material material)
        {
            if (material == null) return;

            // Reset common properties
            if (material.HasProperty("_Color"))
                material.color = Color.white;
            
            if (material.HasProperty("_MainTex"))
                material.mainTexture = null;
            
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.5f);

            // Reset specific shader properties
            if (material.shader.name.Contains("Skybox"))
            {
                if (material.HasProperty("_Color1"))
                    material.SetColor("_Color1", Color.white);
                if (material.HasProperty("_Color2"))
                    material.SetColor("_Color2", Color.white);
                if (material.HasProperty("_Exponent"))
                    material.SetFloat("_Exponent", 1f);
            }
        }

        /// <summary>
        /// Clears all pools and resets statistics
        /// </summary>
        public void ClearPools()
        {
            foreach (var pool in materialPools.Values)
            {
                while (pool.Count > 0)
                {
                    Material material = pool.Dequeue();
                    if (material != null && Application.isPlaying)
                    {
                        Destroy(material);
                    }
                }
            }
            
            materialPools.Clear();
            materialToShader.Clear();
            pooledMaterials.Clear();
            
            materialsCreated = 0;
            materialsReused = 0;
            materialsReturned = 0;
            
            Debug.Log("MaterialPool: All pools cleared");
        }

        /// <summary>
        /// Gets current pool statistics
        /// </summary>
        public string GetPoolStats()
        {
            int totalPooled = 0;
            foreach (var pool in materialPools.Values)
            {
                totalPooled += pool.Count;
            }

            return $"MaterialPool Stats:\n" +
                   $"Created: {materialsCreated}\n" +
                   $"Reused: {materialsReused}\n" +
                   $"Returned: {materialsReturned}\n" +
                   $"Currently Pooled: {totalPooled}\n" +
                   $"Active Pools: {materialPools.Count}";
        }

        private void OnDestroy()
        {
            if (logPoolStats)
            {
                Debug.Log(GetPoolStats());
            }
        }

        private void OnGUI()
        {
            if (logPoolStats && Application.isPlaying)
            {
                GUILayout.BeginArea(new Rect(10, 200, 300, 150));
                GUILayout.Label(GetPoolStats());
                if (GUILayout.Button("Clear Pools"))
                {
                    ClearPools();
                }
                GUILayout.EndArea();
            }
        }
    }
} 