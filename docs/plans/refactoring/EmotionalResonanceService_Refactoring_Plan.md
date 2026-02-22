# EmotionalResonanceService Manager Pattern Refactoring Plan

## Executive Summary

| Metric | Current | After Refactor | Change |
|--------|---------|----------------|--------|
| **Total Lines** | 1,073 | ~450 (across 6 files) | -58% |
| **Coordinator Lines** | 1,073 | ~120 | -89% |
| **Public Methods** | 9 | 9 (delegated) | 0 |
| **Manager Classes** | 0 | 5 | +5 |

---

## Current File Analysis

### File Statistics

```
File: src/SaveState.Application/Mugen/Services/EmotionalResonanceService.cs
Lines: 1,073
Public Methods: 9
Private Methods: 12
Internal Engine Classes: 4
```

### Existing Architecture

The file currently uses an **internal engine pattern** with 4 nested engine classes:

```
EmotionalResonanceService (1,073 lines)
├── EmotionalResonanceServiceEmotionEngine (148 lines)
├── EmotionalResonanceServiceResonanceEngine (70 lines)
├── EmotionalResonanceServiceSpectatorEngine (54 lines)
└── EmotionalResonanceServicePsychologicalEngine (42 lines)
```

### Public API Methods (9)

| Method | Responsibility | Current Lines | Delegates To |
|--------|---------------|---------------|--------------|
| `UpdateEmotionalStateAsync` | Emotion processing | 25 | EmotionEngine |
| `CreateResonanceFieldAsync` | Field creation | 19 | ResonanceEngine |
| `TransferResonanceAsync` | Resonance transfer | 27 | ResonanceEngine |
| `SendSpectatorSupportAsync` | Spectator influence | 21 | SpectatorEngine |
| `CheckBreakingPointAsync` | Breaking point check | 27 | PsychologicalEngine |
| `UpdateCrowdPsychologyAsync` | Crowd state | 24 | SpectatorEngine |
| `CalculateEmotionalSynergyAsync` | Synergy calculation | 20 | EmotionEngine |
| `GetEmotionalAnalyticsAsync` | Analytics | 26 | Self (analytics) |
| `ApplyEmotionalBuffAsync` | Buff application | 22 | EmotionEngine |

### Private Methods (12)

| Method | Purpose | Target Manager |
|--------|---------|----------------|
| `InitializeEmotionalSystem` | System setup | Coordinator |
| `GetOrCreateEmotionalState` | State retrieval | EmotionStateManager |
| `ApplyEmotionalEffectsAsync` | Effect application | EffectApplicationManager |
| `ApplySpectatorInfluenceAsync` | Influence application | SpectatorInfluenceManager |
| `ApplyBreakingPointEffectsAsync` | Breaking point effects | PsychologicalStateManager |
| `ApplyCrowdInfluenceAsync` | Crowd influence | SpectatorInfluenceManager |
| `ApplyBuffEffectsAsync` | Buff effects | EmotionStateManager |
| `CalculateEmotionalEffects` | Effect calculation | EffectApplicationManager |
| `AnalyzeEmotionalDistributionAsync` | Analytics | EmotionalAnalyticsManager |
| `AnalyzeResonanceEventsAsync` | Analytics | EmotionalAnalyticsManager |
| `AnalyzeSpectatorInfluenceAsync` | Analytics | EmotionalAnalyticsManager |
| `AnalyzeBreakingPointsAsync` | Analytics | EmotionalAnalyticsManager |
| `CalculateEmotionalStability` | Analytics | EmotionalAnalyticsManager |

---

## Responsibility Analysis

### Domain Boundaries Identified

