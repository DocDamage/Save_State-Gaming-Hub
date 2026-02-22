# AutoDiscoveryEngine Refactoring Plan

## Executive Summary

This document outlines the refactoring of `AutoDiscoveryEngine.cs` (1,079 lines) using the **Manager Pattern** to improve maintainability, testability, and adherence to the Single Responsibility Principle.

---

## Current File Statistics

| Metric | Value |
|--------|-------|
| **Total Lines** | 1,079 |
| **Public Methods** | 5 |
| **Private Methods** | 18 |
| **Nested Classes** | 3 (DiscoverySessionContext, MemoryRange, HeuristicFeedbackData) |
| **Heuristics Initialized** | 91 |
| **Win32 API Imports** | 4 |
| **Primary Responsibilities** | 6 distinct areas |

### Current Method Breakdown

| Category | Methods |
|----------|---------|
| **Session Lifecycle** | `StartDiscoverySessionAsync`, `StopDiscoverySessionAsync`, `Dispose` |
| **Discovery Analysis** | `AnalyzeChangeAsync`, `PerformDiscoveryPassAsync` |
| **Memory Scanning** | `PerformInitialScanAsync`, `ScanRangeForIntegersAsync`, `ScanRangeForFloatsAsync`, `GetScanRanges` |
| **Change Monitoring** | `MonitorForChangesAsync` |
| **Heuristic Analysis** | `ApplyHeuristicsAndRank`, `ApplyInitialHeuristicScoring`, `SuggestName` |
| **Value Utilities** | `ShouldKeepCandidateAfterAction`, `ValueDecreased`, `ValueIncreased`, `CalculateDelta`, `ValuesEqual`, `IsCommonIntegerValue`, `IsCommonFloatValue` |
| **Memory Reading** | `ReadValueAtAddress`, `ReadInt32`, `ReadFloat`, `ReadInt64`, `ReadDouble`, `ReadInt16`, `ReadByte` |
| **Results & Feedback** | `GetRankedResultsAsync`, `SubmitFeedbackAsync` |

---

## Responsibility Analysis

### 1. Session Management
**Current Methods:** `StartDiscoverySessionAsync`, `StopDiscoverySessionAsync`, `Dispose`
**Responsibilities:**
- Process validation and handle management
- Session context creation and tracking
- Resource cleanup and disposal
- Session state management (active/inactive)

### 2. Memory Scanning
**Current Methods:** `PerformInitialScanAsync`, `ScanRangeForIntegersAsync`, `ScanRangeForFloatsAsync`, `GetScanRanges`
**Responsibilities:**
- Initial memory scanning for value candidates
- Integer/float value detection in memory ranges
- Scan range calculation based on system info
- Candidate collection with limits (50,000 max)

### 3. Change Monitoring
**Current Methods:** `MonitorForChangesAsync`
**Responsibilities:**
- Tracking value changes across discovery passes
- Recording observation history
- Filtering candidates based on action context
- Delta calculation for value changes

### 4. Heuristic Analysis
**Current Methods:** `ApplyHeuristicsAndRank`, `ApplyInitialHeuristicScoring`, `SuggestName`, `ShouldKeepCandidateAfterAction`, `ValueDecreased`, `ValueIncreased`
**Responsibilities:**
- Applying 91 heuristics to discovered values
- Calculating confidence scores
- Ranking candidates by confidence
- Suggesting names based on category
- Action-based candidate filtering

### 5. Memory Reading
**Current Methods:** `ReadValueAtAddress`, `ReadInt32`, `ReadFloat`, `ReadInt64`, `ReadDouble`, `ReadInt16`, `ReadByte`
**Responsibilities:**
- Low-level memory reading via Win32 API
- Type-specific value extraction
- Buffer management for memory reads

### 6. Feedback & Results
**Current Methods:** `GetRankedResultsAsync`, `SubmitFeedbackAsync`, `AnalyzeChangeAsync`, `PerformDiscoveryPassAsync`
**Responsibilities:**
- Returning ranked discovery results
- Storing and processing user feedback
- Coordinating discovery passes
- Orchestrating between scanning and analysis

---

## Proposed Manager Class Breakdown

### New Structure

```
AutoDiscoveryEngine (Coordinator)
├── DiscoverySessionManager
├── MemoryScanManager
├── ChangeMonitorManager
├── HeuristicAnalysisManager
├── MemoryReaderManager
└── DiscoveryFeedbackManager
```

