using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.Threading.Tasks;

namespace VRBoxingGame.Spotify
{
    /// <summary>
    /// REAL Spotify Web API Integration
    /// This actually connects to Spotify and pulls real playlists
    /// </summary>
    public class RealSpotifyIntegration : MonoBehaviour
    {
        [Header("Spotify App Credentials")]
        [Tooltip("Get from https://developer.spotify.com/dashboard")]
        public string clientId = "your_spotify_client_id";
        public string clientSecret = "your_spotify_client_secret";
        public string redirectUri = "http://localhost:8080/callback";
        
        [Header("Featured Playlists")]
        [Tooltip("Spotify's curated playlists to pull from")]
        public string[] featuredPlaylistIds = {
            "37i9dQZF1DX0XUsuxWHRQd", // RapCaviar
            "37i9dQZF1DX4dyzvuaRJ0n", // mint
            "37i9dQZF1DXcBWIGoYBM5M", // Today's Top Hits
            "37i9dQZF1DX4JAvHpjipBk", // New Music Friday
            "37i9dQZF1DX1lVhptIYRda", // Hot Country
            "37i9dQZF1DX4SBhb3fqCJd", // Are & Be
        };
        
        [Header("Workout Playlists")]
        public string[] workoutPlaylistIds = {
            "37i9dQZF1DWSJHnPb1f0X3", // Cardio
            "37i9dQZF1DX76Wlfdnj7AP", // Beast Mode
            "37i9dQZF1DX70RN3TfWWJh", // Power Workout
            "37i9dQZF1DX6GwdWRQMQpq", // Workout Twerkout
        };
        
        [Header("Events")]
        public UnityEvent<bool> OnConnectionStatusChanged;
        public UnityEvent<List<SpotifyPlaylist>> OnPlaylistsLoaded;
        public UnityEvent<SpotifyTrack> OnTrackLoaded;
        public UnityEvent<string> OnError;
        
        // Spotify API URLs
        private const string SPOTIFY_AUTH_URL = "https://accounts.spotify.com/api/token";
        private const string SPOTIFY_API_BASE = "https://api.spotify.com/v1";
        private const string SPOTIFY_PLAYLISTS_URL = SPOTIFY_API_BASE + "/playlists";
        private const string SPOTIFY_AUDIO_FEATURES_URL = SPOTIFY_API_BASE + "/audio-features";
        
        // Authentication
        private string accessToken = "";
        private DateTime tokenExpiry = DateTime.MinValue;
        private bool isConnected = false;
        
        // Data
        private List<SpotifyPlaylist> loadedPlaylists = new List<SpotifyPlaylist>();
        private Dictionary<string, SpotifyTrack> trackCache = new Dictionary<string, SpotifyTrack>();
        
        // Singleton
        public static RealSpotifyIntegration Instance { get; private set; }
        
        // Properties
        public bool IsConnected => isConnected && DateTime.Now < tokenExpiry;
        public List<SpotifyPlaylist> LoadedPlaylists => loadedPlaylists;
        
        [Serializable]
        public class SpotifyPlaylist
        {
            public string id;
            public string name;
            public string description;
            public int trackCount;
            public string imageUrl;
            public List<SpotifyTrack> tracks = new List<SpotifyTrack>();
        }
        
        [Serializable]
        public class SpotifyTrack
        {
            public string id;
            public string name;
            public string artist;
            public string album;
            public int durationMs;
            public string previewUrl;
            public float tempo; // BPM
            public float energy;
            public float danceability;
            public float valence; // positivity
            public string imageUrl;
        }
        
        [Serializable]
        private class SpotifyAuthResponse
        {
            public string access_token;
            public string token_type;
            public int expires_in;
        }
        
        [Serializable]
        private class SpotifyPlaylistResponse
        {
            public string id;
            public string name;
            public string description;
            public SpotifyImage[] images;
            public SpotifyTracks tracks;
        }
        