```
┌─────────────────────────────────────────────────────────────────┐
│                    EMOTIONAL RESONANCE SYSTEM                    │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐  ┌─────────────────────┐               │
│  │   Emotion State     │  │   Resonance Field   │               │
│  │    Management       │  │    Management       │               │
│  │                     │  │                     │               │
│  │ • State tracking    │  │ • Field creation    │               │
│  │ • Trigger processing│  │ • Resonance transfer│               │
│  │ • Synergy calc      │  │ • Field effects     │               │
│  │ • Buff management   │  │                     │               │
│  └─────────────────────┘  └─────────────────────┘               │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐  ┌─────────────────────┐               │
│  │ Spectator Influence │  │ Psychological State │               │
│  │    Management       │  │    Management       │               │
│  │                     │  │                     │               │
│  │ • Support processing│  │ • Breaking points   │               │
│  │ • Crowd psychology  │  │ • Stability calc    │               │
│  │ • Influence effects │  │ • Psychological FX  │               │
│  │                     │  │                     │               │
│  └─────────────────────┘  └─────────────────────┘               │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐                                       │
│  │ Emotional Analytics │                                       │
│  │    Management       │                                       │
│  │                     │                                       │
│  │ • Distribution      │                                       │
│  │ • Event analysis    │                                       │
│  │ • Stability metrics │                                       │
│  │ • Report generation │                                       │
│  └─────────────────────┘                                       │
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow Analysis

```
┌─────────────────────────────────────────────────────────────────────┐
│                         DATA FLOW                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   Triggers/Events         State Management         Effects          │
│   ───────────────         ────────────────         ───────          │
│                                                                     │
│   Combat Events ───────►  Emotion State ───────►  Stat Modifiers    │
│        │                   Management                (Buffs)        │
│        │                        │                                   │
│        ▼                        ▼                                   │
│   Spectator ─────────►  Spectator Influence ───►  Crowd Effects     │
│   Actions                  Management                               │
│        │                        │                                   │
│        ▼                        ▼                                   │
│   Field Creation ────►  Resonance Field ──────►  Synergy Effects    │
│                         Management                                  │
│        │                        │                                   │
│        ▼                        ▼                                   │
│   Breaking Point ────►  Psychological ────────►  Rage/Despair FX    │
│   Conditions               State                                    │
│                                                                     │
│                              │                                      │
│                              ▼                                      │
│                        Emotional Analytics                          │
│                        (Reporting/Analysis)                         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Proposed Manager Class Breakdown

### 1. EmotionStateManager

**Responsibility:** Character emotional state lifecycle and processing

**Public Methods:**
```csharp
Task<EmotionalResonanceServiceResonanceEmotionalState> ProcessTriggerAsync(
    string characterId, 
    EmotionalResonanceServiceEmotionalTrigger trigger, 
    CancellationToken ct);

Task<EmotionalResonanceServiceEmotionalSynergy> CalculateSynergyAsync(
    string character1Id, 
    string character2Id, 
    CancellationToken ct);

Task<EmotionalResonanceServiceEmotionalBuff> CreateBuffAsync(
    string characterId, 
    EmotionalResonanceServiceEmotionalBuffRequest request, 
    CancellationToken ct);

EmotionalResonanceServiceResonanceEmotionalState GetOrCreateState(string characterId);
Task ApplyBuffEffectsAsync(string characterId, EmotionalResonanceServiceEmotionalBuff buff, CancellationToken ct);
```

**Private Methods:**
- `CalculateEmotionChanges`
- `DeterminePrimaryEmotion`
- `DetermineSecondaryEmotion`
- `AddToHistory`
- `CalculateEmotionalCompatibility`
- `GenerateSynergyEffects`
- `DetermineEmotionalBond`
- `GenerateBuffEffects`

**Estimated Lines:** ~220

---

### 2. ResonanceFieldManager

**Responsibility:** Resonance field creation and transfer mechanics

**Public Methods:**
```csharp
Task<EmotionalResonanceServiceResonanceField> CreateFieldAsync(
    EmotionalResonanceServiceResonanceFieldRequest request, 
    CancellationToken ct);

Task<EmotionalResonanceServiceResonanceTransfer> TransferResonanceAsync(
    string sourceCharacterId, 
    string targetCharacterId, 
    EmotionalResonanceServiceResonanceTransferRequest request, 
    CancellationToken ct);
```

**Private Methods:**
- `GenerateFieldEffects`
- `GenerateTransferEffects`

**Estimated Lines:** ~80

---

### 3. SpectatorInfluenceManager

**Responsibility:** Spectator support and crowd psychology management

**Public Methods:**
```csharp
Task<EmotionalResonanceServiceSpectatorInfluence> ProcessSupportAsync(
    string spectatorId, 
    string characterId, 
    EmotionalResonanceServiceSpectatorSupport support, 
    CancellationToken ct);

Task<EmotionalResonanceServiceCrowdPsychology> UpdateCrowdStateAsync(
    string matchId, 
    EmotionalResonanceServiceCrowdEvent crowdEvent, 
    CancellationToken ct);

Task ApplySpectatorInfluenceAsync(
    string characterId, 
    EmotionalResonanceServiceSpectatorInfluence influence, 
    CancellationToken ct);

Task ApplyCrowdInfluenceAsync(
    string characterId, 
    EmotionalResonanceServiceCrowdPsychology crowdState, 
    CancellationToken ct);
```

