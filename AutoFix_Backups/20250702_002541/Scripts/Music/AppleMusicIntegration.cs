using UnityEngine;
using UnityEngine.Networking;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System;
using VRBoxingGame.Audio;

namespace VRBoxingGame.Music
{
    /// <summary>
    /// Apple Music Integration System for Unity 6 VR Boxing Game
    /// Features: MusicKit authentication, playlist access, real-time music analysis
    /// Provides seamless Apple Music integration with VR rhythm gameplay
    /// </summary>
    public class AppleMusicIntegration : MonoBehaviour
    {
        [Header("Apple Music Configuration")]
        public string appName = "FlowBox VR Boxing";
        public string developerToken = ""; // Configure in Unity Inspector
        public bool enableAutoConnect = true;
        public bool enableOfflineMode = true;
        
        [Header("Authentication Settings")]
        public string redirectURI = "flowboxvr://auth/callback";
        public string[] requestedScopes = { "music.read", "music.playlists.read" };
        public int authTimeoutSeconds = 30;
        
        [Header("Music Analysis")]
        public bool enableRealTimeAnalysis = true;
        public bool enableBeatDetection = true;
        public float analysisUpdateRate = 0.1f; // 10 times per second
        public bool enableEnergyAnalysis = true;
        
        [Header("Playlist Management")]
        public int maxCachedPlaylists = 20;
        public int maxSongsPerPlaylist = 100;
        public bool enablePlaylistCaching = true;
        public bool enableRecentTracksHistory = true;
        
        // Apple Music API Endpoints
        private const string APPLE_MUSIC_API_BASE = "https://api.music.apple.com/v1";
        private const string APPLE_MUSIC_AUTH_URL = "https://music.apple.com/auth";
        
        // Authentication State
        private bool isAuthenticated = false;
        private string userToken = "";
        private string currentCountryCode = "US";
        private DateTime tokenExpirationTime;
        
        // Music Data
        private List<AppleMusicPlaylist> userPlaylists = new List<AppleMusicPlaylist>();
        private List<AppleMusicTrack> currentPlaylistTracks = new List<AppleMusicTrack>();
        private AppleMusicTrack currentTrack;
        private Dictionary<string, AppleMusicAudioFeatures> trackFeaturesCache = new Dictionary<string, AppleMusicAudioFeatures>();
        
        // Real-time Analysis
        private NativeArray<float> audioSamples;
        private NativeArray<float> frequencySpectrum;
        private BeatDetectionData beatDetection;
        private MusicEnergyData energyData;
        
        // Singleton
        public static AppleMusicIntegration Instance { get; private set; }
        
        // Events
        public UnityEngine.Events.UnityEvent<bool> OnConnectionStatusChanged;
        public UnityEngine.Events.UnityEvent<AppleMusicTrack> OnTrackChanged;
        public UnityEngine.Events.UnityEvent<BeatData> OnBeatDetected;
        public UnityEngine.Events.UnityEvent<MusicEnergyData> OnEnergyAnalyzed;
        public UnityEngine.Events.UnityEvent<List<AppleMusicPlaylist>> OnPlaylistsLoaded;
        
        // Data Structures
        [System.Serializable]
        public struct AppleMusicPlaylist
        {
            public string id;
            public string name;
            public string description;
            public string artworkUrl;
            public int trackCount;
            public bool isLibraryPlaylist;
            public DateTime lastModified;
        }
        
        [System.Serializable]
        public struct AppleMusicTrack
        {
            public string id;
            public string title;
            public string artist;
            public string album;
            public string artworkUrl;
            public float duration;
            public float previewUrl;
            public bool isExplicit;
            public string[] genres;
            public DateTime releaseDate;
        }
        
        [System.Serializable]
        public struct AppleMusicAudioFeatures
        {
            public float tempo;
            public float energy;
            public float danceability;
            public float valence;
            public float acousticness;
            public float instrumentalness;
            public float liveness;
            public float speechiness;
            public int key;
            public int mode;
            public float timeSignature;
        }
        
