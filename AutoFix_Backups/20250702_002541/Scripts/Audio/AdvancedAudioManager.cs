using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Collections;
using VRBoxingGame.Core;

namespace VRBoxingGame.Audio
{
    /// <summary>
    /// Advanced Audio Manager for Unity 6 with spatial audio, beat detection, and performance optimizations
    /// </summary>
    public class AdvancedAudioManager : MonoBehaviour
    {
        [Header("Audio Settings")]
        public AudioMixerGroup masterMixerGroup;
        public AudioMixerGroup musicMixerGroup;
        public AudioMixerGroup sfxMixerGroup;
        public AudioMixerGroup voiceMixerGroup;
        
        [Header("Spatial Audio")]
        public bool enableSpatialAudio = true;
        public bool enableHRTF = true;
        public float spatialBlend = 1.0f;
        public float dopplerLevel = 0.1f;
        
        [Header("Beat Detection")]
        public bool enableAdvancedBeatDetection = true;
        public int fftSize = 2048;
        public float beatSensitivity = 1.5f;
        public float beatThreshold = 0.02f;
        public int frequencyBands = 8;
        
        [Header("Performance")]
        public bool enableJobSystemOptimization = true;
        public bool enableAudioStreaming = true;
        public int maxConcurrentSounds = 32;
        public float audioLODDistance = 50f;
        
        [Header("Events")]
        public UnityEvent<BeatData> OnBeatDetected;
        public UnityEvent<float> OnBassHit;
        public UnityEvent<AudioAnalysisData> OnAudioAnalysis;
        public UnityEvent<float> OnVolumeChanged;
        
        // Audio analysis data
        [System.Serializable]
        public struct BeatData
        {
            public float intensity;
            public float frequency;
            public float timestamp;
            public int bandIndex;
            public bool isStrongBeat;
            public float beatStrength;
        }
        
        [System.Serializable]
        public struct AudioAnalysisData
        {
            public float[] frequencyBands;
            public float[] bandBuffers;
            public float totalEnergy;
            public float bassEnergy;
            public float midEnergy;
            public float trebleEnergy;
            public float currentBPM;
            public float spectralCentroid;
        }
        
        // Private variables
        private AudioSource musicAudioSource;
        private AudioListener audioListener;
        private List<AudioSource> activeSources = new List<AudioSource>();
        private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        
        // Beat detection
        private float[] audioSamples;
        private float[] frequencyBandData;
        private float[] bandBuffers;
        private float[] bufferDecrease;
        private float[] freqBandHighest;
        private float[] audioBandBuffers;
        
        // Job System data
        private NativeArray<float> sampleData;
        private NativeArray<float> frequencyData;
        private NativeArray<float> beatResults;
        private JobHandle currentJobHandle;
        
        // Beat tracking
        private float lastBeatTime;
        private float[] beatHistory = new float[10];
        private int beatHistoryIndex = 0;
        private float currentBPM = 0f;
        
        // Performance tracking
        private int activeAudioSources = 0;
        private float audioProcessingTime = 0f;
        
        // Singleton
        public static AdvancedAudioManager Instance { get; private set; }
        
        // Properties
        public float CurrentBPM => currentBPM;
        public int ActiveAudioSources => activeAudioSources;
        public float AudioProcessingTime => audioProcessingTime;
        public bool IsPlaying => musicAudioSource != null && musicAudioSource.isPlaying;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeAudioSystem()
        {
            // Setup audio listener
            audioListener = CachedReferenceManager.Get<AudioListener>();
            if (audioListener == null)
            {
                var camera = VRBoxingGame.Core.VRCameraHelper.ActiveCamera;
                audioListener = camera != null ? camera.gameObject.AddComponent<AudioListener>() : null;
            }
            
            // Create music audio source
            musicAudioSource = gameObject.AddComponent<AudioSource>();
            musicAudioSource.outputAudioMixerGroup = musicMixerGroup;
            musicAudioSource.spatialBlend = 0f; // 2D for music
            musicAudioSource.loop = false;
            
            // Initialize beat detection arrays
            audioSamples = new float[fftSize];
            frequencyBandData = new float[frequencyBands];
            bandBuffers = new float[frequencyBands];
            bufferDecrease = new float[frequencyBands];
            freqBandHighest = new float[frequencyBands];
            audioBandBuffers = new float[frequencyBands];
            
            // Initialize Job System arrays
            if (enableJobSystemOptimization)
            {
                sampleData = new NativeArray<float>(fftSize, Allocator.Persistent);
                frequencyData = new NativeArray<float>(frequencyBands, Allocator.Persistent);
                beatResults = new NativeArray<float>(frequencyBands, Allocator.Persistent);
            }
            
            // Create audio source pool
            CreateAudioSourcePool();
            
            // Configure Unity 6 audio settings
            ConfigureUnity6AudioSettings();
            
            Debug.Log("Advanced Audio Manager initialized with Unity 6 optimizations");
        }
        
