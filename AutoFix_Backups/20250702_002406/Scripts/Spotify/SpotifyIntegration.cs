using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using VRBoxingGame.Music;
using System.Threading.Tasks;

namespace VRBoxingGame.Spotify
{
    /// <summary>
    /// Spotify Integration for loading music tracks and metadata
    /// Note: This is a simplified implementation for demonstration
    /// Real Spotify integration requires Spotify Web API and authentication
    /// </summary>
    public class SpotifyIntegration : MonoBehaviour
    {
        [Header("Spotify Settings")]
        public bool enableSpotifyIntegration = false;
        public string clientId = "your_spotify_client_id";
        public string clientSecret = "your_spotify_client_secret";
        public string redirectUri = "http://localhost:8080/callback";
        
        [Header("Local Music Fallback")]
        public AudioClip[] localMusicTracks;
        public TrackInfo[] localTrackInfo;
        
        [Header("Events")]
        public UnityEvent<TrackInfo> OnTrackLoaded;
        public UnityEvent<string> OnTrackChanged;
        public UnityEvent<float> OnPlaybackProgress;
        public UnityEvent<bool> OnConnectionStatusChanged;
        
        // Track information structure
        [System.Serializable]
        public struct TrackInfo
        {
            public string trackName;
            public string artistName;
            public string albumName;
            public float duration;
            public float bpm;
            public string genre;
            public AudioClip audioClip;
            public Texture2D albumArt;
        }
        
        [System.Serializable]
        public struct Playlist
        {
            public string playlistName;
            public string playlistId;
            public TrackInfo[] tracks;
        }
        
        // Private variables
        private List<TrackInfo> availableTracks = new List<TrackInfo>();
        private List<Playlist> availablePlaylists = new List<Playlist>();
        private TrackInfo currentTrack;
        private int currentTrackIndex = 0;
        
        private bool isConnected = false;
        private string accessToken = "";
        private float tokenExpiryTime = 0f;
        
        // Playback control
        private AudioSource audioSource;
        private bool isPlaying = false;
        private bool isPaused = false;
        
        // Singleton
        public static SpotifyIntegration Instance { get; private set; }
        
        // Properties
        public bool IsConnected => isConnected;
        public bool IsPlaying => isPlaying;
        public TrackInfo CurrentTrack => currentTrack;
        public List<TrackInfo> AvailableTracks => availableTracks;
        public float PlaybackProgress => audioSource ? audioSource.time / audioSource.clip.length : 0f;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSpotifyIntegration();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSpotifyIntegration()
        {
            // Create audio source for playback
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.7f;
            audioSource.loop = false;
            
            // Load local tracks first
            LoadLocalTracks();
            
            // Try to connect to Spotify
            if (!string.IsNullOrEmpty(clientId) && clientId != "your_spotify_client_id")
            {
                _ = ConnectToSpotifyAsync();
            }
            else
            {
                Debug.LogWarning("Spotify credentials not configured - using local tracks only");
                isConnected = true; // Mark as connected to use local tracks
                OnConnectionStatusChanged?.Invoke(isConnected);
            }
        }
        
        private void LoadLocalTracks()
        {
            availableTracks.Clear();
            
            // Add local tracks
            for (int i = 0; i < localMusicTracks.Length; i++)
            {
                TrackInfo track = new TrackInfo();
                
                if (i < localTrackInfo.Length)
                {
                    track = localTrackInfo[i];
                }
                else
                {
                    // Generate default track info
                    track.trackName = localMusicTracks[i].name;
                    track.artistName = "Unknown Artist";
                    track.albumName = "Unknown Album";
                    track.genre = "Electronic";
                    track.bpm = 120f;
                }
                
                track.audioClip = localMusicTracks[i];
                track.duration = localMusicTracks[i].length;
                
                availableTracks.Add(track);
            }
            
            Debug.Log($"Loaded {availableTracks.Count} local music tracks");
        }
        
        private async Task ConnectToSpotifyAsync()
        {
            // Real Spotify connection attempt
            Debug.Log("Attempting to connect to Spotify...");
            
            await Task.Delay(2000); // Simulate connection time
            
            // Try real connection first
            bool connectionSuccessful = TryRealSpotifyConnection();
            
            if (connectionSuccessful)
            {
                isConnected = true;
                accessToken = "real_access_token";
                tokenExpiryTime = Time.time + 3600f; // 1 hour
                
                // Load Spotify tracks
                _ = LoadSpotifyTracksAsync();
                Debug.Log("✅ Connected to Spotify successfully!");
            }
            else
            {
                Debug.LogWarning("⚠️ Spotify connection failed - using local tracks as fallback");
                isConnected = true; // Still mark as connected to use local tracks
            }
            
            OnConnectionStatusChanged?.Invoke(isConnected);
        }
        
