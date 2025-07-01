# Technical Context - VR Rhythm Boxing Game

## Technology Stack

### Core Platform
- **Unity 6.0**: Latest LTS with enhanced VR support
- **C# 9.0**: Modern language features and performance
- **Universal Render Pipeline (URP)**: Optimized for VR rendering
- **XR Toolkit**: Unity's official VR framework

### VR Technologies
- **Meta Quest 2/3**: Primary target devices
- **OpenXR**: Cross-platform VR standard
- **Hand Tracking**: Meta's hand tracking SDK
- **Spatial Audio**: 3D positional audio system

### Advanced Unity 6 Features
- **Job System**: Multi-threaded performance optimization
- **Burst Compiler**: High-performance C# compilation
- **Native Collections**: Memory-efficient data structures
- **Render Graph**: Modern rendering pipeline

### External Integrations
- **Spotify Web API**: Music streaming integration (optional)
- **Unity Analytics**: Performance and usage tracking
- **Meta Platform SDK**: Quest-specific features

## Development Environment

### Required Software
```
Unity 6.0.x LTS
Visual Studio 2022 / JetBrains Rider
Meta Quest Developer Hub
Android SDK (for Quest builds)
```

### Unity Packages
```json
{
  "com.unity.burst": "1.8.12",
  "com.unity.collections": "2.2.1", 
  "com.unity.jobs": "0.70.0-preview.7",
  "com.unity.mathematics": "1.3.1",
  "com.unity.render-pipelines.universal": "16.0.6",
  "com.unity.xr.management": "4.4.1",
  "com.unity.xr.oculus": "4.2.0"
}
```

### Project Structure
```
Assets/
├── Scripts/
│   ├── Audio/           # Beat detection & music analysis
│   ├── Boxing/          # Target spawning & hit detection
│   ├── Core/            # Game management
│   ├── Environment/     # Background systems
│   ├── HandTracking/    # VR input handling
│   ├── Performance/     # Optimization systems
│   ├── Spotify/         # Music integration
│   └── UI/              # User interface
├── Materials/           # Rendering materials
├── Prefabs/            # Reusable game objects
├── Scenes/             # Game scenes
└── Audio/              # Music and sound effects
```

## Performance Constraints

### VR Requirements
- **Frame Rate**: 90 FPS (Quest 3), 72 FPS minimum (Quest 2)
- **Latency**: <20ms motion-to-photon for comfortable VR
- **Memory**: <3GB RAM usage (Quest 2 limitation)
- **CPU**: Maintain 60% usage to prevent thermal throttling

### Optimization Strategies
- **Object Pooling**: Prevent garbage collection spikes
- **LOD System**: Distance-based quality reduction
- **Occlusion Culling**: Hide non-visible objects
- **Texture Streaming**: Dynamic texture loading
- **Batch Rendering**: Minimize draw calls

### Performance Monitoring
```csharp
// Real-time performance tracking
VRPerformanceMonitor.Instance.CurrentMetrics
- frameRate: Current FPS
- frameTime: Frame duration in ms
- gpuTime: GPU processing time
- memoryUsage: Current RAM usage
- drawCalls: Rendering draw calls
```

## Technical Constraints

### Hardware Limitations
- **Quest 2**: Snapdragon XR2, 6GB RAM, 72/90Hz display
- **Quest 3**: Snapdragon XR2 Gen 2, 8GB RAM, 90/120Hz display
- **Hand Tracking**: Limited precision, requires good lighting
- **Controller Tracking**: 6DOF with occasional occlusion

### Unity 6 Considerations
- **Job System**: Requires NativeArray for data sharing
- **Burst Compilation**: Limited C# feature support
- **Memory Management**: Manual disposal of native collections
- **Threading**: Main thread UI updates only

### VR-Specific Constraints
- **Comfort**: No rapid camera movement or acceleration
- **Ergonomics**: UI positioned 2-3 meters from user
- **Accessibility**: Support for different play areas
- **Safety**: Guardian system integration

## Audio System Architecture

### Real-Time Analysis
```csharp
// Audio processing pipeline
AudioSource → FFT Analysis → Beat Detection → Target Spawning
```

### Beat Detection Algorithm
- **Frequency Analysis**: 8-band spectrum analysis
- **Energy Tracking**: Bass, mid, treble energy levels
- **BPM Calculation**: Tempo detection and tracking
- **Onset Detection**: Musical event identification

### Supported Audio Formats
- **Local Files**: WAV, MP3, OGG (Unity supported)
- **Streaming**: Spotify Web API (requires authentication)
- **Procedural**: Generated test tracks for development

## Networking & Data

### Local Storage
- **Player Preferences**: Unity PlayerPrefs system
- **Save Data**: JSON serialization for scores/settings
- **Asset Streaming**: Addressables for large assets

### External APIs
- **Spotify Web API**: OAuth 2.0 authentication required
- **Unity Analytics**: Automatic performance data collection
- **Meta Platform**: Quest-specific social features

## Build Configuration

### Quest 2/3 Build Settings
```
Platform: Android
Architecture: ARM64
Minimum API Level: 23 (Android 6.0)
Target API Level: 30 (Android 11)
Scripting Backend: IL2CPP
Api Compatibility: .NET Standard 2.1
```

### XR Settings
```
XR Plugin Management:
- Oculus XR Plugin: Enabled
- Initialize on Startup: True
- Render Mode: Multi Pass (Quest 2), Single Pass (Quest 3)
- Stereo Rendering: Single Pass Instanced
```

### Quality Settings
```
Quest 2 Profile:
- Texture Quality: Medium
- Shadow Quality: Hard Shadows Only  
- Anti Aliasing: 2x MSAA
- Render Scale: 1.0

Quest 3 Profile:
- Texture Quality: High
- Shadow Quality: Soft Shadows
- Anti Aliasing: 4x MSAA
- Render Scale: 1.2
```

## Development Workflow

### Version Control
- **Git**: Source control with LFS for large assets
- **Branching**: Feature branches with main/develop workflow
- **CI/CD**: Automated builds for Quest devices

### Testing Pipeline
1. **Editor Testing**: Initial development and debugging
2. **Link Testing**: Quest connected to PC for rapid iteration
3. **Standalone Testing**: APK builds on actual hardware
4. **Performance Profiling**: Frame rate and memory analysis

### Debugging Tools
- **Unity Profiler**: Performance analysis
- **OVR Metrics Tool**: Quest-specific performance data
- **ADB Logcat**: Android system logs
- **Visual Studio Debugger**: Code debugging

## Security & Privacy

### Data Collection
- **Anonymous Analytics**: Performance metrics only
- **No Personal Data**: No user identification stored
- **Local Storage**: All save data remains on device

### Spotify Integration
- **OAuth 2.0**: Secure authentication flow
- **Limited Scope**: Playlist read-only access
- **Token Management**: Automatic refresh and expiry

This technical foundation supports a high-performance VR rhythm game while maintaining compatibility across target devices and providing room for future expansion. 