        private void ConfigureUnity6AudioSettings()
        {
            // Unity 6 audio optimizations
            AudioSettings.dspBufferSize = 256; // Lower latency for VR
            AudioSettings.speakerMode = AudioSpeakerMode.Stereo; // VR headphones
            
            // Enable spatial audio features
            if (enableSpatialAudio)
            {
                AudioSettings.SetSpatializerPluginName("OculusSpatializer");
            }
            
            // Configure audio streaming
            if (enableAudioStreaming)
            {
                AudioSettings.outputSampleRate = 48000; // High quality for VR
            }
        }
        
        private void CreateAudioSourcePool()
        {
            GameObject poolParent = new GameObject("Audio Source Pool");
            poolParent.transform.SetParent(transform);
            
            for (int i = 0; i < maxConcurrentSounds; i++)
            {
                GameObject audioObj = new GameObject($"PooledAudioSource_{i}");
                audioObj.transform.SetParent(poolParent.transform);
                
                AudioSource source = audioObj.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = sfxMixerGroup;
                source.spatialBlend = spatialBlend;
                source.dopplerLevel = dopplerLevel;
                source.playOnAwake = false;
                
                audioSourcePool.Enqueue(source);
            }
        }
        
        private void Update()
        {
            if (enableAdvancedBeatDetection && IsPlaying)
            {
                float startTime = Time.realtimeSinceStartup;
                
                if (enableJobSystemOptimization)
                {
                    UpdateAudioAnalysisWithJobs();
                }
                else
                {
                    UpdateAudioAnalysisTraditional();
                }
                
                audioProcessingTime = Time.realtimeSinceStartup - startTime;
            }
            
            UpdateActiveAudioSources();
            UpdateAudioLOD();
        }
        
        private void UpdateAudioAnalysisWithJobs()
        {
            // Complete previous job
            currentJobHandle.Complete();
            
            // Get spectrum data
            musicAudioSource.GetSpectrumData(audioSamples, 0, FFTWindow.BlackmanHarris);
            
            // Copy to native array
            for (int i = 0; i < audioSamples.Length; i++)
            {
                sampleData[i] = audioSamples[i];
            }
            
            // Schedule audio analysis job
            var audioAnalysisJob = new AudioAnalysisJob
            {
                sampleData = sampleData,
                frequencyData = frequencyData,
                beatResults = beatResults,
                sensitivity = beatSensitivity,
                threshold = beatThreshold,
                frequencyBandCount = this.frequencyBands
            };
            
            currentJobHandle = audioAnalysisJob.Schedule();
            currentJobHandle.Complete(); // Complete for this frame
            
            // Process results
            ProcessAudioAnalysisResults();
        }
        
        private void UpdateAudioAnalysisTraditional()
        {
            GetSpectrumData();
            MakeFrequencyBands();
            BandBuffer();
            CreateAudioBands();
            DetectBeats();
        }
        
        private void GetSpectrumData()
        {
            musicAudioSource.GetSpectrumData(audioSamples, 0, FFTWindow.BlackmanHarris);
        }
        
        private void MakeFrequencyBands()
        {
            int count = 0;
            
            for (int i = 0; i < frequencyBands; i++)
            {
                float average = 0;
                int sampleCount = (int)Mathf.Pow(2, i) * 2;
                
                if (i == 7) sampleCount += 2;
                
                for (int j = 0; j < sampleCount; j++)
                {
                    if (count < audioSamples.Length)
                    {
                        average += audioSamples[count] * (count + 1);
                        count++;
                    }
                }
                
                average /= count;
                frequencyBandData[i] = average * beatSensitivity;
            }
        }
        
