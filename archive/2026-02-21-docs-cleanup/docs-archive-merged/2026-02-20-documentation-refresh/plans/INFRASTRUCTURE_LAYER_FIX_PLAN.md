# Infrastructure Layer Fix Plan

**Date:** February 13, 2026  
**Status:** COMPLETE - PHASE 3 VERIFICATION (Infrastructure + Solution Green)  
**Original Errors:** 386  
**Target:** 0 Errors  
**Estimated Time:** 6-8 hours  
**Priority:** High

---

## Executive Summary

This plan addresses the 386 compilation errors in the `SaveState.Infrastructure` project. The errors fall into distinct patterns that can be systematically fixed. The primary issues are:

1. **Result Pattern Mismatches** (45% of errors) - Non-generic `Result` being used where `Result<T>` is expected
2. **Collection Type Conversions** (25% of errors) - `List<T>` to `IReadOnlyList<T>` conversion failures
3. **Missing Properties/Members** (20% of errors) - Entities missing expected properties
4. **Guard Accessibility** (5% of errors) - `Guard` class protection level issues
5. **Miscellaneous** (5% of errors) - Various type mismatches

## Progress Update (February 14, 2026)

- [x] SaveState.Infrastructure builds cleanly (0 warnings, 0 errors).
- [x] SaveState.Infrastructure.Tests are green (317 passed, 29 skipped, 0 failed).
- [x] Full solution builds cleanly (0 warnings, 0 errors) after resolving SaveState.Core.Tests compile blockers and warning cleanup.
- [x] Removed temporary `CA1873`/`CA5351` suppressions in plugin projects by fixing logger argument-evaluation warnings and replacing MD5 hashing with SHA256.

---

## Error Analysis by Service

| Service | Error Count | Primary Issue | Complexity |
|---------|-------------|---------------|------------|
| Emulation/Orchestration | 48 | Result<T> conversions | Medium |
| BackupArchive | 42 | Result<T> + IReadOnlyList<T> | Medium |
| Automation/Studio | 42 | Result<T> conversions | Medium |
| Social/Netplay | 106 | Missing properties, Result<T> | High |
| Intelligence | 82 | Missing properties, Result<T> | High |
| Social/Streaming | 28 | Missing properties | Low |
| Other | 38 | Various | Low |

---

## Phase 1: Result Pattern Fixes (Critical Path)

### 1.1 Understanding the Result Pattern

**âŒ INCORRECT - Returns non-generic Result:**
```csharp
// Error: Cannot convert Result to Result<WorkflowExecutionContext>
public Task<Result<WorkflowExecutionContext>> ExecuteAsync(...)
{
    // ...
    return Result.Failure("Cancelled", ErrorType.Cancelled); // âŒ Wrong
}
```

**âœ… CORRECT - Returns generic Result<T>:**
```csharp
// Option 1: Use Result<T>.Failure()
public Task<Result<WorkflowExecutionContext>> ExecuteAsync(...)
{
    // ...
    return Task.FromResult(Result<WorkflowExecutionContext>.Failure(
        "Cancelled", ErrorType.Cancelled)); // âœ… Correct
}

// Option 2: Use .ToResult<T>() for propagation
public Task<Result<WorkflowExecutionContext>> ExecuteAsync(...)
{
    var validationResult = ValidateWorkflow(workflow);
    if (validationResult.IsFailure)
    {
        return Task.FromResult(validationResult.ToResult<WorkflowExecutionContext>()); // âœ… Correct
    }
    // ...
}
```

### 1.2 Common Fix Patterns

**Pattern A: Failure Case Returns**
```csharp
// BEFORE (Error CS0266):
return Result.Failure("Not found", ErrorType.NotFound);

// AFTER (Fixed):
return Result<Workflow>.Failure("Not found", ErrorType.NotFound);
```

**Pattern B: Success Case with Value**
```csharp
// BEFORE (Error CS0266):
return Result.Success(executionContext);

// AFTER (Fixed):
return Result<WorkflowExecutionContext>.Success(executionContext);
```

**Pattern C: Exception Handler Returns**
```csharp
// BEFORE (Error CS0266):
catch (Exception ex)
{
    return Result.Failure($"Failed: {ex.Message}", ErrorType.Internal);
}

// AFTER (Fixed):
catch (Exception ex)
{
    return Result<WorkflowExecutionContext>.Failure(
        $"Failed: {ex.Message}", ErrorType.Internal);
}
```

