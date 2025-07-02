using UnityEngine;
using VRBoxingGame.Boxing;
using VRBoxingGame.Core;

namespace VRBoxingGame.UI
{
    /// <summary>
    /// Utility to create circle prefabs for the rhythm game
    /// </summary>
    public class CirclePrefabCreator : MonoBehaviour
    {
        [Header("Circle Settings")]
        public float circleSize = 0.3f;
        public Material whiteMaterial;
        public Material grayMaterial;
        public Material blockMaterial;
        
        [Header("Generated Prefabs")]
        public GameObject whiteCirclePrefab;
        public GameObject grayCirclePrefab;
        public GameObject blockPrefab;
        
        [ContextMenu("Create Circle Prefabs")]
        public void CreateCirclePrefabs()
        {
            CreateWhiteCircle();
            CreateGrayCircle();
            CreateBlockPrefab();
            
            // Auto-assign to rhythm system
            AssignToRhythmSystem();
            
            Debug.Log("Circle prefabs created and assigned!");
        }
        
        private void CreateWhiteCircle()
        {
            // Create white circle
            GameObject whiteCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            whiteCircle.name = "WhiteCircle";
            whiteCircle.transform.localScale = new Vector3(circleSize, 0.05f, circleSize);
            
            // Set material
            Renderer renderer = whiteCircle.GetComponent<Renderer>();
            if (whiteMaterial != null)
            {
                renderer.material = whiteMaterial;
            }
            else
            {
                renderer.material.color = Color.white;
            }
            
            // Add collider for hand detection
            SphereCollider collider = whiteCircle.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = circleSize * 0.8f;
            
            // Add rhythm circle component
            RhythmCircleComponent rhythmComponent = whiteCircle.AddComponent<RhythmCircleComponent>();
            rhythmComponent.circleType = RhythmTargetSystem.CircleType.White;
            rhythmComponent.requiredHand = RhythmTargetSystem.HandSide.Left;
            
            // Add boxing target component
            BoxingTarget boxingTarget = whiteCircle.AddComponent<BoxingTarget>();
            boxingTarget.requiredHand = BoxingTarget.HandType.Left;
            boxingTarget.baseScore = 100;
            boxingTarget.lifetime = 5f;
            
            whiteCirclePrefab = whiteCircle;
        }
        
        private void CreateGrayCircle()
        {
            // Create gray circle
            GameObject grayCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grayCircle.name = "GrayCircle";
            grayCircle.transform.localScale = new Vector3(circleSize, 0.05f, circleSize);
            
            // Set material
            Renderer renderer = grayCircle.GetComponent<Renderer>();
            if (grayMaterial != null)
            {
                renderer.material = grayMaterial;
            }
            else
            {
                renderer.material.color = Color.gray;
            }
            
            // Add collider for hand detection
            SphereCollider collider = grayCircle.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = circleSize * 0.8f;
            
            // Add rhythm circle component
            RhythmCircleComponent rhythmComponent = grayCircle.AddComponent<RhythmCircleComponent>();
            rhythmComponent.circleType = RhythmTargetSystem.CircleType.Gray;
            rhythmComponent.requiredHand = RhythmTargetSystem.HandSide.Right;
            
            // Add boxing target component
            BoxingTarget boxingTarget = grayCircle.AddComponent<BoxingTarget>();
            boxingTarget.requiredHand = BoxingTarget.HandType.Right;
            boxingTarget.baseScore = 100;
            boxingTarget.lifetime = 5f;
            
            grayCirclePrefab = grayCircle;
        }
        
        private void CreateBlockPrefab()
        {
            // Create spinning block object
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "SpinningBlock";
            block.transform.localScale = Vector3.one * circleSize * 1.5f;
            
            // Set material
            Renderer renderer = block.GetComponent<Renderer>();
            if (blockMaterial != null)
            {
                renderer.material = blockMaterial;
            }
            else
            {
                renderer.material.color = Color.red;
            }
            
            // Add collider for blocking detection
            SphereCollider collider = block.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = circleSize;
            
            // Add block component
            BlockComponent blockComponent = block.AddComponent<BlockComponent>();
            
            blockPrefab = block;
        }
        
        [ContextMenu("Assign to Rhythm System")]
        public void AssignToRhythmSystem()
        {
            RhythmTargetSystem rhythmSystem = CachedReferenceManager.Get<RhythmTargetSystem>();
            if (rhythmSystem != null)
            {
                rhythmSystem.whiteCirclePrefab = whiteCirclePrefab;
                rhythmSystem.grayCirclePrefab = grayCirclePrefab;
                rhythmSystem.combinedBlockPrefab = blockPrefab;
                
                Debug.Log("Prefabs assigned to Rhythm Target System!");
            }
            else
            {
                Debug.LogWarning("No Rhythm Target System found in scene!");
            }
        }
    }
    
    // Component for blocking mechanics
    public class BlockComponent : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            // Check for hand collision during block
            if (other.CompareTag("LeftHand") || other.CompareTag("RightHand"))
            {
                Vector3 blockPosition = transform.position;
                RhythmTargetSystem.Instance?.OnBlockAttempt(blockPosition);
            }
        }
    }
} 