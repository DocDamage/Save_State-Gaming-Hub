# CinematicCameraSystem Refactoring Plan

## Overview

**File:** `src/SaveState.Application/Mugen/Services/CinematicCameraSystem.cs`  
**Current Lines:** 1,057  
**Current Methods:** 10 public + 6 private + 3 nested classes  
**Target:** Split into Manager Pattern following project conventions

---

## Current Structure Analysis

### Service Breakdown

```
CinematicCameraSystem (Coordinator - 482 lines)
├── CameraController (nested class - 30 lines)
├── SequenceDirector (nested class - 16 lines)
├── CameraRigSystem (nested class - 20 lines)
├── Sequence Validation Methods (2 private methods)
├── Performance Analysis Methods (4 private methods)
├── Default Preset Initialization
├── Data Models (~500 lines of DTOs/Enums)
└── Interface Definition (~12 lines)
```

### Responsibility Areas Identified

1. **Camera Sequence Management** - Create, store, validate sequences
2. **Camera Preset Management** - Create, apply, store presets
3. **Camera Path Management** - Create camera paths with waypoints
4. **Cinematic Event Management** - Trigger-based events
5. **Transition Management** - Camera transitions, easing functions
6. **Camera Rig Management** - Physical rig setup, constraints
7. **Sequence Execution** - Orchestrate sequence playback
8. **Camera Control** - Low-level camera state management
9. **Performance Analysis** - Sequence analytics, recommendations

---

## Proposed Manager Structure

### After Refactoring

```
CinematicCameraSystem (Coordinator - ~140 lines)
├── ICinematicCameraSystem (interface - split to separate file)
├── CameraSequenceManager (Sequence CRUD, validation)
├── CameraPresetManager (Preset management, categories)
├── CameraPathManager (Path creation, waypoints, interpolation)
├── CinematicEventManager (Event creation, triggers)
├── CameraTransitionManager (Transitions, easing)
├── CameraRigManager (CameraRigSystem → Manager)
├── SequenceExecutionManager (SequenceDirector → Manager)
├── CameraControllerManager (CameraController → Manager)
└── SequenceAnalyticsManager (Performance analysis)
```

### Manager Classes

#### 1. CameraSequenceManager
**Responsibilities:**
- Create and manage camera sequences
- Sequence validation (timing, continuity)
- Store sequences in memory
- Sequence priority and loop settings

**Methods:**
```csharp
public Task<CameraSequence> CreateSequenceAsync(CameraSequenceRequest request, CancellationToken ct)
public Task<Result> ValidateSequenceAsync(CameraSequence sequence, CancellationToken ct)
public Task<CameraSequence?> GetSequenceAsync(string sequenceId, CancellationToken ct)
public Task UpdateSequenceAsync(CameraSequence sequence, CancellationToken ct)
public Task DeleteSequenceAsync(string sequenceId, CancellationToken ct)
public Task<IReadOnlyList<CameraSequence>> GetSequencesAsync(CancellationToken ct)
```

**Estimated Lines:** ~140

---

#### 2. CameraPresetManager
**Responsibilities:**
- Create and manage camera presets
- Store presets in memory dictionary
- Category-based preset organization
- Default preset initialization (Dramatic Close-Up, Wide Arena, Bullet Time, Dutch Angle)

**Methods:**
```csharp
public Task<CameraPreset> CreatePresetAsync(CameraPresetRequest request, CancellationToken ct)
public Task<CameraPreset?> GetPresetAsync(string presetId, CancellationToken ct)
public Task<IReadOnlyList<CameraPreset>> GetPresetsAsync(CameraCategory? category, CancellationToken ct)
public Task UpdatePresetAsync(CameraPreset preset, CancellationToken ct)
public Task DeletePresetAsync(string presetId, CancellationToken ct)
public Task InitializeDefaultPresetsAsync(CancellationToken ct)
```

**Estimated Lines:** ~150

---

#### 3. CameraPathManager
**Responsibilities:**
- Create camera paths with waypoints
- Path validation (minimum waypoints)
- Interpolation modes (Linear, Bezier, CatmullRom, Hermite)
- Speed curves
- Look-at target management

**Methods:**
```csharp
public Task<CameraPath> CreatePathAsync(CameraPathRequest request, CancellationToken ct)
public Task<Result> ValidatePathAsync(CameraPath path, CancellationToken ct)
public Task<CameraPath?> GetPathAsync(string pathId, CancellationToken ct)
public Task<Vector3> InterpolatePositionAsync(CameraPath path, float t, CancellationToken ct)
public Task<IReadOnlyList<CameraWaypoint>> GenerateSmoothWaypointsAsync(IReadOnlyList<CameraWaypoint> waypoints, CancellationToken ct)
```