### 1.3 Files Requiring Result Pattern Fixes

```bash
# Automation/Studio
src/SaveState.Infrastructure/Automation/Studio/WorkflowEngine.cs
src/SaveState.Infrastructure/Automation/Studio/AutomationStudioService.cs

# BackupArchive
src/SaveState.Infrastructure/BackupArchive/BackupArchiveService.cs

# Emulation/Orchestration
src/SaveState.Infrastructure/Emulation/Orchestration/EmulationOrchestrator.cs
src/SaveState.Infrastructure/Emulation/Orchestration/ProfileManager.cs

# Social/Netplay
src/SaveState.Infrastructure/Social/Netplay/MatchmakingEngine.cs
src/SaveState.Infrastructure/Social/Netplay/RollbackNetcodeService.cs
src/SaveState.Infrastructure/Social/Netplay/RelayService.cs
```

---

## Phase 2: Collection Type Conversions

### 2.1 IReadOnlyList<T> Conversion Pattern

**âŒ INCORRECT:**
```csharp
// Error: Cannot convert Result<List<T>> to Result<IReadOnlyList<T>>
public Task<Result<IReadOnlyList<WorkflowExecution>>> GetExecutionsAsync(...)
{
    var list = new List<WorkflowExecution>();
    // ... populate list
    return Task.FromResult(Result<List<WorkflowExecution>>.Success(list)); // âŒ Wrong
}
```

**âœ… CORRECT:**
```csharp
// Option 1: Use interface type in Result
public Task<Result<IReadOnlyList<WorkflowExecution>>> GetExecutionsAsync(...)
{
    var list = new List<WorkflowExecution>();
    // ... populate list
    return Task.FromResult(Result<IReadOnlyList<WorkflowExecution>>.Success(list)); // âœ… Correct
}

// Option 2: Cast the list
public Task<Result<IReadOnlyList<WorkflowExecution>>> GetExecutionsAsync(...)
{
    var list = new List<WorkflowExecution>();
    // ... populate list
    return Task.FromResult(Result<IReadOnlyList<WorkflowExecution>>.Success(
        list.AsReadOnly())); // âœ… Also correct
}
```

### 2.2 Common Fix Locations

```csharp
// In AutomationStudioService.cs (line ~366)
// BEFORE:
return Task.FromResult(Result<List<WorkflowExecution>>.Success(history));

// AFTER:
return Task.FromResult(Result<IReadOnlyList<WorkflowExecution>>.Success(history));

// In BackupArchiveService.cs (line ~166)
// BEFORE:
return Task.FromResult(Result<List<BackupJob>>.Success(_jobs.Values.ToList()));

// AFTER:
return Task.FromResult(Result<IReadOnlyList<BackupJob>>.Success(_jobs.Values.ToList()));
```

---

## Phase 3: Missing Properties & Members

### 3.1 Game Entity - Missing `EstimatedTimeToComplete`

**File:** `src/SaveState.Core/GameLibrary/Entities/Game.cs`

```csharp
// Add to Game entity:
public TimeSpan? EstimatedTimeToComplete { get; private set; }

// Add setter method:
public void SetEstimatedTimeToComplete(TimeSpan? value)
{
    EstimatedTimeToComplete = value;
}
```

### 3.2 GameSession Entity - Missing `StartTime`

**File:** `src/SaveState.Core/GameLibrary/Entities/GameSession.cs`

```csharp
// Check if StartTime exists, if not add it:
public DateTime StartTime { get; private set; }

// Or if it exists with different name, add alias property:
public DateTime StartTime => StartedAt; // If StartedAt exists
```

### 3.3 RomFile Entity - Missing `Hash`

**File:** `src/SaveState.Core/Emulation/Entities/RomFile.cs` (or similar)

```csharp
// Add property:
public string Hash { get; private set; } = string.Empty;

// In constructor or factory method:
public RomFile(string path, string hash /* ... */)
{
    // ...
    Hash = hash;
}
```

### 3.4 IGameSessionRepository - Missing `GetByUserIdAsync`

**File:** `src/SaveState.Core/GameLibrary/Repositories/IGameSessionRepository.cs`

```csharp
// Add method to interface:
Task<IReadOnlyList<GameSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
```

**Implementation in:** `src/SaveState.Infrastructure/Persistence/Repositories/GameSessionRepository.cs`

