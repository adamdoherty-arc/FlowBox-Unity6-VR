using UnityEngine;
using System.Collections.Generic;
using System;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// Cached Reference Manager to replace expensive FindObjectOfType calls
    /// Major performance optimization for VR
    /// </summary>
    public class CachedReferenceManager : MonoBehaviour
    {
        private static Dictionary<Type, Component> componentCache = new Dictionary<Type, Component>();
        private static Dictionary<string, GameObject> gameObjectCache = new Dictionary<string, GameObject>();
        
        public static CachedReferenceManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCache();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeCache()
        {
            Debug.Log("🗃️ Initializing Cached Reference Manager...");
            
            // Pre-cache common components
            CacheComponent<GameManager>();
            CacheComponent<AdvancedAudioManager>();
            CacheComponent<HandTrackingManager>();
            CacheComponent<BoxingFormTracker>();
            CacheComponent<RhythmTargetSystem>();
            CacheComponent<SceneLoadingManager>();
            CacheComponent<FlowModeSystem>();
            CacheComponent<TwoHandedStaffSystem>();
            CacheComponent<ComprehensiveDodgingSystem>();
            CacheComponent<AICoachVisualSystem>();
        }
        
        public static T Get<T>() where T : Component
        {
            if (Instance == null) return FindObjectOfType<T>();
            
            Type type = typeof(T);
            
            if (componentCache.TryGetValue(type, out Component cached))
            {
                if (cached != null) return cached as T;
                componentCache.Remove(type);
            }
            
            return Instance.CacheComponent<T>();
        }
        
        public static GameObject GetGameObject(string name)
        {
            if (Instance == null) return GameObject.Find(name);
            
            if (gameObjectCache.TryGetValue(name, out GameObject cached))
            {
                if (cached != null) return cached;
                gameObjectCache.Remove(name);
            }
            
            GameObject found = GameObject.Find(name);
            if (found != null) gameObjectCache[name] = found;
            return found;
        }
        
        private T CacheComponent<T>() where T : Component
        {
            T found = FindObjectOfType<T>();
            if (found != null)
            {
                componentCache[typeof(T)] = found;
                Debug.Log($"📝 Cached {typeof(T).Name}");
            }
            return found;
        }
        
        public static void RegisterComponent<T>(T component) where T : Component
        {
            if (component != null)
            {
                componentCache[typeof(T)] = component;
            }
        }
        
        public void RefreshAllCaches()
        {
            componentCache.Clear();
            gameObjectCache.Clear();
            InitializeCache();
            Debug.Log("🔄 All caches refreshed");
        }
    }
} 