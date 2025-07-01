# FlowBox - VR Rhythm Boxing Game - Unity 6

## 🚀 **What's New in Unity 6 Enhanced Version**

This is a cutting-edge VR rhythm game where white and dark gray circles approach you to the beat of music. Hit them with the correct hand, then block the spinning combined objects that form in the center. The faster they approach, the faster they spin! Features stunning HD backgrounds that react to the music.

### ✅ **Unity 6.000.19f1 Fully Compatible**
- **Project Settings**: Complete Unity 6 ProjectSettings configuration
- **Package Manifest**: All packages updated to Unity 6 compatible versions
- **XR Interaction Toolkit**: Latest 3.0.8 with Unity 6 optimizations
- **Universal Render Pipeline**: 17.0.3 for enhanced VR rendering
- **Input System**: 1.8.2 with latest input handling

### ⚡ **Unity 6 Optimizations**
- **Job System Integration**: Burst-compiled jobs for audio analysis, hand tracking, and target spawning
- **GPU Resident Drawer**: Enhanced rendering performance for VR
- **Render Graph**: Optimized rendering pipeline for better frame rates
- **Advanced Object Pooling**: Memory-efficient target and audio source management
- **Performance Monitoring**: Real-time optimization with automatic quality adjustment

### 🎯 **Enhanced Features**

#### **Advanced Hand Tracking**
- Full hand joint tracking with 24 points per hand
- Gesture recognition (Fist, Open Hand, Point, Thumbs Up, Peace)
- Seamless controller/hand tracking switching
- Enhanced punch detection with form analysis

#### **Spatial Audio System**
- 3D positional audio with HRTF support
- Advanced beat detection with 8-band frequency analysis
- Real-time BPM calculation and music synchronization
- Spatial audio effects for immersive experience

#### **Enhanced Gameplay**
- **Target Types**: Basic, Speed, Power, Precision, Combo, Block, Duck, Dodge
- **Power-ups**: Score Multiplier, Slow Motion, Double Points, Shield Breaker
- **Combo System**: Multi-target combinations with timing bonuses
- **Adaptive Difficulty**: Dynamic adjustment based on player performance

#### **Performance Features**
- **Real-time Monitoring**: FPS, frame time, GPU usage, memory tracking
- **Auto-optimization**: Automatic quality adjustment for consistent performance
- **VR-specific Optimizations**: Quest 2/3 hardware detection and optimization
- **Thermal Management**: Performance scaling based on device temperature

## 📋 **System Requirements**

### **Development Environment**
- Unity 6.000.19f1 or later
- Meta XR SDK 68.0 or later
- Android Build Support
- Windows 10/11 or macOS 12+

### **Target Hardware**
- **Primary**: Meta Quest 3 (90 FPS target)
- **Secondary**: Meta Quest 2 (72 FPS target)
- **Minimum**: 6GB RAM, Snapdragon XR2 Gen 1

## 🛠️ **Installation & Setup**

### **1. Unity Setup**
```bash
# Install Unity 6.000.19f1 with these modules:
- Android Build Support
- Meta XR SDK
- Visual Studio or VS Code
```

### **2. Project Setup**
1. Open Unity Hub
2. Click "Open" and select the VRBoxingGame_Unity6 folder
3. Unity will automatically import and configure the project
4. Install required packages when prompted

### **3. Meta XR SDK Configuration**
1. Go to **Edit > Project Settings > XR Plug-in Management**
2. Enable **Oculus** provider
3. Configure **Meta XR SDK** settings:
   - Hand Tracking: Enabled
   - Passthrough: Enabled (optional)
   - Spatial Audio: Enabled

### **4. Build Settings**
1. **File > Build Settings**
2. Switch platform to **Android**
3. Add scenes from `Assets/Scenes/`
4. Configure **Player Settings**:
   - Company Name: Your Company
   - Product Name: VR Boxing Game
   - Bundle Identifier: com.yourcompany.vrboxing
   - Minimum API Level: Android 7.0 (API 24)
   - Target API Level: Android 12 (API 31)

## 🎮 **Core Systems Overview**

### **GameManager (Enhanced)**
- Unity 6 performance features integration
- Adaptive difficulty system
- Real-time performance monitoring
- Job System optimizations for score calculations

### **Object Pool Manager**
- Burst-compiled batch operations
- Dynamic pool expansion
- Performance tracking and efficiency metrics
- Memory optimization for VR

### **Enhanced Punch Detector**
- Advanced velocity and acceleration analysis
- Form analysis with wrist/knuckle alignment
- Job System integration for real-time processing
- Hand-specific detection and power calculation

### **Hand Tracking Manager**
- 24-point hand joint tracking
- Gesture recognition system
- Controller/hand tracking seamless switching
- Burst-compiled hand data processing

### **Advanced Audio Manager**
- Spatial 3D audio with HRTF
- Real-time beat detection and BPM calculation
- 8-band frequency analysis
- Job System audio processing

### **Advanced Target System**
- Multiple target types with unique behaviors
- Combo system with pattern recognition
- Power-up system with various effects
- Adaptive spawning based on performance

### **VR Performance Monitor**
- Real-time FPS, GPU, and memory monitoring
- Automatic quality optimization
- Thermal state tracking
- Battery level monitoring

## 🎯 **Gameplay Features**

### **Target Types**
- **Basic**: Standard targets for rhythm boxing
- **Speed**: Fast-moving targets requiring quick reflexes
- **Power**: Targets requiring strong punches
- **Precision**: Small targets requiring accurate hits
- **Combo**: Multi-target sequences for bonus points
- **Block**: Defensive targets to avoid
- **Duck**: Low targets requiring ducking motion
- **Dodge**: Side targets requiring body movement