```csharp
public async Task<IReadOnlyList<GameSession>> GetByUserIdAsync(
    Guid userId, CancellationToken ct = default)
{
    return await _context.GameSessions
        .Where(s => s.UserId == userId)
        .OrderByDescending(s => s.StartedAt)
        .ToListAsync(ct);
}
```

---

## Phase 4: Guard Accessibility Fix

### 4.1 Issue Analysis

The `Guard` class is showing as inaccessible (CS0122). This typically means:
1. The class is `internal` but needs to be `public`
2. The class is in a different assembly not referenced properly

### 4.2 Fix Location

**File:** `src/SaveState.Core/Common/Guard.cs` (or create if missing)

```csharp
// Ensure the class is PUBLIC:
namespace SaveState.Core.Common;

/// <summary>
/// Guard clauses for argument validation.
/// </summary>
public static class Guard
{
    public static void AgainstNull(object? argument, string argumentName)
    {
        if (argument is null)
            throw new ArgumentNullException(argumentName);
    }

    public static void AgainstNullOrEmpty(string? argument, string argumentName)
    {
        if (string.IsNullOrEmpty(argument))
            throw new ArgumentException("Value cannot be null or empty.", argumentName);
    }

    public static void AgainstDefault<T>(T argument, string argumentName) where T : struct
    {
        if (argument.Equals(default(T)))
            throw new ArgumentException("Value cannot be default.", argumentName);
    }
}
```

---

## Phase 5: ActionResult Parameter Mismatch

### 5.1 Issue Description

In `WorkflowEngine.cs` line 114, there's a parameter type mismatch when creating `ActionResult`.

### 5.2 Fix

```csharp
// BEFORE (Error CS1503):
var resultRecord = new ActionResult(
    ActionId: action.Id,
    Type: action.Type,
    Status: actionResult.IsSuccess ? ActionStatus.Completed : ActionStatus.Failed,
    Duration: TimeSpan.FromMilliseconds(100),
    Output: actionResult.IsSuccess ? actionResult.Value : null,  // âŒ actionResult.Value might not be Dictionary
    ErrorMessage: actionResult.IsFailure ? actionResult.Error : null);

// AFTER (Fixed):
// Check the actual type of actionResult.Value
object? outputValue = null;
if (actionResult.IsSuccess && actionResult.Value is not null)
{
    // If Value is already a Dictionary, use it
    if (actionResult.Value is Dictionary<string, object> dict)
    {
        outputValue = dict;
    }
    else
    {
        // Wrap non-dictionary values
        outputValue = new Dictionary<string, object> { ["result"] = actionResult.Value };
    }
}

var resultRecord = new ActionResult(
    ActionId: action.Id,
    Type: action.Type,
    Status: actionResult.IsSuccess ? ActionStatus.Completed : ActionStatus.Failed,
    Duration: TimeSpan.FromMilliseconds(100),
    Output: outputValue as Dictionary<string, object>,
    ErrorMessage: actionResult.IsFailure ? actionResult.Error : null);
```

---

## Phase 6: Miscellaneous Type Fixes

### 6.1 TimeSpan Operator Issues

**Error:** `Operator '?' cannot be applied to operand of type 'TimeSpan'`

**Fix:** TimeSpan is a value type (struct), so use `TimeSpan?` for nullable:

```csharp
// BEFORE:
TimeSpan duration = GetDuration();
var nullable = duration?.TotalHours; // âŒ Error

// AFTER:
TimeSpan? duration = GetDurationNullable();
var nullable = duration?.TotalHours; // âœ… Correct
// OR
TimeSpan duration = GetDuration();
var totalHours = duration.TotalHours; // âœ… Correct (no null check needed)
```

### 6.2 Nullable Reference Type Fixes

```csharp
// BEFORE:
public Task<Result<string>> GetDescriptionAsync()
{
    string description = null; // âŒ Warning/error
    return Task.FromResult(Result<string>.Success(description));
}

// AFTER:
public Task<Result<string>> GetDescriptionAsync()
{
    string? description = null; // âœ… Correct
    if (description is null)
        return Task.FromResult(Result<string>.Failure("Not found"));
    return Task.FromResult(Result<string>.Success(description));
}
```

---

## Implementation Order

### Week 1: Core Pattern Fixes (Days 1-3)
1. **Day 1:** Fix all `Result<T>` conversions in Automation/Studio
2. **Day 2:** Fix all `Result<T>` conversions in BackupArchive and Emulation
3. **Day 3:** Fix all `IReadOnlyList<T>` conversions