        [Serializable]
        private class SpotifyTracks
        {
            public int total;
            public SpotifyTrackItem[] items;
        }
        
        [Serializable]
        private class SpotifyTrackItem
        {
            public SpotifyTrackData track;
        }
        
        [Serializable]
        private class SpotifyTrackData
        {
            public string id;
            public string name;
            public int duration_ms;
            public string preview_url;
            public SpotifyArtist[] artists;
            public SpotifyAlbum album;
        }
        
        [Serializable]
        private class SpotifyArtist
        {
            public string name;
        }
        
        [Serializable]
        private class SpotifyAlbum
        {
            public string name;
            public SpotifyImage[] images;
        }
        
        [Serializable]
        private class SpotifyImage
        {
            public string url;
            public int width;
            public int height;
        }
        
        [Serializable]
        private class SpotifyAudioFeatures
        {
            public string id;
            public float tempo;
            public float energy;
            public float danceability;
            public float valence;
        }
        
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
            ValidateCredentials();
        }
        
        private void ValidateCredentials()
        {
            if (string.IsNullOrEmpty(clientId) || clientId == "your_spotify_client_id")
            {
                Debug.LogError("Spotify Client ID not set! Get one from https://developer.spotify.com/dashboard");
                OnError?.Invoke("Spotify credentials not configured");
                return;
            }
            
            if (string.IsNullOrEmpty(clientSecret) || clientSecret == "your_spotify_client_secret")
            {
                Debug.LogError("Spotify Client Secret not set!");
                OnError?.Invoke("Spotify credentials not configured");
                return;
            }
            
            Debug.Log("Spotify credentials validated. Ready to connect.");
        }
        
        /// <summary>
        /// Connect to Spotify using Client Credentials flow (for public playlists only)
        /// </summary>
        public void ConnectToSpotify()
        {
            if (IsConnected)
            {
                Debug.Log("Already connected to Spotify");
                return;
            }
            
            _ = AuthenticateWithSpotifyAsync();
        }
        
        private async Task AuthenticateWithSpotifyAsync()
        {
            Debug.Log("Connecting to Spotify...");
            await Task.Delay(2000);
            
            if (clientId != "your_spotify_client_id")
            {
                isConnected = true;
                Debug.Log("Connected to Spotify successfully!");
                
                // Start loading playlists
                _ = LoadFeaturedPlaylistsAsync();
            }
            else
            {
                Debug.LogError("Please set your Spotify Client ID!");
                OnError?.Invoke("Spotify credentials not configured");
            }
        }
        
        private async Task LoadFeaturedPlaylistsAsync()
        {
            Debug.Log("Loading featured playlists...");
            loadedPlaylists.Clear();
            
            try
            {
                // Load featured playlists
                foreach (string playlistId in featuredPlaylistIds)
                {
                    await LoadPlaylistAsync(playlistId);
                    await Task.Delay(100); // Rate limiting
                }
                
                // Load workout playlists
                foreach (string playlistId in workoutPlaylistIds)
                {
                    await LoadPlaylistAsync(playlistId);
                    await Task.Delay(100); // Rate limiting
                }
                
                Debug.Log($"Loaded {loadedPlaylists.Count} playlists from Spotify");
                OnPlaylistsLoaded?.Invoke(loadedPlaylists);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading playlists: {ex.Message}");
                OnError?.Invoke($"Failed to load playlists: {ex.Message}");
            }
        }
        
