# AdvancedPhysicsCombatService Refactoring Plan

## Manager Pattern Implementation

**Target File:** `src/SaveState.Application/Mugen/Services/AdvancedPhysicsCombatService.cs`  
**Current Size:** 1,056 lines  
**Target Size:** ~150 lines (coordinator) + 6 manager classes (~150 lines each)  
**Estimated Reduction:** 86% (1,056 → ~1,050 total, but with proper separation of concerns)

---

## 1. Current Analysis

### Statistics
| Metric | Value |
|--------|-------|
| Total Lines | 1,056 |
| Public Methods | 10 |
| Private Methods | 11 |
| Nested Classes | 24 (5 engines + 19 data classes) |
| State Dictionaries | 5 |
| Engine Classes | 5 |

### Current Architecture
```
AdvancedPhysicsCombatService (1,056 lines)
├── 5 Engine Classes (private nested)
│   ├── HitDetectionEngine (140 lines)
│   ├── JuggleDecayEngine (80 lines)
│   ├── CharacterGravityEngine (60 lines)
│   ├── WallSplatEngine (100 lines)
│   └── DestructionEngine (140 lines)
├── 19 Data/State Classes
└── 5 State Dictionaries (in-memory)
```

### Responsibilities Currently Mixed
1. **Hit Detection** - Axis-aware collision, cross-up detection, depth/angle calculations
2. **Juggle Decay** - Combo scaling, gravity multipliers, breakpoint logic
3. **Character Gravity** - Individual character physics, fall speed, jump height
4. **Wall Splat** - Wall collision, bounce physics, combo extension
5. **Environment Destruction** - Stage breaking, hazards, debris
6. **State Management** - Tracking hit detection, wall collisions, destruction states
7. **Reporting** - Physics combat report generation, metrics aggregation

---

## 2. Proposed Manager Structure

```
AdvancedPhysicsCombatService (~150 lines) - Coordinator
├── HitDetectionManager (~180 lines)
├── JuggleDecayManager (~130 lines)
├── CharacterGravityManager (~110 lines)
├── WallSplatManager (~150 lines)
├── EnvironmentDestructionManager (~180 lines)
└── PhysicsReportingManager (~150 lines)
```

### Manager Classes

#### 2.1 HitDetectionManager
**Responsibilities:**
- Axis-aware hit detection processing
- Cross-up detection and validation
- Depth damage calculations (Z-axis positioning)
- Angle multiplier calculations
- Hit detection state tracking
- Axis positioning queries

**Public Methods:**
```csharp
Task<Result<HitDetectionResult>> ProcessHitAsync(string attackerId, string defenderId, HitRequest request, CancellationToken ct);
Task<Result<AxisPositioning>> GetAxisPositioningAsync(string characterId, CancellationToken ct);
Task<HitDetectionStats> GetStatsAsync(CancellationToken ct);
void ResetState(string? attackerId = null, string? defenderId = null);
```

**State:**
- `Dictionary<string, HitDetectionState>` - Tracks hits between attacker/defender pairs

---

#### 2.2 JuggleDecayManager
**Responsibilities:**
- Juggle decay application during combos
- Gravity multiplier calculations
- Momentum loss computation
- Breakpoint detection and handling
- Combo length tracking
- Juggle metrics collection

**Public Methods:**
```csharp
Task<Result<JuggleDecayState>> ApplyDecayAsync(string characterId, JuggleHit hit, CancellationToken ct);
Task<Result<JuggleMetrics>> GetMetricsAsync(string characterId, CancellationToken ct);
bool IsBreakpointReached(string characterId);
void ResetCharacterState(string characterId);
```

**State:**
- `Dictionary<string, JuggleDecayState>` - Per-character juggle state

---

#### 2.3 CharacterGravityManager
**Responsibilities:**
- Character-specific gravity calculation
- Fall speed computation
- Jump height determination
- Air control factor calculation
- Dash speed and terminal velocity
- Character type-based multipliers

