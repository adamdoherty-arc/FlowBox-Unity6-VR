using UnityEngine;
using UnityEngine.Rendering;
using VRBoxingGame.Boxing;
using VRBoxingGame.Performance;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using VRBoxingGame.Core;

namespace VRBoxingGame.Environment
{
    /// <summary>
    /// Fish Target Behavior - Implements size-dependent AI behaviors
    /// Small fish scatter, medium fish retreat and regroup, large fish become aggressive
    /// </summary>
    public class FishTargetBehavior : MonoBehaviour
    {
        [Header("Fish Properties")]
        public FishSize fishSize = FishSize.Small;
        public RhythmTargetSystem.CircleType circleType;
        public float baseSpeed = 2f;
        public float aggressionMultiplier = 2f;
        
        [Header("Behavior States")]
        public bool isScattered = false;
        public bool isStunned = false;
        public bool isAggressive = false;
        public bool isRetreating = false;
        
        public enum FishSize
        {
            Small,
            Medium,
            Large
        }
        
        // Private variables
        private Vector3 originalPosition;
        private Vector3 scatterDirection;
        private float scatterTimer = 0f;
        private float scatterDuration = 2f;
        private float stunTimer = 0f;
        private float stunDuration = 1.5f;
        private float aggressionTimer = 0f;
        private float aggressionDuration = 5f;
        private Rigidbody fishRigidbody;
        private int hitCount = 0;
        
        private void Start()
        {
            originalPosition = transform.position;
            fishRigidbody = GetComponent<Rigidbody>();
            if (fishRigidbody == null)
            {
                fishRigidbody = gameObject.AddComponent<Rigidbody>();
                fishRigidbody.useGravity = false;
                fishRigidbody.drag = 2f;
            }
        }
        
        private void Update()
        {
            UpdateBehaviorStates();
            ApplyFishMovement();
        }
        
        public void Initialize(RhythmTargetSystem.CircleType type, FishSize size)
        {
            circleType = type;
            fishSize = size;
            
            // Set speed based on size
            switch (size)
            {
                case FishSize.Small:
                    baseSpeed = 3f;
                    break;
                case FishSize.Medium:
                    baseSpeed = 2f;
                    break;
                case FishSize.Large:
                    baseSpeed = 1.5f;
                    break;
            }
        }
        
        private void UpdateBehaviorStates()
        {
            // Update scatter behavior
            if (isScattered)
            {
                scatterTimer += Time.deltaTime;
                if (scatterTimer >= scatterDuration)
                {
                    isScattered = false;
                    scatterTimer = 0f;
                }
            }
            
            // Update stun behavior
            if (isStunned)
            {
                stunTimer += Time.deltaTime;
                if (stunTimer >= stunDuration)
                {
                    isStunned = false;
                    stunTimer = 0f;
                }
            }
            
            // Update aggression behavior
            if (isAggressive)
            {
                aggressionTimer += Time.deltaTime;
                if (aggressionTimer >= aggressionDuration)
                {
                    isAggressive = false;
                    aggressionTimer = 0f;
                }
            }
        }
        
        private void ApplyFishMovement()
        {
            if (fishRigidbody == null) return;
            
            Vector3 movement = Vector3.zero;
            float currentSpeed = baseSpeed;
            
            if (isScattered)
            {
                // Scatter away from hit point
                movement = scatterDirection * currentSpeed * 2f;
            }
            else if (isStunned)
            {
                // Stunned fish move slowly
                movement = -scatterDirection * currentSpeed * 0.3f;
            }
            else if (isAggressive)
            {
                // Aggressive fish move toward player
                Vector3 playerPosition = VRBoxingGame.Core.VRCameraHelper.PlayerPosition;
                Vector3 directionToPlayer = (playerPosition - transform.position).normalized;
                movement = directionToPlayer * currentSpeed * aggressionMultiplier;
            }
            else if (isRetreating)
            {
                // Retreating fish move away from player
                Vector3 playerPosition = VRBoxingGame.Core.VRCameraHelper.PlayerPosition;
                Vector3 directionFromPlayer = (transform.position - playerPosition).normalized;
                movement = directionFromPlayer * currentSpeed;
            }
            else
            {
                // Normal swimming behavior
                movement = GetNormalSwimmingMovement();
            }
            
            fishRigidbody.AddForce(movement, ForceMode.Acceleration);
        }
        
