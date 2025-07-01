using UnityEngine;
using System.Collections;
using VRBoxingGame.Audio;
using System.Threading.Tasks;

namespace VRBoxingGame.Audio
{
    /// <summary>
    /// Simple audio generator for testing the rhythm game
    /// Creates a basic beat pattern without requiring external audio files
    /// </summary>
    public class TestTrack : MonoBehaviour
    {
        [Header("Test Track Settings")]
        public float bpm = 120f;
        public float trackLength = 180f; // 3 minutes
        public bool autoPlay = true;
        
        [Header("Beat Generation")]
        public float beatVolume = 0.5f;
        public float bassVolume = 0.3f;
        public int sampleRate = 44100;
        
        private AudioSource audioSource;
        private AdvancedAudioManager audioManager;
        private bool isPlaying = false;
        private float currentTime = 0f;
        private float beatInterval;
        
        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioManager = FindObjectOfType<AdvancedAudioManager>();
            
            beatInterval = 60f / bpm;
            
            if (autoPlay)
            {
                _ = StartTestTrackAsync();
            }
        }
        
        private async Task StartTestTrackAsync()
        {
            await Task.Delay(1000); // Wait for other systems to initialize
            
            GenerateTestTrack();
            PlayTestTrack();
        }
        
        private void GenerateTestAudio()
        {
            // Create a simple beat pattern
            int samples = Mathf.RoundToInt(trackLength * sampleRate);
            float[] audioData = new float[samples];
            
            float beatsPerSecond = bpm / 60f;
            
            for (int i = 0; i < samples; i++)
            {
                float time = (float)i / sampleRate;
                float beatTime = time * beatsPerSecond;
                
                // Generate kick drum on every beat
                float kickPhase = (beatTime % 1f);
                if (kickPhase < 0.1f)
                {
                    float kickEnvelope = Mathf.Exp(-kickPhase * 50f);
                    audioData[i] += Mathf.Sin(2f * Mathf.PI * 60f * time) * kickEnvelope * bassVolume;
                }
                
                // Generate hi-hat on off-beats
                float hihatPhase = ((beatTime + 0.5f) % 1f);
                if (hihatPhase < 0.05f)
                {
                    float hihatEnvelope = Mathf.Exp(-hihatPhase * 100f);
                    float noise = Random.Range(-1f, 1f);
                    audioData[i] += noise * hihatEnvelope * beatVolume * 0.3f;
                }
                
                // Generate snare on beats 2 and 4
                float snarePhase = ((beatTime + 2f) % 4f);
                if (snarePhase < 0.1f && (snarePhase >= 1.9f && snarePhase <= 2.1f || snarePhase >= 3.9f))
                {
                    float snareEnvelope = Mathf.Exp(-snarePhase * 30f);
                    float snareNoise = Random.Range(-1f, 1f);
                    audioData[i] += snareNoise * snareEnvelope * beatVolume;
                }
                
                // Add some bass line
                float bassFreq = 80f + 20f * Mathf.Sin(2f * Mathf.PI * time * 0.25f);
                audioData[i] += Mathf.Sin(2f * Mathf.PI * bassFreq * time) * bassVolume * 0.5f;
            }
            
            // Create AudioClip from generated data
            AudioClip generatedClip = AudioClip.Create("TestTrack", samples, 1, sampleRate, false);
            generatedClip.SetData(audioData, 0);
            
            audioSource.clip = generatedClip;
            audioSource.loop = true;
            
            Debug.Log($"Generated test track: {trackLength}s at {bpm} BPM");
        }
        
        public void PlayTestTrack()
        {
            if (audioSource.clip != null)
            {
                audioSource.Play();
                isPlaying = true;
                
                // Notify audio manager
                if (audioManager != null)
                {
                    audioManager.SetMusicSource(audioSource);
                }
                
                Debug.Log("Playing test track");
            }
        }
        
        public void StopTestTrack()
        {
            audioSource.Stop();
            isPlaying = false;
        }
        
        public void SetBPM(float newBPM)
        {
            bpm = Mathf.Clamp(newBPM, 60f, 200f);
            beatInterval = 60f / bpm;
            
            // Regenerate audio with new BPM
            if (isPlaying)
            {
                StopTestTrack();
                GenerateTestAudio();
                PlayTestTrack();
            }
        }
        
        /// <summary>
        /// Public method to generate test track - called by setup scripts
        /// </summary>
        public void GenerateTestTrack()
        {
            GenerateTestAudio();
            Debug.Log("Test track generated and ready to play");
        }
        
        private void Update()
        {
            if (isPlaying && audioSource.isPlaying)
            {
                currentTime = audioSource.time;
                
                // Send beat events manually for testing
                float beatPosition = (currentTime * bpm / 60f) % 1f;
                if (beatPosition < 0.1f && Time.frameCount % 10 == 0) // Throttle beat events
                {
                    SendBeatEvent();
                }
            }
        }
        
        private void SendBeatEvent()
        {
            if (audioManager != null)
            {
                // Simulate beat detection data
                var beatData = new AdvancedAudioManager.BeatData
                {
                    beatStrength = 0.8f,
                    isStrongBeat = true,
                    bpm = bpm,
                    timeStamp = Time.time
                };
                
                audioManager.SimulateBeatDetection(beatData);
            }
        }
        
        private void OnValidate()
        {
            bpm = Mathf.Clamp(bpm, 60f, 200f);
            trackLength = Mathf.Clamp(trackLength, 30f, 600f);
            beatVolume = Mathf.Clamp01(beatVolume);
            bassVolume = Mathf.Clamp01(bassVolume);
        }
    }
} 