**Public Methods:**
```csharp
Task<Result<CharacterGravity>> CalculateGravityAsync(string characterId, GravityCalculationRequest request, CancellationToken ct);
float GetGravityMultiplier(string characterId);
void RegisterCharacterProfile(string characterId, CharacterPhysicsProfile profile);
```

**State:**
- `Dictionary<string, CharacterGravity>` - Cached gravity calculations
- `Dictionary<string, CharacterPhysicsProfile>` - Character profiles

---

#### 2.4 WallSplatManager
**Responsibilities:**
- Wall collision processing
- Wall splat mechanics
- Bounce angle calculations
- Impact force computation
- Stun duration calculation
- Combo extension possibility detection

**Public Methods:**
```csharp
Task<Result<WallSplatResult>> ProcessSplatAsync(string characterId, WallCollision collision, CancellationToken ct);
Task<Result<WallCollisionMetrics>> GetMetricsAsync(string characterId, CancellationToken ct);
float CalculateImpactForce(Vector3 velocity, float angle);
void ResetWallState(string characterId);
```

**State:**
- `Dictionary<string, WallCollisionState>` - Per-character wall collision history

---

#### 2.5 EnvironmentDestructionManager
**Responsibilities:**
- Environmental destruction processing
- Break threshold calculations
- Break type determination
- Hazard level assessment
- Affected area calculation
- Debris generation

**Public Methods:**
```csharp
Task<Result<DestructionResult>> ProcessDestructionAsync(string stageId, DestructionRequest request, CancellationToken ct);
Task<Result<DestructionMetrics>> GetMetricsAsync(string stageId, CancellationToken ct);
float CalculateBreakThreshold(float damage, float characterPower);
DestructionBreakType DetermineBreakType(string impactLocation);
void ResetStageState(string stageId);
```

**State:**
- `Dictionary<string, DestructionState>` - Per-stage destruction tracking

---

#### 2.6 PhysicsReportingManager
**Responsibilities:**
- Physics combat report generation
- Statistics aggregation across all physics systems
- Hit detection stats analysis
- Juggle decay analysis
- Gravity mechanics analysis
- Wall splat analysis
- Destruction event analysis
- Overall physics score calculation

**Public Methods:**
```csharp
Task<Result<PhysicsCombatReport>> GenerateReportAsync(
    string sessionId, 
    IReadOnlyDictionary<string, HitDetectionState> hitStates,
    IReadOnlyDictionary<string, JuggleDecayState> juggleStates,
    IReadOnlyDictionary<string, CharacterGravity> gravityStates,
    IReadOnlyDictionary<string, WallCollisionState> wallStates,
    IReadOnlyDictionary<string, DestructionState> destructionStates,
    CancellationToken ct);
    
Task<HitDetectionStats> AnalyzeHitDetectionStatsAsync(IReadOnlyDictionary<string, HitDetectionState> states);
Task<JuggleDecayAnalysis> AnalyzeJuggleDecayAsync(IReadOnlyDictionary<string, JuggleDecayState> states);
Task<GravityMechanics> AnalyzeGravityMechanicsAsync(IReadOnlyDictionary<string, CharacterGravity> states);
Task<WallSplatAnalysis> AnalyzeWallSplatsAsync(IReadOnlyDictionary<string, WallCollisionState> states);
Task<DestructionEvents> AnalyzeDestructionEventsAsync(IReadOnlyDictionary<string, DestructionState> states);
float CalculateOverallPhysicsScore(PhysicsCombatReport report);
```

**State:** None (stateless aggregation service)

---

## 3. Before/After Code Structure

