using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using VRBoxingGame.Audio;
using VRBoxingGame.Core;

namespace VRBoxingGame.Music
{
    /// <summary>
    /// Music Reactive System for analyzing audio and providing rhythm data
    /// </summary>
    public class MusicReactiveSystem : MonoBehaviour
    {
        [Header("Audio Analysis")]
        public AudioSource musicSource;
        public int sampleDataLength = 1024;
        public float updateRate = 60f;
        public float beatSensitivity = 1.5f;
        
        [Header("Beat Detection")]
        public float beatThreshold = 0.15f;
        public float beatCooldown = 0.1f;
        public int beatHistorySize = 8;
        
        [Header("Frequency Analysis")]
        public int frequencyBands = 8;
        public float bassRange = 0.125f;      // 0-12.5% of spectrum
        public float midRange = 0.375f;       // 12.5-50% of spectrum
        public float trebleRange = 0.5f;      // 50-100% of spectrum
        
        [Header("BPM Detection")]
        public bool enableBPMDetection = true;
        public float bpmUpdateInterval = 2f;
        public float minBPM = 60f;
        public float maxBPM = 200f;
        
        [Header("Events")]
        public UnityEvent<BeatInfo> OnBeatDetected;
        public UnityEvent<FrequencyData> OnFrequencyUpdate;
        public UnityEvent<float> OnBPMChanged;
        public UnityEvent<MusicEnergy> OnEnergyUpdate;
        
        // Data structures
        [System.Serializable]
        public struct BeatInfo
        {
            public float intensity;
            public float timestamp;
            public bool isKick;
            public bool isSnare;
            public bool isStrongBeat;
            public float confidence;
        }
        
        [System.Serializable]
        public struct FrequencyData
        {
            public float[] bands;
            public float bass;
            public float mid;
            public float treble;
            public float total;
            public float spectralCentroid;
        }
        
        [System.Serializable]
        public struct MusicEnergy
        {
            public float instantEnergy;
            public float averageEnergy;
            public float variance;
            public bool isEnergyPeak;
        }
        
        // Private variables
        private float[] sampleData;
        private float[] frequencyBands;
        private float[] bandBuffer;
        private float[] bandVelocity;
        private Queue<float> beatHistory;
        private Queue<float> energyHistory;
        
        private float lastBeatTime;
        private float lastUpdateTime;
        private float lastBPMUpdateTime;
        private float currentBPM = 120f;
        
        // Job System data
        private NativeArray<float> nativeSampleData;
        private NativeArray<float> nativeFrequencyData;
        private NativeArray<float> beatResults;
        private JobHandle currentJobHandle;
        
        // Analysis data
        private float instantEnergy;
        private float averageEnergy;
        private float energyVariance;
        
        // Singleton
        public static MusicReactiveSystem Instance { get; private set; }
        
        // Properties
        public float CurrentBPM => currentBPM;
        public float[] FrequencyBands => frequencyBands;
        public float InstantEnergy => instantEnergy;
        public float AverageEnergy => averageEnergy;
        public bool IsPlaying => musicSource != null && musicSource.isPlaying;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeMusicSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeMusicSystem()
        {
            // Initialize arrays
            sampleData = new float[sampleDataLength];
            frequencyBands = new float[frequencyBands];
            bandBuffer = new float[frequencyBands];
            bandVelocity = new float[frequencyBands];
            beatHistory = new Queue<float>();
            energyHistory = new Queue<float>();
            
            // Initialize Job System arrays
            nativeSampleData = new NativeArray<float>(sampleDataLength, Allocator.Persistent);
            nativeFrequencyData = new NativeArray<float>(frequencyBands, Allocator.Persistent);
            beatResults = new NativeArray<float>(4, Allocator.Persistent);
            
            // Find music source if not assigned
            if (musicSource == null)
            {
                musicSource = CachedReferenceManager.Get<AudioSource>();
            }
            
            Debug.Log("Music Reactive System initialized");
        }
        
        private void Update()
        {
            if (!IsPlaying) return;
            
            float deltaTime = Time.time - lastUpdateTime;
            if (deltaTime >= 1f / updateRate)
            {
                AnalyzeAudio();
                lastUpdateTime = Time.time;
            }
            
            if (enableBPMDetection && Time.time - lastBPMUpdateTime >= bpmUpdateInterval)
            {
                UpdateBPM();
                lastBPMUpdateTime = Time.time;
            }
        }
        
        private void AnalyzeAudio()
        {
            // Get spectrum data
            musicSource.GetSpectrumData(sampleData, 0, FFTWindow.BlackmanHarris);
            
            // Copy to native array for job processing
            for (int i = 0; i < sampleData.Length; i++)
            {
                nativeSampleData[i] = sampleData[i];
            }
            
            // Complete previous job
            currentJobHandle.Complete();
            
            // Schedule audio analysis job
            var analysisJob = new AudioAnalysisJob
            {
                sampleData = nativeSampleData,
                frequencyData = nativeFrequencyData,
                beatResults = beatResults,
                sensitivity = beatSensitivity,
                threshold = beatThreshold,
                bandCount = frequencyBands
            };
            
            currentJobHandle = analysisJob.Schedule();
            currentJobHandle.Complete(); // Complete for this frame
            
            // Process results
            ProcessAnalysisResults();
        }
        