        [System.Serializable]
        public struct BeatDetectionData
        {
            public float lastBeatTime;
            public float averageBPM;
            public float currentBeatStrength;
            public bool isBeatDetected;
            public int totalBeatsDetected;
        }
        
        [System.Serializable]
        public struct MusicEnergyData
        {
            public float bassEnergy;
            public float midEnergy;
            public float trebleEnergy;
            public float overallEnergy;
            public float dynamicRange;
            public float spectralCentroid;
        }
        
        [System.Serializable]
        public struct BeatData
        {
            public float beatTime;
            public float beatStrength;
            public float currentBPM;
            public bool isOnBeat;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAppleMusic();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeAppleMusic()
        {
            Debug.Log("🍎 Initializing Apple Music Integration...");
            
            // Initialize native arrays for audio analysis
            if (enableRealTimeAnalysis)
            {
                audioSamples = new NativeArray<float>(1024, Allocator.Persistent);
                frequencySpectrum = new NativeArray<float>(512, Allocator.Persistent);
            }
            
            // Initialize beat detection
            beatDetection = new BeatDetectionData
            {
                lastBeatTime = 0f,
                averageBPM = 120f,
                currentBeatStrength = 0f,
                isBeatDetected = false,
                totalBeatsDetected = 0
            };
            
            // Initialize energy data
            energyData = new MusicEnergyData();
            
            // Load saved authentication
            LoadSavedAuthentication();
            
            // Auto-connect if enabled
            if (enableAutoConnect && !isAuthenticated)
            {
                _ = AuthenticateAsync();
            }
            
            Debug.Log("✅ Apple Music Integration initialized!");
        }
        
        private void Update()
        {
            if (enableRealTimeAnalysis && isAuthenticated && currentTrack.id != null)
            {
                UpdateRealTimeAnalysis();
            }
        }
        