### Before (Current - Monolithic)
```csharp
public class AdvancedPhysicsCombatService : IAdvancedPhysicsCombatService
{
    private readonly Dictionary<string, HitDetectionState> _hitDetectionStates = new();
    private readonly Dictionary<string, JuggleDecayState> _juggleDecayStates = new();
    private readonly Dictionary<string, CharacterGravity> _characterGravities = new();
    private readonly Dictionary<string, WallCollisionState> _wallStates = new();
    private readonly Dictionary<string, DestructionState> _destructionStates = new();
    private readonly HitDetectionEngine _hitDetectionEngine;
    private readonly JuggleDecayEngine _juggleDecayEngine;
    private readonly CharacterGravityEngine _characterGravityEngine;
    private readonly WallSplatEngine _wallSplatEngine;
    private readonly DestructionEngine _destructionEngine;

    public async Task<Result<HitDetectionResult>> ProcessAxisAwareHitAsync(...)
    {
        var hitResult = await _hitDetectionEngine.ProcessHitAsync(...);
        UpdateHitDetectionState(attackerId, defenderId, hitResult);
        return Result.Success(hitResult);
    }

    // 10 public methods, 11 private methods, 24 nested classes = 1,056 lines
}
```

### After (Refactored - Manager Pattern)
```csharp
/// <summary>
/// Coordinator service for advanced physics combat operations.
/// Delegates all operations to specialized managers.
/// </summary>
public class AdvancedPhysicsCombatService : IAdvancedPhysicsCombatService
{
    private readonly ILogger<AdvancedPhysicsCombatService> _logger;
    private readonly HitDetectionManager _hitDetectionManager;
    private readonly JuggleDecayManager _juggleDecayManager;
    private readonly CharacterGravityManager _characterGravityManager;
    private readonly WallSplatManager _wallSplatManager;
    private readonly EnvironmentDestructionManager _environmentDestructionManager;
    private readonly PhysicsReportingManager _physicsReportingManager;

    public AdvancedPhysicsCombatService(
        ILogger<AdvancedPhysicsCombatService> logger,
        HitDetectionManager hitDetectionManager,
        JuggleDecayManager juggleDecayManager,
        CharacterGravityManager characterGravityManager,
        WallSplatManager wallSplatManager,
        EnvironmentDestructionManager environmentDestructionManager,
        PhysicsReportingManager physicsReportingManager)
    {
        _logger = logger;
        _hitDetectionManager = hitDetectionManager;
        _juggleDecayManager = juggleDecayManager;
        _characterGravityManager = characterGravityManager;
        _wallSplatManager = wallSplatManager;
        _environmentDestructionManager = environmentDestructionManager;
        _physicsReportingManager = physicsReportingManager;

        _logger.LogInformation("Advanced physics combat service initialized");
    }

    // Hit Detection Operations
    public Task<Result<HitDetectionResult>> ProcessAxisAwareHitAsync(
        string attackerId, string defenderId, HitRequest request, CancellationToken ct = default)
        => _hitDetectionManager.ProcessHitAsync(attackerId, defenderId, request, ct);

    public Task<Result<AxisPositioning>> GetAxisPositioningAsync(string characterId, CancellationToken ct = default)
        => _hitDetectionManager.GetAxisPositioningAsync(characterId, ct);

    // Juggle Decay Operations
    public Task<Result<JuggleDecayState>> ApplyJuggleDecayAsync(
        string characterId, JuggleHit hit, CancellationToken ct = default)
        => _juggleDecayManager.ApplyDecayAsync(characterId, hit, ct);

    public Task<Result<JuggleMetrics>> GetJuggleMetricsAsync(string characterId, CancellationToken ct = default)
        => _juggleDecayManager.GetMetricsAsync(characterId, ct);

    // Character Gravity Operations
    public Task<Result<CharacterGravity>> CalculateCharacterGravityAsync(
        string characterId, GravityCalculationRequest request, CancellationToken ct = default)
        => _characterGravityManager.CalculateGravityAsync(characterId, request, ct);

    // Wall Splat Operations
    public Task<Result<WallSplatResult>> ProcessWallSplatAsync(
        string characterId, WallCollision collision, CancellationToken ct = default)
        => _wallSplatManager.ProcessSplatAsync(characterId, collision, ct);

    public Task<Result<WallCollisionMetrics>> GetWallCollisionMetricsAsync(string characterId, CancellationToken ct = default)
        => _wallSplatManager.GetMetricsAsync(characterId, ct);

    // Environment Destruction Operations
    public Task<Result<DestructionResult>> ProcessEnvironmentDestructionAsync(
        string stageId, DestructionRequest request, CancellationToken ct = default)
        => _environmentDestructionManager.ProcessDestructionAsync(stageId, request, ct);

    public Task<Result<DestructionMetrics>> GetDestructionMetricsAsync(string stageId, CancellationToken ct = default)
        => _environmentDestructionManager.GetMetricsAsync(stageId, ct);

    // Reporting Operations
    public async Task<Result<PhysicsCombatReport>> GeneratePhysicsCombatReportAsync(
        string sessionId, CancellationToken ct = default)
    {
        return await _physicsReportingManager.GenerateReportAsync(
            sessionId,
            _hitDetectionManager.GetAllStates(),
            _juggleDecayManager.GetAllStates(),
            _characterGravityManager.GetAllStates(),
            _wallSplatManager.GetAllStates(),
            _environmentDestructionManager.GetAllStates(),
            ct);
    }
}
```