        private bool TryRealSpotifyConnection()
        {
            // Check if we have valid credentials
            if (string.IsNullOrEmpty(clientId) || clientId == "your_spotify_client_id")
            {
                Debug.LogWarning("Spotify Client ID not configured - using local music");
                return false;
            }
            
            if (string.IsNullOrEmpty(clientSecret) || clientSecret == "your_spotify_client_secret")
            {
                Debug.LogWarning("Spotify Client Secret not configured - using local music");
                return false;
            }
            
            // If we have RealSpotifyIntegration, use that
            var realSpotify = FindObjectOfType<RealSpotifyIntegration>();
            if (realSpotify != null)
            {
                realSpotify.ConnectToSpotify();
                return realSpotify.IsConnected;
            }
            
            // TODO: Implement actual Spotify Web API authentication here
            // For now, return false to use local tracks
            return false;
        }
        
        private async Task LoadSpotifyTracksAsync()
        {
            // Simulate loading Spotify tracks
            Debug.Log("Loading Spotify tracks...");
            
            await Task.Delay(1000);
            
            // In a real implementation, this would make API calls to:
            // - Get user's playlists
            // - Get tracks from playlists
            // - Get track audio features (BPM, energy, etc.)
            
            Debug.Log("Spotify tracks loaded (simulated)");
        }
        
        private void Update()
        {
            // Handle track playback
            if (isPlaying && audioSource && audioSource.isPlaying)
            {
                OnPlaybackProgress?.Invoke(PlaybackProgress);
                
                // Check if track finished
                if (!audioSource.isPlaying && !isPaused)
                {
                    OnTrackFinished();
                }
            }
            
            // Check token expiry
            if (isConnected && Time.time > tokenExpiryTime)
            {
                _ = RefreshAccessTokenAsync();
            }
        }
        
        private async Task RefreshAccessTokenAsync()
        {
            Debug.Log("Refreshing Spotify access token...");
            
            await Task.Delay(1000);
            
            // In real implementation, would refresh the token
            tokenExpiryTime = Time.time + 3600f; // Extend for another hour
            Debug.Log("Access token refreshed");
        }
        
        public void PlayTrack(int trackIndex)
        {
            if (trackIndex < 0 || trackIndex >= availableTracks.Count) return;
            
            currentTrackIndex = trackIndex;
            currentTrack = availableTracks[trackIndex];
            
            if (currentTrack.audioClip != null)
            {
                audioSource.clip = currentTrack.audioClip;
                audioSource.Play();
                isPlaying = true;
                isPaused = false;
                
                // Notify music reactive system
                if (MusicReactiveSystem.Instance != null)
                {
                    MusicReactiveSystem.Instance.PlayMusic(currentTrack.audioClip);
                }
                
                OnTrackLoaded?.Invoke(currentTrack);
                OnTrackChanged?.Invoke($"{currentTrack.artistName} - {currentTrack.trackName}");
                
                Debug.Log($"Playing: {currentTrack.artistName} - {currentTrack.trackName}");
            }
        }
        
        public void PlayTrack(string trackName)
        {
            for (int i = 0; i < availableTracks.Count; i++)
            {
                if (availableTracks[i].trackName.ToLower().Contains(trackName.ToLower()))
                {
                    PlayTrack(i);
                    return;
                }
            }
            
            Debug.LogWarning($"Track not found: {trackName}");
        }
        
        public void PlayNextTrack()
        {
            int nextIndex = (currentTrackIndex + 1) % availableTracks.Count;
            PlayTrack(nextIndex);
        }
        
        public void PlayPreviousTrack()
        {
            int prevIndex = currentTrackIndex - 1;
            if (prevIndex < 0) prevIndex = availableTracks.Count - 1;
            PlayTrack(prevIndex);
        }
        
        public void PauseTrack()
        {
            if (audioSource && audioSource.isPlaying)
            {
                audioSource.Pause();
                isPaused = true;
                isPlaying = false;
                Debug.Log("Track paused");
            }
        }
        
        public void ResumeTrack()
        {
            if (audioSource && isPaused)
            {
                audioSource.UnPause();
                isPaused = false;
                isPlaying = true;
                Debug.Log("Track resumed");
            }
        }
        