### Week 2: Entity and Property Fixes (Days 4-6)
4. **Day 4:** Add missing properties to Core entities (Game, GameSession, RomFile)
5. **Day 5:** Add missing repository methods
6. **Day 6:** Fix Guard accessibility and miscellaneous issues

### Week 3: Verification and Testing (Days 7-8)
7. **Day 7:** Full build verification and regression testing
8. **Day 8:** Code review and documentation updates

---

## Regression Checklist

### Before Each Fix Batch
- [ ] Run `dotnet build` to capture baseline error count
- [ ] Run existing tests to ensure no test failures
- [ ] Create git commit/backup of working state

### After Each Fix Batch
- [ ] Run `dotnet build` to verify errors decreased
- [ ] Run `dotnet test` to verify no new test failures
- [ ] Check for new warnings introduced
- [ ] Verify the fix pattern is consistent

### Final Verification
- [x] SaveState.Infrastructure builds with 0 errors and 0 warnings
- [x] SaveState.Infrastructure.Tests pass
- [x] Full solution builds with 0 errors
- [x] Full solution builds with 0 warnings (or documented exceptions)
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] AGENTS.md updated with new patterns if needed

---

## Build Commands

```bash
# Build Infrastructure project only
dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj

# Build with error count
dotnet build src/SaveState.Infrastructure/SaveState.Infrastructure.csproj 2>&1 | grep ": error" | wc -l

# Build full solution
dotnet build SaveStateReborn.sln

# Run tests
dotnet test tests/SaveState.Infrastructure.Tests/

# Watch mode for rapid iteration
dotnet watch build --project src/SaveState.Infrastructure/SaveState.Infrastructure.csproj
```

---

## Edge Cases and Special Handling

### Edge Case 1: Covariant Return Types
Some methods may need to return a more derived type:

```csharp
// If you have:
public Task<Result<object>> GetDataAsync();

// But need to return a specific type:
public Task<Result<Workflow>> GetWorkflowAsync()
{
    Workflow workflow = GetWorkflow();
    // This works because Result<T> is covariant in some patterns
    return Task.FromResult(Result<Workflow>.Success(workflow));
}
```

### Edge Case 2: Async Method Returns
Be careful with async/await and Result patterns:

```csharp
// âŒ WRONG - Mixing await and Task.FromResult
public async Task<Result<Workflow>> GetWorkflowAsync()
{
    var result = await FetchAsync();
    return Task.FromResult(Result<Workflow>.Success(result)); // âŒ Double wrapping
}

// âœ… CORRECT
public async Task<Result<Workflow>> GetWorkflowAsync()
{
    var result = await FetchAsync();
    return Result<Workflow>.Success(result); // âœ… Direct return
}
```

### Edge Case 3: Null Value Handling
When the value might be null:

```csharp
public Task<Result<WorkflowExecutionContext?>> GetExecutionAsync(string id)
{
    var execution = FindExecution(id);
    if (execution is null)
    {
        // Return success with null value (valid case)
        return Task.FromResult(Result<WorkflowExecutionContext?>.Success(null));
        
        // OR return failure
        // return Task.FromResult(Result<WorkflowExecutionContext?>.Failure("Not found"));
    }
    return Task.FromResult(Result<WorkflowExecutionContext?>.Success(execution));
}
```

---

## Success Criteria

| Metric | Before | After |
|--------|--------|-------|
| Build Errors | 386 | 0 âœ… |
| Build Warnings | ~100 | â‰¤50 (documented) |
| Unit Tests Passing | TBD | 100% |
| Integration Tests | TBD | 100% |
| Code Coverage | TBD | No decrease |

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Breaking API changes | Medium | High | Maintain backward compatibility |
| Test failures | High | Medium | Fix tests or document intentional changes |
| Performance regression | Low | Medium | Benchmark critical paths |
| Merge conflicts | Medium | Low | Coordinate with other developers |

---

## Related Documents

- [MUGEN_LAYER_FIX_PLAN.md](./MUGEN_LAYER_FIX_PLAN.md) - Previous phase
- [AGENTS.md](../../AGENTS.md) - Project guidelines
- [PATTERNS_COOKBOOK.md](../../docs/architecture/PATTERNS_COOKBOOK.md) - Code patterns reference

---

*Plan Status: COMPLETE - PHASE 3 VERIFICATION (Infrastructure + Solution Green)*  
*Last Updated: February 14, 2026*  
*Next Review: After full test suite verification*



