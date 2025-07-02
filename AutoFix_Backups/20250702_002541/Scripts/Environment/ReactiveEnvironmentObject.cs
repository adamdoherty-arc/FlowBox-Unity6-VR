using UnityEngine;
using VRBoxingGame.Audio;
using VRBoxingGame.Core;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Makes environment objects react to music and audio frequency bands
    /// </summary>
    public class ReactiveEnvironmentObject : MonoBehaviour
    {
        [Header("Reactivity Settings")]
        public bool reactToScale = true;
        public bool reactToRotation = false;
        public bool reactToColor = false;
        public bool reactToLight = false;
        
        [Header("Audio Settings")]
        [Range(0, 7)]
        public int frequencyBand = 0;
        public float reactivityMultiplier = 1.0f;
        public float smoothSpeed = 5.0f;
        
        [Header("Scale Reaction")]
        public Vector3 baseScale = Vector3.one;
        public float scaleMultiplier = 0.5f;
        
        [Header("Rotation Reaction")]
        public Vector3 rotationSpeed = Vector3.zero;
        
        [Header("Color Reaction")]
        public Color baseColor = Color.white;
        public Color reactiveColor = Color.red;
        
        // Components
        private Renderer objectRenderer;
        private Light lightComponent;
        private Transform objectTransform;
        private Material originalMaterial;
        
        // Audio data
        private AdvancedAudioManager audioManager;
        private float currentAudioLevel = 0f;
        
        void Start()
        {
            // Get components
            objectTransform = transform;
            objectRenderer = GetComponent<Renderer>();
            lightComponent = GetComponent<Light>();
            
            // Store original values
            baseScale = objectTransform.localScale;
            if (objectRenderer != null)
            {
                originalMaterial = objectRenderer.material;
                baseColor = originalMaterial.color;
            }
            
            // Find audio manager
            audioManager = CachedReferenceManager.Get<AdvancedAudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning($"ReactiveEnvironmentObject on {gameObject.name} couldn't find AdvancedAudioManager");
            }
        }
        
        void Update()
        {
            if (audioManager == null) return;
            
            // Get audio data
            currentAudioLevel = GetAudioLevel();
            
            // Apply reactions
            if (reactToScale)
            {
                ReactToScale();
            }
            
            if (reactToRotation)
            {
                ReactToRotation();
            }
            
            if (reactToColor && objectRenderer != null)
            {
                ReactToColor();
            }
            
            if (reactToLight && lightComponent != null)
            {
                ReactToLight();
            }
        }
        
        private float GetAudioLevel()
        {
            // Try to get frequency band data from audio manager
            float[] frequencyBands = audioManager.GetFrequencyBands();
            if (frequencyBands != null && frequencyBands.Length > frequencyBand)
            {
                return frequencyBands[frequencyBand] * reactivityMultiplier;
            }
            
            // Fallback to simulated audio data
            return Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
        }
        
        private void ReactToScale()
        {
            Vector3 targetScale = baseScale + (baseScale * currentAudioLevel * scaleMultiplier);
            objectTransform.localScale = Vector3.Lerp(objectTransform.localScale, targetScale, Time.deltaTime * smoothSpeed);
        }
        
        private void ReactToRotation()
        {
            Vector3 currentRotation = rotationSpeed * currentAudioLevel * Time.deltaTime;
            objectTransform.Rotate(currentRotation);
        }
        
        private void ReactToColor()
        {
            Color targetColor = Color.Lerp(baseColor, reactiveColor, currentAudioLevel);
            Material material = objectRenderer.material;
            material.color = Color.Lerp(material.color, targetColor, Time.deltaTime * smoothSpeed);
        }
        
        private void ReactToLight()
        {
            float baseIntensity = 1.0f;
            float targetIntensity = baseIntensity + (currentAudioLevel * 2.0f);
            lightComponent.intensity = Mathf.Lerp(lightComponent.intensity, targetIntensity, Time.deltaTime * smoothSpeed);
        }
        
        void OnValidate()
        {
            // Clamp frequency band
            frequencyBand = Mathf.Clamp(frequencyBand, 0, 7);
        }
    }
} 