**Estimated Lines:** ~130

---

#### 4. CinematicEventManager
**Responsibilities:**
- Create cinematic events
- Trigger condition management
- Event priority handling
- One-time vs repeatable events
- Audio cue management

**Methods:**
```csharp
public Task<CinematicEvent> CreateEventAsync(CinematicEventRequest request, CancellationToken ct)
public Task<CinematicEvent?> GetEventAsync(string eventId, CancellationToken ct)
public Task<bool> CheckTriggerConditionAsync(TriggerCondition condition, CameraContext context, CancellationToken ct)
public Task<IReadOnlyList<CinematicEvent>> GetActiveEventsAsync(CancellationToken ct)
public Task MarkEventExecutedAsync(string eventId, CancellationToken ct)
```

**Estimated Lines:** ~120

---

#### 5. CameraTransitionManager
**Responsibilities:**
- Create camera transitions
- Transition types (Cut, Fade, Wipe, Zoom, Pan, Custom)
- Easing functions (Linear, EaseIn, EaseOut, EaseInOut, Bounce, Elastic)
- Transition parameters
- Transition duration management

**Methods:**
```csharp
public Task<CameraTransition> CreateTransitionAsync(CameraTransitionRequest request, CancellationToken ct)
public Task<float> ApplyEasingAsync(float t, EasingFunction easing, CancellationToken ct)
public Task ExecuteTransitionAsync(CameraTransition transition, CameraState from, CameraState to, CancellationToken ct)
public IReadOnlyList<EasingFunction> GetAvailableEasingFunctions()
public IReadOnlyList<TransitionType> GetAvailableTransitionTypes()
```

**Estimated Lines:** ~110

---

#### 6. CameraRigManager
**Responsibilities:**
- Camera rig setup (Dolly, Crane, Jib, SteadyCam, Handheld)
- Rig constraints (Distance, Angle, Height, Speed)
- Multi-camera rig management
- Rig automation settings

**Methods:**
```csharp
public Task<CameraRig> SetupRigAsync(CameraRigRequest request, CancellationToken ct)
public Task<CameraRig?> GetRigAsync(string rigId, CancellationToken ct)
public Task ApplyConstraintAsync(CameraRig rig, RigConstraint constraint, CancellationToken ct)
public Task<IReadOnlyList<CameraPosition>> CalculateRigPositionsAsync(CameraRig rig, CancellationToken ct)
public Task UpdateAutomationSettingsAsync(string rigId, RigAutomationSettings settings, CancellationToken ct)
```

**Estimated Lines:** ~120

---

#### 7. SequenceExecutionManager
**Responsibilities:**
- Execute camera sequences
- Trigger evaluation
- Audio sync
- Transition orchestration
- Sequence interruption/resumption

**Methods:**
```csharp
public Task ExecuteSequenceAsync(CameraSequence sequence, CameraContext context, CancellationToken ct)
public Task ExecuteSequenceAsync(string sequenceId, CameraContext context, CancellationToken ct)
public Task PauseExecutionAsync(string executionId, CancellationToken ct)
public Task ResumeExecutionAsync(string executionId, CancellationToken ct)
public Task StopExecutionAsync(string executionId, CancellationToken ct)
public Task<SequenceExecutionState> GetExecutionStateAsync(string executionId, CancellationToken ct)
```

**Estimated Lines:** ~140

---

#### 8. CameraControllerManager
**Responsibilities:**
- Low-level camera state management
- Apply presets to camera
- Current state retrieval
- Camera interpolation
- Field of view management

**Methods:**
```csharp
public Task ApplyPresetAsync(CameraPreset preset, CameraContext context, CancellationToken ct)
public Task<CameraState> GetCurrentStateAsync(CameraContext context, CancellationToken ct)
public Task SetCameraPositionAsync(Vector3 position, CancellationToken ct)
public Task SetCameraTargetAsync(Vector3 target, CancellationToken ct)
public Task SetFieldOfViewAsync(float fov, CancellationToken ct)
public Task<CameraState> InterpolateStateAsync(CameraState from, CameraState to, float t, CancellationToken ct)
```

**Estimated Lines:** ~110

---

#### 9. SequenceAnalyticsManager
**Responsibilities:**
- Sequence performance analysis
- Trigger efficiency calculation
- Camera stability metrics
- Performance score calculation
- Recommendations generation

**Methods:**
```csharp
public Task<SequenceAnalytics> AnalyzeSequenceAsync(string sequenceId, CancellationToken ct)
public Task<SequenceAnalytics> AnalyzeSequenceAsync(CameraSequence sequence, CancellationToken ct)
public double CalculateTriggerEfficiency(CameraSequence sequence)
public double CalculateCameraStability(CameraSequence sequence)
public double CalculatePerformanceScore(CameraSequence sequence)
public IReadOnlyList<string> GenerateRecommendations(CameraSequence sequence)
```