        private Vector3 GetNormalSwimmingMovement()
        {
            // Gentle swimming pattern
            float time = Time.time;
            return new Vector3(
                Mathf.Sin(time * 0.5f) * baseSpeed * 0.3f,
                Mathf.Sin(time * 0.7f) * baseSpeed * 0.2f,
                Mathf.Cos(time * 0.6f) * baseSpeed * 0.3f
            );
        }
        
        // Size-specific behaviors
        public void ScatterFromHit(Vector3 hitPosition)
        {
            if (fishSize != FishSize.Small) return;
            
            isScattered = true;
            scatterTimer = 0f;
            scatterDirection = (transform.position - hitPosition).normalized;
            
            // Add some randomness to scatter direction
            scatterDirection += Random.insideUnitSphere * 0.3f;
            scatterDirection.Normalize();
            
            // Immediate impulse
            if (fishRigidbody != null)
            {
                fishRigidbody.AddForce(scatterDirection * baseSpeed * 5f, ForceMode.Impulse);
            }
        }
        
        public void GetStunnedAndRetreat(Vector3 hitPosition)
        {
            if (fishSize != FishSize.Medium) return;
            
            hitCount++;
            if (hitCount >= GetRequiredHitsForMediumFish())
            {
                isStunned = true;
                isRetreating = true;
                stunTimer = 0f;
                
                // Calculate retreat direction (away from hit position)
                scatterDirection = (transform.position - hitPosition).normalized;
                
                // Start retreat and regroup sequence
                _ = RetreatAndRegroupAsync();
                
                Debug.Log($"Medium fish {name} stunned and retreating");
            }
        }
        
        public void GetPushedBackAndReturnAggressive(Vector3 hitPosition)
        {
            if (fishSize != FishSize.Large) return;
            
            // Get pushed back
            Vector3 pushDirection = (transform.position - hitPosition).normalized;
            transform.position += pushDirection * 2f;
            
            // Become aggressive after a short delay
            _ = BecomeAggressiveAfterDelayAsync(1f);
            
            Debug.Log($"Large fish {name} pushed back and becoming aggressive");
        }
        
        public void RegroupWithSchool()
        {
            isRetreating = false;
            isStunned = false;
            hitCount = 0;
        }
        
        public void SetAggressive(bool aggressive)
        {
            isAggressive = aggressive;
            if (aggressive)
            {
                aggressionTimer = 0f;
            }
        }
        
        private int GetRequiredHitsForMediumFish()
        {
            return Random.Range(2, 4); // 2-3 hits required
        }
        