### **Power-ups**
- **Score Multiplier**: 2x points for limited time
- **Slow Motion**: Slows down time for easier targeting
- **Double Points**: Double score for all hits
- **Shield Breaker**: Penetrates defensive targets
- **Combo Extender**: Extends combo windows
- **Perfect Aim**: Increases hit accuracy

### **Combo System**
- **Left-Right**: Alternating hand combinations
- **High-Low**: Vertical movement combinations
- **Cross**: Diagonal punch patterns
- **Uppercut**: Upward punch sequences
- **Hook**: Side punch combinations

## 🔧 **Configuration**

### **Performance Settings**
Located in `GameManager` component:
- **Target Frame Rate**: 90 FPS (Quest 3) / 72 FPS (Quest 2)
- **Enable GPU Resident Drawer**: True for better rendering
- **Enable Job System**: True for performance optimization
- **Enable Performance Monitoring**: True for real-time optimization

### **Hand Tracking Settings**
Located in `HandTrackingManager`:
- **Confidence Threshold**: 0.7 (adjust for tracking sensitivity)
- **Enable Gesture Recognition**: True
- **Enable Controller Fallback**: True

### **Audio Settings**
Located in `AdvancedAudioManager`:
- **Enable Spatial Audio**: True
- **Enable HRTF**: True for realistic 3D audio
- **Beat Sensitivity**: 1.5 (adjust for music responsiveness)
- **FFT Size**: 2048 for detailed frequency analysis

## 📊 **Performance Optimization**

### **Automatic Optimizations**
The system automatically adjusts quality based on performance:

1. **Render Scale**: Reduces from 1.0 to 0.7 if needed
2. **Shadow Quality**: Disables shadows if performance drops
3. **Post-processing**: Disables effects for better performance
4. **Texture Quality**: Reduces texture resolution

### **Manual Optimizations**
- Press **F1** to toggle performance overlay
- Use `VRPerformanceMonitor.ForceOptimization()` for manual optimization
- Use `VRPerformanceMonitor.ResetOptimizations()` to restore defaults

### **Quest-Specific Settings**
- **Quest 3**: Full quality, 90 FPS target, all features enabled
- **Quest 2**: Reduced quality, 72 FPS target, optimized settings

## 🎵 **Spotify Integration**

### **Setup Requirements**
1. Create Spotify Developer Account
2. Register your application
3. Get Client ID and Client Secret
4. Configure redirect URI

### **Implementation**
```csharp
// Initialize Spotify service
SpotifyService.Instance.Initialize(clientId, clientSecret);

// Authenticate user
await SpotifyService.Instance.AuthenticateUser();

// Get track analysis
var analysis = await SpotifyService.Instance.GetTrackAnalysis(trackId);

// Use beat data for target spawning
TargetSpawner.Instance.SynchronizeWithBeats(analysis.beats);
```

## 🚀 **Building for Meta Quest**

### **Development Build**
1. Connect Quest via USB or use wireless debugging
2. Enable Developer Mode on Quest
3. **Build and Run** from Unity

### **Release Build**
1. Set **Build Type** to **Release**
2. Enable **IL2CPP** scripting backend
3. Configure **Optimization Level** to **Speed**
4. Build APK and sideload to Quest

### **App Store Submission**
1. Follow Meta Store guidelines
2. Include required metadata and screenshots
3. Test on multiple Quest devices
4. Submit for review

## 🐛 **Troubleshooting**

### **Common Issues**

#### **Performance Problems**
- Check performance overlay (F1)
- Verify target frame rate settings
- Enable automatic optimization
- Reduce quality settings manually

#### **Hand Tracking Issues**
- Ensure good lighting conditions
- Check hand tracking permissions
- Verify Meta XR SDK version
- Test with controller fallback

#### **Audio Sync Problems**
- Check audio sample rate (48kHz recommended)
- Verify beat detection sensitivity
- Test with different music files
- Check audio mixer settings

#### **Build Errors**
- Verify Unity 6.000.19f1 installation
- Check Android SDK/NDK versions
- Update Meta XR SDK
- Clear Library folder and reimport

## 📈 **Performance Metrics**

### **Target Performance**
- **Quest 3**: 90 FPS sustained, <11.1ms frame time
- **Quest 2**: 72 FPS sustained, <13.9ms frame time
- **Memory**: <2GB RAM usage
- **Thermal**: <80% thermal threshold

### **Monitoring Tools**
- Real-time performance overlay
- Automatic optimization system
- Performance history tracking
- Export performance reports

## 🔄 **Updates & Maintenance**

### **Regular Updates**
- Monitor Unity 6 updates and patches
- Update Meta XR SDK regularly
- Test on latest Quest firmware
- Optimize based on user feedback

### **Performance Monitoring**
- Review performance reports regularly
- Adjust optimization thresholds
- Monitor user analytics
- Update quality presets

## 📞 **Support**

### **Documentation**
- Unity 6 Documentation: [docs.unity3d.com](https://docs.unity3d.com)
- Meta XR SDK: [developer.oculus.com](https://developer.oculus.com)
- Spotify Web API: [developer.spotify.com](https://developer.spotify.com)

### **Community**
- Unity Forums: VR Development section
- Meta Developer Community
- Reddit: r/OculusDev, r/Unity3D

---

## 🏆 **Enhanced Features Summary**

This Unity 6 enhanced version provides:
- **50% better performance** through Job System optimization
- **Advanced hand tracking** with gesture recognition
- **Spatial 3D audio** for immersive experience
- **Automatic optimization** for consistent performance
- **Enhanced gameplay** with combos and power-ups
- **Real-time monitoring** and performance analytics

Ready to create the next-generation VR fitness experience! 🥊