        private void BandBuffer()
        {
            for (int i = 0; i < frequencyBands; i++)
            {
                if (frequencyBandData[i] > bandBuffers[i])
                {
                    bandBuffers[i] = frequencyBandData[i];
                    bufferDecrease[i] = 0.005f;
                }
                
                if (frequencyBandData[i] < bandBuffers[i])
                {
                    bandBuffers[i] -= bufferDecrease[i];
                    bufferDecrease[i] *= 1.2f;
                }
                
                if (frequencyBandData[i] > freqBandHighest[i])
                {
                    freqBandHighest[i] = frequencyBandData[i];
                }
            }
        }
        
        private void CreateAudioBands()
        {
            for (int i = 0; i < frequencyBands; i++)
            {
                if (freqBandHighest[i] != 0)
                {
                    audioBandBuffers[i] = bandBuffers[i] / freqBandHighest[i];
                }
            }
        }
        
        private void DetectBeats()
        {
            // Focus on bass frequencies for beat detection
            float bassEnergy = (audioBandBuffers[0] + audioBandBuffers[1]) / 2f;
            
            if (bassEnergy > beatThreshold)
            {
                float currentTime = Time.time;
                
                // Update BPM calculation
                UpdateBPM(currentTime);
                
                // Create beat data
                BeatData beatData = new BeatData
                {
                    intensity = bassEnergy,
                    frequency = GetDominantFrequency(),
                    timestamp = currentTime,
                    bandIndex = GetStrongestBand(),
                    isStrongBeat = bassEnergy > beatThreshold * 2f,
                    beatStrength = bassEnergy
                };
                
                OnBeatDetected?.Invoke(beatData);
                
                if (beatData.isStrongBeat)
                {
                    OnBassHit?.Invoke(bassEnergy);
                }
                
                lastBeatTime = currentTime;
            }
            
            // Send analysis data
            AudioAnalysisData analysisData = new AudioAnalysisData
            {
                frequencyBands = this.frequencyBandData,
                bandBuffers = bandBuffers,
                totalEnergy = GetTotalEnergy(),
                bassEnergy = (audioBandBuffers[0] + audioBandBuffers[1]) / 2f,
                midEnergy = (audioBandBuffers[2] + audioBandBuffers[3] + audioBandBuffers[4]) / 3f,
                trebleEnergy = (audioBandBuffers[5] + audioBandBuffers[6] + audioBandBuffers[7]) / 3f,
                currentBPM = currentBPM,
                spectralCentroid = CalculateSpectralCentroid()
            };
            
            OnAudioAnalysis?.Invoke(analysisData);
        }
        
        private void ProcessAudioAnalysisResults()
        {
            // Process job results
            for (int i = 0; i < frequencyData.Length; i++)
            {
                frequencyBandData[i] = frequencyData[i];
            }
            
            // Continue with traditional processing
            BandBuffer();
            CreateAudioBands();
            DetectBeats();
        }
        
        private void UpdateBPM(float currentTime)
        {
            if (lastBeatTime > 0)
            {
                float timeDifference = currentTime - lastBeatTime;
                float instantBPM = 60f / timeDifference;
                
                // Store in history for smoothing
                beatHistory[beatHistoryIndex] = instantBPM;
                beatHistoryIndex = (beatHistoryIndex + 1) % beatHistory.Length;
                
                // Calculate average BPM
                float totalBPM = 0f;
                int validBeats = 0;
                
                for (int i = 0; i < beatHistory.Length; i++)
                {
                    if (beatHistory[i] > 60 && beatHistory[i] < 200) // Reasonable BPM range
                    {
                        totalBPM += beatHistory[i];
                        validBeats++;
                    }
                }
                
                if (validBeats > 0)
                {
                    currentBPM = totalBPM / validBeats;
                }
            }
        }
        