        private async Task RetreatAndRegroupAsync()
        {
            try
            {
                await Task.Delay(3000); // 3 second retreat
                
                if (this != null && gameObject != null)
                {
                    isStunned = false;
                    isRetreating = false;
                    hitCount = 0;
                    Debug.Log($"Medium fish {name} finished retreating and regrouped");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in fish retreat: {ex.Message}");
            }
        }
        
        private async Task BecomeAggressiveAfterDelayAsync(float delay)
        {
            try
            {
                await Task.Delay((int)(delay * 1000));
                
                if (this != null && gameObject != null)
                {
                    isAggressive = true;
                    aggressionTimer = 0f;
                    Debug.Log($"Large fish {name} became aggressive");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in fish aggression: {ex.Message}");
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Handle hand collision
            if (other.CompareTag("LeftHand") || other.CompareTag("RightHand"))
            {
                // Trigger hit based on circle type and hand
                RhythmTargetSystem.HandSide requiredHand = circleType == RhythmTargetSystem.CircleType.White ? 
                    RhythmTargetSystem.HandSide.Left : RhythmTargetSystem.HandSide.Right;
                
                bool correctHand = (other.CompareTag("LeftHand") && requiredHand == RhythmTargetSystem.HandSide.Left) ||
                                  (other.CompareTag("RightHand") && requiredHand == RhythmTargetSystem.HandSide.Right);
                
                if (correctHand)
                {
                    OnFishHit(other.transform.position);
                }
            }
        }
        
        private void OnFishHit(Vector3 hitPosition)
        {
            // Create hit effect based on fish size
            CreateHitEffect(hitPosition);
            
            // Apply size-specific behavior
            switch (fishSize)
            {
                case FishSize.Small:
                    ScatterFromHit(hitPosition);
                    break;
                    
                case FishSize.Medium:
                    GetStunnedAndRetreat(hitPosition);
                    break;
                    
                case FishSize.Large:
                    GetPushedBackAndReturnAggressive(hitPosition);
                    break;
            }
            
            // Notify the underwater fish system
            var fishSystem = CachedReferenceManager.Get<UnderwaterFishSystem>();
            if (fishSystem != null)
            {
                var hitData = new RhythmTargetSystem.CircleHitData
                {
                    circleType = circleType,
                    requiredHand = circleType == RhythmTargetSystem.CircleType.White ? 
                        RhythmTargetSystem.HandSide.Left : RhythmTargetSystem.HandSide.Right,
                    hitPosition = hitPosition,
                    accuracy = 1f,
                    speed = baseSpeed,
                    isPerfectTiming = true
                };
                
                fishSystem.OnFishHit(hitData);
            }
        }
        
        private void CreateHitEffect(Vector3 hitPosition)
        {
            // Create bubble effect
            GameObject effect = new GameObject("FishHitEffect");
            effect.transform.position = hitPosition;
            
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.startSize = Random.Range(0.1f, 0.3f);
            main.startColor = new Color(0.7f, 0.9f, 1f, 0.8f);
            main.maxParticles = 20;
            
            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 10)
            });
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
            
            // Auto-destroy after effect
            Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// School Behavior - Manages fish flocking behavior
    /// </summary>
    public class SchoolBehavior : MonoBehaviour
    {
        [Header("School Settings")]
        public float schoolRadius = 3f;
        public float cohesionStrength = 1f;
        public float separationStrength = 2f;
        public float alignmentStrength = 1f;
        
        private Vector3 schoolCenter;
        private Rigidbody fishRigidbody;
        
        private void Start()
        {
            fishRigidbody = GetComponent<Rigidbody>();
        }
        
        public void Initialize(Vector3 center, float radius)
        {
            schoolCenter = center;
            schoolRadius = radius;
        }
        
        public void UpdateSchoolPosition(Vector3 newCenter)
        {
            schoolCenter = newCenter;
        }
        
        private void FixedUpdate()
        {
            if (fishRigidbody == null) return;
            
            ApplyFlockingBehavior();
        }
        
        private void ApplyFlockingBehavior()
        {
            Vector3 cohesion = GetCohesionForce();
            Vector3 separation = GetSeparationForce();
            Vector3 alignment = GetAlignmentForce();
            
            Vector3 totalForce = cohesion * cohesionStrength + 
                               separation * separationStrength + 
                               alignment * alignmentStrength;
            
            fishRigidbody.AddForce(totalForce, ForceMode.Acceleration);
        }
        
        private Vector3 GetCohesionForce()
        {
            // Move toward school center
            Vector3 directionToCenter = schoolCenter - transform.position;
            float distance = directionToCenter.magnitude;
            
            if (distance > schoolRadius)
            {
                return directionToCenter.normalized * (distance - schoolRadius);
            }
            
            return Vector3.zero;
        }
        
        private Vector3 GetSeparationForce()
        {
            // Avoid crowding neighbors
            Vector3 separationForce = Vector3.zero;
            int neighborCount = 0;
            
            Collider[] nearbyFish = Physics.OverlapSphere(transform.position, 1f);
            foreach (var fish in nearbyFish)
            {
                if (fish.gameObject != gameObject && fish.CompareTag("Fish"))
                {
                    Vector3 diff = transform.position - fish.transform.position;
                    float distance = diff.magnitude;
                    if (distance > 0)
                    {
                        separationForce += diff.normalized / distance;
                        neighborCount++;
                    }
                }
            }
            
            if (neighborCount > 0)
            {
                separationForce /= neighborCount;
            }
            
            return separationForce;
        }
        
        private Vector3 GetAlignmentForce()
        {
            // Align with neighbors' velocity
            Vector3 averageVelocity = Vector3.zero;
            int neighborCount = 0;
            
            Collider[] nearbyFish = Physics.OverlapSphere(transform.position, 2f);
            foreach (var fish in nearbyFish)
            {
                if (fish.gameObject != gameObject && fish.CompareTag("Fish"))
                {
                    Rigidbody fishRb = fish.GetComponent<Rigidbody>();
                    if (fishRb != null)
                    {
                        averageVelocity += fishRb.velocity;
                        neighborCount++;
                    }
                }
            }
            
            if (neighborCount > 0)
            {
                averageVelocity /= neighborCount;
                return (averageVelocity - fishRigidbody.velocity) * 0.1f;
            }
            
            return Vector3.zero;
        }
    }
    
    /// <summary>
    /// Bioluminescence Effect - Manages fish glow effects
    /// </summary>
    public class BioluminescenceEffect : MonoBehaviour
    {
        [Header("Glow Settings")]
        public Color glowColor = Color.cyan;
        public float intensity = 1f;
        public float pulseSpeed = 2f;
        public bool enablePulse = true;
        
        private Light glowLight;
        private Renderer fishRenderer;
        private Material glowMaterial;
        private float baseIntensity;
        
        private void Start()
        {
            SetupBioluminescence();
        }
        
        public void Initialize(Color color)
        {
            glowColor = color;
            baseIntensity = intensity;
        }
        
        private void SetupBioluminescence()
        {
            // Create glow light
            GameObject lightGO = new GameObject("BiolumLight");
            lightGO.transform.SetParent(transform);
            lightGO.transform.localPosition = Vector3.zero;
            
            glowLight = lightGO.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.range = 5f;
            glowLight.intensity = intensity;
            glowLight.color = glowColor;
            
            // Create glowing material with pooling
            fishRenderer = GetComponent<Renderer>();
            if (fishRenderer != null)
            {
                glowMaterial = MaterialPool.Instance != null ? 
                    MaterialPool.Instance.GetURPLitMaterial(glowColor) :
                    new Material(fishRenderer.material);
                glowMaterial.color = glowColor;
                glowMaterial.EnableKeyword("_EMISSION");
                glowMaterial.SetColor("_EmissionColor", glowColor * intensity);
                
                fishRenderer.material = glowMaterial;
            }
            
            baseIntensity = intensity;
        }
        
        private void Update()
        {
            if (enablePulse)
            {
                ApplyPulseEffect();
            }
        }
        
        private void ApplyPulseEffect()
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.3f;
            float currentIntensity = intensity * pulse;
            
            if (glowLight != null)
            {
                glowLight.intensity = currentIntensity;
            }
            
            if (glowMaterial != null)
            {
                glowMaterial.SetColor("_EmissionColor", glowColor * currentIntensity);
            }
        }
        
        public void SetIntensity(float newIntensity)
        {
            intensity = newIntensity;
            
            if (glowLight != null)
            {
                glowLight.intensity = intensity;
            }
            
            if (glowMaterial != null)
            {
                glowMaterial.SetColor("_EmissionColor", glowColor * intensity);
            }
        }
        
        public void SetColor(Color newColor)
        {
            glowColor = newColor;
            
            if (glowLight != null)
            {
                glowLight.color = glowColor;
            }
            
            if (glowMaterial != null)
            {
                glowMaterial.SetColor("_EmissionColor", glowColor * intensity);
            }
        }
    }
}