using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using VRBoxingGame.Core;
using VRBoxingGame.HandTracking;
using VRBoxingGame.Boxing;
using System.Collections;
using System.Collections.Generic;

namespace VRBoxingGame.Setup
{
    /// <summary>
    /// VR 360-Degree Movement System for FlowBox
    /// Enables continuous turning, room-scale tracking, and Supernatural-style movement
    /// </summary>
    public class VR360MovementSystem : MonoBehaviour
    {
        [Header("360-Degree Movement Settings")]
        public bool enableRoomScaleTracking = true;
        public bool enableContinuousTurning = true;
        public bool enableSnapTurning = true;
        public bool enableComfortTurning = false;
        
        [Header("Tracking Configuration")]
        public TrackingOriginMode trackingOriginMode = TrackingOriginMode.Floor;
        public bool enableBoundarySystem = true;
        public bool showPlayAreaBoundary = true;
        public float playAreaWarningDistance = 0.5f;
        
        [Header("Continuous Turning")]
        [Range(30f, 180f)]
        public float continuousTurningSpeed = 90f;
        public float turningDeadZone = 0.3f;
        public bool invertTurning = false;
        
        [Header("Snap Turning")]
        [Range(15f, 90f)]
        public float snapTurnAngle = 30f;
        public float snapTurnCooldown = 0.3f;
        
        [Header("Comfort Settings")]
        public bool enableVignetting = true;
        public bool enableTeleportation = false;
        public float vignetteIntensity = 0.7f;
        public float vignetteSpeed = 3f;
        
        [Header("Supernatural Features")]
        public bool enableFullBodyTracking = true;
        public bool enableTargetFollowing = true;
        public bool requirePlayerToTurnAround = true;
        public float targetSpawnRadius = 2.5f;
        public int spawnPointCount = 8;
        
        // Components
        private XROrigin xrOrigin;
        private LocomotionSystem locomotionSystem;
        private TeleportationProvider teleportationProvider;
        private SnapTurnProvider snapTurnProvider;
        private ContinuousTurnProvider continuousTurnProvider;
        private ActionBasedSnapTurnProvider actionSnapTurnProvider;
        private ActionBasedContinuousTurnProvider actionContinuousTurnProvider;
        
        // Input actions
        private InputDevice leftController;
        private InputDevice rightController;
        private bool snapTurnCooldownActive = false;
        
        // Boundary tracking
        private List<Vector3> playAreaBoundary = new List<Vector3>();
        private Vector3 playAreaCenter;
        private float playAreaRadius;
        
        // 360-degree state
        private float currentPlayerRotation = 0f;
        private Vector3 lastPlayerPosition;
        private int fullRotationsCompleted = 0;
        
        // Events
        public System.Action<float> OnPlayerRotationChanged;
        public System.Action<int> OnFullRotationCompleted;
        public System.Action<Vector3> OnPlayerPositionChanged;
        public System.Action<bool> OnBoundaryWarning;
        
        public static VR360MovementSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeVR360Movement();
        }
        
        private void InitializeVR360Movement()
        {
            Debug.Log("🌀 Initializing VR 360-Degree Movement System...");
            
            SetupXROrigin();
            ConfigureTrackingOrigin();
            SetupLocomotionSystem();
            SetupTurningProviders();
            Setup360Features();
            
            if (enableBoundarySystem)
            {
                StartCoroutine(SetupBoundarySystem());
            }
            
            Debug.Log("✅ VR 360-Degree Movement System initialized!");
        }
        
        private void SetupXROrigin()
        {
            xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("❌ XROrigin not found! Creating new XR Origin...");
                CreateXROrigin();
            }
            
            // Ensure proper configuration for 360-degree movement
            if (xrOrigin.CameraFloorOffsetObject == null)
            {
                GameObject cameraOffset = new GameObject("Camera Offset");
                cameraOffset.transform.SetParent(xrOrigin.transform);
                xrOrigin.CameraFloorOffsetObject = cameraOffset;
            }
            
