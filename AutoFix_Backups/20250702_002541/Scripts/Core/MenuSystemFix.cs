using UnityEngine;
using VRBoxingGame.UI;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Menu System Fix - Resolves conflicts between 3 competing menu systems
    /// Ensures only the optimized menu system is active
    /// </summary>
    public class MenuSystemFix : MonoBehaviour
    {
        [Header("Menu System Management")]
        public bool autoFixOnStart = true;
        public bool enableOptimizedMenuOnly = true;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                FixMenuSystemConflicts();
            }
        }
        
        private void FixMenuSystemConflicts()
        {
            Debug.Log("🔧 Fixing menu system conflicts...");
            
            // Find all menu systems
            var mainMenuSystems = FindObjectsOfType<MainMenuSystem>();
            var enhancedMenuSystems = FindObjectsOfType<EnhancedMainMenuSystem>();
            var optimizedMenuSystems = FindObjectsOfType<EnhancedMainMenuSystemOptimized>();
            
            Debug.Log($"Found menu systems: {mainMenuSystems.Length} Main, {enhancedMenuSystems.Length} Enhanced, {optimizedMenuSystems.Length} Optimized");
            
            if (enableOptimizedMenuOnly)
            {
                // Disable legacy menu systems
                foreach (var menu in mainMenuSystems)
                {
                    menu.gameObject.SetActive(false);
                    Debug.Log("❌ Disabled MainMenuSystem");
                }
                
                foreach (var menu in enhancedMenuSystems)
                {
                    menu.gameObject.SetActive(false);
                    Debug.Log("❌ Disabled EnhancedMainMenuSystem");
                }
                
                // Ensure optimized menu is active
                if (optimizedMenuSystems.Length == 0)
                {
                    CreateOptimizedMenuSystem();
                }
                else
                {
                    foreach (var menu in optimizedMenuSystems)
                    {
                        menu.gameObject.SetActive(true);
                        Debug.Log("✅ Enabled EnhancedMainMenuSystemOptimized");
                    }
                }
            }
            
            Debug.Log("✅ Menu system conflicts resolved");
        }
        
        private void CreateOptimizedMenuSystem()
        {
            Debug.Log("🏗️ Creating optimized menu system...");
            
            GameObject menuObj = new GameObject("Enhanced Main Menu System (Optimized)");
            menuObj.AddComponent<EnhancedMainMenuSystemOptimized>();
            
            Debug.log("✅ Created optimized menu system");
        }
        
        [ContextMenu("Fix Menu Conflicts")]
        public void ManualFix()
        {
            FixMenuSystemConflicts();
        }
    }
} 