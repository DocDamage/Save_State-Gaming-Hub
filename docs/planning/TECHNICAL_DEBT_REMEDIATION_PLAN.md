# Technical Debt Remediation Plan

**Created**: January 16, 2026
**Last Updated**: January 16, 2026
**Status**: Phase 1 In Progress
**Priority**: Post-Release Polish

## Overview

This plan addresses remaining technical debt identified during the comprehensive codebase audit. All critical issues have been resolved; these items are code quality improvements for a polish phase.

---

## Phase 1: Result Pattern Conversion (~90 instances)

**Priority**: Medium
**Effort**: 4-6 hours
**Impact**: Improved error handling consistency
**Status**: ✅ PARTIALLY COMPLETE (OAuth/Cloud services done)

### Target Files

#### 1.1 DialogService (High concentration) - ⚠️ SKIPPED (By Design)

- **File**: `src/SaveState.Presentation/Services/DialogService.cs`
- **Issue**: Multiple `return null` instead of `Result<T>.Failure()`
- **Resolution**: These are UI dialog methods where `null` = user cancelled (not an error). This is the standard pattern for optional dialog results and does NOT need conversion.

#### 1.2 GameMemoryReader - ⚠️ SKIPPED (By Design)

- **File**: `src/SaveState.Infrastructure/GameLibrary/Services/GameMemoryReader.cs`
- **Issue**: Returns null on read failures
- **Resolution**: These are internal helper methods (`ReadInt32`, `ReadByte`, `ReadFloat`) where nullable returns are appropriate for "no value read" semantics. The calling code handles nulls correctly.

#### 1.3 OAuth Services - ✅ COMPLETE

- **Files**:
  - `src/SaveState.Core/Sync/ICloudAuthenticationService.cs` - Interface updated
  - `src/SaveState.Infrastructure/Sync/CloudAuthenticationService.cs` - Implementation updated
- **Changes Made**:
  - `AuthenticateAsync()` now returns `Result<OAuth2TokenResponse>` instead of `OAuth2TokenResponse?`
  - `RefreshTokenAsync()` now returns `Result<OAuth2TokenResponse>` instead of `OAuth2TokenResponse?`
  - Added proper error types: `ErrorType.Unauthorized`, `ErrorType.Cancelled`, `ErrorType.ExternalService`
  - Added detailed error messages for debugging

#### 1.4 Cloud Storage Providers - ✅ COMPLETE

- **Files**:
  - `src/SaveState.Infrastructure/Sync/GoogleDriveStorageProvider.cs` - Updated to use Result pattern
  - `src/SaveState.Infrastructure/Sync/OneDriveStorageProvider.cs` - Updated to use Result pattern
- **Changes Made**:
  - Updated callers to check `result.IsSuccess` instead of `!= null`
  - Added logging for authentication failures with error details

### Implementation Pattern

```csharp
// Before
public async Task<Token?> GetTokenAsync()
{
    if (condition) return null;
    return token;
}

// After
public async Task<Result<Token>> GetTokenAsync()
{
    if (condition)
        return Result.Failure<Token>("Token expired or not found", ErrorType.Authentication);
    return Result.Success(token);
}
```

---

## Phase 2: Pagination for GetAllAsync Calls

**Priority**: Medium
**Effort**: 2-3 hours
**Impact**: Performance improvement for large datasets

### Target Methods

#### 2.1 Repository Layer
- **Pattern**: Replace `GetAllAsync()` with `GetPagedAsync(int page, int pageSize)`
- **Files to audit**:
  - `src/SaveState.Infrastructure/Repositories/GameRepository.cs`
  - `src/SaveState.Infrastructure/Repositories/SaveStateRepository.cs`
  - `src/SaveState.Infrastructure/Repositories/AchievementRepository.cs`

#### 2.2 Service Layer Consumers
- Update callers to use pagination or streaming patterns
- Add `IAsyncEnumerable<T>` variants for large dataset operations

### Implementation Pattern

```csharp
// Before
public async Task<IReadOnlyList<Game>> GetAllAsync()

// After
public async Task<PagedResult<Game>> GetPagedAsync(
    int page = 1,
    int pageSize = 50,
    CancellationToken ct = default)

public IAsyncEnumerable<Game> StreamAllAsync(CancellationToken ct = default)
```

---

## Phase 3: Debug Logging Cleanup (~25 statements)

**Priority**: Low
**Effort**: 1 hour
**Impact**: Cleaner production logs

### Target Files

| File | Estimated Count |
|------|-----------------|
| `SqliteVectorStore.cs` | 8 |
| `SemanticKnowledgeClient.cs` | 6 |
| `MarkdownKnowledgeBaseService.cs` | 5 |
| `AiOrchestrator.cs` | 6 |

### Action Items

1. Remove or downgrade `LogDebug` statements containing sensitive data
2. Keep structured logging for operational insights
3. Ensure no PII or secrets in log messages

### Implementation Pattern

```csharp
// Remove
_logger.LogDebug("Processing query: {Query}", userQuery);

// Keep (operational)
_logger.LogDebug("Vector search returned {Count} results in {ElapsedMs}ms", count, elapsed);
```

---

## Phase 4: Documentation Updates

**Priority**: Low
**Effort**: 30 minutes
**Impact**: Developer experience

### CLAUDE.md Updates

1. Update "Known Issues & Technical Debt" section
2. Mark completed items from this session
3. Add new completion metrics

### Updates to Make

```markdown
## Completed This Session (Jan 16, 2026)

- ✅ AutoSaveManager: Added IDisposable, event unsubscription
- ✅ PerformanceProfiler: Fixed sync-over-async in Dispose
- ✅ ItchGameProviderPlugin: Added IDisposable for HttpClient
- ✅ MugenManagerPlugin: Added IDisposable for HttpClient
- ✅ NetworkCommands: Converted Thread.Sleep to Task.Delay
- ✅ AudioOptimizerFactory: Platform-specific factory pattern
- ✅ NetworkOptimizerFactory: Platform-specific factory pattern
```

---

## Execution Order

| Phase | Priority | Dependencies | Estimated Time |
|-------|----------|--------------|----------------|
| Phase 3 | First | None | 1 hour |
| Phase 1 | Second | None | 4-6 hours |
| Phase 2 | Third | Phase 1 | 2-3 hours |
| Phase 4 | Last | All above | 30 minutes |

**Total Estimated Effort**: 8-11 hours

---

## Acceptance Criteria

- [ ] Zero `return null` in service/repository methods (use Result pattern)
- [ ] All list operations support pagination
- [ ] No debug logs with sensitive data
- [ ] CLAUDE.md reflects current state
- [ ] Build: 0 errors, 0 warnings
- [ ] All existing tests pass

---

## Notes

### Items Documented as Acceptable

These items were reviewed and determined to be acceptable:

1. **VirtualizedCollection blocking indexer** - Required by IList interface
2. **Thread.Sleep in PerformanceMetricsCollector** - Required for performance counter stabilization
3. **Thread.Sleep in PerformanceMonitor** - Required for performance counter stabilization

### Items Already Fixed (This Session)

1. AutoSaveManager event leak
2. PerformanceProfiler sync-over-async
3. Plugin HttpClient disposal (Itch, MugenManager)
4. NetworkCommands Thread.Sleep → Task.Delay
5. Audio/Network optimizer factory patterns
