using UnityEngine;
using VRBoxingGame.UI;
using VRBoxingGame.Boxing;
using VRBoxingGame.Audio;

namespace VRBoxingGame.Setup
{
    /// <summary>
    /// Comprehensive VR Scene Setup - Configures all systems for immediate gameplay
    /// </summary>
    public class VRSceneSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        public bool setupOnStart = true;
        public bool createPrefabsOnStart = true;
        public bool assignMaterialsOnStart = true;
        
        [Header("Debug")]
        public bool enableDebugLogs = true;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupCompleteVRScene();
            }
        }
        
        [ContextMenu("Setup Complete VR Scene")]
        public void SetupCompleteVRScene()
        {
            Log("🚀 Starting Complete VR Scene Setup...");
            
            // Step 1: Create and assign materials
            if (assignMaterialsOnStart)
            {
                AssignMaterials();
            }
            
            // Step 2: Create prefabs
            if (createPrefabsOnStart)
            {
                CreateAndAssignPrefabs();
            }
            
            // Step 3: Setup audio system
            SetupAudioSystem();
            
            // Step 4: Verify hand colliders
            VerifyHandColliders();
            
            // Step 5: Setup UI connections
            SetupUIConnections();
            
            // Step 6: Initialize background system
            InitializeBackgroundSystem();
            
            Log("✅ VR Scene Setup Complete! Game is ready to play!");
        }
        
        private void AssignMaterials()
        {
            Log("📦 Assigning Materials...");
            
            var prefabCreator = FindObjectOfType<CirclePrefabCreator>();
            if (prefabCreator != null)
            {
                // Load materials from Resources or create them
                prefabCreator.whiteMaterial = Resources.Load<Material>("WhiteCircleMaterial");
                prefabCreator.grayMaterial = Resources.Load<Material>("GrayCircleMaterial");
                prefabCreator.blockMaterial = Resources.Load<Material>("RedBlockMaterial");
                
                // If not found in Resources, try to find in project
                if (prefabCreator.whiteMaterial == null)
                {
                    prefabCreator.whiteMaterial = FindMaterialByName("WhiteCircleMaterial");
                }
                if (prefabCreator.grayMaterial == null)
                {
                    prefabCreator.grayMaterial = FindMaterialByName("GrayCircleMaterial");
                }
                if (prefabCreator.blockMaterial == null)
                {
                    prefabCreator.blockMaterial = FindMaterialByName("RedBlockMaterial");
                }
                
                Log($"Materials assigned: White={prefabCreator.whiteMaterial != null}, Gray={prefabCreator.grayMaterial != null}, Block={prefabCreator.blockMaterial != null}");
            }
        }
        
        private Material FindMaterialByName(string materialName)
        {
            // This is a simplified approach - in a real project you'd use AssetDatabase
            var allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var mat in allMaterials)
            {
                if (mat.name == materialName)
                {
                    return mat;
                }
            }
            return null;
        }
        
        private void CreateAndAssignPrefabs()
        {
            Log("🎯 Creating Circle Prefabs...");
            
            var prefabCreator = FindObjectOfType<CirclePrefabCreator>();
            if (prefabCreator != null)
            {
                prefabCreator.CreateCirclePrefabs();
                Log("Prefabs created and assigned to RhythmTargetSystem");
            }
            else
            {
                LogWarning("CirclePrefabCreator not found in scene!");
            }
        }
        
        private void SetupAudioSystem()
        {
            Log("🎵 Setting up Audio System...");
            
            var audioManager = FindObjectOfType<AdvancedAudioManager>();
            var testTrack = FindObjectOfType<TestTrack>();
            
            if (audioManager != null && testTrack != null)
            {
                // Generate procedural test track
                testTrack.GenerateTestTrack();
                
                // Assign to audio manager
                var audioSource = testTrack.GetComponent<AudioSource>();
                if (audioSource != null && audioSource.clip != null)
                {
                    audioManager.SetMusicSource(audioSource);
                    Log("Test track generated and assigned to AdvancedAudioManager");
                }
            }
            else
            {
                LogWarning($"Audio setup incomplete: AudioManager={audioManager != null}, TestTrack={testTrack != null}");
            }
        }
        
        private void VerifyHandColliders()
        {
            Log("✋ Verifying Hand Colliders...");
            
            var leftHand = GameObject.FindGameObjectWithTag("LeftHand");
            var rightHand = GameObject.FindGameObjectWithTag("RightHand");
            
            bool leftHandOK = VerifyHandSetup(leftHand, "LeftHand");
            bool rightHandOK = VerifyHandSetup(rightHand, "RightHand");
            
            Log($"Hand verification: Left={leftHandOK}, Right={rightHandOK}");
        }
        
        private bool VerifyHandSetup(GameObject hand, string expectedTag)
        {
            if (hand == null)
            {
                LogWarning($"Hand with tag '{expectedTag}' not found!");
                return false;
            }
            
            var collider = hand.GetComponent<Collider>();
            if (collider == null)
            {
                LogWarning($"{expectedTag} missing collider!");
                return false;
            }
            
            if (!collider.isTrigger)
            {
                LogWarning($"{expectedTag} collider should be a trigger!");
                collider.isTrigger = true;
            }
            
            return true;
        }
        
        private void SetupUIConnections()
        {
            Log("🖥️ Setting up UI Connections...");
            
            var gameUI = FindObjectOfType<GameUI>();
            if (gameUI != null)
            {
                gameUI.FindAndAssignUIElements();
                Log("GameUI elements found and assigned");
            }
            else
            {
                LogWarning("GameUI not found in scene!");
            }
        }
        
        private void InitializeBackgroundSystem()
        {
            Log("🌌 Initializing Background System...");
            
            var backgroundSystem = FindObjectOfType<VRBoxingGame.Environment.DynamicBackgroundSystem>();
            if (backgroundSystem != null)
            {
                // Set first theme
                backgroundSystem.SwitchToTheme(0);
                Log("Background system initialized with first theme");
            }
            else
            {
                LogWarning("DynamicBackgroundSystem not found!");
            }
        }
        
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[VRSceneSetup] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[VRSceneSetup] {message}");
            }
        }
        
        [ContextMenu("Verify Scene Readiness")]
        public void VerifySceneReadiness()
        {
            Log("🔍 Verifying Scene Readiness...");
            
            bool allSystemsReady = true;
            
            // Check core systems
            allSystemsReady &= CheckSystem<GameManager>("GameManager");
            allSystemsReady &= CheckSystem<RhythmTargetSystem>("RhythmTargetSystem");
            allSystemsReady &= CheckSystem<AdvancedAudioManager>("AdvancedAudioManager");
            allSystemsReady &= CheckSystem<VRBoxingGame.HandTracking.HandTrackingManager>("HandTrackingManager");
            allSystemsReady &= CheckSystem<GameUI>("GameUI");
            
            // Check prefabs
            var rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            if (rhythmSystem != null)
            {
                bool prefabsReady = rhythmSystem.whiteCirclePrefab != null && 
                                   rhythmSystem.grayCirclePrefab != null && 
                                   rhythmSystem.combinedBlockPrefab != null;
                Log($"Prefabs ready: {prefabsReady}");
                allSystemsReady &= prefabsReady;
            }
            
            // Check hands
            bool handsReady = GameObject.FindGameObjectWithTag("LeftHand") != null && 
                             GameObject.FindGameObjectWithTag("RightHand") != null;
            Log($"Hands ready: {handsReady}");
            allSystemsReady &= handsReady;
            
            if (allSystemsReady)
            {
                Log("🎉 SCENE IS READY FOR GAMEPLAY!");
            }
            else
            {
                LogWarning("⚠️ Scene setup incomplete. Run 'Setup Complete VR Scene' to fix.");
            }
        }
        
        private bool CheckSystem<T>(string systemName) where T : Component
        {
            bool exists = FindObjectOfType<T>() != null;
            Log($"{systemName}: {(exists ? "✅" : "❌")}");
            return exists;
        }
    }
} 