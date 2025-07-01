using UnityEngine;
using UnityEngine.Events;
using VRBoxingGame.Boxing;
using System.Collections;
using System.Threading.Tasks;

namespace VRBoxingGame.Boxing
{
    /// <summary>
    /// Individual boxing target component for rhythm circles
    /// </summary>
    public class BoxingTarget : MonoBehaviour
    {
        [Header("Target Settings")]
        public float lifetime = 5f;
        public int baseScore = 100;
        public HandType requiredHand = HandType.Either;
        
        [Header("Visual Settings")]
        public float hitEffectDuration = 0.3f;
        public AnimationCurve scaleOnHit = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);
        
        public enum HandType
        {
            Either,
            Left,
            Right
        }
        
        // Events
        public UnityEvent<int> OnTargetHit;
        public UnityEvent OnTargetMissed;
        
        // Private variables
        private float spawnTime;
        private bool isHit = false;
        private Renderer targetRenderer;
        private Collider targetCollider;
        private Vector3 originalScale;
        
        // Properties
        public bool IsHit => isHit;
        public float TimeAlive => Time.time - spawnTime;
        public float TimeRemaining => lifetime - TimeAlive;
        
        private void Start()
        {
            spawnTime = Time.time;
            targetRenderer = GetComponent<Renderer>();
            targetCollider = GetComponent<Collider>();
            originalScale = transform.localScale;
            
            // Auto-destroy after lifetime
            Destroy(gameObject, lifetime);
        }
        
        private void Update()
        {
            // Flash warning when time is running out
            if (TimeRemaining < 1f && !isHit)
            {
                float alpha = Mathf.PingPong(Time.time * 5f, 1f);
                if (targetRenderer != null)
                {
                    Color color = targetRenderer.material.color;
                    color.a = alpha;
                    targetRenderer.material.color = color;
                }
            }
        }
        
        public void OnHit(HandType handUsed)
        {
            if (isHit) return;
            
            // Check if correct hand was used
            if (requiredHand != HandType.Either && requiredHand != handUsed)
            {
                return; // Wrong hand used
            }
            
            isHit = true;
            
            // Calculate score based on timing
            float timingScore = CalculateTimingScore();
            int finalScore = Mathf.RoundToInt(baseScore * timingScore);
            
            // Trigger events
            OnTargetHit?.Invoke(finalScore);
            
            // Visual feedback
            _ = HitEffectAsync();
            
            // Disable target
            if (targetCollider != null)
                targetCollider.enabled = false;
        }
        
        private float CalculateTimingScore()
        {
            float timeAlive = TimeAlive;
            float perfectTime = lifetime * 0.8f; // 80% of lifetime is perfect timing
            float timeDiff = Mathf.Abs(timeAlive - perfectTime);
            float maxDiff = lifetime * 0.2f;
            
            return Mathf.Clamp01(1f - (timeDiff / maxDiff));
        }
        
        private async Task HitEffectAsync()
        {
            try
            {
                if (targetRenderer != null)
                {
                    // Flash effect
                    Color originalColor = targetRenderer.material.color;
                    targetRenderer.material.color = Color.white;
                    
                    await Task.Delay(50); // 50ms flash
                    
                    if (targetRenderer != null)
                        targetRenderer.material.color = originalColor;
                }
                
                // Scale animation
                float duration = 0.3f;
                float elapsedTime = 0f;
                Vector3 targetScale = originalScale * 1.3f;
                
                while (elapsedTime < duration && gameObject != null)
                {
                    elapsedTime += Time.deltaTime;
                    
                    float progress = elapsedTime / duration;
                    float scaleMultiplier = scaleOnHit.Evaluate(progress);
                    
                    transform.localScale = Vector3.Lerp(originalScale, targetScale, scaleMultiplier);
                    
                    // Fade out
                    if (targetRenderer != null)
                    {
                        Color color = targetRenderer.material.color;
                        color.a = 1f - progress;
                        targetRenderer.material.color = color;
                    }
                    
                    await Task.Yield();
                }
                
                // Destroy after effect
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in hit effect: {ex.Message}");
                
                // Ensure object is destroyed even on error
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            }
        }
        
        private void OnDestroy()
        {
            if (!isHit)
            {
                OnTargetMissed?.Invoke();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (isHit) return;
            
            HandType handUsed = HandType.Either;
            
            if (other.CompareTag("LeftHand"))
            {
                handUsed = HandType.Left;
            }
            else if (other.CompareTag("RightHand"))
            {
                handUsed = HandType.Right;
            }
            else
            {
                return; // Not a hand
            }
            
            OnHit(handUsed);
        }
    }
} 