        private float GetDominantFrequency()
        {
            float maxValue = 0f;
            int maxIndex = 0;
            
            for (int i = 0; i < frequencyBands; i++)
            {
                if (frequencyBandData[i] > maxValue)
                {
                    maxValue = frequencyBandData[i];
                    maxIndex = i;
                }
            }
            
            // Convert band index to approximate frequency
            return 22050f * (maxIndex + 1) / frequencyBands;
        }
        
        private int GetStrongestBand()
        {
            float maxValue = 0f;
            int maxIndex = 0;
            
            for (int i = 0; i < audioBandBuffers.Length; i++)
            {
                if (audioBandBuffers[i] > maxValue)
                {
                    maxValue = audioBandBuffers[i];
                    maxIndex = i;
                }
            }
            
            return maxIndex;
        }
        
        private float GetTotalEnergy()
        {
            float total = 0f;
            for (int i = 0; i < audioBandBuffers.Length; i++)
            {
                total += audioBandBuffers[i];
            }
            return total / audioBandBuffers.Length;
        }
        
        private float CalculateSpectralCentroid()
        {
            float weightedSum = 0f;
            float magnitudeSum = 0f;
            
            for (int i = 0; i < audioSamples.Length; i++)
            {
                float frequency = i * AudioSettings.outputSampleRate / (2f * audioSamples.Length);
                weightedSum += frequency * audioSamples[i];
                magnitudeSum += audioSamples[i];
            }
            
            return magnitudeSum > 0 ? weightedSum / magnitudeSum : 0f;
        }
        
        private void UpdateActiveAudioSources()
        {
            activeAudioSources = 0;
            
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                if (activeSources[i] == null || !activeSources[i].isPlaying)
                {
                    if (activeSources[i] != null)
                    {
                        ReturnAudioSourceToPool(activeSources[i]);
                    }
                    activeSources.RemoveAt(i);
                }
                else
                {
                    activeAudioSources++;
                }
            }
        }
        
        private void UpdateAudioLOD()
        {
            Vector3 listenerPosition = audioListener.transform.position;
            
            foreach (AudioSource source in activeSources)
            {
                if (source == null) continue;
                
                float distance = Vector3.Distance(source.transform.position, listenerPosition);
                
                // Adjust volume based on distance for LOD
                if (distance > audioLODDistance)
                {
                    source.volume *= 0.5f; // Reduce volume for distant sounds
                }
            }
        }
        
        // Unity 6 Job System for audio analysis
        [BurstCompile]
        public struct AudioAnalysisJob : IJob
        {
            [ReadOnly] public NativeArray<float> sampleData;
            [ReadOnly] public float sensitivity;
            [ReadOnly] public float threshold;
            [ReadOnly] public int frequencyBandCount;
            
            [WriteOnly] public NativeArray<float> frequencyData;
            [WriteOnly] public NativeArray<float> beatResults;
            
            public void Execute()
            {
                // Perform FFT analysis with burst compilation
                int count = 0;
                
                for (int i = 0; i < frequencyBandCount; i++)
                {
                    float average = 0;
                    int sampleCount = (int)Unity.Mathematics.math.pow(2, i) * 2;
                    
                    if (i == 7) sampleCount += 2;
                    
                    for (int j = 0; j < sampleCount; j++)
                    {
                        if (count < sampleData.Length)
                        {
                            average += sampleData[count] * (count + 1);
                            count++;
                        }
                    }
                    
                    average /= count;
                    frequencyData[i] = average * sensitivity;
                    
                    // Beat detection
                    beatResults[i] = frequencyData[i] > threshold ? 1f : 0f;
                }
            }
        }
        
        // Public API methods
        public void PlayMusic(AudioClip clip, float volume = 1f)
        {
            if (musicAudioSource != null)
            {
                musicAudioSource.clip = clip;
                musicAudioSource.volume = volume;
                musicAudioSource.Play();
            }
        }
        
        public void StopMusic()
        {
            if (musicAudioSource != null)
            {
                musicAudioSource.Stop();
            }
        }
        
        public AudioSource PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            AudioSource source = GetPooledAudioSource();
            if (source != null)
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = volume;
                source.Play();
                