            Debug.Log($"✅ XR Origin configured: {xrOrigin.name}");
        }
        
        private void CreateXROrigin()
        {
            GameObject xrOriginObj = new GameObject("XR Origin (360 Movement)");
            xrOrigin = xrOriginObj.AddComponent<XROrigin>();
            
            // Camera offset for floor tracking
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOriginObj.transform);
            xrOrigin.CameraFloorOffsetObject = cameraOffset;
            
            // Main camera setup
            GameObject mainCameraObj = new GameObject("Main Camera");
            mainCameraObj.transform.SetParent(cameraOffset.transform);
            mainCameraObj.tag = "MainCamera";
            
            Camera mainCamera = mainCameraObj.AddComponent<Camera>();
            mainCameraObj.AddComponent<AudioListener>();
            mainCameraObj.AddComponent<TrackedPoseDriver>();
            
            xrOrigin.Camera = mainCamera;
        }
        
        private void ConfigureTrackingOrigin()
        {
            Debug.Log("🎯 Configuring tracking origin for 360-degree movement...");
            
            var inputSubsystems = new System.Collections.Generic.List<XRInputSubsystem>();
            SubsystemManager.GetInstances(inputSubsystems);
            
            foreach (var inputSubsystem in inputSubsystems)
            {
                if (inputSubsystem.SetTrackingOriginMode(trackingOriginMode))
                {
                    Debug.Log($"✅ Tracking origin set to: {trackingOriginMode}");
                    break;
                }
            }
            
            if (enableRoomScaleTracking)
            {
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                Debug.Log("✅ Room-scale tracking enabled");
            }
        }
        
        private void SetupLocomotionSystem()
        {
            locomotionSystem = FindObjectOfType<LocomotionSystem>();
            if (locomotionSystem == null)
            {
                GameObject locomotionObj = new GameObject("Locomotion System");
                locomotionSystem = locomotionObj.AddComponent<LocomotionSystem>();
            }
            
            locomotionSystem.xrOrigin = xrOrigin;
            
            Debug.Log("✅ Locomotion system configured");
        }
        
        private void SetupTurningProviders()
        {
            SetupContinuousTurning();
            SetupSnapTurning();
        }
        
        private void SetupContinuousTurning()
        {
            GameObject continuousTurnObj = new GameObject("Continuous Turn Provider");
            continuousTurnObj.transform.SetParent(transform);
            
            actionContinuousTurnProvider = continuousTurnObj.AddComponent<ActionBasedContinuousTurnProvider>();
            actionContinuousTurnProvider.system = locomotionSystem;
            actionContinuousTurnProvider.turnSpeed = continuousTurningSpeed;
            
            // Configure for 360-degree movement
            actionContinuousTurnProvider.enableTurnLeftRight = true;
            actionContinuousTurnProvider.enableTurnAround = true;
            
            Debug.Log($"✅ Continuous turning enabled - Speed: {continuousTurningSpeed}°/s");
        }
        
        private void SetupSnapTurning()
        {
            GameObject snapTurnObj = new GameObject("Snap Turn Provider");
            snapTurnObj.transform.SetParent(transform);
            
            actionSnapTurnProvider = snapTurnObj.AddComponent<ActionBasedSnapTurnProvider>();
            actionSnapTurnProvider.system = locomotionSystem;
            actionSnapTurnProvider.turnAmount = snapTurnAngle;
            
            Debug.Log($"✅ Snap turning enabled - Angle: {snapTurnAngle}°");
        }
        
        private void Setup360Features()
        {
            if (enableFullBodyTracking)
            {
                EnableFullBodyTracking();
            }
            
            if (enableTargetFollowing)
            {
                ConfigureTargetSpawning();
            }
            
            if (requirePlayerToTurnAround)
            {
                StartCoroutine(TrackPlayerRotation());
            }
        }
        
        private void EnableFullBodyTracking()
        {
            // Enhanced body awareness for 360-degree movement
            var handTrackingManager = FindObjectOfType<HandTrackingManager>();
            if (handTrackingManager != null)
            {
                handTrackingManager.enableHandTracking = true;
                Debug.Log("✅ Full body tracking awareness enabled");
            }
        }
        
        private void ConfigureTargetSpawning()
        {
            var rhythmSystem = FindObjectOfType<RhythmTargetSystem>();
            if (rhythmSystem != null)
            {
                Enable360TargetSpawning(rhythmSystem);
                Debug.Log("✅ 360-degree target spawning configured");
            }
        }
        
        private void Enable360TargetSpawning(RhythmTargetSystem rhythmSystem)
        {
            // Create spawn points around player for 360-degree targets
            for (int i = 0; i < spawnPointCount; i++)
            {
                float angle = (i / (float)spawnPointCount) * 360f;
                float radian = angle * Mathf.Deg2Rad;
                
                Vector3 spawnPosition = new Vector3(
                    Mathf.Sin(radian) * targetSpawnRadius,
                    1.5f, // Chest height
                    Mathf.Cos(radian) * targetSpawnRadius
                );
                
                GameObject spawnPoint = new GameObject($"360_SpawnPoint_{i}");
                spawnPoint.transform.position = spawnPosition;
                spawnPoint.transform.SetParent(xrOrigin.transform);
                
                // Add sphere collider for spawn area detection
                SphereCollider spawnCollider = spawnPoint.AddComponent<SphereCollider>();
                spawnCollider.isTrigger = true;
                spawnCollider.radius = 0.3f;
                
                // Add component to handle 360-degree spawning
                var spawner360 = spawnPoint.AddComponent<Target360Spawner>();
                spawner360.spawnIndex = i;
                spawner360.spawnAngle = angle;
                spawner360.rhythmSystem = rhythmSystem;
                
                // Tag for identification
                spawnPoint.tag = "SpawnPoint360";
            }
            
            // Update rhythm system to use 360-degree spawning
            if (rhythmSystem != null)
            {
                // Set spawn points on rhythm system
                var spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint360");
                if (spawnPoints.Length > 0)
                {
                    // Update left and right spawn points to use 360-degree system
                    rhythmSystem.leftSpawnPoint = spawnPoints[6].transform; // 270 degrees (left)
                    rhythmSystem.rightSpawnPoint = spawnPoints[2].transform; // 90 degrees (right)
                    rhythmSystem.centerPoint = xrOrigin.transform; // Player center
                }
            }
        }
        
        private IEnumerator SetupBoundarySystem()
        {
            yield return new WaitForSeconds(1f);
            
            var inputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(inputSubsystems);
            
            foreach (var inputSubsystem in inputSubsystems)
            {
                List<Vector3> boundaryPoints = new List<Vector3>();
                if (inputSubsystem.TryGetBoundaryPoints(boundaryPoints))
                {
                    playAreaBoundary = boundaryPoints;
                    CalculatePlayAreaMetrics();
                    Debug.Log($"✅ Play area boundary found: {boundaryPoints.Count} points");
                    break;
                }
            }
        }
        
        private void CalculatePlayAreaMetrics()
        {
            if (playAreaBoundary.Count == 0) return;
            
            // Calculate center
            Vector3 center = Vector3.zero;
            foreach (var point in playAreaBoundary)
            {
                center += point;
            }
            playAreaCenter = center / playAreaBoundary.Count;
            
            // Calculate radius (furthest point from center)
            playAreaRadius = 0f;
            foreach (var point in playAreaBoundary)
            {
                float distance = Vector3.Distance(playAreaCenter, point);
                if (distance > playAreaRadius)
                {
                    playAreaRadius = distance;
                }
            }
            
            Debug.Log($"Play area - Center: {playAreaCenter}, Radius: {playAreaRadius:F2}m");
        }
        
        private void CreateBoundaryVisualization()
        {
            if (playAreaBoundary.Count < 3) return;
            
            GameObject boundaryObj = new GameObject("Play Area Boundary");
            boundaryObj.transform.SetParent(transform);
            
            LineRenderer lineRenderer = boundaryObj.AddComponent<LineRenderer>();
            lineRenderer.material = CreateBoundaryMaterial();
            lineRenderer.color = Color.cyan;
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.positionCount = playAreaBoundary.Count + 1;
            
            for (int i = 0; i < playAreaBoundary.Count; i++)
            {
                lineRenderer.SetPosition(i, playAreaBoundary[i]);
            }
            lineRenderer.SetPosition(playAreaBoundary.Count, playAreaBoundary[0]); // Close the loop
        }
        
        private Material CreateBoundaryMaterial()
        {
            // Create a simple unlit material for boundary lines
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0f, 1f, 1f, 0.5f); // Semi-transparent cyan
            return mat;
        }
        
        private IEnumerator TrackPlayerRotation()
        {
            while (true)
            {
                if (xrOrigin != null && xrOrigin.Camera != null)
                {
                    // Track player's head rotation for full 360-degree turns
                    float currentRotation = xrOrigin.Camera.transform.eulerAngles.y;
                    float rotationDelta = Mathf.DeltaAngle(currentPlayerRotation, currentRotation);
                    
                    currentPlayerRotation = currentRotation;
                    
                    // Check for full rotation completion
                    if (Mathf.Abs(rotationDelta) > 300f) // Full turn detection
                    {
                        fullRotationsCompleted++;
                        OnFullRotationCompleted?.Invoke(fullRotationsCompleted);
                        Debug.Log($"🌀 Full rotation completed! Total: {fullRotationsCompleted}");
                    }
                    
                    OnPlayerRotationChanged?.Invoke(currentPlayerRotation);
                    
                    // Check boundary proximity
                    CheckBoundaryProximity();
                }
                
                yield return new WaitForSeconds(0.1f); // 10 FPS tracking
            }
        }
        
        private void CheckBoundaryProximity()
        {
            if (playAreaBoundary.Count == 0 || xrOrigin?.Camera == null) return;
            
            Vector3 playerPosition = xrOrigin.Camera.transform.position;
            float distanceToCenter = Vector3.Distance(playerPosition, playAreaCenter);
            
            bool nearBoundary = distanceToCenter > (playAreaRadius - playAreaWarningDistance);
            OnBoundaryWarning?.Invoke(nearBoundary);
            
            if (nearBoundary && Time.frameCount % 60 == 0) // Log every second
            {
                Debug.LogWarning($"⚠️ Player near boundary! Distance: {distanceToCenter:F2}m");
            }
        }
        
        private void Update()
        {
            // Handle manual input for debugging
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                RotatePlayer(-snapTurnAngle);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                RotatePlayer(snapTurnAngle);
            }
        }
        
        public void RotatePlayer(float angle)
        {
            if (xrOrigin != null)
            {
                xrOrigin.transform.Rotate(0, angle, 0);
                Debug.Log($"Player rotated by {angle}°");
            }
        }
        
        public void EnableContinuousTurning(bool enable)
        {
            enableContinuousTurning = enable;
            if (actionContinuousTurnProvider != null)
            {
                actionContinuousTurnProvider.enabled = enable;
            }
        }
        
        public void EnableSnapTurning(bool enable)
        {
            enableSnapTurning = enable;
            if (actionSnapTurnProvider != null)
            {
                actionSnapTurnProvider.enabled = enable;
            }
        }
        
        public Vector3 GetPlayerPosition()
        {
            return xrOrigin?.Camera?.transform.position ?? Vector3.zero;
        }
        
        public float GetPlayerRotation()
        {
            return currentPlayerRotation;
        }
        
        public int GetFullRotationsCompleted()
        {
            return fullRotationsCompleted;
        }
        
        public bool IsPlayerNearBoundary()
        {
            if (playAreaBoundary.Count == 0 || xrOrigin?.Camera == null) return false;
            
            Vector3 playerPosition = xrOrigin.Camera.transform.position;
            float distanceToCenter = Vector3.Distance(playerPosition, playAreaCenter);
            
            return distanceToCenter > (playAreaRadius - playAreaWarningDistance);
        }
        
        [ContextMenu("Test 360 Movement")]
        public void Test360Movement()
        {
            Debug.Log("🧪 Testing 360-degree movement...");
            Debug.Log($"Tracking Origin: {trackingOriginMode}");
            Debug.Log($"Room Scale: {enableRoomScaleTracking}");
            Debug.Log($"Continuous Turn: {enableContinuousTurning} ({continuousTurningSpeed}°/s)");
            Debug.Log($"Snap Turn: {enableSnapTurning} ({snapTurnAngle}°)");
            Debug.Log($"Play Area Points: {playAreaBoundary.Count}");
            Debug.Log($"Full Rotations: {fullRotationsCompleted}");
        }
        
        private void OnDrawGizmos()
        {
            // Draw play area boundary
            if (playAreaBoundary.Count > 2)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < playAreaBoundary.Count; i++)
                {
                    int nextIndex = (i + 1) % playAreaBoundary.Count;
                    Gizmos.DrawLine(playAreaBoundary[i], playAreaBoundary[nextIndex]);
                }
                
                // Draw center and radius
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(playAreaCenter, playAreaRadius);
                Gizmos.DrawWireSphere(playAreaCenter, playAreaRadius - playAreaWarningDistance);
            }
            
            // Draw target spawn radius
            if (xrOrigin != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(xrOrigin.transform.position, targetSpawnRadius);
            }
        }
    }
} 