using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using VRBoxingGame.Performance;

namespace VRBoxingGame.Performance
{
    /// <summary>
    /// Unity 6 Compute Shader GPU-Driven Rendering System
    /// Handles 10,000+ objects with GPU culling, LOD selection, and instanced rendering
    /// Features: GPU-driven culling, compute shader optimization, advanced instancing
    /// </summary>
    public class ComputeShaderRenderingSystem : MonoBehaviour
    {
        [Header("Compute Shader Settings")]
        public ComputeShader cullingComputeShader;
        public ComputeShader instancedRenderingShader;
        public ComputeShader lodSelectionShader;
        public bool enableGPUCulling = true;
        
        [Header("Instance Management")]
        public int maxInstances = 10000;
        public int instanceBatchSize = 1023; // GPU instancing limit
        public bool enableDynamicBatching = true;
        public bool enableGPUInstancing = true;
        
        [Header("LOD System")]
        public float[] lodDistances = { 10f, 25f, 50f, 100f };
        public Mesh[] lodMeshes = new Mesh[4];
        public Material[] lodMaterials = new Material[4];
        public bool enableComputeLOD = true;
        
        [Header("Culling Settings")]
        public bool enableFrustumCulling = true;
        public bool enableOcclusionCulling = true;
        public bool enableDistanceCulling = true;
        public float maxRenderDistance = 100f;
        
        [Header("Performance")]
        public bool enableAsyncGPUReadback = true;
        public int computeThreadGroups = 64;
        public bool enableTemporalCaching = true;
        
        // Compute Buffers
        private ComputeBuffer instanceDataBuffer;
        private ComputeBuffer cullingResultsBuffer;
        private ComputeBuffer lodDataBuffer;
        private ComputeBuffer visibilityBuffer;
        private ComputeBuffer drawArgsBuffer;
        
        // GPU Data Structures
        private struct InstanceData
        {
            public Matrix4x4 transformMatrix;
            public Vector4 boundsCenter;
            public Vector4 boundsSize;
            public float lodBias;
            public int materialIndex;
            public int isVisible;
            public int lodLevel;
        }
        
        private struct CullingData
        {
            public Matrix4x4 viewProjectionMatrix;
            public Vector4 cameraPosition;
            public Vector4 cameraForward;
            public Vector4 frustumPlanes0;
            public Vector4 frustumPlanes1;
            public Vector4 frustumPlanes2;
            public Vector4 frustumPlanes3;
            public Vector4 frustumPlanes4;
            public Vector4 frustumPlanes5;
            public float maxDistance;
            public int enableFrustum;
            public int enableDistance;
            public int enableOcclusion;
        }
        
        // Instance Management
        private List<InstanceData> instanceDataList = new List<InstanceData>();
        private List<RenderBatch> renderBatches = new List<RenderBatch>();
        private NativeArray<Matrix4x4> instanceMatrices;
        private NativeArray<Vector4> instanceColors;
        
        // Compute Shader Kernels
        private int cullingKernel;
        private int lodSelectionKernel;
        private int instancedRenderKernel;
        
        // Performance Tracking
        private int visibleInstanceCount = 0;
        private int culledInstanceCount = 0;
        private float lastCullingTime = 0f;
        private AsyncGPUReadbackRequest readbackRequest;
        
        // Camera reference
        private Camera renderingCamera;
        private Plane[] frustumPlanes = new Plane[6];
        
        // Singleton
        public static ComputeShaderRenderingSystem Instance { get; private set; }
        
        // Properties
        public int VisibleInstanceCount => visibleInstanceCount;
        public int CulledInstanceCount => culledInstanceCount;
        public float CullingPerformance => lastCullingTime;
        public bool IsGPUCullingActive => enableGPUCulling && cullingComputeShader != null;
        
        private struct RenderBatch
        {
            public Mesh mesh;
            public Material material;
            public int startIndex;
            public int count;
            public int lodLevel;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeComputeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeComputeSystem()
        {
            Debug.Log("🖥️ Initializing Compute Shader Rendering System...");
            
            // Get main camera
            renderingCamera = VRBoxingGame.Core.VRCameraHelper.ActiveCamera;
            if (renderingCamera == null)
            {
                renderingCamera = Camera.main;
            }
            
            // Initialize compute shaders
            if (cullingComputeShader != null)
            {
                cullingKernel = cullingComputeShader.FindKernel("CSCulling");
                lodSelectionKernel = cullingComputeShader.FindKernel("CSLODSelection");
            }
            
            // Initialize compute buffers
            InitializeComputeBuffers();
            
            // Initialize native arrays
            instanceMatrices = new NativeArray<Matrix4x4>(maxInstances, Allocator.Persistent);
            instanceColors = new NativeArray<Vector4>(maxInstances, Allocator.Persistent);
            
            // Load default shaders if not assigned
            LoadDefaultComputeShaders();
            
            Debug.Log($"✅ Compute Shader System initialized - Max Instances: {maxInstances}");
        }
        