        private void ProcessAnalysisResults()
        {
            // Copy frequency data back
            for (int i = 0; i < frequencyBands.Length; i++)
            {
                frequencyBands[i] = nativeFrequencyData[i];
            }
            
            // Calculate frequency ranges
            FrequencyData freqData = CalculateFrequencyRanges();
            
            // Update energy analysis
            UpdateEnergyAnalysis(freqData.total);
            
            // Detect beats
            DetectBeats(freqData);
            
            // Send events
            OnFrequencyUpdate?.Invoke(freqData);
            
            MusicEnergy energyData = new MusicEnergy
            {
                instantEnergy = instantEnergy,
                averageEnergy = averageEnergy,
                variance = energyVariance,
                isEnergyPeak = instantEnergy > averageEnergy + energyVariance
            };
            
            OnEnergyUpdate?.Invoke(energyData);
        }
        
        private FrequencyData CalculateFrequencyRanges()
        {
            FrequencyData data = new FrequencyData
            {
                bands = new float[frequencyBands.Length]
            };
            
            // Copy frequency bands
            for (int i = 0; i < frequencyBands.Length; i++)
            {
                data.bands[i] = frequencyBands[i];
            }
            
            // Calculate bass (low frequencies)
            int bassEnd = Mathf.RoundToInt(frequencyBands.Length * bassRange);
            for (int i = 0; i < bassEnd; i++)
            {
                data.bass += frequencyBands[i];
            }
            data.bass /= bassEnd;
            
            // Calculate mid frequencies
            int midStart = bassEnd;
            int midEnd = Mathf.RoundToInt(frequencyBands.Length * midRange);
            for (int i = midStart; i < midEnd; i++)
            {
                data.mid += frequencyBands[i];
            }
            data.mid /= (midEnd - midStart);
            
            // Calculate treble (high frequencies)
            int trebleStart = midEnd;
            for (int i = trebleStart; i < frequencyBands.Length; i++)
            {
                data.treble += frequencyBands[i];
            }
            data.treble /= (frequencyBands.Length - trebleStart);
            
            // Calculate total energy
            data.total = (data.bass + data.mid + data.treble) / 3f;
            
            // Calculate spectral centroid
            data.spectralCentroid = CalculateSpectralCentroid();
            
            return data;
        }
        
        private float CalculateSpectralCentroid()
        {
            float weightedSum = 0f;
            float magnitudeSum = 0f;
            
            for (int i = 0; i < sampleData.Length; i++)
            {
                float frequency = i * AudioSettings.outputSampleRate / (2f * sampleData.Length);
                weightedSum += frequency * sampleData[i];
                magnitudeSum += sampleData[i];
            }
            
            return magnitudeSum > 0 ? weightedSum / magnitudeSum : 0f;
        }
        
        private void UpdateEnergyAnalysis(float currentEnergy)
        {
            instantEnergy = currentEnergy;
            
            // Add to history
            energyHistory.Enqueue(instantEnergy);
            if (energyHistory.Count > 43) // ~1 second of history at 43 FPS
            {
                energyHistory.Dequeue();
            }
            
            // Calculate average energy
            float totalEnergy = 0f;
            foreach (float energy in energyHistory)
            {
                totalEnergy += energy;
            }
            averageEnergy = totalEnergy / energyHistory.Count;
            
            // Calculate variance
            float variance = 0f;
            foreach (float energy in energyHistory)
            {
                variance += Mathf.Pow(energy - averageEnergy, 2);
            }
            energyVariance = Mathf.Sqrt(variance / energyHistory.Count);
        }
        
        private void DetectBeats(FrequencyData freqData)
        {
            // Beat detection based on energy and bass
            float beatStrength = freqData.bass * 2f + freqData.total;
            bool isBeat = beatStrength > beatThreshold && 
                         Time.time - lastBeatTime > beatCooldown &&
                         instantEnergy > averageEnergy + energyVariance * 0.5f;
            
            if (isBeat)
            {
                // Determine beat type
                bool isKick = freqData.bass > freqData.mid && freqData.bass > freqData.treble;
                bool isSnare = freqData.mid > freqData.bass && freqData.mid > freqData.treble;
                bool isStrongBeat = beatStrength > beatThreshold * 1.5f;
                
                // Calculate confidence
                float confidence = Mathf.Clamp01(beatStrength / (beatThreshold * 2f));
                
                BeatInfo beatInfo = new BeatInfo
                {
                    intensity = beatStrength,
                    timestamp = Time.time,
                    isKick = isKick,
                    isSnare = isSnare,
                    isStrongBeat = isStrongBeat,
                    confidence = confidence
                };
                
                // Add to beat history for BPM calculation
                beatHistory.Enqueue(Time.time);
                if (beatHistory.Count > beatHistorySize)
                {
                    beatHistory.Dequeue();
                }
                
                OnBeatDetected?.Invoke(beatInfo);
                lastBeatTime = Time.time;
            }
        }
        