        public void StopTrack()
        {
            if (audioSource)
            {
                audioSource.Stop();
                isPlaying = false;
                isPaused = false;
                Debug.Log("Track stopped");
            }
        }
        
        public void SetVolume(float volume)
        {
            if (audioSource)
            {
                audioSource.volume = Mathf.Clamp01(volume);
            }
        }
        
        public void SeekToPosition(float normalizedPosition)
        {
            if (audioSource && audioSource.clip)
            {
                float targetTime = normalizedPosition * audioSource.clip.length;
                audioSource.time = Mathf.Clamp(targetTime, 0f, audioSource.clip.length);
            }
        }
        
        private void OnTrackFinished()
        {
            isPlaying = false;
            isPaused = false;
            
            // Auto-play next track
            PlayNextTrack();
        }
        
        public List<TrackInfo> GetTracksByGenre(string genre)
        {
            List<TrackInfo> genreTracks = new List<TrackInfo>();
            
            foreach (var track in availableTracks)
            {
                if (track.genre.ToLower().Contains(genre.ToLower()))
                {
                    genreTracks.Add(track);
                }
            }
            
            return genreTracks;
        }
        
        public List<TrackInfo> GetTracksByBPMRange(float minBPM, float maxBPM)
        {
            List<TrackInfo> bpmTracks = new List<TrackInfo>();
            
            foreach (var track in availableTracks)
            {
                if (track.bpm >= minBPM && track.bpm <= maxBPM)
                {
                    bpmTracks.Add(track);
                }
            }
            
            return bpmTracks;
        }
        
        public void CreateWorkoutPlaylist(float targetBPM, float tolerance = 10f)
        {
            List<TrackInfo> workoutTracks = GetTracksByBPMRange(targetBPM - tolerance, targetBPM + tolerance);
            
            if (workoutTracks.Count > 0)
            {
                Debug.Log($"Created workout playlist with {workoutTracks.Count} tracks (BPM: {targetBPM})");
                
                // Play first track from workout playlist
                int originalIndex = availableTracks.IndexOf(workoutTracks[0]);
                if (originalIndex >= 0)
                {
                    PlayTrack(originalIndex);
                }
            }
            else
            {
                Debug.LogWarning("No tracks found for workout playlist");
            }
        }
        
        // Spotify Web API simulation methods
        private async Task GetUserPlaylistsAsync()
        {
            // Simulate API call
            await Task.Delay(500);
            
            // In real implementation, would make HTTP request to:
            // https://api.spotify.com/v1/me/playlists
            
            Debug.Log("Fetched user playlists (simulated)");
        }
        
        private async Task GetPlaylistTracksAsync(string playlistId)
        {
            // Simulate API call
            await Task.Delay(500);
            
            // In real implementation, would make HTTP request to:
            // https://api.spotify.com/v1/playlists/{playlist_id}/tracks
            
            Debug.Log($"Fetched playlist tracks for {playlistId} (simulated)");
        }
        
        private async Task GetTrackAudioFeaturesAsync(string trackId)
        {
            // Simulate API call
            await Task.Delay(300);
            
            // In real implementation, would make HTTP request to:
            // https://api.spotify.com/v1/audio-features/{track_id}
            
            Debug.Log($"Fetched audio features for {trackId} (simulated)");
        }
        
        // Public API for external systems
        public TrackInfo GetCurrentTrackInfo()
        {
            return currentTrack;
        }
        
        public float GetCurrentBPM()
        {
            return currentTrack.bpm;
        }
        
        public bool HasTracks()
        {
            return availableTracks.Count > 0;
        }
        
        public void ShuffleTracks()
        {
            // Shuffle available tracks
            for (int i = 0; i < availableTracks.Count; i++)
            {
                TrackInfo temp = availableTracks[i];
                int randomIndex = Random.Range(i, availableTracks.Count);
                availableTracks[i] = availableTracks[randomIndex];
                availableTracks[randomIndex] = temp;
            }
            
            Debug.Log("Tracks shuffled");
        }
        
        private void OnDestroy()
        {
            StopTrack();
        }
        
        // Debug methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintTracks()
        {
            Debug.Log("=== Available Tracks ===");
            for (int i = 0; i < availableTracks.Count; i++)
            {
                var track = availableTracks[i];
                Debug.Log($"{i}: {track.artistName} - {track.trackName} (BPM: {track.bpm}, Duration: {track.duration:F1}s)");
            }
        }
    }
} 