**Private Methods:**
- `GenerateSupportEffects`

**Estimated Lines:** ~90

---

### 4. PsychologicalStateManager

**Responsibility:** Breaking point mechanics and psychological effects

**Public Methods:**
```csharp
Task<EmotionalResonanceServiceBreakingPoint> CheckBreakingPointAsync(
    EmotionalResonanceServiceResonanceEmotionalState state, 
    CancellationToken ct);

Task ApplyBreakingPointEffectsAsync(
    string characterId, 
    EmotionalResonanceServiceBreakingPoint breakingPoint, 
    CancellationToken ct);
```

**Private Methods:**
- `GenerateBreakingPointEffects`

**Estimated Lines:** ~60

---

### 5. EffectApplicationManager

**Responsibility:** Cross-cutting effect calculations and applications

**Public Methods:**
```csharp
Dictionary<EmotionalResonanceServiceEmotion, float> CalculateEmotionalEffects(
    EmotionalResonanceServiceResonanceEmotionalState state);

Task ApplyEmotionalEffectsAsync(
    string characterId, 
    EmotionalResonanceServiceResonanceEmotionalState state, 
    CancellationToken ct);
```

**Estimated Lines:** ~50

---

### 6. EmotionalAnalyticsManager

**Responsibility:** Analytics generation and reporting

**Public Methods:**
```csharp
Task<EmotionalResonanceServiceEmotionalAnalytics> GenerateAnalyticsAsync(
    string characterId, 
    TimeSpan period, 
    CancellationToken ct);

float CalculateEmotionalStability(string characterId);
```

**Private Methods:**
- `AnalyzeEmotionalDistributionAsync`
- `AnalyzeResonanceEventsAsync`
- `AnalyzeSpectatorInfluenceAsync`
- `AnalyzeBreakingPointsAsync`

**Estimated Lines:** ~100

---

## Refactoring Structure

### Before (Current)

```csharp
// EmotionalResonanceService.cs - 1,073 lines
public class EmotionalResonanceService : EmotionalResonanceServiceIEmotionalResonanceService
{
    private readonly Dictionary<string, EmotionalResonanceServiceResonanceEmotionalState> _characterEmotions;
    private readonly Dictionary<string, EmotionalResonanceServiceResonanceField> _resonanceFields;
    private readonly Dictionary<string, EmotionalResonanceServiceSpectatorInfluence> _spectatorInfluences;
    
    private readonly EmotionalResonanceServiceEmotionEngine _emotionEngine;
    private readonly EmotionalResonanceServiceResonanceEngine _resonanceEngine;
    private readonly EmotionalResonanceServiceSpectatorEngine _spectatorEngine;
    private readonly EmotionalResonanceServicePsychologicalEngine _psychologicalEngine;

    // 9 public methods + 12 private methods + 4 engine classes
    // ... 1,073 lines total
}
```

### After (Proposed)

```csharp
// EmotionalResonanceService.cs - ~120 lines
public class EmotionalResonanceService : IEmotionalResonanceService
{
    private readonly IEmotionStateManager _emotionStateManager;
    private readonly IResonanceFieldManager _resonanceFieldManager;
    private readonly ISpectatorInfluenceManager _spectatorInfluenceManager;
    private readonly IPsychologicalStateManager _psychologicalStateManager;
    private readonly IEffectApplicationManager _effectApplicationManager;
    private readonly IEmotionalAnalyticsManager _emotionalAnalyticsManager;

    public EmotionalResonanceService(
        IEmotionStateManager emotionStateManager,
        IResonanceFieldManager resonanceFieldManager,
        ISpectatorInfluenceManager spectatorInfluenceManager,
        IPsychologicalStateManager psychologicalStateManager,
        IEffectApplicationManager effectApplicationManager,
        IEmotionalAnalyticsManager emotionalAnalyticsManager)
    {
        _emotionStateManager = emotionStateManager;
        _resonanceFieldManager = resonanceFieldManager;
        _spectatorInfluenceManager = spectatorInfluenceManager;
        _psychologicalStateManager = psychologicalStateManager;
        _effectApplicationManager = effectApplicationManager;
        _emotionalAnalyticsManager = emotionalAnalyticsManager;
    }

    // 9 public methods - pure delegation
}
```