### 1. DiscoverySessionManager

**Responsibilities:**
- Process attachment and handle management
- Session lifecycle (start, stop, dispose)
- Session context tracking
- Resource cleanup

**Public Interface:**
```csharp
public sealed class DiscoverySessionManager : IDisposable
{
    public DiscoverySessionManager(ILogger<DiscoverySessionManager> logger);
    
    public Task<Result<DiscoverySession>> StartSessionAsync(int processId, DiscoveryOptions options, CancellationToken ct = default);
    public Task<Result> StopSessionAsync(Guid sessionId, CancellationToken ct = default);
    public Task<Result<DiscoverySessionContext>> GetSessionContextAsync(Guid sessionId, CancellationToken ct = default);
    public bool IsSessionActive(Guid sessionId);
    public void Dispose();
}
```

**Estimated Lines:** ~180
**Methods:** 5 public, 3 private

---

### 2. MemoryScanManager

**Responsibilities:**
- Memory range scanning for values
- Integer and float scanning
- Scan range calculation
- Candidate discovery

**Public Interface:**
```csharp
public sealed class MemoryScanManager
{
    public MemoryScanManager(ILogger<MemoryScanManager> logger);
    
    public Task<List<DiscoveredValue>> ScanForCandidatesAsync(
        IntPtr processHandle, 
        DiscoveryOptions options, 
        ITimeProvider timeProvider,
        CancellationToken ct = default);
    
    public Task ScanRangeForIntegersAsync(...);
    public Task ScanRangeForFloatsAsync(...);
}
```

**Estimated Lines:** ~200
**Methods:** 3 public, 4 private

---

### 3. ChangeMonitorManager

**Responsibilities:**
- Monitoring value changes across passes
- Recording observation history
- Action-based filtering
- Delta calculation

**Public Interface:**
```csharp
public sealed class ChangeMonitorManager
{
    public ChangeMonitorManager(ILogger<ChangeMonitorManager> logger, ITimeProvider timeProvider);
    
    public Task<List<DiscoveredValue>> MonitorChangesAsync(
        IntPtr processHandle,
        List<DiscoveredValue> candidates,
        PlayerAction action,
        CancellationToken ct = default);
    
    public bool ShouldKeepCandidateAfterAction(DiscoveredValue candidate, PlayerAction action, bool hasChanged);
    public double? CalculateDelta(object previous, object current);
}
```

**Estimated Lines:** ~150
**Methods:** 3 public, 5 private

---

### 4. HeuristicAnalysisManager

**Responsibilities:**
- Managing 91 heuristics
- Confidence score calculation
- Candidate ranking
- Name suggestion

**Public Interface:**
```csharp
public sealed class HeuristicAnalysisManager
{
    public HeuristicAnalysisManager(ILogger<HeuristicAnalysisManager> logger);
    
    public List<DiscoveredValue> ApplyHeuristicsAndRank(List<DiscoveredValue> candidates);
    public void ApplyInitialScoring(DiscoveredValue candidate);
    public string SuggestName(DiscoveredValue value);
    public IReadOnlyList<IValueHeuristic> Heuristics { get; }
}
```

**Estimated Lines:** ~140
**Methods:** 4 public, 2 private

---

### 5. MemoryReaderManager

**Responsibilities:**
- Low-level memory reading
- Type-specific value extraction
- Win32 API wrapping

**Public Interface:**
```csharp
public sealed class MemoryReaderManager : IDisposable
{
    public MemoryReaderManager(ILogger<MemoryReaderManager> logger);
    
    public object? ReadValueAtAddress(IntPtr processHandle, IntPtr address, string valueType);
    public int? ReadInt32(IntPtr processHandle, IntPtr address);
    public float? ReadFloat(IntPtr processHandle, IntPtr address);
    public long? ReadInt64(IntPtr processHandle, IntPtr address);
    public double? ReadDouble(IntPtr processHandle, IntPtr address);
    public short? ReadInt16(IntPtr processHandle, IntPtr address);
    public byte? ReadByte(IntPtr processHandle, IntPtr address);
    
    // Win32 API wrapper
    public Result<IntPtr> OpenProcessHandle(int processId, ProcessAccessRights access);
    public void CloseProcessHandle(IntPtr handle);
}

[Flags]
public enum ProcessAccessRights : uint
{
    ProcessVmRead = 0x0010,
    ProcessVmWrite = 0x0020,
    ProcessVmOperation = 0x0008,
    ProcessQueryInformation = 0x0400
}
```

