using UnityEngine;
using VRBoxingGame.Setup;
using VRBoxingGame.Core;

namespace VRBoxingGame.Setup
{
    /// <summary>
    /// Simple setup script for TestScene - ensures rain scene works immediately
    /// </summary>
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Test Scene Configuration")]
        public bool setupOnAwake = true;
        public bool enableRainSceneByDefault = true;
        
        private void Awake()
        {
            if (setupOnAwake)
            {
                SetupTestScene();
            }
        }
        
        [ContextMenu("Setup Test Scene")]
        public void SetupTestScene()
        {
            Debug.Log("🎮 Setting up Test Scene for Rain Scene gameplay...");
            
            // Find or create CompleteGameSetup
            CompleteGameSetup gameSetup = CachedReferenceManager.Get<CompleteGameSetup>();
            if (gameSetup == null)
            {
                GameObject setupObj = new GameObject("Complete Game Setup");
                gameSetup = setupObj.AddComponent<CompleteGameSetup>();
                
                // Configure for rain scene
                gameSetup.enableRainScene = enableRainSceneByDefault;
                gameSetup.startWithRainScene = enableRainSceneByDefault;
                gameSetup.setupOnStart = false; // We'll trigger it manually
            }
            
            // Trigger the complete setup
            gameSetup.SetupCompleteVRBoxingGame();
            
            Debug.Log("✅ Test Scene setup complete! Rain scene should be ready to play!");
        }
    }
} 