---

## Code Examples

### Example 1: UpdateEmotionalStateAsync

**BEFORE (25 lines in service):**
```csharp
public async Task<Result<EmotionalResonanceServiceResonanceEmotionalState>> UpdateEmotionalStateAsync(
    string characterId, 
    EmotionalResonanceServiceEmotionalTrigger trigger, 
    CancellationToken ct = default)
{
    try
    {
        _logger.LogInformation("Updating emotional state for character {CharacterId}", characterId);

        var currentState = GetOrCreateEmotionalState(characterId);
        var updatedState = await _emotionEngine.ProcessTriggerAsync(currentState, trigger, ct);

        _characterEmotions[characterId] = updatedState;
        await ApplyEmotionalEffectsAsync(characterId, updatedState, ct);

        _logger.LogInformation("Emotional state updated: {CharacterId} -> {PrimaryEmotion}",
            characterId, updatedState.PrimaryEmotion);

        return Result.Success<EmotionalResonanceServiceResonanceEmotionalState>(updatedState);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating emotional state for character {CharacterId}", characterId);
        return Result.Failure<EmotionalResonanceServiceResonanceEmotionalState>($"Emotional state update failed: {ex.Message}");
    }
}
```

**AFTER (8 lines in coordinator):**
```csharp
public async Task<Result<EmotionalResonanceServiceResonanceEmotionalState>> UpdateEmotionalStateAsync(
    string characterId, 
    EmotionalResonanceServiceEmotionalTrigger trigger, 
    CancellationToken ct = default)
{
    var result = await _emotionStateManager.ProcessTriggerAsync(characterId, trigger, ct);
    if (result.IsSuccess)
    {
        await _effectApplicationManager.ApplyEmotionalEffectsAsync(characterId, result.Value, ct);
    }
    return result;
}
```

---

### Example 2: GetEmotionalAnalyticsAsync

**BEFORE (26 lines with inline analytics):**
```csharp
public async Task<Result<EmotionalResonanceServiceEmotionalAnalytics>> GetEmotionalAnalyticsAsync(
    string characterId, 
    TimeSpan period, 
    CancellationToken ct = default)
{
    try
    {
        _logger.LogInformation("Generating emotional analytics for character {CharacterId}", characterId);

        var analytics = new EmotionalResonanceServiceEmotionalAnalytics
        {
            CharacterId = characterId,
            Period = period,
            EmotionalDistribution = await AnalyzeEmotionalDistributionAsync(characterId, period, ct),
            ResonanceEvents = await AnalyzeResonanceEventsAsync(characterId, period, ct),
            SpectatorInfluence = await AnalyzeSpectatorInfluenceAsync(characterId, period, ct),
            BreakingPointHistory = await AnalyzeBreakingPointsAsync(characterId, period, ct),
            EmotionalStability = CalculateEmotionalStability(characterId),
            GeneratedAt = _timeProvider.UtcNow
        };

        return Result.Success<EmotionalResonanceServiceEmotionalAnalytics>(analytics);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating emotional analytics");
        return Result.Failure<EmotionalResonanceServiceEmotionalAnalytics>($"Analytics generation failed: {ex.Message}");
    }
}
```

**AFTER (4 lines in coordinator):**
```csharp
public async Task<Result<EmotionalResonanceServiceEmotionalAnalytics>> GetEmotionalAnalyticsAsync(
    string characterId, 
    TimeSpan period, 
    CancellationToken ct = default)
{
    return await _emotionalAnalyticsManager.GenerateAnalyticsAsync(characterId, period, ct);
}
```

---

### Example 3: EmotionStateManager Implementation

```csharp
// EmotionStateManager.cs - ~220 lines
public class EmotionStateManager : IEmotionStateManager
{
    private readonly ILogger<EmotionStateManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, EmotionalResonanceServiceResonanceEmotionalState> _characterEmotions = new();

    public EmotionStateManager(ILogger<EmotionStateManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<EmotionalResonanceServiceResonanceEmotionalState>> ProcessTriggerAsync(
        string characterId,
        EmotionalResonanceServiceEmotionalTrigger trigger,
        CancellationToken ct)
    {
        try
        {
            var currentState = GetOrCreateState(characterId);
            var updatedState = await ProcessTriggerInternalAsync(currentState, trigger, ct);
            _characterEmotions[characterId] = updatedState;
            return Result.Success(updatedState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing trigger for {CharacterId}", characterId);
            return Result.Failure<EmotionalResonanceServiceResonanceEmotionalState>($"Trigger processing failed: {ex.Message}");
        }
    }

    public EmotionalResonanceServiceResonanceEmotionalState GetOrCreateState(string characterId)
    {
        if (!_characterEmotions.TryGetValue(characterId, out var state))
        {
            state = CreateDefaultState(characterId);
            _characterEmotions[characterId] = state;
        }
        return state;
    }

    // ... other methods from EmotionalResonanceServiceEmotionEngine
}
```