        private void InitializeComputeBuffers()
        {
            // Instance data buffer
            instanceDataBuffer = new ComputeBuffer(maxInstances, sizeof(float) * 32); // Matrix4x4 + extras
            
            // Culling results buffer
            cullingResultsBuffer = new ComputeBuffer(maxInstances, sizeof(int));
            
            // LOD data buffer
            lodDataBuffer = new ComputeBuffer(maxInstances, sizeof(int));
            
            // Visibility buffer
            visibilityBuffer = new ComputeBuffer(maxInstances, sizeof(int));
            
            // Draw arguments buffer for GPU instancing
            drawArgsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        }
        
        private void LoadDefaultComputeShaders()
        {
            if (cullingComputeShader == null)
            {
                cullingComputeShader = Resources.Load<ComputeShader>("ComputeShaders/InstancedCulling");
            }
            
            if (instancedRenderingShader == null)
            {
                instancedRenderingShader = Resources.Load<ComputeShader>("ComputeShaders/InstancedRendering");
            }
            
            // Create default LOD materials if not assigned
            SetupDefaultLODMaterials();
        }
        
        private void SetupDefaultLODMaterials()
        {
            if (lodMaterials[0] == null)
            {
                for (int i = 0; i < lodMaterials.Length; i++)
                {
                    lodMaterials[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    lodMaterials[i].enableInstancing = true;
                    
                    // Set LOD-specific properties
                    float lodQuality = 1f - (i * 0.25f);
                    lodMaterials[i].SetFloat("_Smoothness", lodQuality);
                    lodMaterials[i].SetFloat("_Metallic", lodQuality * 0.5f);
                }
            }
        }
        
        private void Update()
        {
            if (!IsGPUCullingActive) return;
            
            // Update camera frustum
            UpdateCameraFrustum();
            
            // Perform GPU culling
            PerformGPUCulling();
            
            // Handle async readback
            if (enableAsyncGPUReadback && readbackRequest.hasError == false && readbackRequest.done)
            {
                ProcessGPUReadback();
            }
            
            // Render visible instances
            RenderVisibleInstances();
        }
        
        private void UpdateCameraFrustum()
        {
            if (renderingCamera == null) return;
            
            GeometryUtility.CalculateFrustumPlanes(renderingCamera, frustumPlanes);
        }
        
        private void PerformGPUCulling()
        {
            float startTime = Time.realtimeSinceStartup;
            
            // Prepare culling data
            CullingData cullingData = new CullingData
            {
                viewProjectionMatrix = renderingCamera.projectionMatrix * renderingCamera.worldToCameraMatrix,
                cameraPosition = renderingCamera.transform.position,
                cameraForward = renderingCamera.transform.forward,
                maxDistance = maxRenderDistance,
                enableFrustum = enableFrustumCulling ? 1 : 0,
                enableDistance = enableDistanceCulling ? 1 : 0,
                enableOcclusion = enableOcclusionCulling ? 1 : 0
            };
            
            // Set frustum planes
            if (frustumPlanes.Length >= 6)
            {
                cullingData.frustumPlanes0 = new Vector4(frustumPlanes[0].normal.x, frustumPlanes[0].normal.y, frustumPlanes[0].normal.z, frustumPlanes[0].distance);
                cullingData.frustumPlanes1 = new Vector4(frustumPlanes[1].normal.x, frustumPlanes[1].normal.y, frustumPlanes[1].normal.z, frustumPlanes[1].distance);
                cullingData.frustumPlanes2 = new Vector4(frustumPlanes[2].normal.x, frustumPlanes[2].normal.y, frustumPlanes[2].normal.z, frustumPlanes[2].distance);
                cullingData.frustumPlanes3 = new Vector4(frustumPlanes[3].normal.x, frustumPlanes[3].normal.y, frustumPlanes[3].normal.z, frustumPlanes[3].distance);
                cullingData.frustumPlanes4 = new Vector4(frustumPlanes[4].normal.x, frustumPlanes[4].normal.y, frustumPlanes[4].normal.z, frustumPlanes[4].distance);
                cullingData.frustumPlanes5 = new Vector4(frustumPlanes[5].normal.x, frustumPlanes[5].normal.y, frustumPlanes[5].normal.z, frustumPlanes[5].distance);
            }
            
            // Set compute shader data
            cullingComputeShader.SetBuffer(cullingKernel, "InstanceDataBuffer", instanceDataBuffer);
            cullingComputeShader.SetBuffer(cullingKernel, "CullingResultsBuffer", cullingResultsBuffer);
            cullingComputeShader.SetBuffer(cullingKernel, "VisibilityBuffer", visibilityBuffer);
            
            // Set culling parameters
            cullingComputeShader.SetMatrix("_ViewProjectionMatrix", cullingData.viewProjectionMatrix);
            cullingComputeShader.SetVector("_CameraPosition", cullingData.cameraPosition);
            cullingComputeShader.SetVector("_CameraForward", cullingData.cameraForward);
            cullingComputeShader.SetFloat("_MaxDistance", cullingData.maxDistance);
            cullingComputeShader.SetInt("_EnableFrustumCulling", cullingData.enableFrustum);
            cullingComputeShader.SetInt("_EnableDistanceCulling", cullingData.enableDistance);
            
            // Dispatch compute shader
            int threadGroups = Mathf.CeilToInt((float)instanceDataList.Count / computeThreadGroups);
            cullingComputeShader.Dispatch(cullingKernel, threadGroups, 1, 1);
            
            // LOD selection
            if (enableComputeLOD && lodSelectionKernel >= 0)
            {
                cullingComputeShader.SetBuffer(lodSelectionKernel, "InstanceDataBuffer", instanceDataBuffer);
                cullingComputeShader.SetBuffer(lodSelectionKernel, "LODDataBuffer", lodDataBuffer);
                cullingComputeShader.SetFloatArray("_LODDistances", lodDistances);
                cullingComputeShader.Dispatch(lodSelectionKernel, threadGroups, 1, 1);
            }
            
            // Request async readback
            if (enableAsyncGPUReadback)
            {
                readbackRequest = AsyncGPUReadback.Request(cullingResultsBuffer);
            }
            
            lastCullingTime = Time.realtimeSinceStartup - startTime;
        }
        
        private void ProcessGPUReadback()
        {
            var cullingResults = readbackRequest.GetData<int>();
            
            visibleInstanceCount = 0;
            culledInstanceCount = 0;
            
            for (int i = 0; i < cullingResults.Length && i < instanceDataList.Count; i++)
            {
                if (cullingResults[i] > 0)
                {
                    visibleInstanceCount++;
                }
                else
                {
                    culledInstanceCount++;
                }
            }
        }
        
        private void RenderVisibleInstances()
        {
            if (!enableGPUInstancing) return;
            
            // Create render batches
            CreateRenderBatches();
            
            // Render each batch
            foreach (var batch in renderBatches)
            {
                if (batch.count > 0 && batch.mesh != null && batch.material != null)
                {
                    // Prepare draw arguments
                    uint[] drawArgs = new uint[5];
                    drawArgs[0] = batch.mesh.GetIndexCount(0);
                    drawArgs[1] = (uint)batch.count;
                    drawArgs[2] = batch.mesh.GetIndexStart(0);
                    drawArgs[3] = batch.mesh.GetBaseVertex(0);
                    drawArgs[4] = 0;
                    
                    drawArgsBuffer.SetData(drawArgs);
                    
                    // Set material properties
                    batch.material.SetBuffer("_InstanceDataBuffer", instanceDataBuffer);
                    batch.material.SetInt("_StartIndex", batch.startIndex);
                    
                    // Draw mesh instances
                    Graphics.DrawMeshInstancedIndirect(
                        batch.mesh,
                        0,
                        batch.material,
                        new Bounds(Vector3.zero, Vector3.one * 1000f),
                        drawArgsBuffer
                    );
                }
            }
        }
        
        private void CreateRenderBatches()
        {
            renderBatches.Clear();
            
            // Group instances by LOD level and material
            for (int lodLevel = 0; lodLevel < lodMeshes.Length; lodLevel++)
            {
                if (lodMeshes[lodLevel] == null || lodMaterials[lodLevel] == null) continue;
                
                int batchStartIndex = 0;
                int batchCount = 0;
                
                for (int i = 0; i < instanceDataList.Count; i++)
                {
                    // Check if instance should be in this LOD batch
                    if (ShouldIncludeInBatch(i, lodLevel))
                    {
                        if (batchCount == 0)
                        {
                            batchStartIndex = i;
                        }
                        batchCount++;
                        
                        // If batch is full, create it
                        if (batchCount >= instanceBatchSize)
                        {
                            renderBatches.Add(new RenderBatch
                            {
                                mesh = lodMeshes[lodLevel],
                                material = lodMaterials[lodLevel],
                                startIndex = batchStartIndex,
                                count = batchCount,
                                lodLevel = lodLevel
                            });
                            
                            batchCount = 0;
                        }
                    }
                }
                
                // Add remaining instances in batch
                if (batchCount > 0)
                {
                    renderBatches.Add(new RenderBatch
                    {
                        mesh = lodMeshes[lodLevel],
                        material = lodMaterials[lodLevel],
                        startIndex = batchStartIndex,
                        count = batchCount,
                        lodLevel = lodLevel
                    });
                }
            }
        }
        
        private bool ShouldIncludeInBatch(int instanceIndex, int lodLevel)
        {
            if (instanceIndex >= instanceDataList.Count) return false;
            
            var instance = instanceDataList[instanceIndex];
            return instance.isVisible > 0 && instance.lodLevel == lodLevel;
        }
        
        // Public API
        public void AddInstance(Matrix4x4 transform, Vector3 boundsCenter, Vector3 boundsSize, int materialIndex = 0)
        {
            if (instanceDataList.Count >= maxInstances) return;
            
            InstanceData newInstance = new InstanceData
            {
                transformMatrix = transform,
                boundsCenter = new Vector4(boundsCenter.x, boundsCenter.y, boundsCenter.z, 1f),
                boundsSize = new Vector4(boundsSize.x, boundsSize.y, boundsSize.z, 1f),
                lodBias = 1f,
                materialIndex = materialIndex,
                isVisible = 1,
                lodLevel = 0
            };
            
            instanceDataList.Add(newInstance);
            
            // Update GPU buffer
            if (instanceDataBuffer != null)
            {
                instanceDataBuffer.SetData(instanceDataList.ToArray());
            }
        }
        
        public void RemoveInstance(int index)
        {
            if (index >= 0 && index < instanceDataList.Count)
            {
                instanceDataList.RemoveAt(index);
                
                // Update GPU buffer
                if (instanceDataBuffer != null)
                {
                    instanceDataBuffer.SetData(instanceDataList.ToArray());
                }
            }
        }
        
        public void ClearAllInstances()
        {
            instanceDataList.Clear();
            
            if (instanceDataBuffer != null)
            {
                instanceDataBuffer.SetData(new InstanceData[0]);
            }
        }
        
        public void SetLODDistances(float[] distances)
        {
            if (distances.Length == lodDistances.Length)
            {
                lodDistances = distances;
            }
        }
        
        public ComputeShaderStats GetPerformanceStats()
        {
            return new ComputeShaderStats
            {
                totalInstances = instanceDataList.Count,
                visibleInstances = visibleInstanceCount,
                culledInstances = culledInstanceCount,
                cullingTime = lastCullingTime,
                renderBatches = renderBatches.Count,
                gpuMemoryUsage = GetGPUMemoryUsage()
            };
        }
        
        private float GetGPUMemoryUsage()
        {
            float totalMemory = 0f;
            
            if (instanceDataBuffer != null) totalMemory += instanceDataBuffer.count * instanceDataBuffer.stride;
            if (cullingResultsBuffer != null) totalMemory += cullingResultsBuffer.count * cullingResultsBuffer.stride;
            if (lodDataBuffer != null) totalMemory += lodDataBuffer.count * lodDataBuffer.stride;
            if (visibilityBuffer != null) totalMemory += visibilityBuffer.count * visibilityBuffer.stride;
            
            return totalMemory / (1024f * 1024f); // Convert to MB
        }
        
        private void OnDestroy()
        {
            // Dispose compute buffers
            instanceDataBuffer?.Dispose();
            cullingResultsBuffer?.Dispose();
            lodDataBuffer?.Dispose();
            visibilityBuffer?.Dispose();
            drawArgsBuffer?.Dispose();
            
            // Dispose native arrays
            if (instanceMatrices.IsCreated) instanceMatrices.Dispose();
            if (instanceColors.IsCreated) instanceColors.Dispose();
        }
        
        [System.Serializable]
        public struct ComputeShaderStats
        {
            public int totalInstances;
            public int visibleInstances;
            public int culledInstances;
            public float cullingTime;
            public int renderBatches;
            public float gpuMemoryUsage;
        }
    }
} 