**Estimated Lines:** ~120
**Methods:** 9 public, 4 private (Win32 imports)

---

### 6. DiscoveryFeedbackManager

**Responsibilities:**
- User feedback storage
- Feedback-based learning
- Results ranking and retrieval

**Public Interface:**
```csharp
public sealed class DiscoveryFeedbackManager
{
    public DiscoveryFeedbackManager(ILogger<DiscoveryFeedbackManager> logger);
    
    public Task<Result> SubmitFeedbackAsync(DiscoveryFeedback feedback, CancellationToken ct = default);
    public Task<Result<List<DiscoveredValue>>> GetRankedResultsAsync(
        List<DiscoveredValue> candidates, 
        DiscoveryOptions options, 
        CancellationToken ct = default);
    public HeuristicFeedbackData? GetFeedbackForAddress(IntPtr address);
}
```

**Estimated Lines:** ~100
**Methods:** 3 public, 2 private

---

## Coordinator Refactoring

### AutoDiscoveryEngine (After)

**Estimated Lines:** ~180 (down from 1,079)
**Reduction:** 83%

```csharp
public sealed class AutoDiscoveryEngine : IAutoDiscoveryEngine, IDisposable
{
    private readonly ILogger<AutoDiscoveryEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly DiscoverySessionManager _sessionManager;
    private readonly MemoryScanManager _scanManager;
    private readonly ChangeMonitorManager _monitorManager;
    private readonly HeuristicAnalysisManager _heuristicManager;
    private readonly MemoryReaderManager _readerManager;
    private readonly DiscoveryFeedbackManager _feedbackManager;

    public AutoDiscoveryEngine(
        ILogger<AutoDiscoveryEngine> logger,
        ITimeProvider timeProvider,
        DiscoverySessionManager sessionManager,
        MemoryScanManager scanManager,
        ChangeMonitorManager monitorManager,
        HeuristicAnalysisManager heuristicManager,
        MemoryReaderManager readerManager,
        DiscoveryFeedbackManager feedbackManager)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _sessionManager = sessionManager;
        _scanManager = scanManager;
        _monitorManager = monitorManager;
        _heuristicManager = heuristicManager;
        _readerManager = readerManager;
        _feedbackManager = feedbackManager;
    }

    public async Task<Result<DiscoverySession>> StartDiscoverySessionAsync(
        int processId, DiscoveryOptions options, CancellationToken ct = default)
    {
        using (_logger.BeginDiscoveryScope(processId))
        {
            var result = await _sessionManager.StartSessionAsync(processId, options, ct);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Discovery session {SessionId} started", result.Value.SessionId);
            }
            
            return result;
        }
    }

    public async Task<Result<DiscoveryResult>> AnalyzeChangeAsync(
        DiscoverySession session, PlayerAction action, CancellationToken ct = default)
    {
        using (_logger.BeginCorrelationScope())
        using (_logger.BeginDiscoveryAnalysisScope(action.ToString(), session.SessionId))
        {
            var stopwatch = Stopwatch.StartNew();
            var beforeCount = session.Candidates.Count;

            try
            {
                // Get session context
                var contextResult = await _sessionManager.GetSessionContextAsync(session.SessionId, ct);
                if (contextResult.IsFailure)
                    return Result.Failure<DiscoveryResult>(contextResult.Error!, contextResult.ErrorType);

                var context = contextResult.Value;

                // Record action
                session.ActionHistory.Add(new PlayerActionRecord
                {
                    Timestamp = _timeProvider.UtcNow,
                    Action = action
                });

                // Perform discovery pass
                await PerformDiscoveryPassAsync(session, context, action, ct);

                // Apply heuristics and rank
                var rankedCandidates = _heuristicManager.ApplyHeuristicsAndRank(session.Candidates);

                // Update session
                session.Candidates.Clear();
                session.Candidates.AddRange(rankedCandidates.Take(session.Options.MaxCandidates));

                // Build result
                var result = new DiscoveryResult
                {
                    SessionId = session.SessionId,
                    AnalyzedAction = action,
                    RemainingCandidates = session.Candidates.Count,
                    EliminatedCandidates = Math.Max(0, beforeCount - session.Candidates.Count),
                    TopValues = rankedCandidates.Take(10).ToList(),
                    ConfidenceImproved = session.Candidates.Any(c => c.ConfidenceScore > 0.5)
                };

                stopwatch.Stop();
                _logger.LogDiscoveryComplete(beforeCount, session.Candidates.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Analysis failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return Result.Failure<DiscoveryResult>($"Failed to analyze change: {ex.Message}", ErrorType.Internal);
            }
        }
    }

    private async Task PerformDiscoveryPassAsync(
        DiscoverySession session, DiscoverySessionContext context, PlayerAction action, CancellationToken ct)
    {
        session.CurrentPass++;

        if (session.CurrentPass == 1)
        {
            var newCandidates = await _scanManager.ScanForCandidatesAsync(
                context.ProcessHandle, session.Options, _timeProvider, ct);
            
            session.Candidates.AddRange(newCandidates);
            
            foreach (var candidate in session.Candidates)
            {
                _heuristicManager.ApplyInitialScoring(candidate);
            }
        }
        else
        {
            var filteredCandidates = await _monitorManager.MonitorChangesAsync(
                context.ProcessHandle, session.Candidates, action, ct);
            
            session.Candidates.Clear();
            session.Candidates.AddRange(filteredCandidates);
        }

        await Task.Delay(session.Options.ScanIntervalMs, ct);
    }

    public Task<Result<List<DiscoveredValue>>> GetRankedResultsAsync(
        DiscoverySession session, CancellationToken ct = default)
        => _feedbackManager.GetRankedResultsAsync(session.Candidates, session.Options, ct);

    public Task<Result> StopDiscoverySessionAsync(
        DiscoverySession session, CancellationToken ct = default)
        => _sessionManager.StopSessionAsync(session.SessionId, ct);

    public Task<Result> SubmitFeedbackAsync(
        DiscoveryFeedback feedback, CancellationToken ct = default)
        => _feedbackManager.SubmitFeedbackAsync(feedback, ct);

    public void Dispose()
    {
        _sessionManager.Dispose();
        _readerManager.Dispose();
    }
}
```