---

## File Structure After Refactoring

```
src/SaveState.Application/Mugen/Services/EmotionalResonance/
├── EmotionalResonanceService.cs              (~120 lines - coordinator)
├── Managers/
│   ├── EmotionStateManager.cs                (~220 lines)
│   ├── ResonanceFieldManager.cs              (~80 lines)
│   ├── SpectatorInfluenceManager.cs          (~90 lines)
│   ├── PsychologicalStateManager.cs          (~60 lines)
│   ├── EffectApplicationManager.cs           (~50 lines)
│   └── EmotionalAnalyticsManager.cs          (~100 lines)
├── Interfaces/
│   ├── IEmotionalResonanceService.cs
│   ├── IEmotionStateManager.cs
│   ├── IResonanceFieldManager.cs
│   ├── ISpectatorInfluenceManager.cs
│   ├── IPsychologicalStateManager.cs
│   ├── IEffectApplicationManager.cs
│   └── IEmotionalAnalyticsManager.cs
└── Models/
    └── [Move all DTO/Model classes here]
```

---

## Key Challenges and Edge Cases

### Challenge 1: Shared State Management

**Problem:** Multiple managers need access to `_characterEmotions` dictionary.

**Solutions Considered:**
1. **Option A:** Pass dictionary to managers (breaks encapsulation)
2. **Option B:** Each manager has its own state (duplication issues)
3. **Option C:** Central state service (recommended) ✅

**Recommended Solution:**
```csharp
public interface IEmotionalStateRepository
{
    EmotionalResonanceServiceResonanceEmotionalState GetOrCreate(string characterId);
    void Update(string characterId, EmotionalResonanceServiceResonanceEmotionalState state);
    bool TryGet(string characterId, out EmotionalResonanceServiceResonanceEmotionalState state);
}

// Injected into managers that need state access
```

---

### Challenge 2: Effect Application Chain

**Problem:** `UpdateEmotionalStateAsync` triggers `ApplyEmotionalEffectsAsync` which needs access to emotional calculations.

**Current Flow:**
```
UpdateEmotionalStateAsync → ApplyEmotionalEffectsAsync → CalculateEmotionalEffects
```

**Solution:** Keep effect calculation in dedicated manager, state manager returns state:
```csharp
public async Task<Result<EmotionalResonanceServiceResonanceEmotionalState>> UpdateEmotionalStateAsync(...)
{
    var result = await _emotionStateManager.ProcessTriggerAsync(characterId, trigger, ct);
    if (result.IsSuccess)
    {
        await _effectApplicationManager.ApplyEmotionalEffectsAsync(characterId, result.Value, ct);
    }
    return result;
}
```

---

### Challenge 3: Cross-Manager Resonance Transfer

**Problem:** `TransferResonanceAsync` updates both source and target emotional states.

**Current Implementation:**
```csharp
// Updates emotional states directly in service
if (_characterEmotions.TryGetValue(sourceCharacterId, out var sourceState))
    sourceState.Intensity *= (float)(1 - request.TransferAmount);
```

**Solution:** Managers expose update methods:
```csharp
public async Task<Result<EmotionalResonanceServiceResonanceTransfer>> TransferResonanceAsync(...)
{
    var transfer = await _resonanceFieldManager.TransferResonanceAsync(sourceId, targetId, request, ct);
    
    // Delegate state updates to state manager
    await _emotionStateManager.ModifyIntensityAsync(sourceId, 1 - request.TransferAmount);
    await _emotionStateManager.ModifyIntensityAsync(targetId, 1 + request.TransferAmount);
    
    return transfer;
}
```

---

### Challenge 4: Circular Dependencies

**Risk:** Analytics manager needs state data, state manager might need analytics.

**Prevention:**
- Analytics manager only reads (never writes)
- Pass state as parameters, don't inject state manager into analytics