**Estimated Lines:** ~140

---

## Before/After Code Structure

### Before (Current)

```csharp
// CinematicCameraSystem.cs - 1,057 lines
public class CinematicCameraSystem : ICinematicCameraSystem
{
    private readonly Dictionary<string, CameraSequence> _cameraSequences = new();
    private readonly Dictionary<string, CameraPreset> _cameraPresets = new();
    private readonly CameraController _cameraController;
    private readonly SequenceDirector _sequenceDirector;
    private readonly CameraRigSystem _cameraRigSystem;
    
    public async Task<Result<CameraSequence>> CreateCameraSequenceAsync(CameraSequenceRequest request, CancellationToken ct = default)
    {
        // Sequence creation with validation
        var sequence = new CameraSequence { ... };
        var validation = await ValidateSequenceAsync(sequence, ct);
        if (!validation.IsSuccess) { ... }
        _cameraSequences[sequence.SequenceId] = sequence;
        return Result.Success(sequence);
    }
    
    public async Task<Result> ExecuteCameraSequenceAsync(string sequenceId, CameraContext context, CancellationToken ct = default)
    {
        // Delegates to _sequenceDirector
        await _sequenceDirector.ExecuteSequenceAsync(sequence, context, ct);
    }
    
    // 8 more public methods...
    // 6 private analysis/validation methods...
    // 3 nested classes (CameraController, SequenceDirector, CameraRigSystem)...
    // 45+ data model classes and enums...
}
```

### After (Target)

```csharp
// CinematicCameraSystem.cs - ~140 lines
public class CinematicCameraSystem : ICinematicCameraSystem
{
    private readonly CameraSequenceManager _sequenceManager;
    private readonly CameraPresetManager _presetManager;
    private readonly CameraPathManager _pathManager;
    private readonly CinematicEventManager _eventManager;
    private readonly CameraTransitionManager _transitionManager;
    private readonly CameraRigManager _rigManager;
    private readonly SequenceExecutionManager _executionManager;
    private readonly CameraControllerManager _controllerManager;
    private readonly SequenceAnalyticsManager _analyticsManager;
    
    public CinematicCameraSystem(
        CameraSequenceManager sequenceManager,
        CameraPresetManager presetManager,
        CameraPathManager pathManager,
        CinematicEventManager eventManager,
        CameraTransitionManager transitionManager,
        CameraRigManager rigManager,
        SequenceExecutionManager executionManager,
        CameraControllerManager controllerManager,
        SequenceAnalyticsManager analyticsManager)
    {
        _sequenceManager = sequenceManager;
        _presetManager = presetManager;
        _pathManager = pathManager;
        _eventManager = eventManager;
        _transitionManager = transitionManager;
        _rigManager = rigManager;
        _executionManager = executionManager;
        _controllerManager = controllerManager;
        _analyticsManager = analyticsManager;
    }
    
    public Task<Result<CameraSequence>> CreateCameraSequenceAsync(CameraSequenceRequest request, CancellationToken ct = default)
        => _sequenceManager.CreateSequenceAsync(request, ct);
    
    public Task<Result> ExecuteCameraSequenceAsync(string sequenceId, CameraContext context, CancellationToken ct = default)
        => _executionManager.ExecuteSequenceAsync(sequenceId, context, ct);
    
    // Other methods delegate to appropriate managers...
}

// Managers/CameraSequenceManager.cs - ~140 lines
public class CameraSequenceManager
{
    private readonly ILogger<CameraSequenceManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, CameraSequence> _sequences = new();
    
    public async Task<CameraSequence> CreateSequenceAsync(CameraSequenceRequest request, CancellationToken ct)
    {
        // Full implementation with validation
    }
    
    public async Task<Result> ValidateSequenceAsync(CameraSequence sequence, CancellationToken ct)
    {
        // Validation logic
    }
}

// Similar for other managers...
```

---

## Data Model Restructuring

### New File Structure

