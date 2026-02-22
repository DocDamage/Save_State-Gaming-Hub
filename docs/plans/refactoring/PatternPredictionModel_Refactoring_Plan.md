# PatternPredictionModel Refactoring Plan

## Overview

**File:** `src/SaveState.Infrastructure/GameLibrary/ML/PatternPredictionModel.cs`  
**Current Lines:** 1,062  
**Target Lines:** ~180 lines (coordinator) + 5 managers (~150-200 lines each)  
**Pattern:** Manager Pattern with Coordinator

---

## File Statistics

| Metric | Current | Target |
|--------|---------|--------|
| Total Lines | 1,062 | ~900 (split across 6 files) |
| Public Methods | 9 | 9 (delegated) |
| Private Methods | 15 | 0 (moved to managers) |
| Helper Classes | 5 | 5 (relocated) |
| Responsibilities | 5 | 1 (coordinator only) |

---

## Responsibility Analysis

### Current Responsibilities (Violating SRP)

1. **Discovery Data Management**
   - `_discoveries` list maintenance
   - ReaderWriterLockSlim for thread safety
   - CRUD operations on discovery records

2. **Genre Profile Management**
   - `_genreProfiles` dictionary
   - Genre pattern profiling and updates
   - Genre-based prediction generation

3. **Engine Profile Management**
   - `_engineProfiles` dictionary
   - Engine pattern profiling and updates
   - Engine-based prediction generation

4. **Pattern Prediction Engine**
   - Multi-source prediction merging
   - Confidence calculation algorithms
   - Historical data analysis

5. **Integration with External Components**
   - `EnginePatternDatabase` usage
   - `StatisticalPatternValidator` delegation
   - Process memory operations for relative addresses

---

## Proposed Manager Classes

### 1. DiscoveryRepositoryManager

**Responsibility:** Thread-safe storage and retrieval of successful discoveries

**Key Methods:**
```csharp
public sealed class DiscoveryRepositoryManager
{
    public Result RecordDiscovery(SuccessfulDiscovery discovery);
    public IReadOnlyList<SuccessfulDiscovery> GetAllDiscoveries();
    public IReadOnlyList<SuccessfulDiscovery> GetByGenre(GameGenre genre);
    public IReadOnlyList<SuccessfulDiscovery> GetByEngine(GameEngine engine);
    public IReadOnlyList<SuccessfulDiscovery> GetByPatternType(string patternType);
    public void ClearDiscoveries();
    public PredictionModelStats CalculateStatistics();
}
```

**State to Manage:**
- `_discoveries` list
- `_lock` (ReaderWriterLockSlim)

---

### 2. GenreProfileManager

**Responsibility:** Genre-specific pattern profiling and predictions

**Key Methods:**
```csharp
public sealed class GenreProfileManager
{
    public void UpdateProfile(SuccessfulDiscovery discovery);
    public List<PredictedPattern> GetPredictions(GameGenre genre, long moduleBaseAddress);
    public List<AddressProbability> GetLikelyAddresses(GameGenre genre, string patternType);
    public IReadOnlyDictionary<GameGenre, GenrePatternProfile> GetAllProfiles();
    public void InitializeProfiles();
    public void ClearProfiles();
}
```

**State to Manage:**
- `_genreProfiles` dictionary

---

### 3. EngineProfileManager

**Responsibility:** Engine-specific pattern profiling and predictions

**Key Methods:**
```csharp
public sealed class EngineProfileManager
{
    public void UpdateProfile(SuccessfulDiscovery discovery);
    public List<PredictedPattern> GetPredictions(GameEngine engine, long moduleBaseAddress);
    public List<EngineMemoryPattern> GetEnginePatterns(GameEngine engine);
    public GameEngine DetectEngine(Process process);
    public IReadOnlyDictionary<GameEngine, EnginePatternProfile> GetAllProfiles();
    public void InitializeProfiles();
    public void ClearProfiles();
}
```

**State to Manage:**
- `_engineProfiles` dictionary
- `_engineDatabase` (EnginePatternDatabase)

---

### 4. PredictionEngineManager

