using UnityEngine;
using VRBoxingGame.Boxing;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Setup
{
    /// <summary>
    /// Handles 360-degree target spawning for immersive VR boxing experience
    /// Integrates with boxing form tracking and stance switching
    /// </summary>
    public class Target360Spawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        public int spawnIndex;
        public float spawnAngle;
        public RhythmTargetSystem rhythmSystem;
        
        [Header("Boxing Form Integration")]
        public bool adaptToStance = true;
        public bool preferDominantHand = true;
        public float stanceInfluence = 0.7f;
        
        [Header("Visual Feedback")]
        public bool showSpawnIndicator = true;
        public Color orthodoxColor = Color.blue;
        public Color southpawColor = Color.red;
        public Color neutralColor = Color.white;
        
        private LineRenderer spawnIndicator;
        private BoxingFormTracker formTracker;
        private VR360MovementSystem movementSystem;
        
        // Spawn probability modifiers
        private float orthodoxProbability = 1f;
        private float southpawProbability = 1f;
        private float lastSpawnTime;
        
        private void Start()
        {
            InitializeSpawner();
        }
        
        private void InitializeSpawner()
        {
            // Get references
            formTracker = BoxingFormTracker.Instance;
            movementSystem = VR360MovementSystem.Instance;
            
            // Create visual indicator
            if (showSpawnIndicator)
            {
                CreateSpawnIndicator();
            }
            
            // Calculate stance probabilities based on position
            CalculateStanceProbabilities();
        }
        
        private void CreateSpawnIndicator()
        {
            GameObject indicatorObj = new GameObject("SpawnIndicator");
            indicatorObj.transform.SetParent(transform);
            indicatorObj.transform.localPosition = Vector3.zero;
            
            spawnIndicator = indicatorObj.AddComponent<LineRenderer>();
            spawnIndicator.material = MaterialPool.Instance != null ? 
                MaterialPool.Instance.GetURPLitMaterial(neutralColor) :
                new Material(Shader.Find("Universal Render Pipeline/Lit"));
            spawnIndicator.startWidth = 0.02f;
            spawnIndicator.endWidth = 0.02f;
            spawnIndicator.positionCount = 2;
            spawnIndicator.enabled = false;
            
            // Set initial positions
            spawnIndicator.SetPosition(0, transform.position);
            spawnIndicator.SetPosition(1, transform.position + Vector3.up * 0.5f);
        }
        
        private void CalculateStanceProbabilities()
        {
            // Calculate probabilities based on spawn angle
            // Orthodox stance favors targets that benefit right-hand power
            // Southpaw stance favors targets that benefit left-hand power
            
            float normalizedAngle = spawnAngle / 360f;
            
            // Orthodox: stronger on right side (90-180 degrees)
            // Targets at 45-135 degrees are optimal for orthodox right cross
            float orthodoxOptimal = Mathf.Abs(normalizedAngle - 0.25f); // 90 degrees
            orthodoxProbability = 1f - orthodoxOptimal;
            
            // Southpaw: stronger on left side (180-270 degrees)  
            // Targets at 225-315 degrees are optimal for southpaw left cross
            float southpawOptimal = Mathf.Abs(normalizedAngle - 0.75f); // 270 degrees
            southpawProbability = 1f - southpawOptimal;
            
            // Normalize probabilities
            float total = orthodoxProbability + southpawProbability;
            if (total > 0)
            {
                orthodoxProbability /= total;
                southpawProbability /= total;
            }
        }
        
        private void Update()
        {
            UpdateSpawnIndicator();
        }
        
        private void UpdateSpawnIndicator()
        {
            if (!showSpawnIndicator || spawnIndicator == null) return;
            
            // Update indicator color based on current stance and probability
            Color indicatorColor = neutralColor;
            
            if (formTracker != null)
            {
                var currentStance = formTracker.CurrentStance;
                
                if (currentStance == BoxingFormTracker.BoxingStance.Orthodox)
                {
                    indicatorColor = Color.Lerp(neutralColor, orthodoxColor, orthodoxProbability);
                }
                else if (currentStance == BoxingFormTracker.BoxingStance.Southpaw)
                {
                    indicatorColor = Color.Lerp(neutralColor, southpawColor, southpawProbability);
                }
            }
            
            spawnIndicator.material.color = indicatorColor;
            
            // Show indicator when targets might spawn here
            bool shouldShow = ShouldShowIndicator();
            spawnIndicator.enabled = shouldShow;
        }
        
        private bool ShouldShowIndicator()
        {
            // Show indicator based on recent activity and stance compatibility
            if (formTracker == null) return false;
            
            float timeSinceLastSpawn = Time.time - lastSpawnTime;
            bool recentActivity = timeSinceLastSpawn < 2f;
            
            float stanceCompatibility = GetStanceCompatibility();
            bool stanceMatch = stanceCompatibility > 0.6f;
            
            return recentActivity || stanceMatch;
        }
        
        public float GetSpawnProbability()
        {
            if (!adaptToStance || formTracker == null)
                return 1f;
            
            var currentStance = formTracker.CurrentStance;
            float baseProbability = 1f;
            
            // Modify probability based on stance
            if (currentStance == BoxingFormTracker.BoxingStance.Orthodox)
            {
                baseProbability = orthodoxProbability;
            }
            else if (currentStance == BoxingFormTracker.BoxingStance.Southpaw)
            {
                baseProbability = southpawProbability;
            }
            
            // Apply stance influence
            float finalProbability = Mathf.Lerp(1f, baseProbability, stanceInfluence);
            
            // Bonus for good form
            if (formTracker.IsStanceOptimal())
            {
                finalProbability *= 1.2f;
            }
            
            return Mathf.Clamp01(finalProbability);
        }
        
        public float GetStanceCompatibility()
        {
            if (formTracker == null) return 0.5f;
            
            var currentStance = formTracker.CurrentStance;
            
            if (currentStance == BoxingFormTracker.BoxingStance.Orthodox)
            {
                return orthodoxProbability;
            }
            else if (currentStance == BoxingFormTracker.BoxingStance.Southpaw)
            {
                return southpawProbability;
            }
            
            return 0.5f; // Neutral compatibility
        }
        
        public RhythmTargetSystem.HandSide GetRecommendedHand()
        {
            if (!preferDominantHand || formTracker == null)
                return Random.value < 0.5f ? RhythmTargetSystem.HandSide.Left : RhythmTargetSystem.HandSide.Right;
            
            var currentStance = formTracker.CurrentStance;
            float stanceCompatibility = GetStanceCompatibility();
            
            // High compatibility = use dominant hand
            // Low compatibility = use non-dominant hand for training
            if (stanceCompatibility > 0.7f)
            {
                // Use dominant hand for this stance
                if (currentStance == BoxingFormTracker.BoxingStance.Orthodox)
                {
                    return RhythmTargetSystem.HandSide.Right; // Orthodox right hand dominant
                }
                else
                {
                    return RhythmTargetSystem.HandSide.Left; // Southpaw left hand dominant
                }
            }
            else
            {
                // Use non-dominant hand for practice
                if (currentStance == BoxingFormTracker.BoxingStance.Orthodox)
                {
                    return RhythmTargetSystem.HandSide.Left;
                }
                else
                {
                    return RhythmTargetSystem.HandSide.Right;
                }
            }
        }
        
        public Vector3 GetOptimalTargetPosition()
        {
            Vector3 basePosition = transform.position;
            
            // Adjust height based on stance and angle
            float heightAdjustment = 0f;
            
            if (formTracker != null)
            {
                // Targets in power zones should be at chest height
                float stanceCompatibility = GetStanceCompatibility();
                heightAdjustment = (stanceCompatibility - 0.5f) * 0.3f; // ±15cm adjustment
            }
            
            return basePosition + Vector3.up * heightAdjustment;
        }
        
        public void OnTargetSpawned()
        {
            lastSpawnTime = Time.time;
            
            // Visual feedback for spawn
            if (spawnIndicator != null)
            {
                StartCoroutine(SpawnFeedbackCoroutine());
            }
        }
        
        private System.Collections.IEnumerator SpawnFeedbackCoroutine()
        {
            // Brief flash when target spawns
            Color originalColor = spawnIndicator.material.color;
            spawnIndicator.material.color = Color.white;
            spawnIndicator.enabled = true;
            
            yield return new WaitForSeconds(0.1f);
            
            spawnIndicator.material.color = originalColor;
        }
        
        [ContextMenu("Test Spawn Probability")]
        public void TestSpawnProbability()
        {
            float probability = GetSpawnProbability();
            float compatibility = GetStanceCompatibility();
            var recommendedHand = GetRecommendedHand();
            
            Debug.Log($"Spawner {spawnIndex} ({spawnAngle:F0}°):");
            Debug.Log($"  Spawn Probability: {probability:F2}");
            Debug.Log($"  Stance Compatibility: {compatibility:F2}");
            Debug.Log($"  Recommended Hand: {recommendedHand}");
        }
        
        private void OnDrawGizmos()
        {
            // Draw spawn zone
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Draw angle indicator
            Vector3 angleDirection = new Vector3(
                Mathf.Sin(spawnAngle * Mathf.Deg2Rad),
                0,
                Mathf.Cos(spawnAngle * Mathf.Deg2Rad)
            );
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Vector3.zero, angleDirection * 3f);
            
            // Draw stance compatibility
            if (Application.isPlaying && formTracker != null)
            {
                float compatibility = GetStanceCompatibility();
                Gizmos.color = Color.Lerp(Color.red, Color.green, compatibility);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.1f);
            }
        }
    }
} 