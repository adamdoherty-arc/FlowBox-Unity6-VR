using UnityEngine;
using UnityEngine.XR;

namespace VRBoxingGame.Core
{
    /// <summary>
    /// VR-compatible camera helper that replaces Camera.main usage
    /// Automatically finds the proper XR camera for VR environments
    /// </summary>
    public static class VRCameraHelper
    {
        private static Camera cachedCamera;
        private static Transform cachedTransform;
        private static bool hasCheckedForCamera = false;

        /// <summary>
        /// Gets the active VR camera (replaces Camera.main)
        /// </summary>
        public static Camera ActiveCamera
        {
            get
            {
                if (cachedCamera == null || !hasCheckedForCamera)
                {
                    RefreshCameraCache();
                }
                return cachedCamera;
            }
        }

        /// <summary>
        /// Gets the active VR camera transform (replaces Camera.main.transform)
        /// </summary>
        public static Transform ActiveCameraTransform
        {
            get
            {
                if (cachedTransform == null || !hasCheckedForCamera)
                {
                    RefreshCameraCache();
                }
                return cachedTransform;
            }
        }

        /// <summary>
        /// Gets the player's head position in VR
        /// </summary>
        public static Vector3 PlayerPosition
        {
            get
            {
                var camera = ActiveCamera;
                return camera != null ? camera.transform.position : Vector3.zero;
            }
        }

        /// <summary>
        /// Gets the player's look direction in VR
        /// </summary>
        public static Vector3 PlayerForward
        {
            get
            {
                var camera = ActiveCamera;
                return camera != null ? camera.transform.forward : Vector3.forward;
            }
        }

        /// <summary>
        /// Refreshes the camera cache - call when cameras might have changed
        /// </summary>
        public static void RefreshCameraCache()
        {
            hasCheckedForCamera = true;
            cachedCamera = null;
            cachedTransform = null;

            // Priority 1: Find XR camera
            if (XRSettings.enabled)
            {
                // Look for XR Origin camera
                var xrOrigin = Object.FindObjectOfType<UnityEngine.XR.XROrigin>();
                if (xrOrigin != null && xrOrigin.Camera != null)
                {
                    cachedCamera = xrOrigin.Camera;
                    cachedTransform = cachedCamera.transform;
                    return;
                }

                // Look for camera with XR device tracking
                Camera[] cameras = Object.FindObjectsOfType<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam.enabled && cam.gameObject.activeInHierarchy)
                    {
                        // Check if this camera is the main XR camera
                        if (cam.CompareTag("MainCamera") || cam.name.Contains("XR") || cam.name.Contains("VR"))
                        {
                            cachedCamera = cam;
                            cachedTransform = cam.transform;
                            return;
                        }
                    }
                }
            }

            // Priority 2: Find main camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cachedCamera = mainCamera;
                cachedTransform = mainCamera.transform;
                return;
            }

            // Priority 3: Find any active camera
            Camera[] allCameras = Object.FindObjectsOfType<Camera>();
            foreach (var cam in allCameras)
            {
                if (cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    cachedCamera = cam;
                    cachedTransform = cam.transform;
                    return;
                }
            }

            Debug.LogWarning("VRCameraHelper: No active camera found!");
        }

        /// <summary>
        /// Checks if we're currently in VR mode
        /// </summary>
        public static bool IsVRActive
        {
            get { return XRSettings.enabled && XRSettings.loadedDeviceName != "None"; }
        }

        /// <summary>
        /// Gets screen point to world ray (VR compatible)
        /// </summary>
        public static Ray ScreenPointToRay(Vector3 screenPoint)
        {
            var camera = ActiveCamera;
            if (camera != null)
            {
                return camera.ScreenPointToRay(screenPoint);
            }
            return new Ray(Vector3.zero, Vector3.forward);
        }

        /// <summary>
        /// Converts world position to screen point (VR compatible)
        /// </summary>
        public static Vector3 WorldToScreenPoint(Vector3 worldPoint)
        {
            var camera = ActiveCamera;
            if (camera != null)
            {
                return camera.WorldToScreenPoint(worldPoint);
            }
            return Vector3.zero;
        }
    }
} 