---

## 4. File Structure After Refactoring

```
src/SaveState.Application/Mugen/Services/AdvancedPhysics/
├── AdvancedPhysicsCombatService.cs              (150 lines - coordinator)
├── Managers/
│   ├── HitDetectionManager.cs                   (180 lines)
│   ├── JuggleDecayManager.cs                    (130 lines)
│   ├── CharacterGravityManager.cs               (110 lines)
│   ├── WallSplatManager.cs                      (150 lines)
│   ├── EnvironmentDestructionManager.cs         (180 lines)
│   └── PhysicsReportingManager.cs               (150 lines)
├── Models/
│   ├── HitDetectionResult.cs
│   ├── HitRequest.cs
│   ├── HitDetectionState.cs
│   ├── JuggleDecayState.cs
│   ├── JuggleHit.cs
│   ├── CharacterGravity.cs
│   ├── GravityCalculationRequest.cs
│   ├── WallSplatResult.cs
│   ├── WallCollision.cs
│   ├── WallCollisionState.cs
│   ├── DestructionResult.cs
│   ├── DestructionRequest.cs
│   ├── DestructionState.cs
│   ├── AxisPositioning.cs
│   ├── JuggleMetrics.cs
│   ├── WallCollisionMetrics.cs
│   ├── DestructionMetrics.cs
│   ├── PhysicsCombatReport.cs
│   ├── HitDetectionStats.cs
│   ├── JuggleDecayAnalysis.cs
│   ├── GravityMechanics.cs
│   ├── WallSplatAnalysis.cs
│   ├── DestructionEvents.cs
│   └── Enums.cs                                 (BreakType, etc.)
└── Interfaces/
    ├── IHitDetectionManager.cs
    ├── IJuggleDecayManager.cs
    ├── ICharacterGravityManager.cs
    ├── IWallSplatManager.cs
    ├── IEnvironmentDestructionManager.cs
    ├── IPhysicsReportingManager.cs
    └── IAdvancedPhysicsCombatService.cs
```

---

## 5. Edge Cases and Migration Challenges

### 5.1 State Synchronization
**Challenge:** Currently, state dictionaries are updated immediately after engine calls.

**Solution:** Managers own their state dictionaries. Coordinator delegates to managers which handle both processing and state updates atomically.

```csharp
// In HitDetectionManager
public async Task<Result<HitDetectionResult>> ProcessHitAsync(...)
{
    var result = await ProcessHitInternalAsync(...);
    UpdateState(attackerId, defenderId, result); // State managed internally
    return result;
}
```

### 5.2 Reporting Dependencies
**Challenge:** Report generation needs access to all state dictionaries.

**Solution:** 
1. Managers expose `GetAllStates()` method returning `IReadOnlyDictionary`
2. Reporting manager receives all states as parameters (stateless)
3. No direct coupling between managers

### 5.3 Cross-Manager Calculations
**Challenge:** Some calculations (like overall physics score) may need data from multiple managers.

