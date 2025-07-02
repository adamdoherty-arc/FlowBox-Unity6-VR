using UnityEngine;
using VRBoxingGame.Environment;
using VRBoxingGame.Core;
using VRBoxingGame.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRBoxingGame.Testing
{
    /// <summary>
    /// Validates rain scene functionality and integration
    /// </summary>
    public class RainSceneValidator : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool runValidationOnStart = true;
        public bool enableDebugLogs = true;
        
        [Header("Test Results")]
        [SerializeField] private bool rainSceneCreatorValid = false;
        [SerializeField] private bool sceneLoadingManagerValid = false;
        [SerializeField] private bool audioManagerValid = false;
        [SerializeField] private bool sceneTransformationValid = false;
        
        private void Start()
        {
            if (runValidationOnStart)
            {
                ValidateRainScene();
            }
        }
        
        [ContextMenu("Validate Rain Scene")]
        public void ValidateRainScene()
        {
            Debug.Log("🔍 Starting Rain Scene Validation...");
            
            ValidateRainSceneCreator();
            ValidateSceneLoadingManager();
            ValidateAudioManager();
            ValidateSceneTransformation();
            
            bool allValid = rainSceneCreatorValid && sceneLoadingManagerValid && 
                           audioManagerValid && sceneTransformationValid;
            
            if (allValid)
            {
                Debug.Log("✅ Rain Scene Validation PASSED - All systems ready!");
            }
            else
            {
                Debug.LogWarning("⚠️ Rain Scene Validation FAILED - Check individual components");
            }
        }
        
        private void ValidateRainSceneCreator()
        {
            LogDebug("Validating RainSceneCreator...");
            
            var rainCreator = CachedReferenceManager.Get<RainSceneCreator>();
            if (rainCreator == null)
            {
                // Create one for testing
                GameObject rainObj = new GameObject("Rain Scene Creator (Test)");
                rainCreator = rainObj.AddComponent<RainSceneCreator>();
            }
            
            // Test method existence
            try
            {
                rainCreator.CreateCompleteRainEnvironment();
                rainCreator.DestroyRainEnvironment();
                rainSceneCreatorValid = true;
                LogDebug("✅ RainSceneCreator validation passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ RainSceneCreator validation failed: {e.Message}");
                rainSceneCreatorValid = false;
            }
        }
        
        private void ValidateSceneLoadingManager()
        {
            LogDebug("Validating SceneLoadingManager...");
            
            var sceneManager = CachedReferenceManager.Get<SceneLoadingManager>();
            if (sceneManager == null)
            {
                GameObject sceneObj = new GameObject("Scene Loading Manager (Test)");
                sceneManager = sceneObj.AddComponent<SceneLoadingManager>();
            }
            
            try
            {
                // Test that rain storm scene type exists
                var rainStormType = SceneLoadingManager.SceneType.RainStorm;
                sceneManager.GetSceneName(rainStormType);
                sceneLoadingManagerValid = true;
                LogDebug("✅ SceneLoadingManager validation passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ SceneLoadingManager validation failed: {e.Message}");
                sceneLoadingManagerValid = false;
            }
        }
        
        private void ValidateAudioManager()
        {
            LogDebug("Validating AdvancedAudioManager...");
            
            var audioManager = CachedReferenceManager.Get<AdvancedAudioManager>();
            if (audioManager == null)
            {
                GameObject audioObj = new GameObject("Advanced Audio Manager (Test)");
                audioManager = audioObj.AddComponent<AdvancedAudioManager>();
            }
            
            try
            {
                // Test new environmental audio methods
                audioManager.SetEnvironmentalAudio(true);
                audioManager.SetEnvironmentalAudio(false);
                audioManager.SetUnderwaterMode(true);
                audioManager.SetUnderwaterMode(false);
                audioManagerValid = true;
                LogDebug("✅ AdvancedAudioManager validation passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ AdvancedAudioManager validation failed: {e.Message}");
                audioManagerValid = false;
            }
        }
        
        private void ValidateSceneTransformation()
        {
            LogDebug("Validating SceneTransformationSystem...");
            
            var transformSystem = CachedReferenceManager.Get<SceneTransformationSystem>();
            if (transformSystem == null)
            {
                GameObject transformObj = new GameObject("Scene Transformation System (Test)");
                transformSystem = transformObj.AddComponent<SceneTransformationSystem>();
            }
            
            try
            {
                // Test rain scene transformation
                transformSystem.SetSceneType(SceneTransformationSystem.SceneType.RainStorm);
                
                // Create test target
                GameObject testTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                testTarget.name = "TestTarget";
                
                var transformedTarget = transformSystem.TransformTarget(testTarget, 
                    RhythmTargetSystem.CircleType.White);
                
                if (transformedTarget != null)
                {
                    sceneTransformationValid = true;
                    LogDebug("✅ SceneTransformationSystem validation passed");
                }
                else
                {
                    Debug.LogError("❌ SceneTransformationSystem returned null transformed target");
                    sceneTransformationValid = false;
                }
                
                // Cleanup
                DestroyImmediate(testTarget);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ SceneTransformationSystem validation failed: {e.Message}");
                sceneTransformationValid = false;
            }
        }
        
        [ContextMenu("Test Rain Scene Loading")]
        public async Task TestRainSceneLoading()
        {
            Debug.Log("🌧️ Testing Rain Scene Loading...");
            
            var sceneManager = SceneLoadingManager.Instance;
            if (sceneManager == null)
            {
                Debug.LogError("❌ SceneLoadingManager instance not found");
                return;
            }
            
            try
            {
                await sceneManager.LoadSceneAsync(SceneLoadingManager.SceneType.RainStorm);
                Debug.Log("✅ Rain scene loaded successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Rain scene loading failed: {e.Message}");
            }
        }
        
        [ContextMenu("Test Rain Target Transformation")]
        public void TestRainTargetTransformation()
        {
            Debug.Log("🎯 Testing Rain Target Transformation...");
            
            var transformSystem = SceneTransformationSystem.Instance;
            if (transformSystem == null)
            {
                Debug.LogError("❌ SceneTransformationSystem instance not found");
                return;
            }
            
            // Set to rain scene
            transformSystem.SetSceneType(SceneTransformationSystem.SceneType.RainStorm);
            
            // Create test targets
            GameObject whiteTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject grayTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            whiteTarget.name = "WhiteTarget_Test";
            grayTarget.name = "GrayTarget_Test";
            
            try
            {
                var transformedWhite = transformSystem.TransformTarget(whiteTarget, 
                    RhythmTargetSystem.CircleType.White);
                var transformedGray = transformSystem.TransformTarget(grayTarget, 
                    RhythmTargetSystem.CircleType.Gray);
                
                if (transformedWhite != null && transformedGray != null)
                {
                    Debug.Log("✅ Rain target transformation successful!");
                    LogDebug($"White target: {transformedWhite.name}");
                    LogDebug($"Gray target: {transformedGray.name}");
                }
                else
                {
                    Debug.LogError("❌ Rain target transformation failed - null results");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Rain target transformation failed: {e.Message}");
            }
            finally
            {
                // Cleanup
                DestroyImmediate(whiteTarget);
                DestroyImmediate(grayTarget);
            }
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }
        
        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Rain Scene Validation Status:");
            
            GUILayout.Label($"RainSceneCreator: {(rainSceneCreatorValid ? "✅" : "❌")}");
            GUILayout.Label($"SceneLoadingManager: {(sceneLoadingManagerValid ? "✅" : "❌")}");
            GUILayout.Label($"AudioManager: {(audioManagerValid ? "✅" : "❌")}");
            GUILayout.Label($"SceneTransformation: {(sceneTransformationValid ? "✅" : "❌")}");
            
            if (GUILayout.Button("Validate Rain Scene"))
            {
                ValidateRainScene();
            }
            
            if (GUILayout.Button("Test Rain Scene Loading"))
            {
                TestRainSceneLoading();
            }
            
            GUILayout.EndArea();
        }
    }
} 