**Responsibility:** Pattern prediction algorithm orchestration and merging

**Key Methods:**
```csharp
public sealed class PredictionEngineManager
{
    public List<PredictedPattern> PredictPatterns(
        GameGenre genre,
        GameEngine engine,
        List<PredictedPattern> genrePredictions,
        List<PredictedPattern> enginePredictions,
        List<PredictedPattern> historicalPredictions,
        long moduleBaseAddress);
    
    private List<PredictedPattern> GetHistoricalPredictions(
        IReadOnlyList<SuccessfulDiscovery> discoveries,
        GameGenre genre,
        GameEngine engine,
        long moduleBaseAddress);
    
    private List<PredictedPattern> MergePredictions(List<PredictedPattern> predictions);
    private (double Min, double Max) GetExpectedRange(string patternType);
}
```

---

### 5. PatternAddressCalculator

**Responsibility:** Memory address calculations and conversions

**Key Methods:**
```csharp
public sealed class PatternAddressCalculator
{
    public long CalculateRelativeAddress(long absoluteAddress, int processId, string moduleName);
    public long CalculateAbsoluteAddress(long relativeAddress, int processId, string moduleName);
}
```

---

## Before/After Code Structure

### BEFORE (Current)

```csharp
public sealed class PatternPredictionModel
{
    private readonly List<SuccessfulDiscovery> _discoveries;
    private readonly Dictionary<GameGenre, GenrePatternProfile> _genreProfiles;
    private readonly Dictionary<GameEngine, EnginePatternProfile> _engineProfiles;
    private readonly EnginePatternDatabase _engineDatabase;
    private readonly StatisticalPatternValidator _validator;
    private readonly ITimeProvider _timeProvider;
    private readonly ReaderWriterLockSlim _lock;

    public PatternPredictionModel(ITimeProvider timeProvider) { ... }
    public Result RecordSuccess(SuccessfulDiscovery discovery, int processId) { ... }
    public List<PredictedPattern> PredictPatterns(GameGenre genre, GameEngine engine, long moduleBaseAddress) { ... }
    public List<AddressProbability> GetLikelyAddresses(GameGenre genre, string patternType, GameEngine? engine) { ... }
    public ValidationResult ValidatePattern(long address, List<ValueObservation> history, string patternType) { ... }
    public PredictionModelStats GetStatistics() { ... }
    public void ClearDiscoveries() { ... }
    public List<EngineMemoryPattern> GetEnginePatterns(GameEngine engine) { ... }
    
    // ~15 private helper methods...
    // 5 nested helper classes...
}
```

**Problems:**
- 1,062 lines in single file
- Mixes data storage, ML algorithms, and address calculations
- Hard to test individual components
- Thread safety logic scattered
- Violates Single Responsibility Principle

---

### AFTER (Refactored)

#### Coordinator: PatternPredictionModel