**Solution:** PhysicsReportingManager receives all state as parameters, calculates cross-cutting metrics independently.

### 5.4 Cache Integration
**Challenge:** Current service uses `ICacheService` but only for logging/placement.

**Solution:** Cache can be injected into individual managers if needed, or removed if unused.

### 5.5 Interface Compatibility
**Challenge:** `IAdvancedPhysicsCombatService` must remain stable during refactoring.

**Solution:** Keep interface unchanged. Only internal implementation changes.

---

## 6. Implementation Phases

### Phase 1: Preparation (1-2 hours)
1. Create directory structure (`AdvancedPhysics/Managers`, `AdvancedPhysics/Models`, etc.)
2. Extract all data classes to separate files in `Models/`
3. Create manager interfaces in `Interfaces/`
4. Verify project builds after file moves

### Phase 2: Manager Implementation (4-6 hours)
1. Implement `HitDetectionManager` with tests
2. Implement `JuggleDecayManager` with tests
3. Implement `CharacterGravityManager` with tests
4. Implement `WallSplatManager` with tests
5. Implement `EnvironmentDestructionManager` with tests
6. Implement `PhysicsReportingManager` with tests

### Phase 3: Coordinator Refactoring (2-3 hours)
1. Refactor `AdvancedPhysicsCombatService` to coordinator pattern
2. Update DI registration
3. Run all existing tests
4. Verify backward compatibility

### Phase 4: Cleanup (1 hour)
1. Remove old nested engine classes
2. Clean up using statements
3. Update XML documentation
4. Run full test suite

---

## 7. DI Registration Updates

```csharp
// In Program.cs or DI configuration
services.AddScoped<HitDetectionManager>();
services.AddScoped<JuggleDecayManager>();
services.AddScoped<CharacterGravityManager>();
services.AddScoped<WallSplatManager>();
services.AddScoped<EnvironmentDestructionManager>();
services.AddScoped<PhysicsReportingManager>();

// Keep existing registration
services.AddScoped<IAdvancedPhysicsCombatService, AdvancedPhysicsCombatService>();
```

---

## 8. Testing Strategy

### Unit Tests Per Manager
- `HitDetectionManagerTests` - Test hit detection, cross-up, state management
- `JuggleDecayManagerTests` - Test decay calculation, breakpoints, metrics
- `CharacterGravityManagerTests` - Test gravity calculation, character profiles
- `WallSplatManagerTests` - Test wall collision, bounce physics
- `EnvironmentDestructionManagerTests` - Test destruction processing, hazard calculation
- `PhysicsReportingManagerTests` - Test report generation, aggregation

### Integration Tests
- `AdvancedPhysicsCombatServiceTests` - Test coordinator integration, verify backward compatibility

---

## 9. Success Metrics

| Metric | Before | After | Target |
|--------|--------|-------|--------|
| Service Lines | 1,056 | ~150 | 85% reduction |
| Max Class Size | 1,056 | ~180 | 83% reduction |
| Testability | Low | High | Improved |
| Responsibilities/Class | 7 | 1 | SRP compliance |
| Public Methods/Class | 10 | 3-5 avg | Reduced API surface |

---

## 10. Summary

This refactoring will transform the monolithic `AdvancedPhysicsCombatService` (1,056 lines) into a clean coordinator service (~150 lines) that delegates to 6 focused managers. Each manager handles a single responsibility:

1. **HitDetectionManager** - All hit detection and cross-up logic
2. **JuggleDecayManager** - Combo scaling and juggle physics
3. **CharacterGravityManager** - Per-character physics properties
4. **WallSplatManager** - Wall collision mechanics
5. **EnvironmentDestructionManager** - Stage destruction and hazards
6. **PhysicsReportingManager** - Statistics aggregation and reporting

**Benefits:**
- Single Responsibility Principle compliance
- Improved testability (test managers independently)
- Reduced cognitive load per file
- Easier maintenance and debugging
- Clear separation of concerns
- Consistent with established Manager Pattern in codebase