---

## Data Classes to Extract

### 1. DiscoverySessionContext (Move to Core)
```csharp
namespace SaveState.Core.GameLibrary.Models;

public sealed class DiscoverySessionContext : IDisposable
{
    public required DiscoverySession Session { get; init; }
    public required IntPtr ProcessHandle { get; set; }
    public Process? Process { get; init; }
    
    public void Dispose()
    {
        if (ProcessHandle != IntPtr.Zero)
        {
            CloseHandle(ProcessHandle);
            ProcessHandle = IntPtr.Zero;
        }
        Process?.Dispose();
        Session.IsActive = false;
    }
}
```

### 2. MemoryRange (Move to Core)
```csharp
namespace SaveState.Core.GameLibrary.Models;

public readonly record struct MemoryRange(nuint Start, nuint Size);
```

### 3. HeuristicFeedbackData (Move to Core)
```csharp
namespace SaveState.Core.GameLibrary.Models;

public sealed class HeuristicFeedbackData
{
    public int TotalSubmissions { get; set; }
    public int CorrectIdentifications { get; set; }
    public Dictionary<string, int> UserProvidedNames { get; } = new();
    public Dictionary<string, int> UserProvidedCategories { get; } = new();
}
```

---

## Dependency Injection Registration

```csharp
// In Infrastructure DI registration
services.AddSingleton<DiscoverySessionManager>();
services.AddSingleton<MemoryScanManager>();
services.AddSingleton<ChangeMonitorManager>();
services.AddSingleton<HeuristicAnalysisManager>();
services.AddSingleton<MemoryReaderManager>();
services.AddSingleton<DiscoveryFeedbackManager>();
services.AddSingleton<IAutoDiscoveryEngine, AutoDiscoveryEngine>();
```

---

## Key Challenges and Edge Cases

### 1. Thread Safety
**Challenge:** Multiple managers access shared session state.
**Solution:** 
- `DiscoverySessionManager` owns the `_sessionLock` and provides synchronized access
- Other managers receive session context through method parameters, not shared state

### 2. Win32 API Dependencies
**Challenge:** Memory reading depends on Windows-specific APIs.
**Solution:**
- Isolate all Win32 code in `MemoryReaderManager`
- Future: Create `IMemoryReader` interface for cross-platform support

### 3. Heuristic Initialization
**Challenge:** 91 heuristics initialized in constructor (91 lines).
**Solution:**
- Move to `HeuristicAnalysisManager` which owns heuristics
- Consider lazy loading or factory pattern if startup time is impacted