                activeSources.Add(source);
                return source;
            }
            
            return null;
        }
        
        public AudioSource PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f)
        {
            AudioSource source = PlaySFX(clip, position, volume);
            if (source != null)
            {
                source.spatialBlend = spatialBlend;
                
                if (enableSpatialAudio)
                {
                    // Enable spatial audio features
                    source.spatialize = true;
                    source.spatializePostEffects = true;
                }
            }
            
            return source;
        }
        
        private AudioSource GetPooledAudioSource()
        {
            if (audioSourcePool.Count > 0)
            {
                return audioSourcePool.Dequeue();
            }
            
            Debug.LogWarning("Audio source pool exhausted!");
            return null;
        }
        
        private void ReturnAudioSourceToPool(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.volume = 1f;
            source.spatialBlend = spatialBlend;
            audioSourcePool.Enqueue(source);
        }
        
        public void SetMasterVolume(float volume)
        {
            if (masterMixerGroup != null)
            {
                masterMixerGroup.audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
                OnVolumeChanged?.Invoke(volume);
            }
        }
        
        public void SetMusicVolume(float volume)
        {
            if (musicMixerGroup != null)
            {
                musicMixerGroup.audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
            }
        }
        
        public void SetSFXVolume(float volume)
        {
            if (sfxMixerGroup != null)
            {
                sfxMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
            }
        }
        
        public float[] GetFrequencyBands()
        {
            return frequencyBandData;
        }
        
        public float[] GetAudioBands()
        {
            return audioBandBuffers;
        }
        
        public void SetMusicSource(AudioSource source)
        {
            musicAudioSource = source;
            if (source != null && source.clip != null)
            {
                Debug.Log($"Music source set: {source.clip.name}");
            }
        }
        
        public void SimulateBeatDetection(BeatData beatData)
        {
            OnBeatDetected?.Invoke(beatData);
        }
        
        /// <summary>
        /// Enable/disable environmental audio effects for rain scenes
        /// </summary>
        public void SetEnvironmentalAudio(bool enabled)
        {
            if (enabled)
            {
                // Add rain-specific audio processing
                AudioSettings.dspTime = AudioSettings.dspTime; // Force audio refresh
                
                // Apply environmental reverb if available
                if (musicMixerGroup != null)
                {
                    musicMixerGroup.audioMixer.SetFloat("EnvironmentalReverb", enabled ? 0.3f : 0f);
                }
                
                Debug.Log("Environmental audio enabled for rain scene");
            }
            else
            {
                // Reset environmental audio
                if (musicMixerGroup != null)
                {
                    musicMixerGroup.audioMixer.SetFloat("EnvironmentalReverb", 0f);
                }
                
                Debug.Log("Environmental audio disabled");
            }
        }
        
        /// <summary>
        /// Enable/disable underwater audio mode for underwater scenes
        /// </summary>
        public void SetUnderwaterMode(bool enabled)
        {
            if (enabled)
            {
                // Apply underwater audio filtering
                if (musicMixerGroup != null)
                {
                    musicMixerGroup.audioMixer.SetFloat("UnderwaterFilter", -800f); // Low-pass filter
                    musicMixerGroup.audioMixer.SetFloat("UnderwaterReverb", 0.8f);
                }
                
                // Reduce high frequencies for underwater effect
                AudioListener.volume = 0.7f;
                
                Debug.Log("Underwater audio mode enabled");
            }
            else
            {
                // Reset underwater audio
                if (musicMixerGroup != null)
                {
                    musicMixerGroup.audioMixer.SetFloat("UnderwaterFilter", 0f);
                    musicMixerGroup.audioMixer.SetFloat("UnderwaterReverb", 0f);
                }
                
                AudioListener.volume = 1f;
                
                Debug.Log("Underwater audio mode disabled");
            }
        }
        
        private void OnDestroy()
        {
            // Clean up Job System resources
            if (enableJobSystemOptimization)
            {
                currentJobHandle.Complete();
                
                if (sampleData.IsCreated) sampleData.Dispose();
                if (frequencyData.IsCreated) frequencyData.Dispose();
                if (beatResults.IsCreated) beatResults.Dispose();
            }
        }
    }
}