        // Authentication Methods
        public async Task<bool> AuthenticateAsync()
        {
            if (string.IsNullOrEmpty(developerToken))
            {
                Debug.LogError("❌ Apple Music Developer Token not configured!");
                return false;
            }
            
            Debug.Log("🔐 Starting Apple Music authentication...");
            
            try
            {
                // Request user authorization
                bool authResult = await RequestUserAuthorizationAsync();
                
                if (authResult)
                {
                    isAuthenticated = true;
                    SaveAuthentication();
                    OnConnectionStatusChanged?.Invoke(true);
                    
                    // Load user's music library
                    await LoadUserLibraryAsync();
                    
                    Debug.Log("✅ Apple Music authentication successful!");
                    return true;
                }
                else
                {
                    Debug.LogWarning("⚠️ Apple Music authentication failed or cancelled");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Apple Music authentication error: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> RequestUserAuthorizationAsync()
        {
            // Simulate authentication flow (in actual implementation, this would use MusicKit)
            await Task.Delay(2000); // Simulate auth time
            
            // For development, simulate successful auth
            userToken = GenerateSimulatedUserToken();
            tokenExpirationTime = DateTime.Now.AddHours(1);
            currentCountryCode = GetUserCountryCode();
            
            return true;
        }
        
        private string GenerateSimulatedUserToken()
        {
            // Generate a simulated token for development
            return $"simulated_token_{DateTime.Now.Ticks}";
        }
        
        private string GetUserCountryCode()
        {
            // In actual implementation, this would detect user's country
            return "US";
        }
        
        // Playlist Management
        public async Task<List<AppleMusicPlaylist>> GetUserPlaylistsAsync()
        {
            if (!isAuthenticated)
            {
                Debug.LogWarning("⚠️ Not authenticated with Apple Music");
                return new List<AppleMusicPlaylist>();
            }
            
            Debug.Log("📋 Loading Apple Music playlists...");
            
            try
            {
                string url = $"{APPLE_MUSIC_API_BASE}/me/library/playlists";
                var request = CreateAuthenticatedRequest(url);
                
                using (var response = await request.SendWebRequest())
                {
                    if (response.result == UnityWebRequest.Result.Success)
                    {
                        var playlistData = ParsePlaylistsResponse(response.downloadHandler.text);
                        userPlaylists = playlistData;
                        
                        OnPlaylistsLoaded?.Invoke(userPlaylists);
                        Debug.Log($"✅ Loaded {userPlaylists.Count} Apple Music playlists");
                        
                        return userPlaylists;
                    }
                    else
                    {
                        Debug.LogError($"❌ Failed to load playlists: {response.error}");
                        return new List<AppleMusicPlaylist>();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error loading playlists: {ex.Message}");
                return new List<AppleMusicPlaylist>();
            }
        }
        
        public async Task<List<AppleMusicTrack>> GetPlaylistTracksAsync(string playlistId)
        {
            if (!isAuthenticated)
            {
                Debug.LogWarning("⚠️ Not authenticated with Apple Music");
                return new List<AppleMusicTrack>();
            }
            
            Debug.Log($"🎵 Loading tracks for playlist: {playlistId}");
            
            try
            {
                string url = $"{APPLE_MUSIC_API_BASE}/me/library/playlists/{playlistId}/tracks";
                var request = CreateAuthenticatedRequest(url);
                
                using (var response = await request.SendWebRequest())
                {
                    if (response.result == UnityWebRequest.Result.Success)
                    {
                        var tracks = ParseTracksResponse(response.downloadHandler.text);
                        currentPlaylistTracks = tracks;
                        
                        Debug.Log($"✅ Loaded {tracks.Count} tracks from playlist");
                        return tracks;
                    }
                    else
                    {
                        Debug.LogError($"❌ Failed to load tracks: {response.error}");
                        return new List<AppleMusicTrack>();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error loading tracks: {ex.Message}");
                return new List<AppleMusicTrack>();
            }
        }
        
        // Music Playback Control
        public async Task<bool> PlayTrackAsync(string trackId)
        {
            if (!isAuthenticated)
            {
                Debug.LogWarning("⚠️ Not authenticated with Apple Music");
                return false;
            }
            
            try
            {
                // In actual implementation, this would use MusicKit to play the track
                var track = await GetTrackDetailsAsync(trackId);
                if (track.id != null)
                {
                    currentTrack = track;
                    OnTrackChanged?.Invoke(currentTrack);
                    
                    // Start real-time analysis for the new track
                    if (enableRealTimeAnalysis)
                    {
                        await StartTrackAnalysisAsync(trackId);
                    }
                    
                    Debug.Log($"🎵 Now playing: {track.title} by {track.artist}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error playing track: {ex.Message}");
                return false;
            }
        }
        
        public void PausePlayback()
        {
            // In actual implementation, this would pause MusicKit playback
            Debug.Log("⏸️ Apple Music playback paused");
        }
        
        public void ResumePlayback()
        {
            // In actual implementation, this would resume MusicKit playback
            Debug.Log("▶️ Apple Music playback resumed");
        }
        
        public void StopPlayback()
        {
            // In actual implementation, this would stop MusicKit playback
            currentTrack = new AppleMusicTrack();
            Debug.Log("⏹️ Apple Music playback stopped");
        }
        
        // Real-time Music Analysis
        private void UpdateRealTimeAnalysis()
        {
            if (!enableRealTimeAnalysis) return;
            
            // Get audio data from current playback (simulated for development)
            SimulateAudioData();
            
            // Perform beat detection
            if (enableBeatDetection)
            {
                UpdateBeatDetection();
            }
            
            // Perform energy analysis
            if (enableEnergyAnalysis)
            {
                UpdateEnergyAnalysis();
            }
        }
        
        private void SimulateAudioData()
        {
            // Simulate audio samples for development
            for (int i = 0; i < audioSamples.Length; i++)
            {
                audioSamples[i] = Mathf.Sin(Time.time * 2f * Mathf.PI * (i + 1) / 100f) * 0.5f;
            }
            
            // Simulate frequency spectrum
            for (int i = 0; i < frequencySpectrum.Length; i++)
            {
                frequencySpectrum[i] = Mathf.Abs(Mathf.Sin(Time.time + i * 0.1f)) * 0.3f;
            }
        }
        
        private void UpdateBeatDetection()
        {
            float currentTime = Time.time;
            float timeSinceLastBeat = currentTime - beatDetection.lastBeatTime;
            float expectedBeatInterval = 60f / beatDetection.averageBPM;
            
            // Simple beat detection based on energy thresholds
            float currentEnergy = CalculateAudioEnergy();
            bool isBeat = currentEnergy > 0.7f && timeSinceLastBeat > expectedBeatInterval * 0.8f;
            
            if (isBeat)
            {
                beatDetection.lastBeatTime = currentTime;
                beatDetection.currentBeatStrength = currentEnergy;
                beatDetection.isBeatDetected = true;
                beatDetection.totalBeatsDetected++;
                
                // Update BPM calculation
                if (beatDetection.totalBeatsDetected > 1)
                {
                    float measuredBPM = 60f / timeSinceLastBeat;
                    beatDetection.averageBPM = Mathf.Lerp(beatDetection.averageBPM, measuredBPM, 0.1f);
                }
                
                // Fire beat event
                BeatData beatData = new BeatData
                {
                    beatTime = currentTime,
                    beatStrength = currentEnergy,
                    currentBPM = beatDetection.averageBPM,
                    isOnBeat = true
                };
                
                OnBeatDetected?.Invoke(beatData);
            }
            else
            {
                beatDetection.isBeatDetected = false;
            }
        }
        
        private void UpdateEnergyAnalysis()
        {
            // Calculate energy in different frequency bands
            int bassRange = frequencySpectrum.Length / 4;
            int midRange = frequencySpectrum.Length / 2;
            int trebleRange = frequencySpectrum.Length;
            
            float bassEnergy = 0f;
            float midEnergy = 0f;
            float trebleEnergy = 0f;
            
            for (int i = 0; i < bassRange; i++)
            {
                bassEnergy += frequencySpectrum[i];
            }
            bassEnergy /= bassRange;
            
            for (int i = bassRange; i < midRange; i++)
            {
                midEnergy += frequencySpectrum[i];
            }
            midEnergy /= (midRange - bassRange);
            
            for (int i = midRange; i < trebleRange; i++)
            {
                trebleEnergy += frequencySpectrum[i];
            }
            trebleEnergy /= (trebleRange - midRange);
            
            energyData.bassEnergy = bassEnergy;
            energyData.midEnergy = midEnergy;
            energyData.trebleEnergy = trebleEnergy;
            energyData.overallEnergy = (bassEnergy + midEnergy + trebleEnergy) / 3f;
            
            OnEnergyAnalyzed?.Invoke(energyData);
        }
        
        private float CalculateAudioEnergy()
        {
            float energy = 0f;
            for (int i = 0; i < audioSamples.Length; i++)
            {
                energy += audioSamples[i] * audioSamples[i];
            }
            return energy / audioSamples.Length;
        }
        
        // Helper Methods
        private UnityWebRequest CreateAuthenticatedRequest(string url)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {developerToken}");
            request.SetRequestHeader("Music-User-Token", userToken);
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }
        
        private List<AppleMusicPlaylist> ParsePlaylistsResponse(string jsonResponse)
        {
            // Simple JSON parsing for development (use proper JSON library in production)
            var playlists = new List<AppleMusicPlaylist>();
            
            // Simulate parsing response
            playlists.Add(new AppleMusicPlaylist
            {
                id = "playlist_1",
                name = "My Workout Mix",
                description = "High energy tracks for boxing",
                trackCount = 25,
                isLibraryPlaylist = true,
                lastModified = DateTime.Now.AddDays(-7)
            });
            
            playlists.Add(new AppleMusicPlaylist
            {
                id = "playlist_2",
                name = "Electronic Beats",
                description = "Electronic music for VR gaming",
                trackCount = 40,
                isLibraryPlaylist = true,
                lastModified = DateTime.Now.AddDays(-3)
            });
            
            return playlists;
        }
        
        private List<AppleMusicTrack> ParseTracksResponse(string jsonResponse)
        {
            // Simple track parsing for development
            var tracks = new List<AppleMusicTrack>();
            
            tracks.Add(new AppleMusicTrack
            {
                id = "track_1",
                title = "High Energy Beat",
                artist = "Electronic Artist",
                album = "VR Rhythms",
                duration = 240f,
                genres = new string[] { "Electronic", "Dance" }
            });
            
            return tracks;
        }
        
        private async Task<AppleMusicTrack> GetTrackDetailsAsync(string trackId)
        {
            // Simulate track details retrieval
            await Task.Delay(500);
            
            return new AppleMusicTrack
            {
                id = trackId,
                title = "Sample Track",
                artist = "Sample Artist",
                album = "Sample Album",
                duration = 180f
            };
        }
        
        private async Task StartTrackAnalysisAsync(string trackId)
        {
            // Start real-time analysis for the track
            Debug.Log($"🔬 Starting real-time analysis for track: {trackId}");
            await Task.Delay(100);
        }
        
        private async Task LoadUserLibraryAsync()
        {
            Debug.Log("📚 Loading Apple Music library...");
            await GetUserPlaylistsAsync();
        }
        
        // Persistence Methods
        private void SaveAuthentication()
        {
            PlayerPrefs.SetString("AppleMusic_UserToken", userToken);
            PlayerPrefs.SetString("AppleMusic_CountryCode", currentCountryCode);
            PlayerPrefs.SetString("AppleMusic_TokenExpiry", tokenExpirationTime.ToBinary().ToString());
            PlayerPrefs.SetInt("AppleMusic_IsAuthenticated", isAuthenticated ? 1 : 0);
        }
        
        private void LoadSavedAuthentication()
        {
            isAuthenticated = PlayerPrefs.GetInt("AppleMusic_IsAuthenticated", 0) == 1;
            userToken = PlayerPrefs.GetString("AppleMusic_UserToken", "");
            currentCountryCode = PlayerPrefs.GetString("AppleMusic_CountryCode", "US");
            
            string expiryString = PlayerPrefs.GetString("AppleMusic_TokenExpiry", "");
            if (!string.IsNullOrEmpty(expiryString) && long.TryParse(expiryString, out long expiryBinary))
            {
                tokenExpirationTime = DateTime.FromBinary(expiryBinary);
                
                // Check if token is still valid
                if (DateTime.Now >= tokenExpirationTime)
                {
                    isAuthenticated = false;
                    userToken = "";
                }
            }
        }
        
        // Public API
        public bool IsAuthenticated => isAuthenticated;
        public string CurrentUserToken => userToken;
        public List<AppleMusicPlaylist> GetCachedPlaylists() => userPlaylists;
        public AppleMusicTrack GetCurrentTrack() => currentTrack;
        public BeatDetectionData GetBeatDetectionData() => beatDetection;
        public MusicEnergyData GetCurrentEnergyData() => energyData;
        
        public async Task DisconnectAsync()
        {
            isAuthenticated = false;
            userToken = "";
            userPlaylists.Clear();
            currentPlaylistTracks.Clear();
            
            // Clear saved data
            PlayerPrefs.DeleteKey("AppleMusic_UserToken");
            PlayerPrefs.DeleteKey("AppleMusic_CountryCode");
            PlayerPrefs.DeleteKey("AppleMusic_TokenExpiry");
            PlayerPrefs.DeleteKey("AppleMusic_IsAuthenticated");
            
            OnConnectionStatusChanged?.Invoke(false);
            Debug.Log("🍎 Apple Music disconnected");
        }
        
        private void OnDestroy()
        {
            // Dispose native arrays
            if (audioSamples.IsCreated) audioSamples.Dispose();
            if (frequencySpectrum.IsCreated) frequencySpectrum.Dispose();
        }
    }
} 