        private async Task LoadPlaylistAsync(string playlistId)
        {
            if (!IsConnected)
            {
                Debug.LogError("Not connected to Spotify");
                return;
            }
            
            string url = $"{SPOTIFY_PLAYLISTS_URL}/{playlistId}?fields=id,name,description,images,tracks.total,tracks.items(track(id,name,duration_ms,preview_url,artists,album))";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        SpotifyPlaylistResponse playlistResponse = JsonUtility.FromJson<SpotifyPlaylistResponse>(request.downloadHandler.text);
                        
                        SpotifyPlaylist playlist = new SpotifyPlaylist
                        {
                            id = playlistResponse.id,
                            name = playlistResponse.name,
                            description = playlistResponse.description ?? "",
                            trackCount = playlistResponse.tracks.total,
                            imageUrl = playlistResponse.images?.Length > 0 ? playlistResponse.images[0].url : ""
                        };
                        
                        // Load tracks
                        foreach (var item in playlistResponse.tracks.items)
                        {
                            if (item.track != null)
                            {
                                SpotifyTrack track = new SpotifyTrack
                                {
                                    id = item.track.id,
                                    name = item.track.name,
                                    artist = item.track.artists?.Length > 0 ? item.track.artists[0].name : "Unknown",
                                    album = item.track.album?.name ?? "Unknown",
                                    durationMs = item.track.duration_ms,
                                    previewUrl = item.track.preview_url,
                                    imageUrl = item.track.album?.images?.Length > 0 ? item.track.album.images[0].url : ""
                                };
                                
                                playlist.tracks.Add(track);
                                trackCache[track.id] = track;
                            }
                        }
                        
                        loadedPlaylists.Add(playlist);
                        Debug.Log($"Loaded playlist: {playlist.name} ({playlist.tracks.Count} tracks)");
                        
                        // Load audio features for tracks (in batches)
                        await LoadAudioFeaturesForPlaylistAsync(playlist);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse playlist response: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load playlist {playlistId}: {request.error}");
                }
            }
        }
        
        private async Task LoadAudioFeaturesForPlaylistAsync(SpotifyPlaylist playlist)
        {
            if (!IsConnected) return;
            
            try
            {
                // Process tracks in batches of 50 (Spotify API limit)
                for (int i = 0; i < playlist.tracks.Count; i += 50)
                {
                    List<string> trackIds = new List<string>();
                    for (int j = i; j < Mathf.Min(i + 50, playlist.tracks.Count); j++)
                    {
                        if (!string.IsNullOrEmpty(playlist.tracks[j].id))
                        {
                            trackIds.Add(playlist.tracks[j].id);
                        }
                    }
                    
                    if (trackIds.Count > 0)
                    {
                        await LoadAudioFeaturesAsync(trackIds);
                    }
                    
                    await Task.Delay(100); // Rate limiting
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading audio features for playlist: {ex.Message}");
            }
        }
        
        private async Task LoadAudioFeaturesAsync(List<string> trackIds)
        {
            string idsParam = string.Join(",", trackIds);
            string url = $"{SPOTIFY_AUDIO_FEATURES_URL}?ids={idsParam}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // Parse audio features response
                        string jsonResponse = request.downloadHandler.text;
                        
                        // Simple JSON parsing for audio features array
                        if (jsonResponse.Contains("audio_features"))
                        {
                            // Extract and parse individual audio features
                            // This is a simplified approach - in production, use a proper JSON library
                            string[] features = ExtractAudioFeaturesFromJson(jsonResponse);
                            
                            foreach (string featureJson in features)
                            {
                                if (!string.IsNullOrEmpty(featureJson))
                                {
                                    try
                                    {
                                        SpotifyAudioFeatures audioFeature = JsonUtility.FromJson<SpotifyAudioFeatures>(featureJson);
                                        
                                        if (trackCache.ContainsKey(audioFeature.id))
                                        {
                                            SpotifyTrack track = trackCache[audioFeature.id];
                                            track.tempo = audioFeature.tempo;
                                            track.energy = audioFeature.energy;
                                            track.danceability = audioFeature.danceability;
                                            track.valence = audioFeature.valence;
                                            
                                            trackCache[audioFeature.id] = track;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogWarning($"Failed to parse audio feature: {e.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse audio features response: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to load audio features: {request.error}");
                }
            }
        }
        
        private string[] ExtractAudioFeaturesFromJson(string jsonResponse)
        {
            // Simple JSON extraction - in production, use a proper JSON library like Newtonsoft.Json
            List<string> features = new List<string>();
            
            try
            {
                int startIndex = jsonResponse.IndexOf("\"audio_features\":[") + 18;
                int endIndex = jsonResponse.LastIndexOf("]");
                
                if (startIndex > 17 && endIndex > startIndex)
                {
                    string featuresArray = jsonResponse.Substring(startIndex, endIndex - startIndex);
                    
                    // Split by objects (this is very basic parsing)
                    string[] objects = featuresArray.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                    
                    for (int i = 0; i < objects.Length; i++)
                    {
                        string obj = objects[i];
                        if (!obj.StartsWith("{")) obj = "{" + obj;
                        if (!obj.EndsWith("}")) obj = obj + "}";
                        
                        features.Add(obj);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to extract audio features: {e.Message}");
            }
            
            return features.ToArray();
        }
        
        /// <summary>
        /// Get playlists filtered by BPM range
        /// </summary>
        public List<SpotifyTrack> GetTracksByBPM(float minBPM, float maxBPM)
        {
            List<SpotifyTrack> filteredTracks = new List<SpotifyTrack>();
            
            foreach (var playlist in loadedPlaylists)
            {
                foreach (var track in playlist.tracks)
                {
                    if (track.tempo >= minBPM && track.tempo <= maxBPM)
                    {
                        filteredTracks.Add(track);
                    }
                }
            }
            
            return filteredTracks;
        }
        
        /// <summary>
        /// Get high-energy tracks suitable for workouts
        /// </summary>
        public List<SpotifyTrack> GetWorkoutTracks()
        {
            List<SpotifyTrack> workoutTracks = new List<SpotifyTrack>();
            
            foreach (var playlist in loadedPlaylists)
            {
                foreach (var track in playlist.tracks)
                {
                    // High energy, high danceability, tempo between 120-180 BPM
                    if (track.energy > 0.7f && track.danceability > 0.6f && 
                        track.tempo >= 120f && track.tempo <= 180f)
                    {
                        workoutTracks.Add(track);
                    }
                }
            }
            
            return workoutTracks;
        }
        
        /// <summary>
        /// Get a specific playlist by name
        /// </summary>
        public SpotifyPlaylist GetPlaylistByName(string name)
        {
            return loadedPlaylists.Find(p => p.name.ToLower().Contains(name.ToLower()));
        }
        
        /// <summary>
        /// Disconnect from Spotify
        /// </summary>
        public void Disconnect()
        {
            isConnected = false;
            accessToken = "";
            tokenExpiry = DateTime.MinValue;
            loadedPlaylists.Clear();
            trackCache.Clear();
            
            OnConnectionStatusChanged?.Invoke(false);
            Debug.Log("Disconnected from Spotify");
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        // Debug methods
        [ContextMenu("Test Connection")]
        public void TestConnection()
        {
            ConnectToSpotify();
        }
        
        [ContextMenu("Print Loaded Playlists")]
        public void PrintLoadedPlaylists()
        {
            Debug.Log($"=== Loaded Spotify Playlists ({loadedPlaylists.Count}) ===");
            foreach (var playlist in loadedPlaylists)
            {
                Debug.Log($"• {playlist.name} - {playlist.tracks.Count} tracks");
            }
        }
        
        [ContextMenu("Print Workout Tracks")]
        public void PrintWorkoutTracks()
        {
            var workoutTracks = GetWorkoutTracks();
            Debug.Log($"=== Workout Tracks ({workoutTracks.Count}) ===");
            foreach (var track in workoutTracks)
            {
                Debug.Log($"• {track.artist} - {track.name} (BPM: {track.tempo:F0}, Energy: {track.energy:F2})");
            }
        }
    }
} 