```csharp
public sealed class PatternPredictionModel
{
    private readonly DiscoveryRepositoryManager _discoveryRepository;
    private readonly GenreProfileManager _genreProfileManager;
    private readonly EngineProfileManager _engineProfileManager;
    private readonly PredictionEngineManager _predictionEngine;
    private readonly PatternAddressCalculator _addressCalculator;
    private readonly StatisticalPatternValidator _validator;
    private readonly ITimeProvider _timeProvider;

    public PatternPredictionModel(
        DiscoveryRepositoryManager discoveryRepository,
        GenreProfileManager genreProfileManager,
        EngineProfileManager engineProfileManager,
        PredictionEngineManager predictionEngine,
        PatternAddressCalculator addressCalculator,
        StatisticalPatternValidator validator,
        ITimeProvider timeProvider)
    {
        _discoveryRepository = discoveryRepository;
        _genreProfileManager = genreProfileManager;
        _engineProfileManager = engineProfileManager;
        _predictionEngine = predictionEngine;
        _addressCalculator = addressCalculator;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    public Result RecordSuccess(SuccessfulDiscovery discovery, int processId)
    {
        if (discovery is null)
            return Result.Failure("Discovery cannot be null");

        try
        {
            // Calculate relative address
            if (!string.IsNullOrEmpty(discovery.ModuleName))
            {
                discovery.RelativeAddress = _addressCalculator.CalculateRelativeAddress(
                    discovery.Address, processId, discovery.ModuleName);
            }
            else
            {
                discovery.RelativeAddress = discovery.Address;
            }

            discovery.Timestamp = _timeProvider.UtcNow;

            // Record in repository
            var recordResult = _discoveryRepository.RecordDiscovery(discovery);
            if (recordResult.IsFailure)
                return recordResult;

            // Update profiles
            _genreProfileManager.UpdateProfile(discovery);
            _engineProfileManager.UpdateProfile(discovery);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to record discovery: {ex.Message}", ErrorType.Internal);
        }
    }

    public List<PredictedPattern> PredictPatterns(
        GameGenre genre,
        GameEngine engine,
        long moduleBaseAddress = 0)
    {
        // Collect predictions from all sources
        var genrePredictions = _genreProfileManager.GetPredictions(genre, moduleBaseAddress);
        var enginePredictions = _engineProfileManager.GetPredictions(engine, moduleBaseAddress);
        var discoveries = _discoveryRepository.GetAllDiscoveries();

        // Delegate to prediction engine for merging and ranking
        return _predictionEngine.PredictPatterns(
            genre, engine, genrePredictions, enginePredictions, 
            discoveries, moduleBaseAddress);
    }

    public List<AddressProbability> GetLikelyAddresses(
        GameGenre genre,
        string patternType,
        GameEngine? engine = null)
    {
        return _genreProfileManager.GetLikelyAddresses(genre, patternType, engine,
            _discoveryRepository.GetByGenre(genre));
    }

    public ValidationResult ValidatePattern(
        long address,
        List<ValueObservation> history,
        string patternType)
    {
        return _validator.ValidatePattern(address, history, patternType);
    }

    public PredictionModelStats GetStatistics()
    {
        return _discoveryRepository.CalculateStatistics();
    }

    public void ClearDiscoveries()
    {
        _discoveryRepository.ClearDiscoveries();
        _genreProfileManager.ClearProfiles();
        _engineProfileManager.ClearProfiles();
    }

    public List<EngineMemoryPattern> GetEnginePatterns(GameEngine engine)
    {
        return _engineProfileManager.GetEnginePatterns(engine);
    }
}
```

**Benefits:**
- ~180 lines (84% reduction)
- Single responsibility: coordination only
- Easy to understand flow
- Delegates to focused managers
- Testable by mocking managers

---

## New File Structure

```
src/SaveState.Infrastructure/GameLibrary/ML/
├── PatternPredictionModel.cs                    # Coordinator (~180 lines)
├── Managers/
│   ├── DiscoveryRepositoryManager.cs            # Data storage (~180 lines)
│   ├── GenreProfileManager.cs                   # Genre profiling (~160 lines)
│   ├── EngineProfileManager.cs                  # Engine profiling (~170 lines)
│   ├── PredictionEngineManager.cs               # Prediction algorithms (~200 lines)
│   └── PatternAddressCalculator.cs              # Address calculations (~100 lines)
└── Models/                                      # Existing models (unchanged)
    ├── SuccessfulDiscovery.cs
    ├── PredictedPattern.cs
    ├── AddressProbability.cs
    ├── ValidationResult.cs
    ├── PredictionModelStats.cs
    ├── GenrePatternProfile.cs
    ├── EnginePatternProfile.cs
    ├── PatternFrequency.cs
    ├── EngineMemoryPattern.cs
    ├── EnginePatternDatabase.cs
    └── StatisticalPatternValidator.cs
```

---

## Key Challenges and Edge Cases

### 1. Thread Safety Migration

**Challenge:** The ReaderWriterLockSlim is currently in the main class.

**Solution:** Move lock into `DiscoveryRepositoryManager`:
```csharp
public sealed class DiscoveryRepositoryManager
{
    private readonly List<SuccessfulDiscovery> _discoveries;
    private readonly ReaderWriterLockSlim _lock;

    public Result RecordDiscovery(SuccessfulDiscovery discovery)
    {
        _lock.EnterWriteLock();
        try
        {
            _discoveries.Add(discovery);
            return Result.Success();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    // ... other methods with appropriate locks
}
```

