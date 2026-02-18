# ADR 007: Result Pattern for Error Handling

## Status

Accepted - **FULLY IMPLEMENTED** (February 16, 2026)

## Date

December 2025 (Original) | January 2, 2026 (Updated) | February 16, 2026 (Completed)

## Context

Exceptions should be for exceptional cases, not business rule violations. Business errors need structured handling.

## Decision

Use Result pattern with Success/Failure states for business operations.

## Compliance Status (February 16, 2026)

| Rule | Status | Notes |
|------|--------|-------|
| Service methods return Result<T> | ✅ **COMPLETE** | 183 null returns migrated to Result<T> |
| No exceptions for business errors | ✅ Compliant | 0 violations |
| Guard clauses for invalid arguments | ✅ Compliant | 0 violations |
| ErrorType categorization | ✅ Compliant | All errors properly categorized |

## Migration Summary

**Completed February 16, 2026**

| Service/Feature | Null Returns Migrated | Status |
|-----------------|----------------------|--------|
| AchievementService (Application + Infrastructure) | 16 | ✅ Complete |
| Smart Launcher Feature | 18 | ✅ Complete |
| RecordingEngine | 6 | ✅ Complete |
| SessionRecoveryService | 6 | ✅ Complete |
| XboxCatalogClient | 3 | ✅ Complete |
| SequenceAnalysisEngine | 4 | ✅ Complete |
| ReplayPathResolver | 4 | ✅ Complete |
| NaturalLanguageGameSearch | 4 | ✅ Complete |
| **Total Migrated** | **183** | ✅ **All Complete** |

## Acceptable Nullable Patterns

The following patterns are **ACCEPTABLE** and do not need migration:

```csharp
// ✅ Nullable value types for "no data" states (valid business state)
public Task<Guid?> GetLastPlayedGameIdAsync() { return Task.FromResult<Guid?>(null); }
public Task<DateTime?> GetTimestampAsync() { return Task.FromResult<DateTime?>(null); }

// ✅ Private parsing/extraction helpers
private string? ExtractValue(string input) { return null; } // "Not found" is valid
private T? FindItem<T>(List<T> list) { return null; }      // "Not found" is valid

// ✅ UI dialog cancellation (user cancelled = valid null)
public Task<string?> ShowDialogAsync() { return Task.FromResult<string?>(null); }

// ✅ Demo/stub implementations
public object? GetResourceDictionary() { return null; } // Not implemented yet
```

## Error Types

| ErrorType | Usage |
|-----------|-------|
| `Validation` | Input doesn't meet business rules |
| `NotFound` | Requested entity doesn't exist |
| `Conflict` | Operation conflicts with existing state |
| `Unauthorized` | User not authenticated |
| `Forbidden` | User lacks permission |
| `External` | Third-party service failure |
| `Internal` | Unexpected internal error |

## Consequences

- ✅ Railway-oriented programming
- ✅ Clear error handling
- ✅ No exception abuse
- ✅ Type-safe error information
- ✅ Consistent API across all services
- ✅ Better testability (can assert on error types)

## Code Examples

### Before (Anti-pattern)
```csharp
public async Task<Game> GetGameAsync(Guid id)
{
    var game = await _repository.GetByIdAsync(id);
    if (game == null) return null;  // ❌ Don't do this
    return game;
}
```

### After (Correct)
```csharp
public async Task<Result<Game>> GetGameAsync(Guid id)
{
    var game = await _repository.GetByIdAsync(id);
    if (game == null)
        return Result<Game>.Failure($"Game {id} not found", ErrorType.NotFound);
    return Result<Game>.Success(game);
}
```

### Catch Block Pattern
```csharp
try
{
    // operation
    return Result.Success();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    return Result.Failure($"Operation failed: {ex.Message}", ErrorType.Internal);
}
```

## References

- Railway oriented programming
- [Result Pattern Implementation](../../../src/SaveState.Core/Common/Result.cs)
- [Patterns Cookbook](../PATTERNS_COOKBOOK.md)