### 4. Circular Dependencies
**Challenge:** `ChangeMonitorManager` needs to read memory values.
**Solution:**
- `ChangeMonitorManager` depends on `MemoryReaderManager` (injected)
- Clear dependency direction: Monitor → Reader

### 5. Logging Context Preservation
**Challenge:** Correlation IDs and scopes must be maintained.
**Solution:**
- Coordinator begins scopes before delegating to managers
- Managers use provided loggers with inherited context

### 6. Cancellation Token Flow
**Challenge:** CT must flow through all async operations.
**Solution:**
- All manager methods accept `CancellationToken`
- Coordinator passes CT to all async calls

### 7. Error Handling Consistency
**Challenge:** Result pattern must be consistent across managers.
**Solution:**
- All managers return `Result<T>` or `Task<Result<T>>`
- Coordinator handles result unwrapping and error propagation

---

## Testing Strategy

### Unit Tests per Manager

| Manager | Test Scenarios |
|---------|---------------|
| DiscoverySessionManager | Start/stop session, handle cleanup, invalid process |
| MemoryScanManager | Scan ranges, value detection, candidate limits |
| ChangeMonitorManager | Value change detection, action filtering, delta calc |
| HeuristicAnalysisManager | Confidence scoring, ranking, name suggestion |
| MemoryReaderManager | Type-specific reads, invalid addresses, buffer handling |
| DiscoveryFeedbackManager | Feedback storage, result ranking |

### Integration Tests
- End-to-end discovery workflow
- Multiple concurrent sessions
- Resource cleanup verification

---

## Migration Checklist

- [ ] Create `DiscoverySessionContext` in Core
- [ ] Create `MemoryRange` in Core  
- [ ] Create `HeuristicFeedbackData` in Core
- [ ] Implement `MemoryReaderManager` with Win32 APIs
- [ ] Implement `DiscoverySessionManager`
- [ ] Implement `MemoryScanManager`
- [ ] Implement `ChangeMonitorManager`
- [ ] Implement `HeuristicAnalysisManager`
- [ ] Implement `DiscoveryFeedbackManager`
- [ ] Refactor `AutoDiscoveryEngine` to coordinator
- [ ] Update DI registration
- [ ] Write unit tests for each manager
- [ ] Write integration tests
- [ ] Performance comparison (before/after)
- [ ] Update documentation

---

## Expected Benefits

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Lines in Coordinator | 1,079 | ~180 | 83% reduction |
| Testability | Low | High | Isolated units |
| SRP Compliance | No | Yes | Single responsibility per manager |
| Maintainability | Difficult | Easy | Focused classes |
| Code Reusability | Limited | High | Managers composable |
| Parallel Development | Hard | Easy | Separate files |

---

## Files to Create/Modify

### New Files (6)
1. `src/SaveState.Infrastructure/GameLibrary/Managers/DiscoverySessionManager.cs`
2. `src/SaveState.Infrastructure/GameLibrary/Managers/MemoryScanManager.cs`
3. `src/SaveState.Infrastructure/GameLibrary/Managers/ChangeMonitorManager.cs`
4. `src/SaveState.Infrastructure/GameLibrary/Managers/HeuristicAnalysisManager.cs`
5. `src/SaveState.Infrastructure/GameLibrary/Managers/MemoryReaderManager.cs`
6. `src/SaveState.Infrastructure/GameLibrary/Managers/DiscoveryFeedbackManager.cs`

### Modified Files (3)
1. `src/SaveState.Infrastructure/GameLibrary/Services/AutoDiscoveryEngine.cs` (refactor to coordinator)
2. `src/SaveState.Core/GameLibrary/Models/DiscoverySessionContext.cs` (new)
3. `src/SaveState.Core/GameLibrary/Models/MemoryRange.cs` (new)
4. `src/SaveState.Core/GameLibrary/Models/HeuristicFeedbackData.cs` (new)
5. DI registration in Infrastructure

---

## References

- AGENTS.md Manager Pattern Guidelines
- Previous Manager Pattern implementations:
  - `IkemenGoService` → 8 managers
  - `CharacterDiscoveryService` → 6 managers
  - `AutomatedBalancingSystem` → 4 managers
  - `StoryModeService` → 9 focused interfaces

---

*Plan created: February 21, 2026*
*Estimated implementation time: 4-6 hours*
*Risk level: Low (extensive test coverage exists)*