---

### 2. Cross-Manager Data Dependencies

**Challenge:** `GetLikelyAddresses` needs discoveries filtered by genre AND pattern type.

**Solution:** Pass filtered data as parameter:
```csharp
// In coordinator
var genreDiscoveries = _discoveryRepository.GetByGenre(genre);
return _genreProfileManager.GetLikelyAddresses(genre, patternType, engine, genreDiscoveries);

// In GenreProfileManager
public List<AddressProbability> GetLikelyAddresses(
    GameGenre genre,
    string patternType,
    GameEngine? engine,
    IReadOnlyList<SuccessfulDiscovery> genreDiscoveries)
{
    var query = genreDiscoveries
        .Where(d => d.PatternType.Equals(patternType, StringComparison.OrdinalIgnoreCase));
    // ... rest of logic
}
```

---

### 3. Profile Initialization Order

**Challenge:** Profiles need to be initialized before use.

**Solution:** Initialize in manager constructors:
```csharp
public GenreProfileManager()
{
    _genreProfiles = new Dictionary<GameGenre, GenrePatternProfile>();
    InitializeProfiles();
}

private void InitializeProfiles()
{
    foreach (GameGenre genre in Enum.GetValues<GameGenre>())
    {
        _genreProfiles[genre] = new GenrePatternProfile { Genre = genre };
    }
}
```

---

### 4. Process Memory Access in AddressCalculator

**Challenge:** `CalculateRelativeAddress` accesses process modules.

**Solution:** Isolate in dedicated calculator with proper error handling:
```csharp
public sealed class PatternAddressCalculator
{
    public long CalculateRelativeAddress(long absoluteAddress, int processId, string moduleName)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return absoluteAddress - (long)module.BaseAddress;
                }
            }
        }
        catch
        {
            // Fall back to absolute address
        }
        return absoluteAddress;
    }
}
```

---

### 5. DI Registration Complexity

**Challenge:** 5 new managers need registration.

**Solution:** Add to DI container in infrastructure layer:
```csharp
// In Infrastructure Service Registration
services.AddSingleton<DiscoveryRepositoryManager>();
services.AddSingleton<GenreProfileManager>();
services.AddSingleton<EngineProfileManager>();
services.AddSingleton<PredictionEngineManager>();
services.AddSingleton<PatternAddressCalculator>();
// PatternPredictionModel already registered as singleton
```

---

## Migration Steps

1. **Create Manager Classes** (one at a time)
   - Start with `DiscoveryRepositoryManager` (lowest dependency)
   - Move relevant private methods and state
   - Add unit tests for each manager

2. **Update PatternPredictionModel**
   - Inject managers via constructor
   - Replace private method calls with manager calls
   - Remove moved code

3. **Move Helper Classes**
   - Relocate nested classes to separate files in Models folder

4. **Update Tests**
   - Create unit tests for each manager
   - Update integration tests for coordinator

5. **Verify Behavior**
   - Run existing test suite
   - Validate prediction accuracy unchanged
   - Verify thread safety maintained

---

## Estimated Effort

| Task | Estimated Time |
|------|----------------|
| Create DiscoveryRepositoryManager | 2 hours |
| Create GenreProfileManager | 2 hours |
| Create EngineProfileManager | 2 hours |
| Create PredictionEngineManager | 3 hours |
| Create PatternAddressCalculator | 1 hour |
| Refactor PatternPredictionModel | 2 hours |
| Update Unit Tests | 3 hours |
| Integration Testing | 2 hours |
| **Total** | **17 hours** |

---

## Success Criteria

- [ ] PatternPredictionModel under 200 lines
- [ ] All managers under 250 lines each
- [ ] Existing tests pass without modification
- [ ] New manager unit tests achieve 80%+ coverage
- [ ] No regression in prediction accuracy
- [ ] Thread safety maintained
- [ ] Build succeeds with 0 warnings
