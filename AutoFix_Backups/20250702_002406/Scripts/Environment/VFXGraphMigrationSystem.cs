using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Core;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Migrates legacy particle systems to VFX Graph for optimal VR performance
    /// Handles automatic conversion and performance monitoring
    /// </summary>
    public class VFXGraphMigrationSystem : MonoBehaviour
    {
        [Header("Migration Settings")]
        public bool enableAutoMigration = true;
        public bool enablePerformanceMonitoring = true;
        public float performanceThreshold = 60f; // Target FPS
        
        [Header("VFX Graph Assets")]
        public VisualEffect rainVFXPrefab;
        public VisualEffect sparkleVFXPrefab;
        public VisualEffect smokeVFXPrefab;
        public VisualEffect explosionVFXPrefab;
        public VisualEffect lightningVFXPrefab;
        
        [Header("Migration Targets")]
        public ParticleSystem[] legacyParticleSystems;
        
        // Migration tracking
        private Dictionary<ParticleSystem, VisualEffect> migrationMap = new Dictionary<ParticleSystem, VisualEffect>();
        private List<VFXMigrationData> completedMigrations = new List<VFXMigrationData>();
        private bool isMigrating = false;
        
        // Performance tracking
        private float originalFrameRate = 0f;
        private float postMigrationFrameRate = 0f;
        private int totalParticleSystemsMigrated = 0;
        
        // Singleton
        public static VFXGraphMigrationSystem Instance { get; private set; }
        
        [System.Serializable]
        public class VFXMigrationData
        {
            public string originalName;
            public ParticleSystem originalSystem;
            public VisualEffect migratedVFX;
            public float performanceGain;
            public bool migrationSuccessful;
            public System.DateTime migrationTime;
        }
        
        public enum VFXType
        {
            Rain,
            Sparkle,
            Smoke,
            Explosion,
            Lightning,
            Custom
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMigrationSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeMigrationSystem()
        {
            // Find all particle systems in scene if not manually assigned
            if (legacyParticleSystems == null || legacyParticleSystems.Length == 0)
            {
                legacyParticleSystems = FindObjectsOfType<ParticleSystem>();
            }
            
            Debug.Log($"VFX Graph Migration System initialized with {legacyParticleSystems.Length} particle systems found");
            
            if (enableAutoMigration)
            {
                _ = StartAutoMigrationAsync();
            }
        }
        
        /// <summary>
        /// Automatically migrate all particle systems to VFX Graph
        /// </summary>
        public async Task StartAutoMigrationAsync()
        {
            if (isMigrating)
            {
                Debug.LogWarning("Migration already in progress");
                return;
            }
            
            isMigrating = true;
            originalFrameRate = GetCurrentFrameRate();
            
            Debug.Log("Starting automatic VFX Graph migration...");
            
            try
            {
                for (int i = 0; i < legacyParticleSystems.Length; i++)
                {
                    var particleSystem = legacyParticleSystems[i];
                    if (particleSystem != null && !migrationMap.ContainsKey(particleSystem))
                    {
                        await MigrateParticleSystemAsync(particleSystem);
                        
                        // Small delay between migrations to prevent frame drops
                        await Task.Delay(100);
                    }
                }
                
                postMigrationFrameRate = GetCurrentFrameRate();
                float overallPerformanceGain = postMigrationFrameRate - originalFrameRate;
                
                Debug.Log($"Migration completed! Migrated {totalParticleSystemsMigrated} systems. Performance gain: {overallPerformanceGain:F1} FPS");
                
                // Notify other systems
                if (GameManager.Instance != null)
                {
                    // Could add migration completion event here
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during auto migration: {ex.Message}");
            }
            finally
            {
                isMigrating = false;
            }
        }
        
        /// <summary>
        /// Migrate a specific particle system to VFX Graph
        /// </summary>
        public async Task<VisualEffect> MigrateParticleSystemAsync(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                Debug.LogWarning("Cannot migrate null particle system");
                return null;
            }
            
            if (migrationMap.ContainsKey(particleSystem))
            {
                Debug.LogWarning($"Particle system {particleSystem.name} already migrated");
                return migrationMap[particleSystem];
            }
            
            Debug.Log($"Migrating particle system: {particleSystem.name}");
            
            // Determine VFX type based on particle system properties
            VFXType vfxType = DetermineVFXType(particleSystem);
            
            // Get appropriate VFX prefab
            VisualEffect vfxPrefab = GetVFXPrefab(vfxType);
            
            if (vfxPrefab == null)
            {
                Debug.LogWarning($"No VFX prefab available for type {vfxType}");
                return null;
            }
            
            // Create VFX instance
            VisualEffect vfxInstance = Instantiate(vfxPrefab, particleSystem.transform.position, particleSystem.transform.rotation);
            vfxInstance.transform.SetParent(particleSystem.transform.parent);
            vfxInstance.name = $"VFX_{particleSystem.name}";
            
            // Transfer properties from particle system to VFX
            await TransferPropertiesAsync(particleSystem, vfxInstance, vfxType);
            
            // Record migration
            var migrationData = new VFXMigrationData
            {
                originalName = particleSystem.name,
                originalSystem = particleSystem,
                migratedVFX = vfxInstance,
                migrationSuccessful = true,
                migrationTime = System.DateTime.Now,
                performanceGain = 0f // Will be calculated later
            };
            
            migrationMap[particleSystem] = vfxInstance;
            completedMigrations.Add(migrationData);
            totalParticleSystemsMigrated++;
            
            // Disable original particle system but keep for reference
            particleSystem.gameObject.SetActive(false);
            
            Debug.Log($"Successfully migrated {particleSystem.name} to VFX Graph");
            
            return vfxInstance;
        }
        
        private VFXType DetermineVFXType(ParticleSystem particleSystem)
        {
            string name = particleSystem.name.ToLower();
            
            if (name.Contains("rain") || name.Contains("water") || name.Contains("drop"))
                return VFXType.Rain;
            else if (name.Contains("spark") || name.Contains("star") || name.Contains("glitter"))
                return VFXType.Sparkle;
            else if (name.Contains("smoke") || name.Contains("steam") || name.Contains("mist"))
                return VFXType.Smoke;
            else if (name.Contains("explosion") || name.Contains("burst") || name.Contains("blast"))
                return VFXType.Explosion;
            else if (name.Contains("lightning") || name.Contains("electric") || name.Contains("bolt"))
                return VFXType.Lightning;
            else
                return VFXType.Custom;
        }
        
        private VisualEffect GetVFXPrefab(VFXType vfxType)
        {
            switch (vfxType)
            {
                case VFXType.Rain: return rainVFXPrefab;
                case VFXType.Sparkle: return sparkleVFXPrefab;
                case VFXType.Smoke: return smokeVFXPrefab;
                case VFXType.Explosion: return explosionVFXPrefab;
                case VFXType.Lightning: return lightningVFXPrefab;
                default: return rainVFXPrefab; // Fallback
            }
        }
        
        private async Task TransferPropertiesAsync(ParticleSystem particleSystem, VisualEffect vfxInstance, VFXType vfxType)
        {
            var main = particleSystem.main;
            var emission = particleSystem.emission;
            var shape = particleSystem.shape;
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            
            // Transfer basic properties
            if (vfxInstance.HasFloat("Rate"))
                vfxInstance.SetFloat("Rate", emission.rateOverTime.constant);
                
            if (vfxInstance.HasFloat("Lifetime"))
                vfxInstance.SetFloat("Lifetime", main.startLifetime.constant);
                
            if (vfxInstance.HasFloat("Speed"))
                vfxInstance.SetFloat("Speed", main.startSpeed.constant);
                
            if (vfxInstance.HasVector3("Velocity"))
                vfxInstance.SetVector3("Velocity", velocityOverLifetime.space == ParticleSystemSimulationSpace.Local ? 
                    velocityOverLifetime.linear.constant : Vector3.zero);
            
            // Transfer color
            if (vfxInstance.HasVector4("Color"))
            {
                Color color = main.startColor.color;
                vfxInstance.SetVector4("Color", new Vector4(color.r, color.g, color.b, color.a));
            }
            
            // Transfer size
            if (vfxInstance.HasFloat("Size"))
                vfxInstance.SetFloat("Size", main.startSize.constant);
            
            // Shape-specific properties
            if (shape.enabled)
            {
                if (vfxInstance.HasFloat("ShapeRadius") && shape.shapeType == ParticleSystemShapeType.Circle)
                    vfxInstance.SetFloat("ShapeRadius", shape.radius);
                    
                if (vfxInstance.HasVector3("ShapeSize") && shape.shapeType == ParticleSystemShapeType.Box)
                    vfxInstance.SetVector3("ShapeSize", shape.scale);
            }
            
            // VFX-specific optimizations
            await ApplyVFXOptimizations(vfxInstance, vfxType);
            
            await Task.Yield(); // Allow frame to complete
        }
        
        private async Task ApplyVFXOptimizations(VisualEffect vfxInstance, VFXType vfxType)
        {
            // Apply VR-specific optimizations
            switch (vfxType)
            {
                case VFXType.Rain:
                    // Optimize for rain effects
                    if (vfxInstance.HasInt("MaxParticles"))
                        vfxInstance.SetInt("MaxParticles", 2000); // Reasonable limit for VR
                    break;
                    
                case VFXType.Sparkle:
                    // Optimize for sparkle effects
                    if (vfxInstance.HasFloat("Brightness"))
                        vfxInstance.SetFloat("Brightness", 0.8f); // Reduce brightness for VR comfort
                    break;
                    
                case VFXType.Smoke:
                    // Optimize for smoke effects
                    if (vfxInstance.HasFloat("Density"))
                        vfxInstance.SetFloat("Density", 0.6f); // Reduce density for performance
                    break;
            }
            
            await Task.Yield();
        }
        
        private float GetCurrentFrameRate()
        {
            if (VRPerformanceMonitor.Instance != null)
            {
                return VRPerformanceMonitor.Instance.GetCurrentMetrics().frameRate;
            }
            
            return 1f / Time.deltaTime;
        }
        
        /// <summary>
        /// Get migration statistics
        /// </summary>
        public string GetMigrationReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== VFX Graph Migration Report ===");
            report.AppendLine($"Total Migrations: {totalParticleSystemsMigrated}");
            report.AppendLine($"Performance Gain: {(postMigrationFrameRate - originalFrameRate):F1} FPS");
            report.AppendLine($"Original FPS: {originalFrameRate:F1}");
            report.AppendLine($"Post-Migration FPS: {postMigrationFrameRate:F1}");
            report.AppendLine();
            
            report.AppendLine("Individual Migrations:");
            foreach (var migration in completedMigrations)
            {
                report.AppendLine($"- {migration.originalName}: {(migration.migrationSuccessful ? "SUCCESS" : "FAILED")}");
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// Revert a specific migration
        /// </summary>
        public void RevertMigration(ParticleSystem originalSystem)
        {
            if (migrationMap.ContainsKey(originalSystem))
            {
                var vfxInstance = migrationMap[originalSystem];
                if (vfxInstance != null)
                {
                    Destroy(vfxInstance.gameObject);
                }
                
                originalSystem.gameObject.SetActive(true);
                migrationMap.Remove(originalSystem);
                
                Debug.Log($"Reverted migration for {originalSystem.name}");
            }
        }
        
        /// <summary>
        /// Revert all migrations
        /// </summary>
        public void RevertAllMigrations()
        {
            foreach (var kvp in migrationMap)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
                
                if (kvp.Key != null)
                {
                    kvp.Key.gameObject.SetActive(true);
                }
            }
            
            migrationMap.Clear();
            completedMigrations.Clear();
            totalParticleSystemsMigrated = 0;
            
            Debug.Log("All VFX Graph migrations reverted");
        }
    }
} 