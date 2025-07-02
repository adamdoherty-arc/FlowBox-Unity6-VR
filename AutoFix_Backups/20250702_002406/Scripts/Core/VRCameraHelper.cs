using UnityEngine;
using Unity.XR.CoreUtils;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// VR Camera Helper - Centralized VR camera management to reduce FindObjectOfType calls
    /// </summary>
    public class VRCameraHelper : MonoBehaviour
    {
        private static VRCameraHelper instance;
        private Camera activeCamera;
        private Transform activeCameraTransform;
        private XROrigin xrOrigin;
        private Transform playerTransform;
        
        // Cached references
        public static Camera ActiveCamera => Instance?.activeCamera;
        public static Transform ActiveCameraTransform => Instance?.activeCameraTransform;
        public static Vector3 PlayerPosition => Instance?.playerTransform?.position ?? Vector3.zero;
        public static Vector3 PlayerForward => Instance?.activeCameraTransform?.forward ?? Vector3.forward;
        public static XROrigin XROrigin => Instance?.xrOrigin;
        
        public static VRCameraHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<VRCameraHelper>();
                    if (instance == null)
                    {
                        GameObject helperGO = new GameObject("VR Camera Helper");
                        instance = helperGO.AddComponent<VRCameraHelper>();
                        DontDestroyOnLoad(helperGO);
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCameraReferences();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (instance == this)
            {
                InitializeCameraReferences();
            }
        }
        
        private void InitializeCameraReferences()
        {
            // Find XR Origin
            xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                activeCamera = xrOrigin.Camera;
                activeCameraTransform = activeCamera.transform;
                playerTransform = xrOrigin.CameraFloorOffsetObject.transform;
            }
            else
            {
                // Fallback to regular camera
                activeCamera = Camera.main;
                if (activeCamera != null)
                {
                    activeCameraTransform = activeCamera.transform;
                    playerTransform = activeCameraTransform;
                }
            }
            
            if (activeCamera == null)
            {
                Debug.LogWarning("VRCameraHelper: No camera found. VR functionality may be limited.");
            }
        }
        
        /// <summary>
        /// Get world position relative to player
        /// </summary>
        public static Vector3 GetWorldPositionFromPlayer(Vector3 localOffset)
        {
            if (Instance?.playerTransform != null)
            {
                return Instance.playerTransform.TransformPoint(localOffset);
            }
            return localOffset;
        }
        
        /// <summary>
        /// Get direction from player to target
        /// </summary>
        public static Vector3 GetDirectionToTarget(Vector3 targetPosition)
        {
            if (Instance?.playerTransform != null)
            {
                return (targetPosition - Instance.playerTransform.position).normalized;
            }
            return Vector3.forward;
        }
        
        /// <summary>
        /// Check if position is in player's field of view
        /// </summary>
        public static bool IsInFieldOfView(Vector3 worldPosition, float fovAngle = 90f)
        {
            if (Instance?.activeCameraTransform == null) return false;
            
            Vector3 directionToTarget = (worldPosition - Instance.activeCameraTransform.position).normalized;
            float angle = Vector3.Angle(Instance.activeCameraTransform.forward, directionToTarget);
            
            return angle < fovAngle * 0.5f;
        }
        
        /// <summary>
        /// Get distance from player to position
        /// </summary>
        public static float GetDistanceToPlayer(Vector3 worldPosition)
        {
            if (Instance?.playerTransform != null)
            {
                return Vector3.Distance(Instance.playerTransform.position, worldPosition);
            }
            return float.MaxValue;
        }
        
        /// <summary>
        /// Refresh camera references (call after scene changes)
        /// </summary>
        public static void RefreshCameraReferences()
        {
            if (Instance != null)
            {
                Instance.InitializeCameraReferences();
            }
        }
    }
} 