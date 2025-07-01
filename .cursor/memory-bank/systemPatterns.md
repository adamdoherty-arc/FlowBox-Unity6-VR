# System Patterns - VR Rhythm Boxing Game

## Architecture Overview

### Component-Based Design
The game follows Unity's Entity-Component-System (ECS) principles with clear separation of concerns:

```
GameManager (Singleton)
├── RhythmTargetSystem (Target spawning & management)
├── AdvancedAudioManager (Beat detection & analysis)
├── HandTrackingManager (VR input handling)
├── GameUI (User interface)
├── DynamicBackgroundSystem (Environmental effects)
└── VRPerformanceMonitor (Optimization)
```

### Data Flow Architecture
```
Audio Input → Beat Detection → Target Spawning → Hand Tracking → Hit Detection → Scoring → UI Update
```

## Core System Patterns

### 1. Singleton Pattern
**Used for**: System managers that need global access
```csharp
public static GameManager Instance { get; private set; }
public static AdvancedAudioManager Instance { get; private set; }
public static RhythmTargetSystem Instance { get; private set; }
```

**Benefits**: 
- Global access to critical systems
- Ensures single instance of managers
- Simplified system communication

### 2. Observer Pattern (Unity Events)
**Used for**: Decoupled system communication
```csharp
public UnityEvent<BeatData> OnBeatDetected;
public UnityEvent<CircleHitData> OnCircleHit;
public UnityEvent<BlockData> OnBlockSuccess;
```

**Benefits**:
- Loose coupling between systems
- Easy to add new listeners
- Clean event-driven architecture

### 3. Object Pool Pattern
**Used for**: Performance optimization of spawned objects
```csharp
ObjectPoolManager.Instance.SpawnObject("WhiteCircle", position, rotation);
ObjectPoolManager.Instance.ReturnObject(circleObject);
```

**Benefits**:
- Eliminates garbage collection spikes
- Consistent performance in VR
- Memory usage optimization

### 4. Job System Pattern (Unity 6)
**Used for**: Performance-critical calculations
```csharp
[BurstCompile]
public struct AudioAnalysisJob : IJob
{
    [ReadOnly] public NativeArray<float> sampleData;
    [WriteOnly] public NativeArray<float> frequencyData;
    
    public void Execute() { /* Burst-compiled analysis */ }
}
```

**Benefits**:
- Multi-threaded audio processing
- Burst compilation for maximum performance
- Non-blocking main thread

## Key System Interactions

### Audio → Gameplay Flow
1. **AdvancedAudioManager** analyzes audio stream
2. Detects beats and calculates BPM
3. **RhythmTargetSystem** receives beat events
4. Spawns circles with timing anticipation
5. **HandTrackingManager** detects punches
6. **GameUI** updates score and feedback

### Hand Tracking → Hit Detection
1. **HandTrackingManager** tracks hand positions/velocities
2. **EnhancedPunchDetector** analyzes punch patterns
3. Collision detection with circle targets
4. **RhythmTargetSystem** validates correct hand usage
5. **GameManager** calculates scoring

### Performance Monitoring Loop
1. **VRPerformanceMonitor** tracks frame rate/metrics
2. Detects performance drops
3. Automatically adjusts quality settings
4. **DynamicBackgroundSystem** reduces effects if needed
5. Maintains target frame rate

## Design Patterns Implementation

### State Machine Pattern
**GameManager** uses clear state management:
```csharp
public enum GameState { Menu, Playing, Paused, Finished }
```

### Factory Pattern
**CirclePrefabCreator** generates game objects:
```csharp
public void CreateCirclePrefabs()
{
    CreateWhiteCircle();
    CreateGrayCircle();
    CreateBlockPrefab();
}
```

### Strategy Pattern
**DynamicBackgroundSystem** switches environments:
```csharp
public void LoadTheme(BackgroundType type)
{
    // Different rendering strategies per theme
}
```

## Unity 6 Specific Patterns

### Render Graph Integration
- Custom render passes for VR optimization
- Dynamic quality scaling based on performance
- Efficient post-processing pipeline

### Native Collections Usage
- `NativeArray<T>` for Job System data
- Burst-compiled mathematical operations
- Memory-efficient audio processing

### XR Toolkit Integration
- Hand tracking with controller fallback
- Spatial UI positioning
- Comfort settings for VR

## Error Handling Patterns

### Graceful Degradation
```csharp
if (handTrackingAvailable)
    UseHandTracking();
else
    FallbackToControllers();
```

### Null Reference Protection
```csharp
audioManager?.SetMusicSource(audioSource);
OnBeatDetected?.Invoke(beatData);
```

### Performance Fallbacks
```csharp
if (frameRate < targetFrameRate)
    ReduceQualitySettings();
```

## Communication Patterns

### Event-Driven Communication
- Systems communicate through Unity Events
- Minimal direct references between components
- Easy to add/remove system dependencies

### Data-Oriented Design
- Structs for data (BeatData, CircleHitData, etc.)
- Clear data ownership and flow
- Efficient memory usage patterns

## Testing Patterns

### Component Testing
- Each system can be tested independently
- Mock data injection for unit tests
- VR-specific testing considerations

### Integration Testing
- Full audio → gameplay → UI flow validation
- Performance testing on target hardware
- Hand tracking accuracy validation

## Scalability Patterns

### Modular System Design
- Easy to add new background themes
- Pluggable audio sources (local/Spotify)
- Extensible difficulty systems

### Configuration-Driven Behavior
- Adjustable parameters for all systems
- Runtime quality adjustment
- User preference persistence

This architecture prioritizes VR performance, maintainability, and user experience while leveraging Unity 6's advanced features for optimal performance. 