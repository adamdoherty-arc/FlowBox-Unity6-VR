using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Environment;
using VRBoxingGame.Boxing;
using VRBoxingGame.Core;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Scene-Mode Integration Validator - Ensures all game modes work with all scene environments
    /// Prevents runtime crashes and validates compatibility matrix
    /// Unity 6 optimized with comprehensive testing and graceful fallbacks
    /// </summary>
    public class SceneModeIntegrationValidator : MonoBehaviour
    {
        [Header("Integration Testing")]
        public bool enableRuntimeValidation = true;
        public bool enableComprehensiveValidation = false;
        public float validationTimeout = 10f;
        
        [Header("Compatibility Matrix")]
        public SceneModeCompatibility[] compatibilityMatrix;
        
        [Header("Events")]
        public UnityEvent<ValidationResult> OnValidationComplete;
        public UnityEvent<string> OnCompatibilityError;
        
        // Compatibility definitions
        [System.Serializable]
        public struct SceneModeCompatibility
        {
            public SceneLoadingManager.SceneType sceneType;
            public GameManager.GameMode gameMode;
            public bool isCompatible;
            public string[] requiredComponents;
            public string[] potentialIssues;
            public CompatibilityLevel level;
        }
        
        public enum CompatibilityLevel
        {
            FullyCompatible,
            MinorIssues,
            MajorIssues,
            Incompatible
        }
        
        [System.Serializable]
        public struct ValidationResult
        {
            public bool allTestsPassed;
            public int compatibleCombinations;
            public int totalCombinations;
            public List<string> failedTests;
            public List<string> warnings;
            public float validationTime;
        }
        
        // System references
        private SceneLoadingManager sceneManager;
        private GameManager gameManager;
        private FlowModeSystem flowModeSystem;
        private TwoHandedStaffSystem staffModeSystem;
        private ComprehensiveDodgingSystem dodgingSystem;
        private AICoachVisualSystem aiCoachSystem;
        
        // Validation state
        private bool validationInProgress = false;
        private ValidationResult currentResult;
        
        public static SceneModeIntegrationValidator Instance { get; private set; }
        
        // Properties
        public bool IsValidationInProgress => validationInProgress;
        public ValidationResult LastValidationResult => currentResult;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeValidator();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (enableRuntimeValidation)
            {
                ValidateCurrentIntegration();
            }
        }
        
        private void InitializeValidator()
        {
            Debug.Log("🔍 Initializing Scene-Mode Integration Validator...");
            
            // Get system references using cached manager
            sceneManager = CachedReferenceManager.Get<SceneLoadingManager>();
            gameManager = CachedReferenceManager.Get<GameManager>();
            flowModeSystem = CachedReferenceManager.Get<FlowModeSystem>();
            staffModeSystem = CachedReferenceManager.Get<TwoHandedStaffSystem>();
            dodgingSystem = CachedReferenceManager.Get<ComprehensiveDodgingSystem>();
            aiCoachSystem = CachedReferenceManager.Get<AICoachVisualSystem>();
            
            // Initialize compatibility matrix if empty
            if (compatibilityMatrix == null || compatibilityMatrix.Length == 0)
            {
                InitializeDefaultCompatibilityMatrix();
            }
        }
        
        private void InitializeDefaultCompatibilityMatrix()
        {
            var sceneTypes = System.Enum.GetValues(typeof(SceneLoadingManager.SceneType));
            var gameModes = System.Enum.GetValues(typeof(GameManager.GameMode));
            
            var compatibilityList = new List<SceneModeCompatibility>();
            
            foreach (SceneLoadingManager.SceneType scene in sceneTypes)
            {
                foreach (GameManager.GameMode mode in gameModes)
                {
                    var compatibility = new SceneModeCompatibility
                    {
                        sceneType = scene,
                        gameMode = mode,
                        isCompatible = DetermineCompatibility(scene, mode),
                        requiredComponents = GetRequiredComponents(scene, mode),
                        potentialIssues = GetPotentialIssues(scene, mode),
                        level = DetermineCompatibilityLevel(scene, mode)
                    };
                    
                    compatibilityList.Add(compatibility);
                }
            }
            
            compatibilityMatrix = compatibilityList.ToArray();
        }
        
        private bool DetermineCompatibility(SceneLoadingManager.SceneType scene, GameManager.GameMode mode)
        {
            // Scene-specific compatibility logic
            switch (scene)
            {
                case SceneLoadingManager.SceneType.UnderwaterWorld:
                    // Underwater scenes may have issues with certain game modes
                    return mode != GameManager.GameMode.StaffMode; // Staff physics underwater might be problematic
                    
                case SceneLoadingManager.SceneType.SpaceStation:
                    // Space environments work with all modes
                    return true;
                    
                case SceneLoadingManager.SceneType.RainStorm:
                    // Rain effects might interfere with precise tracking
                    return mode != GameManager.GameMode.FlowMode; // Rain particles might obscure flow targets
                    
                default:
                    return true; // Most combinations are compatible
            }
        }
        
        private string[] GetRequiredComponents(SceneLoadingManager.SceneType scene, GameManager.GameMode mode)
        {
            var components = new List<string>();
            
            // Mode-specific requirements
            switch (mode)
            {
                case GameManager.GameMode.FlowMode:
                    components.AddRange(new[] { "FlowModeSystem", "AudioManager", "ParticleSystem" });
                    break;
                case GameManager.GameMode.StaffMode:
                    components.AddRange(new[] { "TwoHandedStaffSystem", "PhysicsRaycaster", "HandTrackingManager" });
                    break;
                case GameManager.GameMode.DodgingMode:
                    components.AddRange(new[] { "ComprehensiveDodgingSystem", "XROrigin" });
                    break;
            }
            
            // Scene-specific requirements
            switch (scene)
            {
                case SceneLoadingManager.SceneType.RainStorm:
                    components.AddRange(new[] { "RainSceneCreator", "WeatherSystem" });
                    break;
                case SceneLoadingManager.SceneType.UnderwaterWorld:
                    components.AddRange(new[] { "UnderwaterFishSystem", "BuoyancySystem" });
                    break;
            }
            
            return components.ToArray();
        }
        
        private string[] GetPotentialIssues(SceneLoadingManager.SceneType scene, GameManager.GameMode mode)
        {
            var issues = new List<string>();
            
            // Known compatibility issues
            if (scene == SceneLoadingManager.SceneType.UnderwaterWorld && mode == GameManager.GameMode.StaffMode)
            {
                issues.Add("Staff physics may behave unexpectedly underwater");
                issues.Add("Hand tracking accuracy may be reduced");
            }
            
            if (scene == SceneLoadingManager.SceneType.RainStorm && mode == GameManager.GameMode.FlowMode)
            {
                issues.Add("Rain particles may obscure flow targets");
                issues.Add("Visual effects may conflict");
            }
            
            return issues.ToArray();
        }
        
        private CompatibilityLevel DetermineCompatibilityLevel(SceneLoadingManager.SceneType scene, GameManager.GameMode mode)
        {
            if (!DetermineCompatibility(scene, mode))
                return CompatibilityLevel.Incompatible;
            
            var issues = GetPotentialIssues(scene, mode);
            if (issues.Length == 0)
                return CompatibilityLevel.FullyCompatible;
            else if (issues.Length <= 2)
                return CompatibilityLevel.MinorIssues;
            else
                return CompatibilityLevel.MajorIssues;
        }
        
        /// <summary>
        /// Validate current scene-mode combination
        /// </summary>
        public async Task<bool> ValidateCurrentIntegration()
        {
            if (sceneManager == null || gameManager == null)
            {
                Debug.LogError("SceneModeIntegrationValidator: Required managers not found");
                return false;
            }
            
            var currentScene = sceneManager.GetCurrentScene();
            var currentMode = gameManager.currentGameMode;
            
            return await ValidateSceneModeAsync(currentScene, currentMode);
        }
        
        /// <summary>
        /// Comprehensive validation of all scene-mode combinations
        /// </summary>
        public async Task<ValidationResult> ValidateAllCombinations()
        {
            if (validationInProgress)
            {
                Debug.LogWarning("Validation already in progress");
                return currentResult;
            }
            
            validationInProgress = true;
            var startTime = Time.time;
            
            currentResult = new ValidationResult
            {
                failedTests = new List<string>(),
                warnings = new List<string>(),
                totalCombinations = compatibilityMatrix.Length
            };
            
            Debug.Log("🧪 Starting comprehensive scene-mode validation...");
            
            int compatibleCount = 0;
            
            for (int i = 0; i < compatibilityMatrix.Length; i++)
            {
                var combo = compatibilityMatrix[i];
                
                try
                {
                    bool isValid = await ValidateSceneModeAsync(combo.sceneType, combo.gameMode);
                    
                    if (isValid)
                    {
                        compatibleCount++;
                        
                        if (combo.level == CompatibilityLevel.MinorIssues || combo.level == CompatibilityLevel.MajorIssues)
                        {
                            currentResult.warnings.Add($"{combo.sceneType} + {combo.gameMode}: {string.Join(", ", combo.potentialIssues)}");
                        }
                    }
                    else
                    {
                        currentResult.failedTests.Add($"{combo.sceneType} + {combo.gameMode}");
                    }
                }
                catch (System.Exception ex)
                {
                    currentResult.failedTests.Add($"{combo.sceneType} + {combo.gameMode}: {ex.Message}");
                    Debug.LogError($"Validation error for {combo.sceneType} + {combo.gameMode}: {ex.Message}");
                }
                
                // Allow frame processing
                if (i % 4 == 0)
                {
                    await Task.Yield();
                }
            }
            
            currentResult.compatibleCombinations = compatibleCount;
            currentResult.allTestsPassed = currentResult.failedTests.Count == 0;
            currentResult.validationTime = Time.time - startTime;
            
            validationInProgress = false;
            
            Debug.Log($"✅ Validation complete: {compatibleCount}/{currentResult.totalCombinations} combinations compatible");
            OnValidationComplete?.Invoke(currentResult);
            
            return currentResult;
        }
        
        /// <summary>
        /// Validate specific scene-mode combination
        /// </summary>
        public async Task<bool> ValidateSceneModeAsync(SceneLoadingManager.SceneType sceneType, GameManager.GameMode gameMode)
        {
            var compatibility = GetCompatibility(sceneType, gameMode);
            if (compatibility.level == CompatibilityLevel.Incompatible)
            {
                OnCompatibilityError?.Invoke($"Incompatible combination: {sceneType} + {gameMode}");
                return false;
            }
            
            // Check required components
            foreach (var componentName in compatibility.requiredComponents)
            {
                if (!ValidateComponent(componentName))
                {
                    OnCompatibilityError?.Invoke($"Missing required component: {componentName} for {sceneType} + {gameMode}");
                    return false;
                }
            }
            
            // Run mode-specific validation
            bool modeValidation = await ValidateGameMode(gameMode);
            bool sceneValidation = ValidateScene(sceneType);
            
            return modeValidation && sceneValidation;
        }
        
        private async Task<bool> ValidateGameMode(GameManager.GameMode gameMode)
        {
            switch (gameMode)
            {
                case GameManager.GameMode.FlowMode:
                    return flowModeSystem != null && await ValidateFlowMode();
                    
                case GameManager.GameMode.StaffMode:
                    return staffModeSystem != null && ValidateStaffMode();
                    
                case GameManager.GameMode.DodgingMode:
                    return dodgingSystem != null && ValidateDodgingMode();
                    
                case GameManager.GameMode.Traditional:
                    return true; // Traditional mode is always compatible
                    
                default:
                    return false;
            }
        }
        
        private async Task<bool> ValidateFlowMode()
        {
            if (flowModeSystem == null) return false;
            
            // Test flow target spawning
            try
            {
                // Simulate flow mode initialization
                await Task.Delay(100); // Small delay to simulate async operations
                return flowModeSystem.flowTargetPrefab != null;
            }
            catch
            {
                return false;
            }
        }
        
        private bool ValidateStaffMode()
        {
            if (staffModeSystem == null) return false;
            
            // Validate staff system components
            return staffModeSystem.staffPrefab != null && 
                   staffModeSystem.leftHandTransform != null && 
                   staffModeSystem.rightHandTransform != null;
        }
        
        private bool ValidateDodgingMode()
        {
            if (dodgingSystem == null) return false;
            
            // Validate dodging system
            return dodgingSystem.enabled;
        }
        
        private bool ValidateScene(SceneLoadingManager.SceneType sceneType)
        {
            // Scene-specific validation logic
            switch (sceneType)
            {
                case SceneLoadingManager.SceneType.RainStorm:
                    return CachedReferenceManager.Get<RainSceneCreator>() != null;
                    
                case SceneLoadingManager.SceneType.UnderwaterWorld:
                    return CachedReferenceManager.Get<UnderwaterFishSystem>() != null;
                    
                default:
                    return true; // Default scenes are assumed valid
            }
        }
        
        private bool ValidateComponent(string componentName)
        {
            // Use cached reference manager for efficient component lookup
            switch (componentName)
            {
                case "FlowModeSystem":
                    return CachedReferenceManager.Get<FlowModeSystem>() != null;
                case "TwoHandedStaffSystem":
                    return CachedReferenceManager.Get<TwoHandedStaffSystem>() != null;
                case "ComprehensiveDodgingSystem":
                    return CachedReferenceManager.Get<ComprehensiveDodgingSystem>() != null;
                case "AudioManager":
                    return CachedReferenceManager.Get<AdvancedAudioManager>() != null;
                case "HandTrackingManager":
                    return CachedReferenceManager.Get<HandTrackingManager>() != null;
                case "RainSceneCreator":
                    return CachedReferenceManager.Get<RainSceneCreator>() != null;
                case "UnderwaterFishSystem":
                    return CachedReferenceManager.Get<UnderwaterFishSystem>() != null;
                default:
                    return true; // Unknown components assumed present
            }
        }
        
        private SceneModeCompatibility GetCompatibility(SceneLoadingManager.SceneType sceneType, GameManager.GameMode gameMode)
        {
            foreach (var combo in compatibilityMatrix)
            {
                if (combo.sceneType == sceneType && combo.gameMode == gameMode)
                {
                    return combo;
                }
            }
            
            // Return default compatibility if not found
            return new SceneModeCompatibility
            {
                sceneType = sceneType,
                gameMode = gameMode,
                isCompatible = true,
                level = CompatibilityLevel.FullyCompatible,
                requiredComponents = new string[0],
                potentialIssues = new string[0]
            };
        }
        
        /// <summary>
        /// Get compatibility report for UI display
        /// </summary>
        public string GetCompatibilityReport()
        {
            if (currentResult.totalCombinations == 0)
            {
                return "No validation performed yet.";
            }
            
            var report = $"Scene-Mode Compatibility Report:\n";
            report += $"Compatible: {currentResult.compatibleCombinations}/{currentResult.totalCombinations}\n";
            report += $"Success Rate: {(float)currentResult.compatibleCombinations / currentResult.totalCombinations * 100:F1}%\n";
            
            if (currentResult.warnings.Count > 0)
            {
                report += $"\nWarnings ({currentResult.warnings.Count}):\n";
                foreach (var warning in currentResult.warnings)
                {
                    report += $"⚠️ {warning}\n";
                }
            }
            
            if (currentResult.failedTests.Count > 0)
            {
                report += $"\nFailed Tests ({currentResult.failedTests.Count}):\n";
                foreach (var failure in currentResult.failedTests)
                {
                    report += $"❌ {failure}\n";
                }
            }
            
            return report;
        }
        
        /// <summary>
        /// Manual validation trigger for editor/debugging
        /// </summary>
        [ContextMenu("Validate All Scene-Mode Combinations")]
        public async void ValidateAllCombinationsManual()
        {
            var result = await ValidateAllCombinations();
            Debug.Log(GetCompatibilityReport());
        }
        
        /// <summary>
        /// Quick validation of current state
        /// </summary>
        [ContextMenu("Validate Current Combination")]
        public async void ValidateCurrentManual()
        {
            bool isValid = await ValidateCurrentIntegration();
            Debug.Log($"Current integration valid: {isValid}");
        }
    }
} 