```
CinematicCamera/
├── Services/
│   ├── CinematicCameraSystem.cs (coordinator)
│   ├── CameraSequenceManager.cs
│   ├── CameraPresetManager.cs
│   ├── CameraPathManager.cs
│   ├── CinematicEventManager.cs
│   ├── CameraTransitionManager.cs
│   ├── CameraRigManager.cs
│   ├── SequenceExecutionManager.cs
│   ├── CameraControllerManager.cs
│   └── SequenceAnalyticsManager.cs
├── Models/
│   ├── CameraSequence.cs
│   ├── CameraPreset.cs
│   ├── CameraSettings.cs
│   ├── CameraPath.cs
│   ├── CameraWaypoint.cs
│   ├── CinematicEvent.cs
│   ├── CameraTransition.cs
│   ├── CameraMovement.cs
│   ├── SequenceTrigger.cs
│   ├── AudioSyncPoint.cs
│   ├── CameraRig.cs
│   ├── CameraState.cs
│   ├── CameraContext.cs
│   ├── SequenceAnalytics.cs
│   ├── TriggerCondition.cs
│   ├── CameraAudioCue.cs
│   ├── CameraVisualEffect.cs
│   ├── RigSettings.cs
│   ├── PostProcessingSettings.cs
│   ├── CameraPosition.cs
│   ├── RigConstraint.cs
│   └── RigAutomationSettings.cs
├── Enums/
│   ├── CameraCategory.cs
│   ├── TransitionType.cs
│   ├── EasingFunction.cs
│   ├── InterpolationMode.cs
│   ├── CameraTriggerType.cs
│   ├── ProjectionMode.cs
│   ├── RigType.cs
│   └── ConstraintType.cs
└── Interfaces/
    └── ICinematicCameraSystem.cs
```

---

## Edge Cases and Challenges

### 1. Sequence Validation Dependencies
**Challenge:** `ValidateSequenceAsync` needs to check movement timing against sequence duration.

**Solution:**
- Keep validation in `CameraSequenceManager`
- Pass full `CameraSequence` object for validation
- No external dependencies needed

### 2. Sequence Execution State
**Challenge:** `ExecuteCameraSequenceAsync` needs access to sequence storage and director.

**Solution:**
- `SequenceExecutionManager` receives `CameraSequenceManager` via DI
- Or coordinator passes sequence object to execution manager
- Prefer passing object to avoid circular dependencies

### 3. Camera Context Sharing
**Challenge:** `CameraContext` is used across multiple managers.

**Solution:**
- Keep `CameraContext` as shared model
- Pass as parameter to methods that need it
- Contains: ContextId, Player positions, CurrentAction, Timestamp

### 4. Default Preset Initialization
**Challenge:** 4 default presets (Dramatic Close-Up, Wide Arena, Bullet Time, Dutch Angle) with complex settings.

**Solution:**
- Move to `CameraPresetManager.InitializeDefaultPresetsAsync()`
- Call from coordinator constructor or app startup
- Each preset with full `CameraSettings` configuration

### 5. Analytics Dependencies
**Challenge:** `AnalyzeSequencePerformanceAsync` needs sequence data and performs multiple calculations.

**Solution:**
- `SequenceAnalyticsManager` receives sequence from caller (coordinator)
- Coordinator retrieves sequence from `CameraSequenceManager`
- Passes to analytics manager for analysis

### 6. Trigger Evaluation
**Challenge:** Triggers can be game events, health thresholds, combo counts, etc.

**Solution:**
- `CinematicEventManager` owns trigger evaluation
- `CameraContext` provides game state for evaluation
- Extensible trigger condition system

---

## Implementation Steps

### Phase 1: Extract Data Models
1. Create `Models/` directory structure
2. Move all DTO classes to appropriate files
3. Move all enums to `Enums/` directory
4. Update using statements

### Phase 2: Create Managers
1. Create `CameraSequenceManager`
2. Create `CameraPresetManager` with default presets
3. Create `CameraPathManager`
4. Create `CinematicEventManager`
5. Create `CameraTransitionManager`
6. Create `CameraRigManager` from `CameraRigSystem`
7. Create `SequenceExecutionManager` from `SequenceDirector`
8. Create `CameraControllerManager` from `CameraController`
9. Create `SequenceAnalyticsManager` (from private methods)

### Phase 3: Refactor Coordinator
1. Update `CinematicCameraSystem` to use managers
2. Remove nested classes
3. Update constructor to accept managers via DI
4. Simplify methods to delegate calls

### Phase 4: Update DI Registration
1. Register all managers in DI container
2. Update service registration
3. Ensure proper lifetime scopes

### Phase 5: Testing
1. Update unit tests for camera operations
2. Test each manager independently
3. Test sequence execution flow
4. Verify preset application

---

## Statistics Summary

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Files | 1 | 11 | +10 |
| Lines per file | 1,057 | ~140 avg | -87% |
| Classes per file | 4 | 1 | Clean separation |
| Public methods per class | 10 | ~2-4 | Focused |
| Private methods | 6 | 0 (moved to managers) | Organized |
| Testability | Low | High | Isolated units |

---

## References

- [AGENTS.md](../../../AGENTS.md) - Manager Pattern guidelines
- [Interface Segregation ADR](../../../docs/architecture/adrs/) - Interface splitting patterns
- Existing Manager implementations: `IkemenGoService`, `CharacterDiscoveryService`
