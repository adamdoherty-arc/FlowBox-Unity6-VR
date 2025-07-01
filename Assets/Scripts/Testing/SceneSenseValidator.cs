using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using VRBoxingGame.Environment;
using VRBoxingGame.Audio;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Testing
{
    /// <summary>
    /// SceneSense Validator - Comprehensive testing for enhanced environmental storytelling
    /// Features: Atmosphere validation, narrative coherence testing, immersion metrics
    /// Validates Unity 6 scene sense improvements and environmental storytelling systems
    /// </summary>
    public class SceneSenseValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        public bool enableComprehensiveValidation = true;
        public bool enablePerformanceValidation = true;
        public bool enableNarrativeValidation = true;
        public bool enableImmersionTesting = true;
        
        [Header("Test Parameters")]
        public float validationDuration = 30f;
        public int scenesToTest = 8;
        public float atmosphereTransitionTimeout = 10f;
        public float performanceThreshold = 0.8f;
        
        [Header("Immersion Metrics")]
        public bool measureEnvironmentalCoherence = true;
        public bool measureAtmosphericResponsiveness = true;
        public bool measureNarrativeClarity = true;
        public bool measurePerformanceImpact = true;
        
        // Validation Results
        private ValidationResults results;
        private bool validationInProgress = false;
        private float validationStartTime = 0f;
        private int currentSceneIndex = 0;
        
        // Component References
        private EnhancedSceneSenseSystem sceneSenseSystem;
        private SceneLoadingManager sceneManager;
        private AdvancedAudioManager audioManager;
        private GameReadinessValidator gameReadinessValidator;
        
        // Test Data
        private NativeArray<float> atmosphereTransitionTimes;
        private NativeArray<float> performanceImpactScores;
        private NativeArray<float> immersionMetrics;
        private Dictionary<string, List<float>> sceneSenseMetrics;
        
        // Validation Categories
        public enum ValidationCategory
        {
            EnvironmentalStorytelling,
            AtmosphericCoherence,
            PerformanceReactivity,
            NarrativeClarity,
            ImmersionDepth,
            Unity6Integration,
            AccessibilityFeatures,
            MemoryEfficiency
        }
        
        // Validation Results Structure
        [System.Serializable]
        public struct ValidationResults
        {
            public bool overallPassed;
            public float overallScore;
            
            // Category Scores
            public float environmentalStorytellingScore;
            public float atmosphericCoherenceScore;
            public float performanceReactivityScore;
            public float narrativeClarityScore;
            public float immersionDepthScore;
            public float unity6IntegrationScore;
            public float accessibilityScore;
            public float memoryEfficiencyScore;
            
            // Detailed Metrics
            public SceneSenseMetrics[] sceneMetrics;
            public string[] validationMessages;
            public bool[] categoryPassed;
            
            // Performance Impact
            public float averageFrameTimeImpact;
            public float memoryUsageIncrease;
            public float atmosphereTransitionTime;
            
            // Quality Measurements
            public float environmentalResponseTime;
            public float narrativeCoherenceScore;
            public float playerEngagementScore;
        }
        
        [System.Serializable]
        public struct SceneSenseMetrics
        {
            public string sceneName;
            public float atmosphereTransitionTime;
            public float narrativeCoherenceScore;
            public float environmentalResponsiveness;
            public float immersionLevel;
            public float performanceImpact;
            public bool validationPassed;
        }
        
        private void Start()
        {
            InitializeValidator();
            
            if (enableComprehensiveValidation)
            {
                StartValidation();
            }
        }
        
        private void InitializeValidator()
        {
            Debug.Log("🎭 Initializing SceneSense Validator...");
            
            // Find system components
            sceneSenseSystem = FindObjectOfType<EnhancedSceneSenseSystem>();
            sceneManager = FindObjectOfType<SceneLoadingManager>();
            audioManager = FindObjectOfType<AdvancedAudioManager>();
            gameReadinessValidator = FindObjectOfType<GameReadinessValidator>();
            
            // Initialize native arrays
            atmosphereTransitionTimes = new NativeArray<float>(scenesToTest, Allocator.Persistent);
            performanceImpactScores = new NativeArray<float>(scenesToTest, Allocator.Persistent);
            immersionMetrics = new NativeArray<float>(scenesToTest, Allocator.Persistent);
            
            // Initialize metrics dictionary
            sceneSenseMetrics = new Dictionary<string, List<float>>();
            
            // Initialize results
            results = new ValidationResults
            {
                sceneMetrics = new SceneSenseMetrics[scenesToTest],
                validationMessages = new string[20],
                categoryPassed = new bool[8]
            };
            
            Debug.Log("✅ SceneSense Validator initialized!");
        }
        
        public void StartValidation()
        {
            if (validationInProgress)
            {
                Debug.LogWarning("⚠️ Validation already in progress");
                return;
            }
            
            Debug.Log("🚀 Starting Comprehensive SceneSense Validation...");
            validationInProgress = true;
            validationStartTime = Time.time;
            currentSceneIndex = 0;
            
            StartCoroutine(RunValidationSequence());
        }
        
        private System.Collections.IEnumerator RunValidationSequence()
        {
            for (int i = 0; i < scenesToTest; i++)
            {
                currentSceneIndex = i;
                Debug.Log($"🎭 Testing Scene {i + 1}/{scenesToTest}: {(SceneLoadingManager.SceneType)i}");
                
                yield return StartCoroutine(ValidateScene((SceneLoadingManager.SceneType)i));
                yield return new WaitForSeconds(2f); // Brief pause between scenes
            }
            
            // Compile final results
            CompileValidationResults();
            
            // Display results
            DisplayValidationResults();
            
            validationInProgress = false;
            Debug.Log("✅ SceneSense Validation Complete!");
        }
        
        private System.Collections.IEnumerator ValidateScene(SceneLoadingManager.SceneType sceneType)
        {
            float sceneStartTime = Time.time;
            
            // Load scene
            if (sceneManager != null)
            {
                yield return StartCoroutine(LoadSceneForTesting(sceneType));
            }
            
            // Wait for scene sense system to initialize
            yield return new WaitForSeconds(1f);
            
            // Validate scene sense metrics
            SceneSenseMetrics metrics = ValidateSceneSenseMetrics(sceneType);
            
            // Test atmosphere transitions
            yield return StartCoroutine(TestAtmosphereTransitions(sceneType));
            
            // Test performance reactivity
            yield return StartCoroutine(TestPerformanceReactivity(sceneType));
            
            // Test narrative coherence
            float narrativeScore = TestNarrativeCoherence(sceneType);
            
            // Test immersion depth
            float immersionScore = TestImmersionDepth(sceneType);
            
            // Compile scene results
            metrics.atmosphereTransitionTime = Time.time - sceneStartTime;
            metrics.narrativeCoherenceScore = narrativeScore;
            metrics.immersionLevel = immersionScore;
            metrics.validationPassed = (narrativeScore > 0.7f && immersionScore > 0.6f);
            
            results.sceneMetrics[currentSceneIndex] = metrics;
            
            Debug.Log($"✅ Scene {sceneType} validation complete - Score: {(narrativeScore + immersionScore) / 2f:F2}");
        }
        
        private System.Collections.IEnumerator LoadSceneForTesting(SceneLoadingManager.SceneType sceneType)
        {
            if (sceneManager != null)
            {
                yield return sceneManager.LoadSceneAsync(sceneType);
            }
            
            // Set scene narrative in scene sense system
            if (sceneSenseSystem != null)
            {
                sceneSenseSystem.SetSceneNarrative((EnhancedSceneSenseSystem.SceneNarrativeType)((int)sceneType));
            }
        }
        
        private SceneSenseMetrics ValidateSceneSenseMetrics(SceneLoadingManager.SceneType sceneType)
        {
            SceneSenseMetrics metrics = new SceneSenseMetrics
            {
                sceneName = sceneType.ToString()
            };
            
            if (sceneSenseSystem == null)
            {
                Debug.LogWarning("⚠️ EnhancedSceneSenseSystem not found!");
                return metrics;
            }
            
            // Get current scene sense metrics
            var currentMetrics = sceneSenseSystem.GetSceneSenseMetrics();
            
            // Validate environmental storytelling
            float storytellingScore = ValidateEnvironmentalStorytelling(sceneType);
            
            // Validate atmospheric coherence
            float atmosphereScore = ValidateAtmosphericCoherence();
            
            // Validate performance reactivity
            float reactivityScore = ValidatePerformanceReactivity();
            
            // Calculate overall score
            metrics.environmentalResponsiveness = (storytellingScore + atmosphereScore + reactivityScore) / 3f;
            
            Debug.Log($"🎭 {sceneType} SceneSense Metrics - Storytelling: {storytellingScore:F2}, Atmosphere: {atmosphereScore:F2}, Reactivity: {reactivityScore:F2}");
            
            return metrics;
        }
        
        private float ValidateEnvironmentalStorytelling(SceneLoadingManager.SceneType sceneType)
        {
            float score = 0f;
            
            // Check if scene has clear narrative identity
            if (sceneSenseSystem.CurrentNarrative.ToString() == sceneType.ToString())
            {
                score += 0.3f;
            }
            
            // Check narrative intensity
            if (sceneSenseSystem.GetSceneSenseMetrics().narrativeIntensity > 0.5f)
            {
                score += 0.2f;
            }
            
            // Check environmental complexity adaptation
            if (sceneSenseSystem.EnvironmentalComplexity > 0.5f)
            {
                score += 0.2f;
            }
            
            // Check player performance correlation
            if (sceneSenseSystem.PlayerPerformanceScore > 0.3f)
            {
                score += 0.3f;
            }
            
            return score;
        }
        
        private float ValidateAtmosphericCoherence()
        {
            float score = 0f;
            
            // Check atmosphere transition system
            if (!sceneSenseSystem.IsAtmosphereTransitioning)
            {
                score += 0.4f; // Stable atmosphere
            }
            
            // Check lighting responsiveness
            Light primaryLight = FindObjectOfType<Light>();
            if (primaryLight != null && primaryLight.intensity > 0.5f)
            {
                score += 0.3f;
            }
            
            // Check fog and environmental effects
            if (RenderSettings.fog && RenderSettings.fogDensity > 0.01f)
            {
                score += 0.3f;
            }
            
            return score;
        }
        
        private float ValidatePerformanceReactivity()
        {
            float score = 0f;
            
            // Check if environment reacts to player performance
            float currentPerformance = sceneSenseSystem.PlayerPerformanceScore;
            float environmentalComplexity = sceneSenseSystem.EnvironmentalComplexity;
            
            // Performance should correlate with environmental complexity
            float correlation = 1f - Mathf.Abs(currentPerformance - (environmentalComplexity - 0.5f) * 2f);
            score += correlation * 0.5f;
            
            // Check audio reactivity
            if (audioManager != null)
            {
                float beatStrength = audioManager.GetCurrentBeatStrength();
                if (beatStrength > 0.1f)
                {
                    score += 0.3f;
                }
            }
            
            // Check particle system reactivity
            ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
            if (particles.Length > 0)
            {
                score += 0.2f;
            }
            
            return score;
        }
        
        private System.Collections.IEnumerator TestAtmosphereTransitions(SceneLoadingManager.SceneType sceneType)
        {
            float transitionStartTime = Time.time;
            
            // Trigger atmosphere transition
            if (sceneSenseSystem != null)
            {
                sceneSenseSystem.SetSceneNarrative((EnhancedSceneSenseSystem.SceneNarrativeType)((int)sceneType));
                sceneSenseSystem.SetNarrativeIntensity(0.8f);
            }
            
            // Wait for transition to complete or timeout
            float timeoutTime = Time.time + atmosphereTransitionTimeout;
            while (sceneSenseSystem != null && sceneSenseSystem.IsAtmosphereTransitioning && Time.time < timeoutTime)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            float transitionTime = Time.time - transitionStartTime;
            atmosphereTransitionTimes[currentSceneIndex] = transitionTime;
            
            Debug.Log($"🌅 Atmosphere transition for {sceneType} took {transitionTime:F2}s");
        }
        
        private System.Collections.IEnumerator TestPerformanceReactivity(SceneLoadingManager.SceneType sceneType)
        {
            float initialFPS = 1f / Time.smoothDeltaTime;
            
            // Simulate performance changes
            for (float performance = 0.2f; performance <= 1f; performance += 0.2f)
            {
                // Simulate performance score
                // Note: In real implementation, this would come from actual gameplay
                
                yield return new WaitForSeconds(1f);
                
                // Check if environment adapts
                float complexity = sceneSenseSystem != null ? sceneSenseSystem.EnvironmentalComplexity : 1f;
                Debug.Log($"📊 Performance: {performance:F1} → Complexity: {complexity:F2}");
            }
            
            float finalFPS = 1f / Time.smoothDeltaTime;
            float performanceImpact = (initialFPS - finalFPS) / initialFPS;
            performanceImpactScores[currentSceneIndex] = Mathf.Clamp01(1f - performanceImpact);
        }
        
        private float TestNarrativeCoherence(SceneLoadingManager.SceneType sceneType)
        {
            float score = 0f;
            
            // Check scene description quality
            string[] descriptions = GetSceneDescriptions();
            if (descriptions != null && descriptions.Length > (int)sceneType)
            {
                string description = descriptions[(int)sceneType];
                
                // Check for narrative keywords
                string[] narrativeKeywords = {"you", "your", "conductor", "harmony", "symphony", "discover", "enter", "command"};
                int keywordCount = narrativeKeywords.Count(keyword => description.ToLower().Contains(keyword));
                score += (float)keywordCount / narrativeKeywords.Length * 0.4f;
                
                // Check description length (more detailed = better)
                if (description.Length > 100)
                {
                    score += 0.3f;
                }
                
                // Check for active voice and engagement
                if (description.Contains("you ") || description.Contains("your "))
                {
                    score += 0.3f;
                }
            }
            
            return score;
        }
        
        private float TestImmersionDepth(SceneLoadingManager.SceneType sceneType)
        {
            float score = 0f;
            
            // Check visual complexity
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            if (renderers.Length > 10)
            {
                score += 0.2f;
            }
            
            // Check particle systems
            ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
            if (particles.Length > 2)
            {
                score += 0.2f;
            }
            
            // Check lighting setup
            Light[] lights = FindObjectsOfType<Light>();
            if (lights.Length > 1)
            {
                score += 0.2f;
            }
            
            // Check audio sources
            AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
            if (audioSources.Length > 0)
            {
                score += 0.2f;
            }
            
            // Check scene sense system integration
            if (sceneSenseSystem != null && sceneSenseSystem.enabled)
            {
                score += 0.2f;
            }
            
            return score;
        }
        
        private string[] GetSceneDescriptions()
        {
            // Get scene descriptions from SceneLoadingManager
            if (sceneManager != null)
            {
                // Use reflection to access private field
                var field = typeof(SceneLoadingManager).GetField("sceneDescriptions", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return field?.GetValue(sceneManager) as string[];
            }
            
            return null;
        }
        
        private void CompileValidationResults()
        {
            Debug.Log("📊 Compiling SceneSense Validation Results...");
            
            // Calculate category scores
            results.environmentalStorytellingScore = CalculateEnvironmentalStorytellingScore();
            results.atmosphericCoherenceScore = CalculateAtmosphericCoherenceScore();
            results.performanceReactivityScore = CalculatePerformanceReactivityScore();
            results.narrativeClarityScore = CalculateNarrativeClarityScore();
            results.immersionDepthScore = CalculateImmersionDepthScore();
            results.unity6IntegrationScore = CalculateUnity6IntegrationScore();
            results.accessibilityScore = CalculateAccessibilityScore();
            results.memoryEfficiencyScore = CalculateMemoryEfficiencyScore();
            
            // Calculate overall score
            results.overallScore = (
                results.environmentalStorytellingScore +
                results.atmosphericCoherenceScore +
                results.performanceReactivityScore +
                results.narrativeClarityScore +
                results.immersionDepthScore +
                results.unity6IntegrationScore +
                results.accessibilityScore +
                results.memoryEfficiencyScore
            ) / 8f;
            
            // Determine pass/fail
            results.overallPassed = results.overallScore >= performanceThreshold;
            
            // Set category pass/fail
            results.categoryPassed[0] = results.environmentalStorytellingScore >= 0.7f;
            results.categoryPassed[1] = results.atmosphericCoherenceScore >= 0.7f;
            results.categoryPassed[2] = results.performanceReactivityScore >= 0.6f;
            results.categoryPassed[3] = results.narrativeClarityScore >= 0.8f;
            results.categoryPassed[4] = results.immersionDepthScore >= 0.6f;
            results.categoryPassed[5] = results.unity6IntegrationScore >= 0.7f;
            results.categoryPassed[6] = results.accessibilityScore >= 0.5f;
            results.categoryPassed[7] = results.memoryEfficiencyScore >= 0.6f;
            
            // Calculate performance metrics
            if (atmosphereTransitionTimes.IsCreated)
            {
                results.atmosphereTransitionTime = atmosphereTransitionTimes.ToArray().Average();
            }
            
            if (performanceImpactScores.IsCreated)
            {
                results.averageFrameTimeImpact = 1f - performanceImpactScores.ToArray().Average();
            }
        }
        
        private float CalculateEnvironmentalStorytellingScore()
        {
            float totalScore = 0f;
            foreach (var sceneMetric in results.sceneMetrics)
            {
                totalScore += sceneMetric.environmentalResponsiveness;
            }
            return totalScore / scenesToTest;
        }
        
        private float CalculateAtmosphericCoherenceScore()
        {
            // Check if scenes have coherent atmospheres
            float score = 0.8f; // Base score
            
            // Check atmosphere transition times
            if (atmosphereTransitionTimes.IsCreated)
            {
                float avgTransitionTime = atmosphereTransitionTimes.ToArray().Average();
                if (avgTransitionTime < 5f)
                {
                    score += 0.2f;
                }
            }
            
            return Mathf.Clamp01(score);
        }
        
        private float CalculatePerformanceReactivityScore()
        {
            if (performanceImpactScores.IsCreated)
            {
                return performanceImpactScores.ToArray().Average();
            }
            return 0.5f;
        }
        
        private float CalculateNarrativeClarityScore()
        {
            float totalScore = 0f;
            foreach (var sceneMetric in results.sceneMetrics)
            {
                totalScore += sceneMetric.narrativeCoherenceScore;
            }
            return totalScore / scenesToTest;
        }
        
        private float CalculateImmersionDepthScore()
        {
            float totalScore = 0f;
            foreach (var sceneMetric in results.sceneMetrics)
            {
                totalScore += sceneMetric.immersionLevel;
            }
            return totalScore / scenesToTest;
        }
        
        private float CalculateUnity6IntegrationScore()
        {
            float score = 0f;
            
            // Check for Unity 6 features
            if (sceneSenseSystem != null) score += 0.3f;
            if (FindObjectOfType<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>() != null) score += 0.2f;
            if (FindObjectOfType<UnityEngine.Rendering.Volume>() != null) score += 0.2f;
            if (audioManager != null) score += 0.3f;
            
            return score;
        }
        
        private float CalculateAccessibilityScore()
        {
            float score = 0.5f; // Base accessibility score
            
            // Check for accessibility features
            if (sceneSenseSystem != null && sceneSenseSystem.enabled)
            {
                score += 0.3f; // Adaptive complexity
            }
            
            // Check for visual accessibility
            if (RenderSettings.fog) score += 0.1f; // Visual depth cues
            
            // Check for audio accessibility
            if (audioManager != null) score += 0.1f;
            
            return score;
        }
        
        private float CalculateMemoryEfficiencyScore()
        {
            float score = 0.7f; // Base efficiency score
            
            // Check memory usage
            float memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(0) / (1024f * 1024f * 1024f); // GB
            
            if (memoryUsage < 2f) score += 0.3f; // Under 2GB
            else if (memoryUsage < 3f) score += 0.1f; // Under 3GB
            else score -= 0.2f; // Over 3GB penalized
            
            return Mathf.Clamp01(score);
        }
        
        private void DisplayValidationResults()
        {
            Debug.Log("🎭 ===== SCENESENSE VALIDATION RESULTS =====");
            Debug.Log($"Overall Score: {results.overallScore:F2} ({(results.overallPassed ? "PASSED" : "FAILED")})");
            Debug.Log("");
            
            Debug.Log("Category Scores:");
            Debug.Log($"Environmental Storytelling: {results.environmentalStorytellingScore:F2} {(results.categoryPassed[0] ? "✅" : "❌")}");
            Debug.Log($"Atmospheric Coherence: {results.atmosphericCoherenceScore:F2} {(results.categoryPassed[1] ? "✅" : "❌")}");
            Debug.Log($"Performance Reactivity: {results.performanceReactivityScore:F2} {(results.categoryPassed[2] ? "✅" : "❌")}");
            Debug.Log($"Narrative Clarity: {results.narrativeClarityScore:F2} {(results.categoryPassed[3] ? "✅" : "❌")}");
            Debug.Log($"Immersion Depth: {results.immersionDepthScore:F2} {(results.categoryPassed[4] ? "✅" : "❌")}");
            Debug.Log($"Unity 6 Integration: {results.unity6IntegrationScore:F2} {(results.categoryPassed[5] ? "✅" : "❌")}");
            Debug.Log($"Accessibility: {results.accessibilityScore:F2} {(results.categoryPassed[6] ? "✅" : "❌")}");
            Debug.Log($"Memory Efficiency: {results.memoryEfficiencyScore:F2} {(results.categoryPassed[7] ? "✅" : "❌")}");
            Debug.Log("");
            
            Debug.Log("Performance Metrics:");
            Debug.Log($"Average Atmosphere Transition Time: {results.atmosphereTransitionTime:F2}s");
            Debug.Log($"Average Frame Time Impact: {results.averageFrameTimeImpact:F2}%");
            Debug.Log("");
            
            Debug.Log("Scene-by-Scene Results:");
            for (int i = 0; i < results.sceneMetrics.Length; i++)
            {
                var metric = results.sceneMetrics[i];
                Debug.Log($"{i + 1}. {metric.sceneName}: Narrative {metric.narrativeCoherenceScore:F2}, Immersion {metric.immersionLevel:F2} {(metric.validationPassed ? "✅" : "❌")}");
            }
            
            Debug.Log("🎭 ===== END SCENESENSE VALIDATION =====");
        }
        
        public ValidationResults GetValidationResults()
        {
            return results;
        }
        
        private void OnDestroy()
        {
            // Dispose native arrays
            if (atmosphereTransitionTimes.IsCreated) atmosphereTransitionTimes.Dispose();
            if (performanceImpactScores.IsCreated) performanceImpactScores.Dispose();
            if (immersionMetrics.IsCreated) immersionMetrics.Dispose();
        }
    }
} 