        private void UpdateBPM()
        {
            if (beatHistory.Count < 2) return;
            
            // Calculate BPM from beat intervals
            float[] beats = beatHistory.ToArray();
            List<float> intervals = new List<float>();
            
            for (int i = 1; i < beats.Length; i++)
            {
                float interval = beats[i] - beats[i - 1];
                if (interval > 0.2f && interval < 2f) // Reasonable interval range
                {
                    intervals.Add(60f / interval); // Convert to BPM
                }
            }
            
            if (intervals.Count > 0)
            {
                // Calculate median BPM for stability
                intervals.Sort();
                float medianBPM = intervals[intervals.Count / 2];
                
                // Smooth BPM changes
                float targetBPM = Mathf.Clamp(medianBPM, minBPM, maxBPM);
                currentBPM = Mathf.Lerp(currentBPM, targetBPM, 0.1f);
                
                OnBPMChanged?.Invoke(currentBPM);
            }
        }
        
        // Unity 6 Job System for audio analysis
        [BurstCompile]
        public struct AudioAnalysisJob : IJob
        {
            [ReadOnly] public NativeArray<float> sampleData;
            [ReadOnly] public float sensitivity;
            [ReadOnly] public float threshold;
            [ReadOnly] public int bandCount;
            
            [WriteOnly] public NativeArray<float> frequencyData;
            [WriteOnly] public NativeArray<float> beatResults;
            
            public void Execute()
            {
                // Process frequency bands
                int count = 0;
                for (int i = 0; i < bandCount; i++)
                {
                    float average = 0;
                    int sampleCount = (int)math.pow(2, i) * 2;
                    
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
                }
                
                // Calculate beat metrics
                float bassEnergy = (frequencyData[0] + frequencyData[1]) / 2f;
                float totalEnergy = 0f;
                for (int i = 0; i < frequencyData.Length; i++)
                {
                    totalEnergy += frequencyData[i];
                }
                totalEnergy /= frequencyData.Length;
                
                beatResults[0] = bassEnergy;
                beatResults[1] = totalEnergy;
                beatResults[2] = bassEnergy > threshold ? 1f : 0f;
                beatResults[3] = totalEnergy;
            }
        }
        
        // Public API methods
        public void PlayMusic(AudioClip clip)
        {
            if (musicSource != null)
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }
        
        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }
        
        public void SetVolume(float volume)
        {
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Clamp01(volume);
            }
        }
        
        public float GetFrequencyBand(int index)
        {
            if (index >= 0 && index < frequencyBands.Length)
            {
                return frequencyBands[index];
            }
            return 0f;
        }
        
        public float GetBassEnergy()
        {
            if (frequencyBands.Length >= 2)
            {
                return (frequencyBands[0] + frequencyBands[1]) / 2f;
            }
            return 0f;
        }
        
        public float GetMidEnergy()
        {
            if (frequencyBands.Length >= 5)
            {
                return (frequencyBands[2] + frequencyBands[3] + frequencyBands[4]) / 3f;
            }
            return 0f;
        }
        
        public float GetTrebleEnergy()
        {
            if (frequencyBands.Length >= 8)
            {
                return (frequencyBands[5] + frequencyBands[6] + frequencyBands[7]) / 3f;
            }
            return 0f;
        }
        
        public bool IsBeatDetected()
        {
            return Time.time - lastBeatTime < 0.1f;
        }
        
        private void OnDestroy()
        {
            // Clean up Job System resources
            currentJobHandle.Complete();
            
            if (nativeSampleData.IsCreated) nativeSampleData.Dispose();
            if (nativeFrequencyData.IsCreated) nativeFrequencyData.Dispose();
            if (beatResults.IsCreated) beatResults.Dispose();
        }
        
        // Debug visualization
        private void OnGUI()
        {
            if (!Application.isPlaying || !IsPlaying) return;
            
            // Draw frequency bands
            float barWidth = Screen.width / (float)frequencyBands.Length;
            for (int i = 0; i < frequencyBands.Length; i++)
            {
                float barHeight = frequencyBands[i] * Screen.height * 0.5f;
                Rect barRect = new Rect(i * barWidth, Screen.height - barHeight, barWidth - 2, barHeight);
                
                Color barColor = Color.Lerp(Color.blue, Color.red, frequencyBands[i]);
                GUI.color = barColor;
                GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            }
            
            // Reset color
            GUI.color = Color.white;
            
            // Draw BPM
            GUI.Label(new Rect(10, 10, 200, 30), $"BPM: {currentBPM:F1}");
            GUI.Label(new Rect(10, 40, 200, 30), $"Energy: {instantEnergy:F3}");
        }
    }
} 