```csharp
// Good - analytics receives state via parameter
public async Task<EmotionalAnalytics> GenerateAnalyticsAsync(
    string characterId, 
    IEmotionalStateRepository stateRepository,  // Read-only abstraction
    TimeSpan period);
```

---

### Challenge 5: Exception Handling Consistency

**Current:** Each method has try-catch with Result.Failure.

**Solution Options:**
1. Keep try-catch in coordinator (duplicates logic)
2. Move try-catch to managers (recommended) ✅
3. Use middleware/interceptor pattern

**Recommended:**
```csharp
// Managers handle their own exceptions
public class EmotionStateManager : IEmotionStateManager
{
    public async Task<Result<T>> OperationAsync(...)
    {
        try { /* ... */ }
        catch (Exception ex) 
        { 
            _logger.LogError(ex, "...");
            return Result.Failure<T>("..."); 
        }
    }
}

// Coordinator delegates and returns result directly
public Task<Result<T>> OperationAsync(...) 
    => _manager.OperationAsync(...);
```

---

## DI Registration

```csharp
// In Application Layer DI configuration
services.AddEmotionalResonanceServices();

// Extension method
public static IServiceCollection AddEmotionalResonanceServices(this IServiceCollection services)
{
    // Coordinator
    services.AddScoped<IEmotionalResonanceService, EmotionalResonanceService>();
    
    // Managers
    services.AddScoped<IEmotionStateManager, EmotionStateManager>();
    services.AddScoped<IResonanceFieldManager, ResonanceFieldManager>();
    services.AddScoped<ISpectatorInfluenceManager, SpectatorInfluenceManager>();
    services.AddScoped<IPsychologicalStateManager, PsychologicalStateManager>();
    services.AddScoped<IEffectApplicationManager, EffectApplicationManager>();
    services.AddScoped<IEmotionalAnalyticsManager, EmotionalAnalyticsManager>();
    
    // State repository (if using shared state approach)
    services.AddSingleton<IEmotionalStateRepository, EmotionalStateRepository>();
    
    return services;
}
```

---

## Migration Steps

### Phase 1: Infrastructure Setup
1. Create folder structure
2. Create interfaces
3. Create state repository (if needed)

### Phase 2: Manager Implementation (in order)
1. `EmotionStateManager` (core dependency)
2. `ResonanceFieldManager`
3. `SpectatorInfluenceManager`
4. `PsychologicalStateManager`
5. `EffectApplicationManager`
6. `EmotionalAnalyticsManager`

### Phase 3: Coordinator Refactoring
1. Inject managers into `EmotionalResonanceService`
2. Replace method bodies with delegation
3. Remove old engine classes
4. Remove private helper methods

### Phase 4: Cleanup
1. Move model classes to separate files
2. Update DI registration
3. Run tests
4. Update documentation

---

## Testing Strategy

### Unit Tests Per Manager

| Manager | Test Focus |
|---------|-----------|
| EmotionStateManager | State transitions, history limits, emotion determination |
| ResonanceFieldManager | Field effect calculations, transfer mechanics |
| SpectatorInfluenceManager | Support type mapping, crowd mood transitions |
| PsychologicalStateManager | Breaking point thresholds, effect generation |
| EffectApplicationManager | Effect calculations, emotion-to-stat mapping |
| EmotionalAnalyticsManager | Distribution accuracy, stability calculations |

### Integration Tests
- Cross-manager operations (resonance transfer affecting emotions)
- State consistency across managers
- Exception handling chains

---

## Summary

| Aspect | Current | After Refactor | Improvement |
|--------|---------|----------------|-------------|
| **Lines per type** | 1,073 (1 file) | ~120 + 5×~100 = ~620 | -42% |
| **Responsibilities** | 5 mixed | 6 focused | Clear separation |
| **Testability** | Difficult | Easy per manager | +++ |
| **Maintainability** | Low | High | Clear boundaries |
| **Reusability** | None | Managers reusable | +++ |

### Key Benefits

1. **Single Responsibility:** Each manager handles one domain
2. **Testability:** Managers can be unit tested in isolation
3. **Maintainability:** Changes isolated to specific domain
4. **Readability:** Smaller, focused classes
5. **Reusability:** Managers can be composed in different ways

---

*Plan created: February 21, 2026*
*Estimated implementation time: 4-6 hours*
*Risk level: Low (internal refactoring, no public API changes)*
